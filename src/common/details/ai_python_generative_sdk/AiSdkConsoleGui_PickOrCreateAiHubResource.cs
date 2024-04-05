//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

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

            var items = JsonHelpers.DeserializePropertyValueOrDefault<IEnumerable<AiHubResourceInfo>>(json, "resources")
                ?.OrderBy(res => res.DisplayName + " " + res.Name)
                .ThenBy(res => res.RegionLocation)
                .ToArray()
                ?? Array.Empty<AiHubResourceInfo>();

            var choices = new List<string>();
            if (allowCreate)
            {
                choices.Add("(Create w/ integrated Open AI + AI Services)");
                choices.Add("(Create w/ standalone Open AI resource)");
            }

            choices.AddRange(items
                .Select(item =>
                    $"{(string.IsNullOrEmpty(item.DisplayName) ? item.Name : item.DisplayName)} ({item.RegionLocation})"));

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
            AiHubResourceInfo resource = allowCreate
                ? (picked >= 2 ? items[picked - 2] : default)
                : items[picked];

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

        private static async Task<AiHubResourceInfo> TryCreateAiHubResourceInteractive(ICommandValues values, string subscription)
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

        private static AiHubResourceInfo FinishPickOrCreateAiHubResource(ICommandValues values, AiHubResourceInfo resource)
        {
            ResourceIdToken.Data().Set(values, resource.Id);
            ResourceNameToken.Data().Set(values, resource.Name);
            ResourceGroupNameToken.Data().Set(values, resource.Group);
            RegionLocationToken.Data().Set(values, resource.RegionLocation);

            return resource;
        }

        private static async Task<AiHubResourceInfo> TryCreateAiHubResourceInteractive(ICommandValues values, string subscription, string locationName, string groupName, string displayName, string description, string openAiResourceId, string openAiResourceKind, string smartName = null, string smartNameKind = null)
        {
            var sectionHeader = $"\n`CREATE AZURE AI RESOURCE`";
            ConsoleHelpers.WriteLineWithHighlight(sectionHeader);

            var groupOk = !string.IsNullOrEmpty(groupName);
            if (!groupOk)
            {
                var location =  await AzCliConsoleGui.PickRegionLocationAsync(true, subscription, locationName, false);
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

            return JsonHelpers.DeserializePropertyValueOrDefault<AiHubResourceInfo>(json, "resource");
        }
    }
}
