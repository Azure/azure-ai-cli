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
