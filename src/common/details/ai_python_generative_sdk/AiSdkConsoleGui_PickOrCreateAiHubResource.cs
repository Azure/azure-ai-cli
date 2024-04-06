//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

#if USE_PYTHON_HUB_PROJECT_CONNECTION_OR_RELATED

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Azure.AI.Details.Common.CLI.ConsoleGui;
using System.Text.Json;
using System.IO;

namespace Azure.AI.Details.Common.CLI
{
    public partial class AiSdkConsoleGui
    {
        public static async Task<AiHubResourceInfo> PickAiHubResource(ICommandValues values, string subscription)
        {
            return (await PickOrCreateAiHubResource(false, values, subscription)).Item1;
        }

        public static async Task<(AiHubResourceInfo, bool)> PickOrCreateAiHubResource(ICommandValues values, string subscription)
        {
            return await PickOrCreateAiHubResource(true, values, subscription);
        }

        public static async Task<AiHubResourceInfo> CreateAiHubResource(ICommandValues values, string subscription)
        {
            var resource = await TryCreateAiHubResourceInteractive(values, subscription);
            return FinishPickOrCreateAiHubResource(values, resource);
        }

        private static async Task<(AiHubResourceInfo, bool)> PickOrCreateAiHubResource(bool allowCreate, ICommandValues values, string subscription)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`AZURE AI RESOURCE`");
            Console.Write("\rName: *** Loading choices ***");

            var json = PythonSDKWrapper.ListResources(values, subscription);
            if (Program.Debug) Console.WriteLine(json);

            var parsed = !string.IsNullOrEmpty(json) ? JsonDocument.Parse(json) : default;
            var items = parsed?.GetPropertyArrayOrEmpty("resources") ?? Array.Empty<JsonElement>();

            var choices = new List<string>();
            foreach (var item in items)
            {
                var name = item.GetPropertyStringOrNull("name");
                var location = item.GetPropertyStringOrNull("location");
                var displayName = item.GetPropertyStringOrNull("display_name");

                choices.Add(string.IsNullOrEmpty(displayName)
                    ? $"{name} ({location})"
                    : $"{displayName} ({location})");
            }

            if (allowCreate)
            {
                choices.Insert(0, "(Create w/ integrated Open AI + AI Services)");
                choices.Insert(1, "(Create w/ standalone Open AI resource)");
            }

            if (choices.Count == 0)
            {
                throw new ApplicationException($"CANCELED: No resources found");
            }

            Console.Write("\rName: ");
            var picked = ListBoxPicker.PickIndexOf(choices.ToArray());
            if (picked < 0)
            {
                throw new OperationCanceledException($"CANCELED: No resource selected");
            }

            Console.WriteLine($"\rName: {choices[picked]}");
            var resource = allowCreate
                ? (picked >= 2 ? items.ToArray()[picked - 2] : default(JsonElement?))
                : items.ToArray()[picked];

            var byoServices = allowCreate && picked == 1;
            if (byoServices)
            {
                var regionFilter = values.GetOrEmpty("init.service.resource.region.name");
                var groupFilter = values.GetOrEmpty("init.service.resource.group.name");
                var resourceFilter = values.GetOrEmpty("init.service.cognitiveservices.resource.name");
                var kind = values.GetOrDefault("init.service.cognitiveservices.resource.kind", "OpenAI;AIServices");
                var sku = values.GetOrDefault("init.service.cognitiveservices.resource.sku", Program.CognitiveServiceResourceSku);
                var yes = values.GetOrDefault("init.service.cognitiveservices.terms.agree", false);

                var openAiResource = await AzCliConsoleGui.PickOrCreateAndConfigCognitiveServicesOpenAiKindResource(values, true, subscription, regionFilter, groupFilter, resourceFilter, kind, sku, yes);
                values.Reset("service.openai.deployments.picked", "true");

                ResourceGroupNameToken.Data().Set(values, openAiResource.Group);
                values.Reset("service.resource.region.name", openAiResource.RegionLocation);

                values.Reset("service.openai.endpoint", openAiResource.Endpoint);
                values.Reset("service.openai.key", openAiResource.Key);
                values.Reset("service.openai.resource.id", openAiResource.Id);
                values.Reset("service.openai.resource.kind", openAiResource.Kind);
            }

            var createNewHub = allowCreate && (picked == 0 || picked == 1);
            if (createNewHub)
            {
                resource = await TryCreateAiHubResourceInteractive(values, subscription);
            }

            return (FinishPickOrCreateAiHubResource(values, resource), createNewHub);
        }

        private static async Task<JsonElement?> TryCreateAiHubResourceInteractive(ICommandValues values, string subscription)
        {
            var locationName = values.GetOrEmpty("service.resource.region.name");
            var groupName = ResourceGroupNameToken.Data().GetOrDefault(values);
            var displayName = ResourceDisplayNameToken.Data().GetOrDefault(values);
            var description = ResourceDescriptionToken.Data().GetOrDefault(values);

            var openAiResourceId = values.GetOrEmpty("service.openai.resource.id");
            var openAiResourceKind = values.GetOrEmpty("service.openai.resource.kind");

            var smartName = ResourceNameToken.Data().GetOrDefault(values);
            var smartNameKind = smartName != null && smartName.Contains("openai") ? "openai" : "oai";

            return await TryCreateAiHubResourceInteractive(values, subscription, locationName, groupName, displayName, description, openAiResourceId, openAiResourceKind, smartName, smartNameKind);
        }

        private static AiHubResourceInfo FinishPickOrCreateAiHubResource(ICommandValues values, JsonElement? resource)
        {
            var aiHubResource = new AiHubResourceInfo
            {
                Id = resource?.GetPropertyStringOrNull("id"),
                Group = resource?.GetPropertyStringOrNull("resource_group"),
                Name = resource?.GetPropertyStringOrNull("name"),
                RegionLocation = resource?.GetPropertyStringOrNull("location"),
            };

            ResourceIdToken.Data().Set(values, aiHubResource.Id);
            ResourceNameToken.Data().Set(values, aiHubResource.Name);
            ResourceGroupNameToken.Data().Set(values, aiHubResource.Group);
            RegionLocationToken.Data().Set(values, aiHubResource.RegionLocation);

            return aiHubResource;
        }

        private static async Task<JsonElement?> TryCreateAiHubResourceInteractive(ICommandValues values, string subscription, string locationName, string groupName, string displayName, string description, string openAiResourceId, string openAiResourceKind, string smartName = null, string smartNameKind = null)
        {
            var sectionHeader = $"\n`CREATE AZURE AI RESOURCE`";
            ConsoleHelpers.WriteLineWithHighlight(sectionHeader);

            var groupOk = !string.IsNullOrEmpty(groupName);
            if (!groupOk)
            {
                var location =  await AzCliConsoleGui.PickRegionLocationAsync(true, locationName, false);
                locationName = location.Name;
            }

            var (group, createdNew) = await AzCliConsoleGui.PickOrCreateResourceGroup(true, subscription, groupOk ? null : locationName, groupName);
            groupName = group.Name;

            if (string.IsNullOrEmpty(smartName))
            {
                smartName = group.Name;
                smartNameKind = "rg";
            }

            if (createdNew)
            {
                ConsoleHelpers.WriteLineWithHighlight(sectionHeader);
            }

            var name = NamePickerHelper.DemandPickOrEnterName("Name: ", "ai", smartName, smartNameKind, AzCliConsoleGui.GetSubscriptionUserName(subscription));
            displayName ??= name;
            description ??= name;

            Console.Write("*** CREATING ***");
            var json = PythonSDKWrapper.CreateResource(values, subscription, groupName, name, locationName, displayName, description, openAiResourceId, openAiResourceKind);

            Console.WriteLine("\r*** CREATED ***  ");

            var parsed = !string.IsNullOrEmpty(json) ? JsonDocument.Parse(json) : default;
            return parsed?.GetPropertyElementOrNull("resource");
        }
    }
}

#endif