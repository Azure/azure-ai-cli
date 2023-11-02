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
    public partial class AiSdkConsoleGui
    {
        public static AiHubProjectInfo PickOrCreateAndConfigAiHubProject(bool allowCreate, bool allowPick, ICommandValues values, string subscription, string resourceId, string groupName, string openAiEndpoint, string openAiKey, string searchEndpoint, string searchKey)
        {
            var createdProject = false;
            var project = allowCreate && allowPick
                ? PickOrCreateAiHubProject(values, subscription, resourceId, out createdProject)
                : allowCreate
                    ? CreateAiHubProject(values, subscription, resourceId)
                    : allowPick
                        ? PickAiHubProject(values, subscription, resourceId)
                        : throw new ApplicationException($"CANCELED: No project selected");

            GetOrCreateAiHubProjectConnections(values, createdProject, subscription, groupName, project.Name, openAiEndpoint, openAiKey, searchEndpoint, searchKey);
            CreateAiHubProjectConfigJsonFile(subscription, groupName, project.Name);

            return project;
        }

        public static AiHubProjectInfo PickAiHubProject(ICommandValues values, string subscription, string resourceId)
        {
            return PickOrCreateAiHubProject(false, values, subscription, resourceId, out var createNew);
        }

        public static AiHubProjectInfo PickOrCreateAiHubProject(ICommandValues values, string subscription, string resourceId, out bool createNew)
        {
            return PickOrCreateAiHubProject(true, values, subscription, resourceId, out createNew);
        }

        public static AiHubProjectInfo CreateAiHubProject(ICommandValues values, string subscription, string resourceId)
        {
            var project = TryCreateAiHubProjectInteractive(values, subscription, resourceId);
            return AiHubProjectInfoFromToken(values, project);
        }

        private static AiHubProjectInfo PickOrCreateAiHubProject(bool allowCreate, ICommandValues values, string subscription, string resourceId, out bool createNew)
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

            if (allowCreate)
            {
                choices.Insert(0, "(Create new)");
            }

            Console.Write("\rName: ");
            var picked = ListBoxPicker.PickIndexOf(choices.ToArray());
            if (picked < 0)
            {
                throw new ApplicationException($"CANCELED: No project selected");
            }

            Console.WriteLine($"\rName: {choices[picked]}");
            var project = allowCreate
                ? (picked > 0 ? itemJTokens[picked - 1] : null)
                : itemJTokens[picked];

            createNew = allowCreate && picked == 0;
            if (createNew)
            {
                project = TryCreateAiHubProjectInteractive(values, subscription, resourceId);
            }

            return AiHubProjectInfoFromToken(values, project);
        }

        private static JToken TryCreateAiHubProjectInteractive(ICommandValues values, string subscription, string resourceId)
        {
            var group = ResourceGroupNameToken.Data().GetOrDefault(values);
            var location = RegionLocationToken.Data().GetOrDefault(values, "");
            var displayName = ProjectDisplayNameToken.Data().GetOrDefault(values);
            var description = ProjectDescriptionToken.Data().GetOrDefault(values);

            var openAiResourceId = values.GetOrDefault("service.openai.resource.id", "");

            var smartName = ResourceNameToken.Data().GetOrDefault(values);
            var smartNameKind = smartName != null && smartName.Contains("openai") ? "openai" : "oai";

            return TryCreateAiHubProjectInteractive(values, subscription, resourceId, group, location, ref displayName, ref description, openAiResourceId, smartName, smartNameKind);
        }

        private static AiHubProjectInfo AiHubProjectInfoFromToken(ICommandValues values, JToken project)
        {
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

        private static JToken TryCreateAiHubProjectInteractive(ICommandValues values, string subscription, string resourceId, string group, string location, ref string displayName, ref string description, string openAiResourceId, string smartName = null, string smartNameKind = null)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CREATE AZURE AI PROJECT`");

            if (string.IsNullOrEmpty(smartName))
            {
                smartName = group;
                smartNameKind = "rg";
            }

            var name = NamePickerHelper.DemandPickOrEnterName("Name: ", "aiproj", smartName, smartNameKind); // TODO: What will this really be called?
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
            var checkForExistingOpenAiConnection = true;
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

                var message = createSearchConnection ? "\r*** CREATED ***  " : null;
                if (checkForExistingOpenAiConnection)
                {
                    var parsed = !string.IsNullOrEmpty(connectionJson) ? JToken.Parse(connectionJson) : null;
                    var connection = parsed?.Type == JTokenType.Object ? parsed?["connection"] : null;
                    var target = connection?.Type == JTokenType.Object ? connection?["target"]?.ToString() : null;
                    message = target == null
                        ? $"\r*** WARNING: {connectionName} not connection found ***  "
                        : target != openAiEndpoint
                            ? $"\r*** WARNING: {connectionName} found but target is {target} ***  "
                            : $"\r*** MATCHED: {connectionName} ***  ";
                }

                Console.WriteLine(message);
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

                var message = createSearchConnection ? "\r*** CREATED ***  " : null;
                if (checkForExistingSearchConnection)
                {
                    var parsed = !string.IsNullOrEmpty(connectionJson) ? JToken.Parse(connectionJson) : null;
                    var connection = parsed?.Type == JTokenType.Object ? parsed?["connection"] : null;
                    var target = connection?.Type == JTokenType.Object ? connection?["target"]?.ToString() : null;
                    message = target == null
                        ? $"\r*** WARNING: {connectionName} not connection found ***  "
                        : target != searchEndpoint
                            ? $"\r*** WARNING: {connectionName} found but target is {target} ***  "
                            : $"\r*** MATCHED: {connectionName} ***  ";
                }

                Console.WriteLine(message);
                connectionCount++;
            }
        }

        public static void CreateAiHubProjectConfigJsonFile(string subscription, string groupName, string projectName)
        {
            ConfigSetHelpers.ConfigureProject(subscription, groupName, projectName);
            Console.WriteLine();

            dynamic configJsonData = new
            {
                subscription_id = subscription,
                resource_group = groupName,
                // project_name = projectName,
                workspace_name = projectName,
            };

            var configJson = JsonSerializer.Serialize(configJsonData, new JsonSerializerOptions { WriteIndented = true });
            var configJsonFile = new FileInfo("config.json");
            File.WriteAllText(configJsonFile.FullName, configJson + "\n");

            Console.WriteLine($"{configJsonFile.Name} (saved at {configJsonFile.Directory})\n");
            Console.WriteLine("  " + configJson.Replace("\n", "\n  "));
        }
    }
}
