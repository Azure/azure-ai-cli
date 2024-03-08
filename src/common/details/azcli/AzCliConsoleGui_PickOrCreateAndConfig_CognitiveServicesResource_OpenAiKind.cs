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
using Azure.AI.Details.Common.CLI.Telemetry;
using Azure.AI.Details.Common.CLI.Telemetry.Events;

namespace Azure.AI.Details.Common.CLI
{
    public partial class AzCliConsoleGui
    {
        public static async Task<AzCli.CognitiveServicesResourceInfoEx> PickOrCreateAndConfigCognitiveServicesOpenAiKindResource(
            INamedValues values,
            bool interactive,
            bool allowSkipDeployments,
            string subscriptionId,
            string regionFilter = null,
            string groupFilter = null,
            string resourceFilter = null,
            string kinds = null,
            string sku = null,
            bool yes = false,
            string chatDeploymentFilter = null,
            string embeddingsDeploymentFilter = null,
            string evaluationsDeploymentFilter = null)
        {
            kinds ??= "OpenAI;AIServices";
            var sectionHeader = "AZURE OPENAI RESOURCE";

            var regionLocation = !string.IsNullOrEmpty(regionFilter) ? await AzCliConsoleGui.PickRegionLocationAsync(interactive, regionFilter) : new AzCli.AccountRegionLocationInfo();
            var resource = await AzCliConsoleGui.PickOrCreateCognitiveResource(sectionHeader, interactive, subscriptionId, regionLocation.Name, groupFilter, resourceFilter, kinds, sku, yes);

            var (chatDeployment, embeddingsDeployment, evaluationDeployment, keys) = await PickOrCreateAndConfigCognitiveServicesOpenAiKindResourceDeployments(
                values,
                sectionHeader,
                interactive,
                allowSkipDeployments,
                subscriptionId,
                resource,
                chatDeploymentFilter,
                embeddingsDeploymentFilter,
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
                INamedValues values,
                string sectionHeader,
                bool interactive,
                bool allowSkipDeployments,
                string subscriptionId,
                AzCli.CognitiveServicesResourceInfo resource,
                string chatDeploymentFilter = null,
                string embeddingsDeploymentFilter = null,
                string evaluationsDeploymentFilter = null)
        {
            bool createdNew = false;
            AzCli.CognitiveServicesDeploymentInfo? chatDeployment = null, embeddingsDeployment = null, evaluationDeployment = null;
            var token = CancellationToken.None;

            // TODO FIXME Telemetry events should not be raised from here. Will be addressed in future refactor
            await Program.Telemetry.WrapWithTelemetryAsync(async (t) =>
                {
                    (chatDeployment, createdNew) = await PickOrCreateCognitiveServicesResourceDeployment(interactive, allowSkipDeployments, "Chat", subscriptionId, resource.Group, resource.RegionLocation, resource.Name, chatDeploymentFilter)
                        .ConfigureAwait(false);
                },
                (outcome, ex, timeTaken) => CreateInitDeploymentTelemetryEvent(values, InitStage.Chat, outcome, ex, timeTaken, createdNew, chatDeployment),
                token)
                .ConfigureAwait(false);
            await Program.Telemetry.WrapWithTelemetryAsync(async (t) =>
                {
                    (embeddingsDeployment, createdNew) = await PickOrCreateCognitiveServicesResourceDeployment(interactive, allowSkipDeployments, "Embeddings", subscriptionId, resource.Group, resource.RegionLocation, resource.Name, embeddingsDeploymentFilter)
                        .ConfigureAwait(false);
                },
                (outcome, ex, timeTaken) => CreateInitDeploymentTelemetryEvent(values, InitStage.Embeddings, outcome, ex, timeTaken, createdNew, embeddingsDeployment),
                token)
                .ConfigureAwait(false);
            await Program.Telemetry.WrapWithTelemetryAsync(async (t) =>
                {
                    (evaluationDeployment, createdNew) = await PickOrCreateCognitiveServicesResourceDeployment(interactive, allowSkipDeployments, "Evaluation", subscriptionId, resource.Group, resource.RegionLocation, resource.Name, evaluationsDeploymentFilter)
                        .ConfigureAwait(false);
                },
                (outcome, ex, timeTaken) => CreateInitDeploymentTelemetryEvent(values, InitStage.Evaluation, outcome, ex, timeTaken, createdNew, evaluationDeployment),
                token)
                .ConfigureAwait(false);
            
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

        private static ITelemetryEvent CreateInitDeploymentTelemetryEvent(INamedValues values, InitStage stage, Outcome outcome, Exception ex, TimeSpan duration, bool createdNew, AzCli.CognitiveServicesDeploymentInfo? deployment) =>
            values == null ? null : new InitTelemetryEvent(stage)
            {
                Outcome = outcome,
                DurationInMs = duration.TotalMilliseconds,
                Error = ex?.Message,
                Selected = deployment == null
                    ? ex == null ? "skip" : "new"
                    : createdNew ? "new" : "existing",
                RunId = values.GetOrDefault("telemetry.init.run_id", null),
                RunType = values.GetOrDefault("telemetry.init.run_type", null),
            };
    }
}
