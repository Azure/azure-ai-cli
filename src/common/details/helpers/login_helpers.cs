//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Text;
using Azure.AI.Details.Common.CLI.ConsoleGui;

namespace Azure.AI.Details.Common.CLI
{
    public class LoginHelpers
    {
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
