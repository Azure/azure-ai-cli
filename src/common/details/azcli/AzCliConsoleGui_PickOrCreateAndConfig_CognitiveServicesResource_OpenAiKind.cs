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
            (chatDeployment, createdNew) = await Program.Telemetry.WrapAsync(
                () => PickOrCreateCognitiveServicesResourceDeployment(interactive, allowSkipDeployments, "Chat", subscriptionId, resource.Group, resource.RegionLocation, resource.Name, chatDeploymentFilter),
                (outcome, res, ex, timeTaken) => CreateInitDeploymentTelemetryEvent(values, InitStage.Chat, outcome, ex, timeTaken, res))
            .ConfigureAwait(false);

            (embeddingsDeployment, createdNew) = await Program.Telemetry.WrapAsync(
                () => PickOrCreateCognitiveServicesResourceDeployment(interactive, allowSkipDeployments, "Embeddings", subscriptionId, resource.Group, resource.RegionLocation, resource.Name, embeddingsDeploymentFilter),
                (outcome, res,ex, timeTaken) => CreateInitDeploymentTelemetryEvent(values, InitStage.Embeddings, outcome, ex, timeTaken, res))
            .ConfigureAwait(false);

            (evaluationDeployment, createdNew) = await Program.Telemetry.WrapAsync(
                () => PickOrCreateCognitiveServicesResourceDeployment(interactive, allowSkipDeployments, "Evaluation", subscriptionId, resource.Group, resource.RegionLocation, resource.Name, evaluationsDeploymentFilter),
                (outcome, res, ex, timeTaken) => CreateInitDeploymentTelemetryEvent(values, InitStage.Evaluation, outcome, ex, timeTaken, res))
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
