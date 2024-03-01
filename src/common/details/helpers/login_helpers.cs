//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Runtime.CompilerServices;
using System.Text;
using Azure.AI.Details.Common.CLI.ConsoleGui;

namespace Azure.AI.Details.Common.CLI
{
    public class LoginHelpers
    {
        public static async Task<ParsedJsonProcessOutput<T>> GetResponseOnLogin<T>(bool allowInteractiveLogin, string label, Func<Task<ParsedJsonProcessOutput<T>>> getResponse, string titleLabel = "Name")
        {
            Console.Write($"\r{titleLabel}: *** Loading choices ***");
            var response = await getResponse();

            Console.Write($"\r{titleLabel}: ");
            if (string.IsNullOrEmpty(response.Output.StdOutput) && !string.IsNullOrEmpty(response.Output.StdError))
            {
                if (LoginHelpers.HasLoginError(response.Output.StdError))
                {
                    var loginResponse = await LoginHelpers.AttemptLogin(allowInteractiveLogin, $"{label}s");
                    if (!loginResponse.Equals(default(ParsedJsonProcessOutput<AzCli.SubscriptionInfo[]>)))
                    {
                        response = await getResponse();
                    }
                }
                if (string.IsNullOrEmpty(response.Output.StdOutput) && !string.IsNullOrEmpty(response.Output.StdError))
                {
                    throw new ApplicationException($"ERROR: Loading resource {label}s: {response.Output.StdError}");
                }
            }
            return response;
        }

        public static async Task<ParsedJsonProcessOutput<AzCli.SubscriptionInfo[]>> AttemptLogin(bool allowInteractiveLogin, string label)
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

            if (cancelLogin)
            {
                Console.Write($"\r{label}: ");
                ConsoleHelpers.WriteLineError("*** Please run `az login` and try again ***");
                return default;
            }

            Console.Write($"\r{label}: *** Launching `az login` (interactive) ***");
            var response = await AzCli.Login(useDeviceCode);
            Console.Write($"\r{label}: ");
            return response;
        }

        public static bool HasLoginError(string errorMessage) => errorMessage.Split('\'', '"').Contains("az login") || errorMessage.Contains("refresh token");
    }
}
