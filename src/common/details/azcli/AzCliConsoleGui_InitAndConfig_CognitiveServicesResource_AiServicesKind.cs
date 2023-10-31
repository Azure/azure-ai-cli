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
        public static async Task<AzCli.CognitiveServicesResourceInfoEx> InitAndConfigCognitiveServicesAiServicesKindResource(bool interactive, string subscriptionId, string regionFilter = null, string groupFilter = null, string resourceFilter = null, string kind = null, string sku = null, bool yes = false)
        {
            kind ??= "AIServices";
            var sectionHeader = "AI SERVICES";

            var regionLocation = !string.IsNullOrEmpty(regionFilter) ? await AzCliConsoleGui.PickRegionLocationAsync(interactive, regionFilter) : new AzCli.AccountRegionLocationInfo();
            var resource = await AzCliConsoleGui.PickOrCreateCognitiveResource(sectionHeader, interactive, subscriptionId, regionLocation.Name, groupFilter, resourceFilter, kind, sku, yes);

            var chatDeployment = await AzCliConsoleGui.PickOrCreateCognitiveServicesResourceDeployment(interactive, "Chat", subscriptionId, resource.Group, resource.RegionLocation, resource.Name, null);
            var embeddingsDeployment = await AzCliConsoleGui.PickOrCreateCognitiveServicesResourceDeployment(interactive, "Embeddings", subscriptionId, resource.Group, resource.RegionLocation, resource.Name, null);
            var evaluationDeployment = await AzCliConsoleGui.PickOrCreateCognitiveServicesResourceDeployment(interactive, "Evaluation", subscriptionId, resource.Group, resource.RegionLocation, resource.Name, null);

            var keys = await AzCliConsoleGui.LoadCognitiveServicesResourceKeys(sectionHeader, subscriptionId, resource);

            ConfigSetHelpers.ConfigCognitiveServicesAIServicesKindResource(subscriptionId, resource.RegionLocation, resource.Endpoint, chatDeployment, embeddingsDeployment, evaluationDeployment, keys.Key1);

            return new AzCli.CognitiveServicesResourceInfoEx
            {
                Id = resource.Id,
                Group = resource.Group,
                Name = resource.Name,
                Kind = resource.Kind,
                RegionLocation = resource.RegionLocation,
                Endpoint = resource.Endpoint,
                Key = keys.Key1,
                ChatDeployment = chatDeployment.Name,
                EmbeddingsDeployment = embeddingsDeployment.Name,
                EvaluationDeployment = evaluationDeployment.Name
            };
        }
    }
}