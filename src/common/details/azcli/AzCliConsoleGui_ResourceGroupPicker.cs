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
        public class ResourceGroupPicker
        {
            public static async Task<AzCli.ResourceGroupInfo> PickOrCreateResourceGroup(bool interactive, string subscriptionId = null, string regionFilter = null, string groupFilter = null)
            {
                var createNewItem = !string.IsNullOrEmpty(groupFilter)
                    ? $"(Create `{groupFilter}`)"
                    : interactive ? "(Create new)" : null;

                (var group, var error) = await FindGroupAsync(interactive, subscriptionId, regionFilter, groupFilter, createNewItem);
                if ((group != null && group.Value.Name == null) || (group == null && groupFilter == null))
                {
                    (group, error) = await TryCreateResourceGroup(interactive, subscriptionId, regionFilter, groupFilter);
                }

                if (group == null && error != null)
                {
                    throw new ApplicationException($"ERROR: Loading or creating resource group:\n{error}");
                }
                else if (group == null)
                {
                    throw new ApplicationException($"CANCELED: No resource selected");
                }

                return group.Value;
            }

            public static async Task<(AzCli.ResourceGroupInfo? Group, string Error)> FindGroupAsync(bool interactive, string subscription = null, string regionLocation = null, string groupFilter = null, string allowCreateGroupOption = null)
            {
                var allowCreateGroup = !string.IsNullOrEmpty(allowCreateGroupOption);

                Console.Write("\rGroup: *** Loading choices ***");
                var response = await AzCli.ListResourceGroups(subscription, regionLocation);

                Console.Write("\rGroup: ");
                if (string.IsNullOrEmpty(response.StdOutput) && !string.IsNullOrEmpty(response.StdError))
                {
                    ConsoleHelpers.WriteLineError($"ERROR: Loading resource groups: {response.StdError}");
                    return (null, response.StdError);
                }

                var groups = response.Payload
                    .Where(x => MatchGroupFilter(x, groupFilter))
                    .OrderBy(x => x.Name)
                    .ToList();

                var exactMatch = groupFilter != null && groups.Count(x => ExactMatchGroup(x, groupFilter)) == 1;
                if (exactMatch) groups = groups.Where(x => ExactMatchGroup(x, groupFilter)).ToList();

                if (groups.Count() == 0)
                {
                    if  (!allowCreateGroup)
                    {
                        ConsoleHelpers.WriteLineError(response.Payload.Count() > 0
                            ? $"*** No matching resource groups found ***"
                            : $"*** No resource groups found ***");
                        return (null, null);
                    }
                    else if (!interactive)
                    {
                        Console.WriteLine(allowCreateGroupOption);
                        return (new AzCli.ResourceGroupInfo(), null);
                    }
                }
                else if (groups.Count() == 1 && (!interactive || exactMatch))
                {
                    var group = groups.First();
                    DisplayNameAndRegionLocation(group);
                    return (group, null);
                }
                else if (!interactive)
                {
                    ConsoleHelpers.WriteLineError("*** More than 1 resource group found ***");
                    Console.WriteLine();
                    DisplayGroups(groups, "  ");
                    return (null, null);
                }

                return ListBoxPickResourceGroup(groups.ToArray(), allowCreateGroupOption);
            }

            private static async Task<(AzCli.ResourceGroupInfo? Group, string Error)> TryCreateResourceGroup(bool interactive, string subscriptionId, string regionLocationFilter, string groupName)
            {
                ConsoleHelpers.WriteLineWithHighlight("\n`CREATE RESOURCE GROUP`");

                (var regionLocation, var errorLoc) = await RegionLocationPicker.FindRegionAsync(interactive, regionLocationFilter, false);
                if (regionLocation == null) return (null, errorLoc);

                var name = AskPrompt("Name: ", groupName);
                if (string.IsNullOrEmpty(name)) return (null, null);

                Console.Write("*** CREATING ***");
                var response = await AzCli.CreateResourceGroup(subscriptionId, regionLocation.Value.Name, name);

                Console.Write("\r");
                if (string.IsNullOrEmpty(response.StdOutput) && !string.IsNullOrEmpty(response.StdError))
                {
                    ConsoleHelpers.WriteLineError($"ERROR: Creating resource group.\n{response.StdError}");
                    return (null, response.StdError);
                }

                Console.WriteLine("\r*** CREATED ***  ");
                return (response.Payload, null);
            }

            private static (AzCli.ResourceGroupInfo? Group, string Error) ListBoxPickResourceGroup(AzCli.ResourceGroupInfo[] groups, string p0)
            {
                var list = groups.Select(x => x.Name).ToList();

                var hasP0 = !string.IsNullOrEmpty(p0);
                if (hasP0) list.Insert(0, p0);

                var picked = ListBoxPicker.PickIndexOf(list.ToArray());
                if (picked < 0)
                {
                    return (null, null);
                }

                if (hasP0 && picked == 0)
                {
                    Console.WriteLine(p0);
                    return (new AzCli.ResourceGroupInfo(), null);
                }

                if (hasP0) picked--;
                Console.WriteLine(groups[picked].Name);
                return (groups[picked], null);
            }

            static bool ExactMatchGroup(AzCli.ResourceGroupInfo group, string groupFilter)
            {
                return group.Id == groupFilter || group.Name.ToLower() == groupFilter;
            }

            private static bool MatchGroupFilter(AzCli.ResourceGroupInfo group, string groupFilter)
            {
                if (groupFilter == null || ExactMatchGroup(group, groupFilter))
                {
                    return true;
                }

                var name = group.Name.ToLower();
                return name.Contains(groupFilter) || StringHelpers.ContainsAllCharsInOrder(name, groupFilter);
            }

            private static void DisplayGroups(List<AzCli.ResourceGroupInfo> groups, string prefix)
            {
                foreach (var group in groups)
                {
                    Console.Write(prefix);
                    DisplayNameAndRegionLocation(group);
                }
            }

            private static void DisplayNameAndRegionLocation(AzCli.ResourceGroupInfo group)
            {
                Console.Write($"{group.Name} ({group.RegionLocation})");
                Console.WriteLine(new string(' ', 20));
            }
        }
    }
}
