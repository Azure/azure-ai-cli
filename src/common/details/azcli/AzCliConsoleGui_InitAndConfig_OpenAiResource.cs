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
        public static async Task<AzCli.CognitiveServicesOpenAiResourceInfo> InitAndConfigOpenAiResource(bool interactive, string subscriptionId, string regionFilter, string groupFilter, string resourceFilter, string kind, string sku, bool yes)
        {
            var regionLocation = !string.IsNullOrEmpty(regionFilter) ? await AzCliConsoleGui.PickRegionLocationAsync(interactive, regionFilter) : new AzCli.AccountRegionLocationInfo();
            var resource = await AzCliConsoleGui.PickOrCreateCognitiveResource(interactive, subscriptionId, regionLocation.Name, groupFilter, resourceFilter, kind, sku, yes);

            var deployment = await AzCliConsoleGui.PickOrCreateDeployment(interactive, "Chat", subscriptionId, resource.Group, resource.RegionLocation, resource.Name, null);
            var embeddingsDeployment = await AzCliConsoleGui.PickOrCreateDeployment(interactive, "Embeddings", subscriptionId, resource.Group, resource.RegionLocation, resource.Name, null);

            var keys = await AzCliConsoleGui.LoadCognitiveServicesResourceKeys(subscriptionId, resource);

            ConfigSetHelpers.ConfigServiceResource(subscriptionId, resource.RegionLocation, resource.Endpoint, deployment.Name, embeddingsDeployment.Name, keys.Key1);

            return new AzCli.CognitiveServicesOpenAiResourceInfo
            {
                Id = resource.Id,
                Group = resource.Group,
                Name = resource.Name,
                Kind = resource.Kind,
                RegionLocation = resource.RegionLocation,
                Endpoint = resource.Endpoint,
                Key = keys.Key1,
                ChatDeployment = deployment.Name,
                EmbeddingsDeployment = embeddingsDeployment.Name,
            };
        }
    }
}
