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
using Azure.AI.Details.Common.CLI.ConsoleGui;

namespace Azure.AI.Details.Common.CLI
{
    public partial class AzCliConsoleGui
    {
        public static async Task<AzCli.ResourceKeyInfo> LoadSearchResourceKeys(string subscriptionId, AzCli.CognitiveSearchResourceInfo resource)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`AI SEARCH RESOURCE KEYS`");

            Console.Write("Keys: *** Loading ***");
            var response = await Program.SearchClient.GetKeysAsync(subscriptionId, resource.Group, resource.Name, Program.CancelToken);
            var keys = new AzCli.ResourceKeyInfo();
            (keys.Key1, keys.Key2) = response.Value;

            Console.Write("\r");
            Console.WriteLine($"Key1: {keys.Key1.Substring(0, 4)}****************************");
            Console.WriteLine($"Key2: {keys.Key2.Substring(0, 4)}****************************");
            return keys;
        }

        public static async Task<AzCli.CognitiveSearchResourceInfo?> PickOrCreateCognitiveSearchResource(bool allowSkip, string subscription, string location, string groupName, string smartName = null, string smartNameKind = null)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`AI SEARCH RESOURCE`");
            Console.Write("\rName: *** Loading choices ***");

            var response = await Program.SearchClient.GetAllAsync(subscription, location, Program.CancelToken);
            response.ThrowOnFail("Loading search resources");

            var resources = response.Value.OrderBy(x => x.Name).ToList();
            var choices = resources.Select(x => $"{x.Name} ({x.RegionLocation})").ToList();
            choices.Insert(0, "(Create new)");

            if (allowSkip)
            {
                choices.Add("(Skip)");
            }

            Console.Write("\rName: ");

            var picked = ListBoxPicker.PickIndexOf(choices.ToArray());
            if (picked < 0)
            {
                throw new ApplicationException($"CANCELED: No resource selected");
            }

            if (allowSkip && picked == choices.Count - 1)
            {
                Console.WriteLine($"\rName: (Skipped)");
                return null;
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
            var sectionHeader = "\n`CREATE SEARCH RESOURCE`";
            ConsoleHelpers.WriteLineWithHighlight(sectionHeader);

            var groupOk = !string.IsNullOrEmpty(groupName);
            if (!groupOk)
            {
                var location =  await AzCliConsoleGui.PickRegionLocationAsync(true, subscription, locationName, false);
                locationName = location.Name;
            }
            
            var (group, createdNew) = await AzCliConsoleGui.PickOrCreateResourceGroup(true, subscription, groupOk ? null : locationName, groupName);
            groupName = group.Name;

            if (string.IsNullOrEmpty(smartName))
            {
                smartName = group.Name;
                smartNameKind = "rg";
            }

            if (createdNew)
            {
                ConsoleHelpers.WriteLineWithHighlight(sectionHeader);
            }

            var name = NamePickerHelper.DemandPickOrEnterName("Name: ", "search", smartName, smartNameKind, AzCliConsoleGui.GetSubscriptionUserName(subscription));

            Console.Write("*** CREATING ***");
            var response = await Program.SearchClient.CreateAsync(subscription, groupName, locationName, name, Program.CancelToken);

            Console.Write("\r");
            response.ThrowOnFail("Creating search resource");

            Console.WriteLine("\r*** CREATED ***  ");
            return response.Value;
        }
    }
}
