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
        public static async Task<AzCli.CognitiveServicesResourceInfoEx> PickOrCreateAndConfigCognitiveServicesOpenAiKindResource(
            bool interactive,
            string subscriptionId,
            string regionFilter = null,
            string groupFilter = null,
            string resourceFilter = null,
            string kinds = null,
            string sku = null,
            bool yes = false,
            bool skipChat = false,
            bool allowSkipChat = true,
            bool skipEmbeddings = false,
            bool allowSkipEmbeddings = true,
            bool skipEvaluations = false,
            bool allowSkipEvaluations = true,
            string chatDeploymentFilter = null,
            string embeddingsDeploymentFilter = null,
            string evaluationsDeploymentFilter = null)
        {
            kinds ??= "OpenAI;AIServices";
            var sectionHeader = "AZURE OPENAI RESOURCE";

            var regionLocation = !string.IsNullOrEmpty(regionFilter) ? await AzCliConsoleGui.PickRegionLocationAsync(interactive, regionFilter) : new AzCli.AccountRegionLocationInfo();
            var resource = await AzCliConsoleGui.PickOrCreateCognitiveResource(sectionHeader, interactive, subscriptionId, regionLocation.Name, groupFilter, resourceFilter, kinds, sku, yes);

            var (chatDeployment, embeddingsDeployment, evaluationDeployment, keys) = await PickOrCreateAndConfigCognitiveServicesOpenAiKindResourceDeployments(
                sectionHeader,
                interactive,
                subscriptionId,
                resource,
                skipChat,
                allowSkipChat,
                chatDeploymentFilter,
                skipEmbeddings,
                allowSkipEmbeddings,
                embeddingsDeploymentFilter,
                skipEvaluations,
                allowSkipEvaluations,
                evaluationsDeploymentFilter);

            return new AzCli.CognitiveServicesResourceInfoEx
            {
                Id = resource.Id,
                Group = resource.Group,
                Name = resource.Name,
                Kind = resource.Kind,
                RegionLocation = resource.RegionLocation,
                Endpoint = resource.Endpoint,
                Key = keys.Key1,
                ChatDeployment = chatDeployment.HasValue ? chatDeployment.Value.Name : null,
                EmbeddingsDeployment = embeddingsDeployment.HasValue ? embeddingsDeployment.Value.Name : null,
                EvaluationDeployment = evaluationDeployment.HasValue ? evaluationDeployment.Value.Name : null
            };
        }

        public static async Task<(AzCli.CognitiveServicesDeploymentInfo?, AzCli.CognitiveServicesDeploymentInfo?, AzCli.CognitiveServicesDeploymentInfo?, AzCli.CognitiveServicesKeyInfo)>
            PickOrCreateAndConfigCognitiveServicesOpenAiKindResourceDeployments(
                string sectionHeader,
                bool interactive,
                string subscriptionId,
                AzCli.CognitiveServicesResourceInfo resource,
                bool skipChat = false,
                bool allowSkipChat = true,
                string chatDeploymentFilter = null,
                bool skipEmbeddings = true,
                bool allowSkipEmbeddings = true,
                string embeddingsDeploymentFilter = null,
                bool skipEvaluations = true,
                bool allowSkipEvaluations = true,
                string evaluationsDeploymentFilter = null)
        {
            var chatDeployment = !skipChat
                ? await AzCliConsoleGui.PickOrCreateCognitiveServicesResourceDeployment(interactive, allowSkipChat, "Chat", subscriptionId, resource.Group, resource.RegionLocation, resource.Name, chatDeploymentFilter)
                : null;
            var embeddingsDeployment = !skipEmbeddings
                ? await AzCliConsoleGui.PickOrCreateCognitiveServicesResourceDeployment(interactive, allowSkipEmbeddings, "Embeddings", subscriptionId, resource.Group, resource.RegionLocation, resource.Name, embeddingsDeploymentFilter)
                : null;
            var evaluationDeployment = !skipEvaluations
                ? await AzCliConsoleGui.PickOrCreateCognitiveServicesResourceDeployment(interactive, allowSkipEvaluations, "Evaluation", subscriptionId, resource.Group, resource.RegionLocation, resource.Name, evaluationsDeploymentFilter)
                : null;

            var keys = await AzCliConsoleGui.LoadCognitiveServicesResourceKeys(sectionHeader, subscriptionId, resource);
         
            if (resource.Kind == "AIServices")
            {
                ConfigSetHelpers.ConfigCognitiveServicesAIServicesKindResource(subscriptionId, resource.RegionLocation, resource.Endpoint, chatDeployment, embeddingsDeployment, evaluationDeployment, keys.Key1);
            }
            else
            {
                ConfigSetHelpers.ConfigOpenAiResource(subscriptionId, resource.RegionLocation, resource.Endpoint, chatDeployment, embeddingsDeployment, evaluationDeployment, keys.Key1);
            }
         
            return (chatDeployment, embeddingsDeployment, evaluationDeployment, keys);
        }
    }
}
