//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
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
    }

    public class AiSdkConsoleGui
    {
        public static async Task<AiHubResourceInfo> PickOrCreateAiHubResource(ICommandValues values, string subscription)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`AZURE AI RESOURCE`");
            Console.Write("\rName: *** Loading choices ***");

            var json = PythonSDKWrapper.ListResources(values, subscription);
            if (Program.Debug) Console.WriteLine(json);

            var parsed = !string.IsNullOrEmpty(json) ? JToken.Parse(json) : null;
            var items = parsed?.Type == JTokenType.Object ? parsed["resources"] : new JArray();

            var choices = new List<string>();
            foreach (var item in items)
            {
                var name = item["name"].Value<string>();
                var location = item["location"].Value<string>();
                var displayName = item["display_name"].Value<string>();

                choices.Add(string.IsNullOrEmpty(displayName)
                    ? $"{name} ({location})"
                    : $"{displayName} ({location})");
            }

            choices.Insert(0, "(Create new)");

            Console.Write("\rName: ");
            var picked = ListBoxPicker.PickIndexOf(choices.ToArray());
            if (picked < 0)
            {
                throw new ApplicationException($"CANCELED: No resource selected");
            }

            Console.WriteLine($"\rName: {choices[picked]}");
            var resource = picked > 0 ? items.ToArray()[picked - 1] : null;
            if (picked == 0)
            {
                var locationName = values.GetOrDefault("service.resource.region.name", "");
                var groupName = values.GetOrDefault("service.resource.group.name", "");
                var displayName = values.Get("service.resource.display.name", true);
                var description = values.Get("service.resource.description", true);

                resource = await TryCreateAiHubResourceInteractive(values, subscription, locationName, groupName, displayName, description);
            }

            var aiHubResource = new AiHubResourceInfo
            {
                Id = resource["id"].Value<string>(),
                Group = resource["resource_group"].Value<string>(),
                Name = resource["name"].Value<string>(),
                RegionLocation = resource["location"].Value<string>(),
            };

            values.Reset("service.resource.name", aiHubResource.Name);
            values.Reset("service.resource.id", aiHubResource.Id);
            values.Reset("service.resource.group.name", aiHubResource.Group);
            values.Reset("service.region.location", aiHubResource.RegionLocation);

            return aiHubResource;
        }

        private static async Task<JToken> TryCreateAiHubResourceInteractive(ICommandValues values, string subscription, string locationName, string groupName, string displayName, string description)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CREATE AZURE AI RESOURCE`");

            var groupOk = !string.IsNullOrEmpty(groupName);
            if (!groupOk)
            {
                var location =  await AzCliConsoleGui.PickRegionLocationAsync(true, locationName, false);
                locationName = location.Name;
            }

            var group = await AzCliConsoleGui.PickOrCreateResourceGroup(true, subscription, groupOk ? null : locationName, groupName);
            groupName = group.Name;

            var name = DemandAskPrompt("Name: ");
            displayName ??= name;
            description ??= name;

            Console.Write("*** CREATING ***");
            var json = PythonSDKWrapper.CreateResource(values, subscription, groupName, name, locationName, displayName, description);

            Console.WriteLine("\r*** CREATED ***  ");

            var parsed = !string.IsNullOrEmpty(json) ? JToken.Parse(json) : null;
            return parsed["hub"];
        }

        public static AiHubProjectInfo InitAndConfigAiHubProject(ICommandValues values, string subscription, string resourceId, string groupName, string openAiEndpoint, string openAiKey, string searchEndpoint, string searchKey)
        {
            var project = AiSdkConsoleGui.PickOrCreateAiHubProject(values, subscription, resourceId, out var createdProject);

            AiSdkConsoleGui.GetOrCreateAiHubProjectConnections(values, createdProject, subscription, groupName, project.Name, openAiEndpoint, openAiKey, searchEndpoint, searchKey);
            AiSdkConsoleGui.CreatAiHubProjectConfigJsonFile(subscription, groupName, project.Name);

            return project;
        }

        public static AiHubProjectInfo PickOrCreateAiHubProject(ICommandValues values, string subscription, string resourceId, out bool createNew)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`AZURE AI PROJECT`");
            Console.Write("\rName: *** Loading choices ***");

            var json = PythonSDKWrapper.ListProjects(values, subscription);
            if (Program.Debug) Console.WriteLine(json);

            var parsed = !string.IsNullOrEmpty(json) ? JToken.Parse(json) : null;
            var items = parsed?.Type == JTokenType.Object ? parsed["projects"] : new JArray();

            var choices = new List<string>();
            var itemJTokens = new List<JToken>();
            foreach (var item in items)
            {
                var hub = item["workspace_hub"]?.Value<string>();

                var hubOk = !string.IsNullOrEmpty(hub) && hub == resourceId;
                if (!hubOk) continue;

                itemJTokens.Add(item);

                var name = item["name"].Value<string>();
                var location = item["location"].Value<string>();
                var displayName = item["display_name"].Value<string>();

                choices.Add(string.IsNullOrEmpty(displayName)
                    ? $"{name} ({location})"
                    : $"{displayName} ({location})");
            }

            choices.Insert(0, "(Create new)");

            Console.Write("\rName: ");
            var picked = ListBoxPicker.PickIndexOf(choices.ToArray());
            if (picked < 0)
            {
                throw new ApplicationException($"CANCELED: No project selected");
            }

            Console.WriteLine($"\rName: {choices[picked]}");
            var project = picked > 0 ? itemJTokens[picked - 1] : null;
            createNew = picked == 0;
            if (createNew)
            {
                var group = values.GetOrDefault("service.resource.group.name", "");
                var location = values.GetOrDefault("service.region.location", "");
                var displayName = values.Get("service.project.display.name", true);
                var description = values.Get("service.project.description", true);

                var openAiResourceId = values.GetOrDefault("service.openai.resource.id", "");

                project = TryCreateAiHubProjectInteractive(values, subscription, resourceId, group, location, ref displayName, ref description, openAiResourceId);
            }

            var aiHubProject = new AiHubProjectInfo
            {
                Id = project["id"].Value<string>(),
                Group = project["resource_group"].Value<string>(),
                Name = project["name"].Value<string>(),
                DisplayName = project["display_name"].Value<string>(),
                RegionLocation = project["location"].Value<string>(),
            };

            return aiHubProject;
        }

        private static JToken TryCreateAiHubProjectInteractive(ICommandValues values, string subscription, string resourceId, string group, string location, ref string displayName, ref string description, string openAiResourceId)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CREATE AZURE AI PROJECT`");

            var name = DemandAskPrompt("Name: ");
            displayName ??= name;
            description ??= name;

            Console.Write("*** CREATING ***");
            var json = PythonSDKWrapper.CreateProject(values, subscription, group, resourceId, name, location, displayName, description, openAiResourceId);

            Console.WriteLine("\r*** CREATED ***  ");

            var parsed = !string.IsNullOrEmpty(json) ? JToken.Parse(json) : null;
            return parsed["project"];
        }

        public static void GetOrCreateAiHubProjectConnections(ICommandValues values, bool create, string subscription, string groupName, string projectName, string openAiEndpoint, string openAiKey, string searchEndpoint, string searchKey)
        {
            var checkForExistingOpenAiConnection = !create;
            var createOpenAiConnection = !string.IsNullOrEmpty(openAiEndpoint) && !string.IsNullOrEmpty(openAiKey) && !checkForExistingOpenAiConnection;

            var checkForExistingSearchConnection = !create;
            var createSearchConnection = !string.IsNullOrEmpty(searchEndpoint) && !string.IsNullOrEmpty(searchKey) && !checkForExistingSearchConnection;

            var connectionsOk = createOpenAiConnection || createSearchConnection || checkForExistingOpenAiConnection || checkForExistingSearchConnection;
            if (connectionsOk) ConsoleHelpers.WriteLineWithHighlight($"\n`AZURE AI PROJECT CONNECTIONS`\n");

            var connectionCount = 0;

            if (createOpenAiConnection || checkForExistingOpenAiConnection)
            {
                if (connectionCount > 0) Console.WriteLine();

                var connectionName = "Default_AzureOpenAI";
                Console.WriteLine($"Connection: {connectionName}");

                Console.Write(createOpenAiConnection ? "*** CREATING ***" : "*** CHECKING ***");

                var connectionType = "azure_open_ai";
                var connectionJson = createOpenAiConnection
                    ? PythonSDKWrapper.CreateConnection(values, subscription, groupName, projectName, connectionName, connectionType, openAiEndpoint, openAiKey)
                    : PythonSDKWrapper.GetConnection(values, subscription, groupName, projectName, connectionName);

                Console.WriteLine(createOpenAiConnection
                    ? "\r*** CREATED ***  "
                    : "\r*** CHECKED ***  ");
                connectionCount++;
            }

            if (createSearchConnection || checkForExistingSearchConnection)
            {
                if (connectionCount > 0) Console.WriteLine();

                var connectionName = "Default_CognitiveSearch";
                Console.WriteLine($"Connection: {connectionName}");

                Console.Write(createSearchConnection ? "*** CREATING ***" : "*** CHECKING ***");

                var connectionType = "cognitive_search";
                var connectionJson = createSearchConnection
                    ? PythonSDKWrapper.CreateConnection(values, subscription, groupName, projectName, connectionName, connectionType, searchEndpoint, searchKey)
                    : PythonSDKWrapper.GetConnection(values, subscription, groupName, projectName, connectionName);

                Console.WriteLine(createSearchConnection
                    ? "\r*** CREATED ***  "
                    : "\r*** CHECKED ***  ");
                connectionCount++;
            }
        }

        public static void CreatAiHubProjectConfigJsonFile(string subscription, string groupName, string projectName)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`AZURE AI PROJECT CONFIG`\n");

            dynamic configJsonData = new
            {
                subscription_id = subscription,
                resource_group = groupName,
                project_name = projectName,
            };

            var configJson = JsonSerializer.Serialize(configJsonData, new JsonSerializerOptions { WriteIndented = true });
            var configJsonFile = new FileInfo("config.json");
            File.WriteAllText(configJsonFile.FullName, configJson + "\n");

            Console.WriteLine($"{configJsonFile.Name} (saved at {configJsonFile.Directory})\n");
            Console.WriteLine("  " + configJson.Replace("\n", "\n  "));
        }

        private static string AskPrompt(string prompt, string value = null, bool useEditBox = false)
        {
            Console.Write(prompt);

            if (useEditBox)
            {
                var normal = new Colors(ConsoleColor.White, ConsoleColor.Blue);
                var text = EditBoxQuickEdit.Edit(40, 1, normal, value, 128);
                ColorHelpers.ResetColor();
                Console.WriteLine(text);
                return text;
            }

            if (!string.IsNullOrEmpty(value))
            {
                Console.WriteLine(value);
                return value;
            }

            return Console.ReadLine();
        }

        private static string DemandAskPrompt(string prompt, string value = null, bool useEditBox = false)
        {
            var answer = AskPrompt(prompt, value, useEditBox);
            if (string.IsNullOrEmpty(answer))
            {
                ThrowPromptNotAnsweredApplicationException();
            }
            return answer;
        }

        private static void ThrowPromptNotAnsweredApplicationException()
        {
            throw new ApplicationException($"CANCELED: No input provided.");
        }
    }
}
