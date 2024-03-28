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
    public struct AiHubResourceInfo
    {
        public string Id;
        public string Group;
        public string Name;
        public string RegionLocation;
    }

    public struct AiHubProjectInfo
    {
        public string Id;
        public string Group;
        public string Name;
        public string DisplayName;
        public string RegionLocation;
        public string HubId;
    }

    public partial class AiSdkConsoleGui
    {
        public static async Task<(string, AzCli.CognitiveServicesResourceInfo?, AzCli.CognitiveSearchResourceInfo?)> VerifyResourceConnections(ICommandValues values, string subscription, string groupName, string projectName)
        {
            try
            {
                var projectJson = PythonSDKWrapper.ListProjects(values, subscription);
                var projects = JsonDocument.Parse(projectJson).GetPropertyArrayOrNull("projects");
                var project = projects?.FirstOrDefault(x => x.GetProperty("name").ToString() == projectName);
                if (project == null) return (null, null, null);

                var hub = project?.GetPropertyStringOrNull("workspace_hub");
                var hubName = hub.Split('/').Last();

                var json = PythonSDKWrapper.ListConnections(values, subscription, groupName, projectName);
                if (string.IsNullOrEmpty(json)) return (null, null, null);

                var connections = JsonDocument.Parse(json).GetPropertyArrayOrEmpty("connections");
                if (connections.Count() == 0) return (null, null, null);

                var foundOpenAiResource = await FindAndVerifyOpenAiResourceConnection(subscription, connections);
                var foundSearchResource = await FindAndVerifySearchResourceConnection(subscription, connections);

                return (hubName, foundOpenAiResource, foundSearchResource);
            }
            catch (Exception ex)
            {
                FileHelpers.LogException(values, ex);
                return (null, null, null);
            }
        }

        private static async Task<AzCli.CognitiveServicesResourceInfo?> FindAndVerifyOpenAiResourceConnection(string subscription, JsonElement[] connections)
        {
            var openaiConnection = connections.FirstOrDefault(x => x.GetPropertyStringOrNull("name").Contains("Default_AzureOpenAI") && x.GetPropertyStringOrNull("type") == "azure_open_ai");
            if (string.IsNullOrEmpty(openaiConnection.GetPropertyStringOrNull("name"))) return null;

            var openaiEndpoint = openaiConnection.GetPropertyStringOrNull("target");
            if (string.IsNullOrEmpty(openaiEndpoint)) return null;

            var responseOpenAi = await AzCli.ListCognitiveServicesResources(subscription, "OpenAI");
            var responseOpenAiOk = !string.IsNullOrEmpty(responseOpenAi.Output.StdOutput) && string.IsNullOrEmpty(responseOpenAi.Output.StdError);
            if (!responseOpenAiOk) return null;

            Func<string, string, bool> match = (a, b) => {
                return a == b ||
                    a.Replace(".openai.azure.com/", ".cognitiveservices.azure.com/") == b ||
                    b.Replace(".openai.azure.com/", ".cognitiveservices.azure.com/") == a;
            };

            var matchOpenAiEndpoint = responseOpenAi.Payload.Where(x => match(x.Endpoint, openaiEndpoint)).ToList();
            if (matchOpenAiEndpoint.Count() != 1) return null;

            return matchOpenAiEndpoint.First();
        }

        private static async Task<AzCli.CognitiveSearchResourceInfo?> FindAndVerifySearchResourceConnection(string subscription, JsonElement[] connections)
        {
            var searchConnection = connections.FirstOrDefault(x => x.GetPropertyStringOrNull("name").Contains("AzureAISearch") && x.GetPropertyStringOrNull("type") == "cognitive_search");
            if (string.IsNullOrEmpty(searchConnection.GetPropertyStringOrNull("name"))) return null;

            var searchEndpoint = searchConnection.GetPropertyStringOrNull("target");
            if (string.IsNullOrEmpty(searchEndpoint)) return null;

            var responseSearch = await AzCli.ListSearchResources(subscription, null);
            var responseSearchOk = !string.IsNullOrEmpty(responseSearch.Output.StdOutput) && string.IsNullOrEmpty(responseSearch.Output.StdError);
            if (!responseSearchOk) return null;

            var matchSearchEndpoint = responseSearch.Payload.Where(x => x.Endpoint == searchEndpoint).ToList();
            if (matchSearchEndpoint.Count() != 1) return null;

            return matchSearchEndpoint.First();
        }
    }
}
