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
        public static async Task<AzCli.CognitiveServicesResourceInfo> PickOrCreateCognitiveResource(string sectionHeader, bool interactive, string subscriptionId = null, string regionFilter = null, string groupFilter = null, string resourceFilter = null, string kinds = null, string sku = "F0", bool agreeTerms = false)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`{sectionHeader}`");

            var createNewItem = !string.IsNullOrEmpty(resourceFilter)
                ? $"(Create `{resourceFilter}`)"
                : interactive ? "(Create new)" : null;

            var resource = await FindCognitiveServicesResource(interactive, subscriptionId, regionFilter, groupFilter, resourceFilter, kinds, createNewItem);
            if (resource != null && resource.Value.Name == null)
            {
                resource = await TryCreateCognitiveServicesResource(sectionHeader, interactive, subscriptionId, regionFilter, groupFilter, resourceFilter, kinds, sku, agreeTerms);
            }

            if (resource == null)
            {
                throw new ApplicationException($"CANCELED: No resource selected");
            }

            return resource.Value;
        }

        public static async Task<AzCli.CognitiveServicesKeyInfo> LoadCognitiveServicesResourceKeys(string sectionHeader, string subscriptionId, AzCli.CognitiveServicesResourceInfo resource)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`{sectionHeader} KEYS`");

            Console.Write("Keys: *** Loading ***");
            var keys = await AzCli.ListCognitiveServicesKeys(subscriptionId, resource.Group, resource.Name);

            Console.Write("\r");
            Console.WriteLine($"Key1: {keys.Payload.Key1.Substring(0, 4)}****************************");
            Console.WriteLine($"Key2: {keys.Payload.Key2.Substring(0, 4)}****************************");
            return keys.Payload;
        }

        public static async Task<AzCli.CognitiveServicesResourceInfo?> FindCognitiveServicesResource(bool interactive, string subscriptionId = null, string regionLocationFilter = null, string groupFilter = null, string resourceFilter = null, string kinds = null, string allowCreateResourceOption = null)
        {
            var allowCreateResource = !string.IsNullOrEmpty(allowCreateResourceOption);

            var listResourcesFunc = async () => await AzCli.ListCognitiveServicesResources(subscriptionId, kinds);
            var response = await LoginHelpers.GetResponseOnLogin<AzCli.CognitiveServicesResourceInfo[]>(interactive, "resource", listResourcesFunc);

            var resources = response.Payload
                .Where(x => MatchResourceFilter(x, regionLocationFilter, groupFilter, resourceFilter))
                .OrderBy(x => x.Name + x.RegionLocation)
                .ToList();

            var exactMatch = resourceFilter != null && resources.Count(x => ExactMatchResource(x, regionLocationFilter, groupFilter, resourceFilter)) == 1;
            if (exactMatch) resources = resources.Where(x => ExactMatchResource(x, regionLocationFilter, groupFilter, resourceFilter)).ToList();

            if (resources.Count() == 0)
            {
                if (!allowCreateResource)
                {
                    ConsoleHelpers.WriteLineError($"*** No Cognitive Services resources found ***");
                    return null;
                }
                else if (!interactive)
                {
                    Console.WriteLine(allowCreateResourceOption);
                    return new AzCli.CognitiveServicesResourceInfo();
                }
            }
            else if (resources.Count() == 1 && (!interactive || exactMatch))
            {
                var resource = resources.First();
                DisplayName(resource);
                return resource;
            }
            else if (!interactive)
            {
                ConsoleHelpers.WriteLineError("*** More than 1 resource found ***");
                Console.WriteLine();
                DisplayResources(resources, "  ");
                return null;
            }

            return interactive
                ? ListBoxPickCognitiveServicesResource(resources.ToArray(), allowCreateResourceOption)
                : null;
        }

        public static async Task<AzCli.CognitiveServicesResourceInfo?> TryCreateCognitiveServicesResource(string sectionHeader, bool interactive, string subscriptionId = null, string regionLocationFilter = null, string groupFilter = null, string resourceFilter = null, string kinds = null, string sku = "F0", bool agreeTerms = false)
        {
            ConsoleHelpers.WriteLineWithHighlight("\n`RESOURCE GROUP`");

            var regionLocation = !string.IsNullOrEmpty(regionLocationFilter) ? await FindRegionAsync(interactive, regionLocationFilter, true) : new AzCli.AccountRegionLocationInfo();
            if (regionLocation == null) return null;

            var (group, createdNew) = await PickOrCreateResourceGroup(interactive, subscriptionId, regionLocation?.Name, groupFilter);
            var createKind = kinds.Split(';').Last();

            ConsoleHelpers.WriteLineWithHighlight($"\n`CREATE {sectionHeader}`");
            Console.WriteLine($"Region: {group.RegionLocation}");
            Console.WriteLine($"Group: {group.Name}");
            Console.WriteLine($"Kind: {createKind}");

            var smartName = group.Name;
            var smartNameKind = "rg";
            var nameOutKind = createKind?.ToLower() switch
            {
                "aiservices" => "ais",
                "cognitiveservices" => "cs",
                _ => createKind.ToLower()
            };

            var name = string.IsNullOrEmpty(resourceFilter)
                ? NamePickerHelper.DemandPickOrEnterName("Name: ", nameOutKind, smartName, smartNameKind, AzCliConsoleGui.GetSubscriptionUserName(subscriptionId))
                : AskPromptHelper.AskPrompt("Name: ", resourceFilter);
            if (string.IsNullOrEmpty(name)) return null;

            if (!agreeTerms && !CheckAgreeTerms(createKind)) return null;

            Console.Write("*** CREATING ***");
            var response = await AzCli.CreateCognitiveServicesResource(subscriptionId, group.Name, group.RegionLocation, name, createKind, sku);

            Console.Write("\r");
            if (string.IsNullOrEmpty(response.Output.StdOutput) && !string.IsNullOrEmpty(response.Output.StdError))
            {
                throw new ApplicationException($"ERROR: Creating Cognitive Services resource: {response.Output.StdError}");
            }

            Console.WriteLine("\r*** CREATED ***  ");
            return response.Payload;
        }

        private static AzCli.CognitiveServicesResourceInfo? ListBoxPickCognitiveServicesResource(AzCli.CognitiveServicesResourceInfo[] resources, string p0)
        {
            var list = resources.Select(x => $"{x.Name} ({x.RegionLocation}, {x.Kind})").ToList();

            var hasP0 = !string.IsNullOrEmpty(p0);
            if (hasP0) list.Insert(0, p0);

            var picked = ListBoxPicker.PickIndexOf(list.ToArray());
            if (picked < 0)
            {
                return null;
            }

            if (hasP0 && picked == 0)
            {
                Console.WriteLine(p0);
                return new AzCli.CognitiveServicesResourceInfo();
            }

            if (hasP0) picked--;
            Console.WriteLine($"{resources[picked].Name}");
            Console.WriteLine($"Group: {resources[picked].Group}");
            Console.WriteLine($"Region: {resources[picked].RegionLocation}");
            
            return resources[picked];
        }

        private static bool ExactMatchResource(AzCli.CognitiveServicesResourceInfo resource, string regionLocationFilter, string groupFilter, string resourceFilter)
        {
            return !string.IsNullOrEmpty(resourceFilter) && resource.Name.ToLower() == resourceFilter &&
                (string.IsNullOrEmpty(regionLocationFilter) || resource.RegionLocation.ToLower() == regionLocationFilter) &&
                (string.IsNullOrEmpty(groupFilter) || resource.Group.ToLower() == groupFilter);
        }

        private static bool MatchResourceFilter(AzCli.CognitiveServicesResourceInfo resource, string regionLocationFilter, string groupFilter, string resourceFilter)
        {
            if (resourceFilter != null && ExactMatchResource(resource, regionLocationFilter, groupFilter, resourceFilter))
            {
                return true;
            }

            var name = resource.Name.ToLower();
            var group = resource.Group.ToLower();
            var regionLocation = resource.RegionLocation.ToLower();

            return (string.IsNullOrEmpty(resourceFilter) || name.Contains(resourceFilter) || StringHelpers.ContainsAllCharsInOrder(name, resourceFilter)) &&
                (string.IsNullOrEmpty(regionLocationFilter) || name.Contains(regionLocationFilter) || StringHelpers.ContainsAllCharsInOrder(regionLocation, regionLocationFilter)) &&
                (string.IsNullOrEmpty(groupFilter) || name.Contains(groupFilter) || StringHelpers.ContainsAllCharsInOrder(group, groupFilter));
        }

        private static void DisplayResources(List<AzCli.CognitiveServicesResourceInfo> resources, string prefix)
        {
            foreach (var resource in resources)
            {
                Console.Write(prefix);
                DisplayName(resource);
            }
        }

        private static void DisplayName(AzCli.CognitiveServicesResourceInfo resource)
        {
            Console.Write($"{resource.Name} ({resource.RegionLocation})");
            Console.WriteLine(new string(' ', 20));
        }

        private static bool CheckAgreeTerms(string createKind)
        {
            var checkAttestation = createKind.ToLower() switch
            {
                "cognitiveservices" => true,
                "face" => true,
                _ => false
            };

            if (checkAttestation)
            {
                ConsoleHelpers.WriteLineWithHighlight("`#e_;\nI confirm that I have reviewed and acknowledge the terms in the Responsible AI notice.`");
                ConsoleHelpers.WriteLineWithHighlight("`#e_;See: https://aka.ms/azure-ai-cli-responsible-ai-notice`");
                Console.Write("\n     ");
                var yesOrNo = ListBoxPickYesNo();
                Console.WriteLine();
                return yesOrNo;
            }

            return true;
        }

        private static bool ListBoxPickYesNo()
        {
            var choices = "Yes;No".Split(';').ToArray();
            var picked = ListBoxPicker.PickIndexOf(choices);
            Console.WriteLine(picked switch {
                0 => "Yes",
                1 => "No",
                _ => "Canceled"
            });
            return (picked == 0);
        }
    }
}
