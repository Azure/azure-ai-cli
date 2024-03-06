//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;
using Azure.AI.Details.Common.CLI.ConsoleGui;

namespace Azure.AI.Details.Common.CLI
{
    public partial class AzCliConsoleGui
    {
        public static string GetSubscriptionUserName(string subscriptionId)
        {
            if (_subscriptionIdToUserName.TryGetValue(subscriptionId, out var userName))
            {
                return userName;
            }

            return null;
        }

        public static async Task<string> PickSubscriptionIdAsync(bool allowInteractiveLogin, bool allowInteractivePickSubscription, string subscriptionFilter = null)
        {
            if (Guid.TryParse(subscriptionFilter, out var subscriptionId))
            {
                return subscriptionId.ToString();
            }
            
            var subscription = await PickSubscriptionAsync(allowInteractiveLogin, allowInteractivePickSubscription, subscriptionFilter);
            return subscription.Id;
        }

        public static async Task<AzCli.SubscriptionInfo> PickSubscriptionAsync(bool allowInteractiveLogin, bool allowInteractivePickSubscription, string subscriptionFilter = null)
        {
            var subscription = await FindSubscriptionAsync(allowInteractiveLogin,  allowInteractivePickSubscription, subscriptionFilter);
            if (subscription == null)
            {
                throw new ApplicationException($"CANCELED: No subscription selected.");
            }

            await AzCli.SetAccount(subscription.Value.Id);

            return subscription.Value;
        }

        public static async Task<AzCli.SubscriptionInfo?> ValidateSubscriptionAsync(bool allowInteractiveLogin, string subscriptionFilter, string subscriptionLabel)
        {
            return await FindSubscriptionAsync(allowInteractiveLogin, false, subscriptionFilter, subscriptionLabel);
        }

        private static async Task<AzCli.SubscriptionInfo?> FindSubscriptionAsync(bool allowInteractiveLogin, bool allowInteractivePickSubscription, string subscriptionFilter = null, string subscriptionLabel = "Subscription")
        {
            Console.Write($"\r{subscriptionLabel}: *** Loading choices ***");
            var response = await AzCli.ListAccounts();

            var noOutput = string.IsNullOrEmpty(response.Output.StdOutput);
            var hasError = !string.IsNullOrEmpty(response.Output.StdError);
            var hasErrorNotFound = hasError && (response.Output.StdError.Contains(" not ") || response.Output.StdError.Contains("No such file"));

            Console.Write($"\r{subscriptionLabel}: ");
            if (noOutput && hasError && hasErrorNotFound)
            {
                throw new ApplicationException("*** Please install the Azure CLI - https://aka.ms/azcli ***\n\nNOTE: If it's already installed ensure it's in the system PATH and working (try: `az account list`)");
            }
            else if (noOutput && hasError)
            {
                throw new ApplicationException($"*** ERROR: Loading subscriptions ***\n{response.Output.StdError}");
            }

            var needLogin = response.Output.StdError != null && (response.Output.StdError.Split('\'', '"').Contains("az login") || response.Output.StdError.Contains("refresh token"));
            if (needLogin)
            {
                bool cancelLogin = !allowInteractiveLogin;
                bool useDeviceCode = false;
                if (allowInteractiveLogin)
                {
                    ConsoleHelpers.WriteError("*** WARNING: `az login` required ***");
                    Console.Write(" ");

                    var selection = 0;
                    var choices = new List<string>() {
                        "LAUNCH: `az login` (interactive device code)",
                        "CANCEL: `az login ...` (non-interactive)",
                    };

                    if (!OS.IsCodeSpaces())
                    {
                        choices.Insert(0, "LAUNCH: `az login` (interactive browser)");
                        selection = OS.IsWindows() ? 0 : 1;
                    }

                    var picked = ListBoxPicker.PickIndexOf(choices.ToArray(), selection);

                    cancelLogin = picked < 0 || picked == choices.Count() - 1;
                    useDeviceCode = picked == choices.Count() - 2;
                }
                else
                {
                    throw new ApplicationException("Login required");
                }

                if (cancelLogin)
                {
                    Console.Write($"\r{subscriptionLabel}: ");
                    ConsoleHelpers.WriteLineError("*** Please run `az login` and try again ***");
                    throw new OperationCanceledException("Login was canceled");
                }

                Console.Write($"\r{subscriptionLabel}: *** Launching `az login` (interactive) ***");
                response = await AzCli.Login(useDeviceCode);
                Console.Write($"\r{subscriptionLabel}: ");
            }

            var subscriptions = response.Payload
                .Where(x => MatchSubscriptionFilter(x, subscriptionFilter))
                .OrderBy(x => x.Name)
                .ToArray();

            if (subscriptions.Count() == 0)
            {
                string error = response.Payload.Count() > 0
                    ? "No matching subscriptions found"
                    : "No subscriptions found";
                ConsoleHelpers.WriteLineError($"*** {error} ***");
                throw new ApplicationException(error);
            }
            else if (subscriptions.Count() == 1)
            {
                var subscription = subscriptions[0];
                DisplayNameAndId(subscription);
                CacheSubscriptionUserName(subscription);
                return subscription;
            }
            else if (!allowInteractivePickSubscription)
            {
                string error = "More than 1 subscription found";
                ConsoleHelpers.WriteLineError($"*** { error } ***");
                Console.WriteLine();
                DisplaySubscriptions(subscriptions, "  ");
                throw new ApplicationException(error);
            }

            return ListBoxPickSubscription(subscriptions);
        }

        private static AzCli.SubscriptionInfo? ListBoxPickSubscription(AzCli.SubscriptionInfo[] subscriptions)
        {
            var selected = ListBoxPicker.PickValue(
                subscriptions
                .Select(s => new ListBoxPickerChoice<AzCli.SubscriptionInfo>()
                {
                    IsDefault = s.IsDefault,
                    DisplayName = s.Name,
                    Value = s
                }));
            if (selected == null)
            {
                throw new OperationCanceledException("User canceled");
            }

            DisplayNameAndId(selected.Value);
            CacheSubscriptionUserName(selected.Value);
            return selected.Value;
        }

        private static bool MatchSubscriptionFilter(AzCli.SubscriptionInfo subscription, string subscriptionFilter)
        {
            if (subscriptionFilter == null || subscription.Id == subscriptionFilter)
            {
                return true;
            }

            var name = subscription.Name.ToLower();
            return name.Contains(subscriptionFilter) || StringHelpers.ContainsAllCharsInOrder(name, subscriptionFilter);
        }

        private static void DisplaySubscriptions(AzCli.SubscriptionInfo[] subscriptions, string prefix = "")
        {
            foreach (var subscription in subscriptions)
            {
                Console.Write(prefix);
                DisplayNameAndId(subscription);
                CacheSubscriptionUserName(subscription);
            }
        }

        private static void DisplayNameAndId(AzCli.SubscriptionInfo subscription)
        {
            Console.Write($"{subscription.Name} ({subscription.Id})");
            Console.WriteLine(new string(' ', 20));
        }

        private static void CacheSubscriptionUserName(AzCli.SubscriptionInfo subscription)
        {
            _subscriptionIdToUserName[subscription.Id] = subscription.UserName;
        }

        private static Dictionary<string, string> _subscriptionIdToUserName = new Dictionary<string, string>();
    }
}
