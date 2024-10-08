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
using Azure.AI.Details.Common.CLI.Telemetry;
using Azure.AI.Details.Common.CLI.Telemetry.Events;

namespace Azure.AI.Details.Common.CLI
{
    public partial class AzCliConsoleGui
    {
        public static async Task<AzCli.CognitiveServicesResourceInfoEx> PickOrCreateAndConfigCognitiveServicesOpenAiKindResource(
            INamedValues values,
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
            bool skipRealTime = false,
            bool allowSkipRealTime = true,
            string chatDeploymentFilter = null,
            string embeddingsDeploymentFilter = null,
            string realtimeDeploymentFilter = null,
            string chatModelFilter = null,
            string embeddingsModelFilter = null,
            string realtimeModelFilter = null)
        {
            kinds ??= "OpenAI;AIServices";
            var sectionHeader = "AZURE OPENAI RESOURCE";

            var regionLocation = !string.IsNullOrEmpty(regionFilter) ? await AzCliConsoleGui.PickRegionLocationAsync(interactive, regionFilter) : new AzCli.AccountRegionLocationInfo();
            var resource = await AzCliConsoleGui.PickOrCreateCognitiveResource(sectionHeader, interactive, subscriptionId, regionLocation.Name, groupFilter, resourceFilter, kinds, sku, yes);

            var (chatDeployment, embeddingsDeployment, realtimeDeployment, keys) = await PickOrCreateAndConfigCognitiveServicesOpenAiKindResourceDeployments(
                values,
                sectionHeader,
                interactive,
                subscriptionId,
                resource,
                skipChat,
                allowSkipChat,
                chatDeploymentFilter,
                chatModelFilter,
                skipEmbeddings,
                allowSkipEmbeddings,
                embeddingsDeploymentFilter,
                embeddingsModelFilter,
                skipRealTime,
                allowSkipRealTime,
                realtimeDeploymentFilter,
                realtimeModelFilter);

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
                RealTimeDeployment = realtimeDeployment.HasValue ? realtimeDeployment.Value.Name : null
            };
        }

        public static async Task<(AzCli.CognitiveServicesDeploymentInfo?, AzCli.CognitiveServicesDeploymentInfo?, AzCli.CognitiveServicesDeploymentInfo?, AzCli.CognitiveServicesKeyInfo)>
            PickOrCreateAndConfigCognitiveServicesOpenAiKindResourceDeployments(
                INamedValues values,
                string sectionHeader,
                bool interactive,
                string subscriptionId,
                AzCli.CognitiveServicesResourceInfo resource,
                bool skipChat = false,
                bool allowSkipChat = true,
                string chatDeploymentFilter = null,
                string chatModelFilter = null,
                bool skipEmbeddings = true,
                bool allowSkipEmbeddings = true,
                string embeddingsDeploymentFilter = null,
                string embeddingsModelFilter = null,
                bool skipRealtime = true,
                bool allowSkipRealTime = true,
                string realtimeDeploymentFilter = null,
                string realtimeModelFilter = null)
        {
            bool createdNew = false;
            AzCli.CognitiveServicesDeploymentInfo? chatDeployment = null, embeddingsDeployment = null, realtimeDeployment = null;
            var token = CancellationToken.None;

            (chatDeployment, createdNew) = !skipChat
                ? await Program.Telemetry.WrapAsync(
                    () => PickOrCreateCognitiveServicesResourceDeployment(interactive, allowSkipChat, "Chat", subscriptionId, resource.Group, resource.RegionLocation, resource.Name, chatDeploymentFilter, chatModelFilter),
                    (outcome, res, ex, timeTaken) => CreateInitDeploymentTelemetryEvent(values, InitStage.Chat, outcome, ex, timeTaken, res))
                : (null, false);

            (embeddingsDeployment, createdNew) = !skipEmbeddings
                ? await Program.Telemetry.WrapAsync(
                    () => PickOrCreateCognitiveServicesResourceDeployment(interactive, allowSkipEmbeddings, "Embeddings", subscriptionId, resource.Group, resource.RegionLocation, resource.Name, embeddingsDeploymentFilter, embeddingsModelFilter),
                (outcome, res,ex, timeTaken) => CreateInitDeploymentTelemetryEvent(values, InitStage.Embeddings, outcome, ex, timeTaken, res))
                : (null, false);

            (realtimeDeployment, createdNew) = !skipRealtime
                ? await Program.Telemetry.WrapAsync(
                    () => PickOrCreateCognitiveServicesResourceDeployment(interactive, allowSkipRealTime, "RealTime", subscriptionId, resource.Group, resource.RegionLocation, resource.Name, realtimeDeploymentFilter, realtimeModelFilter),
                    (outcome, res, ex, timeTaken) => CreateInitDeploymentTelemetryEvent(values, InitStage.RealTime, outcome, ex, timeTaken, res))
                : (null, false);
            
            var keys = await AzCliConsoleGui.LoadCognitiveServicesResourceKeys(sectionHeader, subscriptionId, resource);
         
            if (resource.Kind == "AIServices")
            {
                ConfigSetHelpers.ConfigCognitiveServicesAIServicesKindResource(subscriptionId, resource.RegionLocation, resource.Endpoint, chatDeployment, embeddingsDeployment, realtimeDeployment, keys.Key1);
            }
            else
            {
                ConfigSetHelpers.ConfigOpenAiResource(subscriptionId, resource.RegionLocation, resource.Endpoint, chatDeployment, embeddingsDeployment, realtimeDeployment, keys.Key1);
            }
         
            return (chatDeployment, embeddingsDeployment, realtimeDeployment, keys);
        }

        private static ITelemetryEvent CreateInitDeploymentTelemetryEvent(INamedValues values, InitStage stage, Outcome outcome, Exception ex, TimeSpan duration, (AzCli.CognitiveServicesDeploymentInfo? deployment, bool createdNew) result) =>
            values == null ? null : new InitTelemetryEvent(stage)
            {
                Outcome = outcome,
                DurationInMs = duration.TotalMilliseconds,
                Error = ex?.Message,
                Selected = result.deployment == null
                    ? ex == null ? "skip" : "new"
                    : result.createdNew ? "new" : "existing",
                RunId = values.GetOrDefault("telemetry.init.run_id", null),
                RunType = values.GetOrDefault("telemetry.init.run_type", null),
            };
    }
}
