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
        public class AiResourcePicker
        {
            public static async Task<AzCli.CognitiveServicesResourceInfo> PickOrCreateCognitiveResource(bool interactive, string subscriptionId = null, string regionFilter = null, string groupFilter = null, string resourceFilter = null, string kind = null, string sku = "F0", bool agreeTerms = false)
            {
                ConsoleHelpers.WriteLineWithHighlight($"\n`{Program.SERVICE_RESOURCE_DISPLAY_NAME_ALL_CAPS}`");

                var createNewItem = !string.IsNullOrEmpty(resourceFilter)
                    ? $"(Create `{resourceFilter}`)"
                    : interactive ? "(Create new)" : null;

                var resource = await FindCognitiveServicesResource(interactive, subscriptionId, regionFilter, groupFilter, resourceFilter, kind, createNewItem);
                if (resource != null && resource.Value.Name == null)
                {
                    resource = await TryCreateCognitiveServicesResource(interactive, subscriptionId, regionFilter, groupFilter, resourceFilter, kind, sku, agreeTerms);
                }

                if (resource == null)
                {
                    throw new ApplicationException($"CANCELED: No resource selected");
                }

                return resource.Value;
            }

            public static async Task<AzCli.CognitiveServicesKeyInfo> LoadCognitiveServicesResourceKeys(string subscriptionId, AzCli.CognitiveServicesResourceInfo resource)
            {
                ConsoleHelpers.WriteLineWithHighlight($"\n`{Program.SERVICE_RESOURCE_DISPLAY_NAME_ALL_CAPS} KEYS`");

                Console.Write("Keys: *** Loading ***");
                var keys = await AzCli.ListCognitiveServicesKeys(subscriptionId, resource.Group, resource.Name);

                Console.Write("\r");
                Console.WriteLine($"Key1: {keys.Payload.Key1.Substring(0, 4)}****************************");
                Console.WriteLine($"Key2: {keys.Payload.Key2.Substring(0, 4)}****************************");
                return keys.Payload;
            }

            public static async Task<AzCli.CognitiveServicesResourceInfo?> FindCognitiveServicesResource(bool interactive, string subscriptionId = null, string regionLocationFilter = null, string groupFilter = null, string resourceFilter = null, string kind = null, string allowCreateResourceOption = null)
            {
                var allowCreateResource = !string.IsNullOrEmpty(allowCreateResourceOption);

                Console.Write("\rName: *** Loading choices ***");
                var response = await AzCli.ListCognitiveServicesResources(subscriptionId, kind);

                Console.Write("\rName: ");
                if (string.IsNullOrEmpty(response.StdOutput) && !string.IsNullOrEmpty(response.StdError))
                {
                    throw new ApplicationException($"ERROR: Loading Cognitive Services resources: {response.StdError}");
                }

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

            public static async Task<AzCli.CognitiveServicesResourceInfo?> TryCreateCognitiveServicesResource(bool interactive, string subscriptionId = null, string regionLocationFilter = null, string groupFilter = null, string resourceFilter = null, string kind = null, string sku = "F0", bool agreeTerms = false)
            {
                ConsoleHelpers.WriteLineWithHighlight("\n`RESOURCE GROUP`");

                var regionLocation = await RegionLocationPicker.FindRegionAsync(interactive, regionLocationFilter, true);
                if (regionLocation == null) return null;

                var group = await ResourceGroupPicker.PickOrCreateResourceGroup(interactive, subscriptionId, regionLocation?.Name, groupFilter);

                ConsoleHelpers.WriteLineWithHighlight($"\n`CREATE {Program.SERVICE_RESOURCE_DISPLAY_NAME_ALL_CAPS}`");
                Console.WriteLine($"Region: {group.RegionLocation}");
                Console.WriteLine($"Group: {group.Name}");

                var name = string.IsNullOrEmpty(resourceFilter)
                    ? NamePickerHelper.DemandPickOrEnterName("Name: ", kind ?? "cs")
                    : AskPromptHelper.AskPrompt("Name: ", resourceFilter);
                if (string.IsNullOrEmpty(name)) return null;

                if (!agreeTerms && !CheckAgreeTerms(kind)) return null;

                Console.Write("*** CREATING ***");
                var response = await AzCli.CreateCognitiveServicesResource(subscriptionId, group.Name, group.RegionLocation, name, kind, sku);

                Console.Write("\r");
                if (string.IsNullOrEmpty(response.StdOutput) && !string.IsNullOrEmpty(response.StdError))
                {
                    throw new ApplicationException($"ERROR: Creating Cognitive Services resource: {response.StdError}");
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

            private static bool CheckAgreeTerms(string kind)
            {
                var checkAttestation = kind.ToLower() switch
                {
                    "face" => true,
                    "cognitiveservices" => true,
                    "openai" => true,
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
}
