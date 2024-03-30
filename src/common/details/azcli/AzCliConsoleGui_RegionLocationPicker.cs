//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Azure.AI.Details.Common.CLI.ConsoleGui;

namespace Azure.AI.Details.Common.CLI
{
    public partial class AzCliConsoleGui
    {
        public static async Task<AzCli.AccountRegionLocationInfo> PickRegionLocationAsync(bool interactive, string subscriptionId, string regionFilter = null, bool allowAnyRegionOption = true)
        {
            var regionLocation = await FindRegionAsync(interactive, subscriptionId, regionFilter, allowAnyRegionOption);
            if (regionLocation == null)
            {
                throw new ApplicationException($"CANCELED: No resource region/location selected.");
            }
            return regionLocation.Value;
        }

        public static async Task<AzCli.AccountRegionLocationInfo?> FindRegionAsync(bool interactive, string subscriptionId, string regionFilter = null, bool allowAnyRegionOption = false)
        {
            var p0 = allowAnyRegionOption ? "(Any region/location)" : null;
            var hasP0 = !string.IsNullOrEmpty(p0);

            Console.Write("\rRegion: *** Loading choices ***");
            var allRegions = await Program.SubscriptionClient.GetAllRegionsAsync(subscriptionId, Program.CancelToken);

            Console.Write("\rRegion: ");
            allRegions.ThrowOnFail("resource region/locations");

            var regions = allRegions.Value
                .Where(x => MatchRegionLocationFilter(x, regionFilter))
                .OrderBy(x => x.RegionalDisplayName)
                .ToArray();

            var exactMatches = regionFilter == null
                ? Array.Empty<AzCli.AccountRegionLocationInfo>()
                : regions.Where(x => ExactMatchRegionLocation(x, regionFilter)).ToArray();
            if (exactMatches.Length == 1) regions = regions.Where(x => ExactMatchRegionLocation(x, regionFilter)).ToArray();

            if (regions.Count() == 0)
            {
                ConsoleHelpers.WriteLineError(allRegions.Value.Count() > 0
                    ? "*** No matching resource region/locations found ***"
                    : "*** No resource region/locations found ***");
                return null;
            }
            else if (regions.Count() == 1 && (!interactive || exactMatches.Length == 1))
            {
                var region = regions.First();
                DisplayNameAndDisplayName(region);
                return region;
            }
            else if (!interactive)
            {
                ConsoleHelpers.WriteLineError("*** More than 1 region/location found ***");
                Console.WriteLine();
                DisplayRegionLocations(regions, "  ");
                return null;
            }

            return interactive
                ? ListBoxPickAccountRegionLocation(regions.ToArray(), p0)
                : null;
        }

        private static AzCli.AccountRegionLocationInfo? ListBoxPickAccountRegionLocation(AzCli.AccountRegionLocationInfo[] items, string p0)
        {
            var list = items.Select(x => x.RegionalDisplayName).ToList();

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
                return new AzCli.AccountRegionLocationInfo();
            }

            if (hasP0) picked--;
            var regionLocation = items[picked];

            DisplayNameAndDisplayName(regionLocation);
            return regionLocation;
        }

        static bool ExactMatchRegionLocation(AzCli.AccountRegionLocationInfo regionLocation, string regionLocationFilter)
        {
            return regionLocation.Name.ToLowerInvariant() == regionLocationFilter || regionLocation.DisplayName == regionLocationFilter || regionLocation.RegionalDisplayName == regionLocationFilter;
        }

        private static bool MatchRegionLocationFilter(AzCli.AccountRegionLocationInfo regionLocation, string regionLocationFilter)
        {
            if (regionLocationFilter == null || ExactMatchRegionLocation(regionLocation, regionLocationFilter))
            {
                return true;
            }

            var displayName = regionLocation.DisplayName.ToLowerInvariant();
            var regionalName = regionLocation.RegionalDisplayName.ToLowerInvariant();

            return displayName.Contains(regionLocationFilter) || StringHelpers.ContainsAllCharsInOrder(displayName, regionLocationFilter) ||
                regionalName.Contains(regionLocationFilter) || StringHelpers.ContainsAllCharsInOrder(regionalName, regionLocationFilter);
        }

        private static void DisplayRegionLocations(IList<AzCli.AccountRegionLocationInfo> regionLocations, string prefix)
        {
            foreach (var regionLocation in regionLocations)
            {
                Console.Write(prefix);
                DisplayNameAndDisplayName(regionLocation);
            }
        }

        private static void DisplayNameAndDisplayName(AzCli.AccountRegionLocationInfo regionLocation)
        {
            Console.Write($"{regionLocation.DisplayName} ({regionLocation.Name})                  ");
            Console.WriteLine(new string(' ', 20));
        }
    }
}
