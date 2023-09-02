//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;
using Azure.AI.Details.Common.CLI.ConsoleGui;

namespace Azure.AI.Details.Common.CLI
{
    public struct AiHubResourceInfo
    {
        public string Id;
        public string Group;
        public string Name;
        public string RegionLocation;
    }

    public class InitCommand : Command
    {
        internal InitCommand(ICommandValues values)
        {
            _values = values.ReplaceValues();
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", true);
        }

        internal bool RunCommand()
        {
            RunInitCommand().Wait();
            return _values.GetOrDefault("passed", true);
        }

        private async Task<bool> RunInitCommand()
        {
            try
            {
                await DoCommand(_values.GetCommand());
                return _values.GetOrDefault("passed", true);
            }
            catch (ApplicationException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                _values.Reset("passed", "false");
                return false;
            }
        }

        private async Task DoCommand(string command)
        {
            DisplayInitServiceBanner();

            CheckPath();

            var interactive = _values.GetOrDefault("init.service.interactive", true);
            switch (command)
            {
                case "init": await DoInitServiceCommand(); break;
                case "init.openai": await DoInitOpenAiCommand(); break;
                case "init.search": await DoInitSearchCommand(); break;
                case "init.resource": await DoInitResourceCommand(); break;
                case "init.project": await DoInitProjectCommand(); break;
            }
        }

        private async Task DoInitServiceCommand()
        {
            StartCommand();

            var interactive = _values.GetOrDefault("init.service.interactive", true);
            await DoInitService(interactive);

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private async Task DoInitOpenAiCommand()
        {
            StartCommand();

            var interactive = _values.GetOrDefault("init.service.interactive", true);
            await DoInitServiceParts(interactive, "openai");

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private async Task DoInitSearchCommand()
        {
            StartCommand();

            var interactive = _values.GetOrDefault("init.service.interactive", true);
            await DoInitServiceParts(interactive, "openai", "search");

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private async Task DoInitResourceCommand()
        {
            StartCommand();

            var interactive = _values.GetOrDefault("init.service.interactive", true);
            await DoInitServiceParts(interactive, "resource");

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private async Task DoInitProjectCommand()
        {
            StartCommand();

            var interactive = _values.GetOrDefault("init.service.interactive", true);
            await DoInitServiceParts(interactive, "openai", "search", "resource", "project");

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private async Task DoInitService(bool interactive)
        {
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // TODO: Add back non-interactive mode support
            await DoInitServiceInteractively();
        }

        private async Task DoInitServiceInteractively()
        {
            Console.Write("Initialize: ");

            var choiceLookup = new Dictionary<string, string>
            {
                ["Azure OpenAI"] = "openai",
                ["Azure OpenAI + Cognitive Search"] = "openai;search",
                ["Azure AI Resource + Project"] = "resource;project",
                ["Azure AI Resource + Project + OpenAI"] = "openai;resource;project",
                ["Azure AI Resource + Project + OpenAI + Cognitive Search"] = "openai;search;resource;project",
            };

            var choices = choiceLookup.Keys.ToArray();

            var picked = ListBoxPicker.PickIndexOf(choices.ToArray(), choices.Count() - 1);
            if (picked < 0)
            {
                Console.WriteLine("\rInitialize: (canceled)");
                return;
            }
    
            var choice = choices[picked];
            Console.WriteLine($"\rInitialize: {choice}");

            await DoInitServiceParts(true, choiceLookup[choice].Split(';', StringSplitOptions.RemoveEmptyEntries));
        }

        private async Task DoInitServiceParts(bool interactive, params string[] operations)
        {
            foreach (var operation in operations)
            {
                var task = operation switch
                {
                    "openai" => DoInitOpenAi(interactive),
                    "search" => DoInitSearch(interactive),
                    "resource" => DoInitHub(interactive),
                    "project" => DoInitProject(interactive),
                    _ => throw new ApplicationException($"WARNING: NOT YET IMPLEMENTED")
                };
                await task;
            }
        }

        private async Task DoInitOpenAi(bool interactive)
        {
            var subscriptionFilter = _values.GetOrDefault("init.service.subscription", "");
            var regionFilter = _values.GetOrDefault("init.service.resource.region.name", "");
            var groupFilter = _values.GetOrDefault("init.service.resource.group.name", "");
            var resourceFilter = _values.GetOrDefault("init.service.cognitiveservices.resource.name", "");
            var kind = _values.GetOrDefault("init.service.cognitiveservices.resource.kind", Program.CognitiveServiceResourceKind);
            var sku = _values.GetOrDefault("init.service.cognitiveservices.resource.sku", Program.CognitiveServiceResourceSku);
            var yes = _values.GetOrDefault("init.service.cognitiveservices.terms.agree", false);

            var subscriptionId = await AzCliConsoleGui.PickSubscriptionIdAsync(interactive, subscriptionFilter);

            ConsoleHelpers.WriteLineWithHighlight($"\n`{Program.SERVICE_RESOURCE_DISPLAY_NAME_ALL_CAPS}`");
            var regionLocation = new AzCli.AccountRegionLocationInfo(); // await AzCliConsoleGui.PickRegionLocationAsync(interactive, regionFilter);
            var resource = await AzCliConsoleGui.PickOrCreateCognitiveResource(interactive, subscriptionId, regionLocation.Name, groupFilter, resourceFilter, kind, sku, yes);
            var region = resource.RegionLocation;
            var endpoint = resource.Endpoint;
            var id = resource.Id;

            ConsoleHelpers.WriteLineWithHighlight($"\n`OPEN AI DEPLOYMENT (CHAT)`");
            var deployment = await AzCliConsoleGui.AiResourceDeploymentPicker.PickOrCreateDeployment(interactive, "Chat", subscriptionId, resource.Group, resource.RegionLocation, resource.Name, null);
            var chatDeploymentName = deployment.Name;

            ConsoleHelpers.WriteLineWithHighlight($"\n`OPEN AI DEPLOYMENT (EMBEDDINGS)`");

            var embeddingsDeployment = await AzCliConsoleGui.AiResourceDeploymentPicker.PickOrCreateDeployment(interactive, "Embeddings", subscriptionId, resource.Group, resource.RegionLocation, resource.Name, null);
            var embeddingsDeploymentName = embeddingsDeployment.Name;

            var keys = await AzCliConsoleGui.LoadCognitiveServicesResourceKeys(subscriptionId, resource);
            var key = keys.Key1;

            ConfigServiceResource(subscriptionId, region, endpoint, chatDeploymentName, embeddingsDeploymentName, key);

            _values.Reset("init.service.subscription", subscriptionId);
            _values.Reset("service.resource.group.name", resource.Group);
            _values.Reset("service.resource.region.name", resource.RegionLocation);
            _values.Reset("service.openai.endpoint", endpoint);
            _values.Reset("service.openai.key", key);
            _values.Reset("service.openai.resource.id", id);
        }

        private async Task DoInitSearch(bool interactive)
        {
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // TODO: Add back non-interactive mode support

            var subscription = _values.GetOrDefault("init.service.subscription", "");
            var location = _values.GetOrDefault("service.resource.region.name", "");
            var groupName = _values.GetOrDefault("service.resource.group.name", "");

            var resource = await PickOrCreateCognitiveSearchResource(subscription, location, groupName);
            var keys = await AzCliConsoleGui.LoadSearchResourceKeys(subscription, resource);
            ConfigSearchResource(resource.Endpoint, keys.Key1);

            _values.Reset("service.search.endpoint", resource.Endpoint);
            _values.Reset("service.search.key", keys.Key1);
        }

        private async Task DoInitHub(bool interactive)
        {
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // TODO: Add back non-interactive mode support

            var subscription = _values.GetOrDefault("init.service.subscription", "");
            if (string.IsNullOrEmpty(subscription))
            {
                subscription = await AzCliConsoleGui.PickSubscriptionIdAsync(interactive);
                _values.Reset("init.service.subscription", subscription);
            }

            var aiHubResource = await PickOrCreateAiHubResource(_values, subscription);
        }

        private async Task DoInitProject(bool interactive)
        {
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // TODO: Add back non-interactive mode support

            var subscription = _values.GetOrDefault("init.service.subscription", "");
            var resourceId = _values.GetOrDefault("service.resource.id", null);
            var groupName = _values.GetOrDefault("service.resource.group.name", null);

            var project = PickOrCreateAiHubProject(_values, subscription, resourceId, out var createdProject);

            var projectName = project["name"].Value<string>();
            var projectId = project["id"].Value<string>();

            _values.Reset("service.project.name", projectName);
            _values.Reset("service.project.id", projectId);

            GetOrCreateAiHubProjectConnections(_values, createdProject, subscription, groupName, projectName,
                _values.GetOrDefault("service.openai.endpoint", null),
                _values.GetOrDefault("service.openai.key", null),
                _values.GetOrDefault("service.search.endpoint", null),
                _values.GetOrDefault("service.search.key", null));

            CreatAiHubProjectConfigJsonFile(subscription, groupName, projectName);
        }

        private void DisplayInitServiceBanner()
        {
            if (_quiet) return;

            var logo = FileHelpers.FindFileInHelpPath($"help/include.{Program.Name}.init.ascii.logo");
            if (!string.IsNullOrEmpty(logo))
            {
                var text = FileHelpers.ReadAllHelpText(logo, Encoding.UTF8);
                ConsoleHelpers.WriteLineWithHighlight(text + "\n");
            }
            else
            {
                ConsoleHelpers.WriteLineWithHighlight($"`{Program.Name.ToUpper()} INIT`");
            }
        }

        private static void ConfigServiceResource(string subscriptionId, string region, string endpoint, string chatDeployment, string embeddingsDeployment, string key)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CONFIG {Program.SERVICE_RESOURCE_DISPLAY_NAME_ALL_CAPS}`");
            Console.WriteLine();

            int maxLabelWidth = 0;
            var actions = new List<Action<int>>(new Action<int>[] {
                Program.InitConfigsSubscription ?
                ConfigSetLambda("@subscription", subscriptionId, "Subscription", subscriptionId, ref maxLabelWidth) : null,
                Program.InitConfigsEndpoint ?
                ConfigSetLambda("@chat.endpoint", endpoint, "Endpoint (chat)", endpoint, ref maxLabelWidth) : null,
                ConfigSetLambda("@chat.deployment", chatDeployment, "Deployment (chat)", chatDeployment, ref maxLabelWidth),
                ConfigSetLambda("@chat.key", key, "Key (chat)", key.Substring(0, 4) + "****************************", ref maxLabelWidth),
                Program.InitConfigsEndpoint ?
                ConfigSetLambda("@search.embeddings.endpoint", endpoint, "Endpoint (embeddings)", endpoint, ref maxLabelWidth) : null,
                ConfigSetLambda("@search.embeddings.deployment", embeddingsDeployment, "Deployment (embeddings)", embeddingsDeployment, ref maxLabelWidth),
                ConfigSetLambda("@search.embeddings.key", key, "Key (embeddings)", key.Substring(0, 4) + "****************************", ref maxLabelWidth),
                ConfigSetLambda("@chat.region", region, "Region", region, ref maxLabelWidth),
            });
            actions.ForEach(x => x?.Invoke(maxLabelWidth));
        }

        private static void ConfigSearchResource(string endpoint, string key)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CONFIG COGNITIVE SEARCH RESOURCE`");
            Console.WriteLine();

            int maxLabelWidth = 0;
            var actions = new List<Action<int>>(new Action<int>[]
            {
                ConfigSetLambda("@search.endpoint", endpoint, "Endpoint (search)", endpoint, ref maxLabelWidth),
                ConfigSetLambda("@search.key", key, "Key (search)", key.Substring(0, 4) + "****************************", ref maxLabelWidth),
            });
            actions.ForEach(x => x(maxLabelWidth));
        }

        private static void ConfigSet(string atFile, string setValue, string message)
        {
            Console.Write($"*** SETTING *** {message}");
            ConfigSet(atFile, setValue);
            Console.WriteLine($"\r  *** SET ***   {message}");
        }

        private static Action<int> ConfigSetLambda(string atFile, string setValue, string displayLabel, string displayValue, ref int maxWidth)
        {
            maxWidth = Math.Max(maxWidth, displayLabel.Length);
            return (int labelWidth) =>
            {
                ConfigSet(atFile, setValue, labelWidth, displayLabel, displayValue);
            };
        }

        private static void ConfigSet(string atFile, string setValue, int labelWidth, string displayLabel, string displayValue)
        {
            displayLabel = displayLabel.PadLeft(labelWidth);
            Console.Write($"*** SETTING *** {displayLabel}");
            ConfigSet(atFile, setValue);
            // Thread.Sleep(50);
            Console.WriteLine($"\r  *** SET ***   {displayLabel}: {displayValue}");
        }

        private static void ConfigSet(string atFile, string setValue)
        {
            var setCommandValues = new CommandValues();
            setCommandValues.Add("x.command", "config");
            setCommandValues.Add("x.config.scope.hive", "local");
            setCommandValues.Add("x.config.command.at.file", atFile);
            setCommandValues.Add("x.config.command.set", setValue);
            var fileName = FileHelpers.GetOutputConfigFileName(atFile, setCommandValues);
            FileHelpers.WriteAllText(fileName, setValue, Encoding.UTF8);
        }

        private static void ThrowInteractiveNotSupportedApplicationException()
        {
            throw new ApplicationException("WARNING: Non-interactive mode not supported");
        }

        private static void ThrowNotImplementedApplicationException(string what)
        {
            throw new ApplicationException($"WARNING: {what} NOT YET IMPLEMENTED");
        }

        private static void ThrowNotImplementedButStartedApplicationException()
        {
            throw new ApplicationException($"WARNING: NOT YET IMPLEMENTED ... started though! 😁");
        }

        private void StartCommand()
        {
            CheckPath();
            LogHelpers.EnsureStartLogFile(_values);

            // _display = new DisplayHelper(_values);

            // _output = new OutputHelper(_values);
            // _output.StartOutput();

            _lock = new SpinLock();
            _lock.StartLock();
        }

        private void StopCommand()
        {
            _lock.StopLock(5000);

            // LogHelpers.EnsureStopLogFile(_values);
            // _output.CheckOutput();
            // _output.StopOutput();

            _stopEvent.Set();
        }

        private static async Task<AzCli.CognitiveSearchResourceInfo> PickOrCreateCognitiveSearchResource(string subscription, string location, string groupName)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`COGNITIVE SEARCH RESOURCE`");
            Console.Write("\rName: *** Loading choices ***");

            var response = await AzCli.ListSearchResources(subscription, location);
            if (string.IsNullOrEmpty(response.StdOutput) && !string.IsNullOrEmpty(response.StdError))
            {
                var output = response.StdError.Replace("\n", "\n  ");
                throw new ApplicationException($"ERROR: Listing search resources\n  {output}");
            }

            var resources = response.Payload.OrderBy(x => x.Name).ToList();
            var choices = resources.Select(x => $"{x.Name} ({x.RegionLocation})").ToList();
            choices.Insert(0, "(Create new)");

            Console.Write("\rName: ");

            var picked = ListBoxPicker.PickIndexOf(choices.ToArray());
            if (picked < 0)
            {
                throw new ApplicationException($"CANCELED: No resource selected");
            }

            Console.WriteLine($"\rName: {choices[picked]}");
            var resource = picked > 0 ? resources[picked - 1] : new AzCli.CognitiveSearchResourceInfo();
            if (picked == 0)
            {
                resource = await TryCreateSearchInteractive(subscription, location, groupName);
            }

            return resource;
        }

        private static async Task<AzCli.CognitiveSearchResourceInfo> TryCreateSearchInteractive(string subscription, string locationName, string groupName)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CREATE COGNITIVE SEARCH RESOURCE`\n");

            var groupOk = !string.IsNullOrEmpty(groupName);
            if (!groupOk)
            {
                var location =  await AzCliConsoleGui.PickRegionLocationAsync(true, locationName, false);
                locationName = location.Name;
            }
            
            var group = await AzCliConsoleGui.PickOrCreateResourceGroup(true, subscription, groupOk ? null : locationName, groupName);
            groupName = group.Name;
            
            var name = DemandAskPrompt("Name: ");

            Console.Write("*** CREATING ***");
            var response = await AzCli.CreateSearchResource(subscription, groupName, locationName, name);

            Console.Write("\r");
            if (string.IsNullOrEmpty(response.StdOutput) && !string.IsNullOrEmpty(response.StdError))
            {
                var output = response.StdError.Replace("\n", "\n  ");
                throw new ApplicationException($"ERROR: Creating resource:\n\n  {output}");
            }

            Console.WriteLine("\r*** CREATED ***  ");
            return response.Payload;
        }

        private static async Task<AiHubResourceInfo> PickOrCreateAiHubResource(ICommandValues values, string subscription)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`AZURE AI RESOURCE`\n");
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
            ConsoleHelpers.WriteLineWithHighlight($"\n`CREATE AZURE AI RESOURCE`\n");

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

        private static JToken PickOrCreateAiHubProject(ICommandValues values, string subscription, string resourceId, out bool createNew)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`AZURE AI PROJECT`\n");
            Console.Write("\rProject: *** Loading choices ***");

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

            Console.Write("\rProject: ");
            var picked = ListBoxPicker.PickIndexOf(choices.ToArray());
            if (picked < 0)
            {
                throw new ApplicationException($"CANCELED: No project selected");
            }

            Console.WriteLine($"\rProject: {choices[picked]}");
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

            return project;
        }

        private static JToken TryCreateAiHubProjectInteractive(ICommandValues values, string subscription, string resourceId, string group, string location, ref string displayName, ref string description, string openAiResourceId)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CREATE AZURE AI PROJECT`\n");

            var name = DemandAskPrompt("Name: ");
            displayName ??= name;
            description ??= name;

            Console.Write("*** CREATING ***");
            var json = PythonSDKWrapper.CreateProject(values, subscription, group, resourceId, name, location, displayName, description, openAiResourceId);

            Console.WriteLine("\r*** CREATED ***  ");

            var parsed = !string.IsNullOrEmpty(json) ? JToken.Parse(json) : null;
            return parsed["project"];
        }

        private static void GetOrCreateAiHubProjectConnections(ICommandValues values, bool create, string subscription, string groupName, string projectName, string openAiEndpoint, string openAiKey, string searchEndpoint, string searchKey)
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

        private static void CreatAiHubProjectConfigJsonFile(string subscription, string groupName, string projectName)
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

        private SpinLock _lock = null;
        private readonly bool _quiet = false;
        private readonly bool _verbose = false;
    }
}
