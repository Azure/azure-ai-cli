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

        public static Task<AzCli.ResourceGroupInfo> PickOrCreateResourceGroup(bool interactive, string subscriptionId = null, string regionFilter = null, string groupFilter = null)
        {
            return ResourceGroupPicker.PickOrCreateResourceGroup(interactive, subscriptionId, regionFilter, groupFilter);
        }

        public static Task<AzCli.CognitiveServicesResourceInfo> PickOrCreateCognitiveResource(string sectionHeader, bool interactive, string subscriptionId = null, string regionFilter = null, string groupFilter = null, string resourceFilter = null, string kind = null, string sku = "F0", bool agreeTerms = false)
        {
            return AiResourcePicker.PickOrCreateCognitiveResource(sectionHeader, interactive, subscriptionId, regionFilter, groupFilter, resourceFilter, kind, sku, agreeTerms);
        }

        public static async Task<AzCli.CognitiveServicesDeploymentInfo> PickOrCreateDeployment(bool interactive, string deploymentExtra, string subscriptionId, string groupName, string resourceRegionLocation, string resourceName, string deploymentFilter)
        {
            return await AzCliConsoleGui.AiResourceDeploymentPicker.PickOrCreateDeployment(interactive, deploymentExtra, subscriptionId, groupName, resourceRegionLocation, resourceName, deploymentFilter);
        }

        public static Task<AzCli.CognitiveServicesKeyInfo> LoadCognitiveServicesResourceKeys(string sectionHeader, string subscriptionId, AzCli.CognitiveServicesResourceInfo resource)
        {
            return AiResourcePicker.LoadCognitiveServicesResourceKeys(sectionHeader, subscriptionId, resource);
        }

        public static async Task<AzCli.CognitiveSearchKeyInfo> LoadSearchResourceKeys(string subscriptionId, AzCli.CognitiveSearchResourceInfo resource)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`AI SEARCH RESOURCE KEYS`");

            Console.Write("Keys: *** Loading ***");
            var keys = await AzCli.ListSearchAdminKeys(subscriptionId, resource.Group, resource.Name);

            Console.Write("\r");
            Console.WriteLine($"Key1: {keys.Payload.Key1.Substring(0, 4)}****************************");
            Console.WriteLine($"Key2: {keys.Payload.Key2.Substring(0, 4)}****************************");
            return keys.Payload;
        }

        public static async Task<AzCli.CognitiveSearchResourceInfo> PickOrCreateCognitiveSearchResource(string subscription, string location, string groupName, string smartName = null, string smartNameKind = null)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`AI SEARCH RESOURCE`");
            Console.Write("\rName: *** Loading choices ***");

            var response = await AzCli.ListSearchResources(subscription, location);
            if (string.IsNullOrEmpty(response.Output.StdOutput) && !string.IsNullOrEmpty(response.Output.StdError))
            {
                var output = response.Output.StdError.Replace("\n", "\n  ");
                throw new ApplicationException($"ERROR: Listing search resources\n  {output}");
            }

            var resources = response.Payload.OrderBy(x => x.Name).ToList();
            var choices = resources.Select(x => $"{x.Name} ({x.RegionLocation})").ToList();
            choices.Insert(0, "(Create new)");

            Console.Write("\rName: ");

            var picked = ListBoxPicker.PickIndexOf(choices.ToArray());
            if (picked < 0)
            {
                throw new ApplicationException($"CANCELED: No resource selected");
            }

            Console.WriteLine($"\rName: {choices[picked]}");
            var resource = picked > 0 ? resources[picked - 1] : new AzCli.CognitiveSearchResourceInfo();
            if (picked == 0)
            {
                resource = await TryCreateSearchInteractive(subscription, location, groupName, smartName, smartNameKind);
            }

            return resource;
        }

        private static async Task<AzCli.CognitiveSearchResourceInfo> TryCreateSearchInteractive(string subscription, string locationName, string groupName, string smartName = null, string smartNameKind = null)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CREATE AI SEARCH RESOURCE`");

            var groupOk = !string.IsNullOrEmpty(groupName);
            if (!groupOk)
            {
                var location =  await AzCliConsoleGui.PickRegionLocationAsync(true, locationName, false);
                locationName = location.Name;
            }
            
            var group = await AzCliConsoleGui.PickOrCreateResourceGroup(true, subscription, groupOk ? null : locationName, groupName);
            groupName = group.Name;

            if (string.IsNullOrEmpty(smartName))
            {
                smartName = group.Name;
                smartNameKind = "rg";
            }

            var name = NamePickerHelper.DemandPickOrEnterName("Name: ", "search", smartName, smartNameKind);

            Console.Write("*** CREATING ***");
            var response = await AzCli.CreateSearchResource(subscription, groupName, locationName, name);

            Console.Write("\r");
            if (string.IsNullOrEmpty(response.Output.StdOutput) && !string.IsNullOrEmpty(response.Output.StdError))
            {
                var output = response.Output.StdError.Replace("\n", "\n  ");
                throw new ApplicationException($"ERROR: Creating resource:\n\n  {output}");
            }

            Console.WriteLine("\r*** CREATED ***  ");
            return response.Payload;
        }
    }
}
