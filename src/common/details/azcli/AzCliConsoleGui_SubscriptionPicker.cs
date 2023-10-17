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
        public class SubscriptionPicker
        {
            public static async Task<string> PickSubscriptionIdAsync(bool interactive, string subscriptionFilter = null)
            {
                if (Guid.TryParse(subscriptionFilter, out var subscriptionId))
                {
                    return subscriptionId.ToString();
                }
                
                var subscription = await PickSubscriptionAsync(interactive, subscriptionFilter);
                return subscription.Id;
            }

            public static async Task<AzCli.SubscriptionInfo> PickSubscriptionAsync(bool interactive, string subscriptionFilter = null)
            {
                var subscription = await FindSubscriptionAsync(interactive, subscriptionFilter);
                if (subscription == null)
                {
                    throw new ApplicationException($"CANCELED: No subscription selected.");
                }

                await AzCli.SetAccount(subscription.Value.Id);

                return subscription.Value;
            }

            private static async Task<AzCli.SubscriptionInfo?> FindSubscriptionAsync(bool interactive, string subscriptionFilter = null)
            {
                Console.Write("\rSubscription: *** Loading choices ***");
                var response = await AzCli.ListAccounts();

                var noOutput = string.IsNullOrEmpty(response.Output.StdOutput);
                var hasError = !string.IsNullOrEmpty(response.Output.StdError);
                var hasErrorNotFound = hasError && (response.Output.StdError.Contains(" not ") || response.Output.StdError.Contains("No such file"));

                Console.Write("\rSubscription: ");
                if (noOutput && hasError && hasErrorNotFound)
                {
                    throw new ApplicationException("*** Please install the Azure CLI - https://aka.ms/azcli ***\n\nNOTE: If it's already installed ensure it's in the system PATH and working (try: `az account list`)");
                }
                else if (noOutput && hasError)
                {
                    throw new ApplicationException($"*** ERROR: Loading subscriptions ***\n{response.Output.StdError}");
                }

                var needLogin = response.Output.StdError != null && response.Output.StdError.Contains("az login");
                if (response.Payload.Count() == 0 && needLogin)
                {
                    bool cancelLogin = !interactive;
                    bool useDeviceCode = false;
                    if (interactive)
                    {
                        ConsoleHelpers.WriteError("*** WARNING: `az login` required ***");
                        Console.Write(" ");

                        var choices = new string[] {
                            "LAUNCH: `az login` (interactive browser)",
                            "LAUNCH: `az login` (interactive device code)",
                            "CANCEL: `az login ...` (non-interactive)",
                        };

                        var selection = OS.IsLinux() ? 1 : 0;
                        var picked = ListBoxPicker.PickIndexOf(choices, selection);

                        cancelLogin = (picked < 0 || picked == 2);
                        useDeviceCode = (picked == 1);
                    }

                    if (cancelLogin)
                    {
                        Console.Write("\rSubscription: ");
                        ConsoleHelpers.WriteLineError("*** Please run `az login` and try again ***");
                        return null;
                    }

                    Console.Write("\rSubscription: *** Launching `az login` (interactive) ***");
                    response = await AzCli.Login(useDeviceCode);
                    Console.Write("\rSubscription: ");
                }

                var subscriptions = response.Payload
                    .Where(x => MatchSubscriptionFilter(x, subscriptionFilter))
                    .OrderBy(x => (x.IsDefault ? "0" : "1") + x.Name)
                    .ToArray();

                if (subscriptions.Count() == 0)
                {
                    ConsoleHelpers.WriteLineError(response.Payload.Count() > 0
                        ? "*** No matching subscriptions found ***"
                        : "*** No subscriptions found ***");
                    return null;
                }
                else if (subscriptions.Count() == 1)
                {
                    var subscription = subscriptions[0];
                    DisplayNameAndId(subscription);
                    return subscription;
                }
                else if (!interactive)
                {
                    ConsoleHelpers.WriteLineError("*** More than 1 subscription found ***");
                    Console.WriteLine();
                    DisplaySubscriptions(subscriptions, "  ");
                    return null;
                }

                return ListBoxPickSubscription(subscriptions);
            }

            private static AzCli.SubscriptionInfo? ListBoxPickSubscription(AzCli.SubscriptionInfo[] subscriptions)
            {
                var list = subscriptions.Select(x => x.Name).ToArray();
                var picked = ListBoxPicker.PickIndexOf(list);
                if (picked < 0)
                {
                    return null;
                }

                var subscription = subscriptions[picked];
                DisplayNameAndId(subscription);
                return subscription;
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
                }
            }

            private static void DisplayNameAndId(AzCli.SubscriptionInfo subscription)
            {
                Console.Write($"{subscription.Name} ({subscription.Id})");
                Console.WriteLine(new string(' ', 20));
            }
        }
    }
}
