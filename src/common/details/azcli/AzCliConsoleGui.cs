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
        public static Task<string> PickSubscriptionIdAsync(bool interactive, string subscriptionFilter = null)
        {
            return SubscriptionPicker.PickSubscriptionIdAsync(interactive, subscriptionFilter);
        }

        public static Task<AzCli.SubscriptionInfo> PickSubscriptionAsync(bool interactive, string subscriptionFilter = null)
        {
            return SubscriptionPicker.PickSubscriptionAsync(interactive, subscriptionFilter);
        }

        public static Task<AzCli.AccountRegionLocationInfo> PickRegionLocationAsync(bool interactive, string regionFilter = null, bool allowAnyRegionOption = true)
        {
            return RegionLocationPicker.PickRegionLocationAsync(interactive, regionFilter, allowAnyRegionOption);
        }

        public static Task<AzCli.CognitiveServicesResourceInfo> PickOrCreateCognitiveResource(bool interactive, string subscriptionId = null, string regionFilter = null, string groupFilter = null, string resourceFilter = null, string kind = null, string sku = "F0", bool agreeTerms = false)
        {
            return AiResourcePicker.PickOrCreateCognitiveResource(interactive, subscriptionId, regionFilter, groupFilter, resourceFilter, kind, sku, agreeTerms);
        }

        public static Task<AzCli.CognitiveServicesKeyInfo> LoadCognitiveServicesResourceKeys(string subscriptionId, AzCli.CognitiveServicesResourceInfo resource)
        {
            return AiResourcePicker.LoadCognitiveServicesResourceKeys(subscriptionId, resource);
        }

        private static string AskPrompt(string prompt, string value = null, bool useEditBox = false)
        {
            Console.Write(prompt);

            if (useEditBox)
            {
                var normal = new Colors(ConsoleColor.White, ConsoleColor.Blue);
                var text = EditBoxQuickEdit.Edit(40, 1, normal, value, 128);
                Console.WriteLine(text);
                return text;
            }

            if (!string.IsNullOrEmpty(value))
            {
                Console.WriteLine(value);
                return value;
            }

            return Console.ReadLine();
        }
    }
}
