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
        public static async Task<AzCli.CognitiveServicesResourceInfoEx> PickOrCreateAndConfigCognitiveServicesCognitiveServicesKindResource(bool interactive, string subscriptionId, string regionFilter = null, string groupFilter = null, string resourceFilter = null, string kinds = null, string sku = null, bool yes = false)
        {
            kinds ??= "CognitiveServices";
            var sectionHeader = "AI SERVICES (v1)";

            var regionLocation = !string.IsNullOrEmpty(regionFilter) ? await AzCliConsoleGui.PickRegionLocationAsync(interactive, regionFilter) : new AzCli.AccountRegionLocationInfo();
            var resource = await AzCliConsoleGui.PickOrCreateCognitiveResource(sectionHeader, interactive, subscriptionId, regionLocation.Name, groupFilter, resourceFilter, kinds, sku, yes);

            var keys = await AzCliConsoleGui.LoadCognitiveServicesResourceKeys(sectionHeader, subscriptionId, resource);

            ConfigSetHelpers.ConfigCognitiveServicesCognitiveServicesKindResource(subscriptionId, resource.RegionLocation, resource.Endpoint, keys.Key1);

            return new AzCli.CognitiveServicesResourceInfoEx
            {
                Id = resource.Id,
                Group = resource.Group,
                Name = resource.Name,
                Kind = resource.Kind,
                RegionLocation = resource.RegionLocation,
                Endpoint = resource.Endpoint,
                Key = keys.Key1,
            };
        }
    }
}
