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
                case "init": await DoInitService(interactive); break;
                case "init.openai": await DoInitServiceParts(interactive, 0, 0); break;
                case "init.search": await DoInitServiceParts(interactive, 0, 1); break;
                case "init.resource": await DoInitServiceParts(interactive, 0, 3); break;
                case "init.project": await DoInitServiceParts(interactive, 0, 2); break;
            }
        }

        private async Task DoInitService(bool interactive)
        {
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // TODO: Add back non-interactive mode support
            await DoInitServiceInteractively();
        }

        private async Task DoInitServiceInteractively()
        {
            Console.Write("Initialize: ");

            var choices = new string[] { "Azure OpenAI", "Azure AI Project", "Azure AI Resource", "Azure Cognitive Search" };
            var choices2 = new string[] { "openai", "search", "resource", "project" };

            var normal = new Colors(ConsoleColor.White, ConsoleColor.Blue);
            var selected = new Colors(ConsoleColor.White, ConsoleColor.Red);

            var width = Math.Max(choices.Max(x => x.Length) + 4, 29);
            var picked = ListBoxPicker.PickIndexOf(choices.ToArray(), width, 30, normal, selected);
            if (picked < 0)
            {
                Console.WriteLine("\rInitialize: (canceled)");
                return;
            }
    
            var choice = string.Join(" + ", choices.Take(picked + 1).Select(x => x.Trim()));
            Console.WriteLine($"\rInitialize: {choice}");

            picked = picked switch
            {
                0 => 0,
                1 => 4,
                2 => 3,
                3 => 2,
                _ => throw new NotImplementedException()
            };

            await DoInitServiceParts(true, 0, picked);
        }

        private async Task DoInitServiceParts(bool interactive, int startWith, int stopWith)
        {
            if (startWith <= 0 && stopWith >= 0) await DoInitOpenAi(interactive);
            if (startWith <= 1 && stopWith >= 1) await DoInitSearch(interactive);
            if (startWith <= 2 && stopWith >= 2) await DoInitHub(interactive);
            if (startWith <= 3 && stopWith >= 3) await DoInitProject(interactive);
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

            var (subscriptionId, region, endpoint, deployment, key) = await GetRegionAndKey(interactive, subscriptionFilter, regionFilter, groupFilter, resourceFilter, kind, sku, yes);
            ConfigServiceResource(subscriptionId, region, endpoint, deployment, key);

            _values.Reset("init.service.subscription", subscriptionId);
            _values.Reset("service.openai.endpoint", endpoint);
            _values.Reset("service.openai.key", key);
        }

        private async Task DoInitSearch(bool interactive)
        {
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // TODO: Add back non-interactive mode support

            ConsoleHelpers.WriteLineWithHighlight($"\n`COGNITIVE SEARCH RESOURCE`\n");
            Console.Write("\rName: *** Loading choices ***");

            var subscription = _values.GetOrDefault("init.service.subscription", "");
            var location = _values.GetOrDefault("init.service.resource.region.name", "");

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

            var normal = new Colors(ConsoleColor.White, ConsoleColor.Blue);
            var selected = new Colors(ConsoleColor.White, ConsoleColor.Red);

            var width = Math.Max(choices.Max(x => x.Length) + 4, 29);
            var picked = ListBoxPicker.PickIndexOf(choices.ToArray(), width, 30, normal, selected);
            if (picked < 0)
            {
                throw new ApplicationException($"CANCELED: No resource selected");
            }

            Console.WriteLine($"\rName: {choices[picked]}");
            var resource = picked > 0 ? resources[picked - 1] : new AzCli.CognitiveSearchResourceInfo();
            if (picked == 0)
            {
                resource = await TryCreateSearchInteractive();
            }

            var key = "????????????????????????????????"; // TODO: Get the real key
            ConfigSearchResource(resource.Endpoint, key);

            _values.Reset("service.search.endpoint", resource.Endpoint);
            _values.Reset("service.search.key", key);

        }

        private async Task<AzCli.CognitiveSearchResourceInfo> TryCreateSearchInteractive()
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CREATE COGNITIVE SEARCH RESOURCE`\n");

            var subscription = _values.GetOrDefault("init.service.subscription", "");
            var location = await AzCliConsoleGui.PickRegionLocationAsync(true, null, false);
            var group = await AzCliConsoleGui.PickOrCreateResourceGroup(true, subscription, location.Name);
            var name = DemandAskPrompt("Name: ");

            Console.Write("*** CREATING ***");
            var response = await AzCli.CreateSearchResource(subscription, group.Name, location.Name, name);

            Console.Write("\r");
            if (string.IsNullOrEmpty(response.StdOutput) && !string.IsNullOrEmpty(response.StdError))
            {
                var output = response.StdError.Replace("\n", "\n  ");
                throw new ApplicationException($"ERROR: Creating resource:\n\n  {output}");
            }

            Console.WriteLine("\r*** CREATED ***  ");
            return response.Payload;
        }

        private async Task DoInitHub(bool interactive)
        {
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // TODO: Add back non-interactive mode support

            ConsoleHelpers.WriteLineWithHighlight($"\n`AZURE AI RESOURCE`\n");
            Console.Write("\rName: *** Loading choices ***");

            var subscription = _values.GetOrDefault("init.service.subscription", "");

            var json = PythonSDKWrapper.ListResources(_values, subscription);
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
            var width = Math.Max(choices.Max(x => x.Length) + 4, 29);

            var normal = new Colors(ConsoleColor.White, ConsoleColor.Blue);
            var selected = new Colors(ConsoleColor.White, ConsoleColor.Red);

            Console.Write("\rName: ");
            var picked = ListBoxPicker.PickIndexOf(choices.ToArray(), width, 30, normal, selected);
            if (picked < 0)
            {
                throw new ApplicationException($"CANCELED: No resource selected");
            }

            Console.WriteLine($"\rName: {choices[picked]}");
            var resource = picked > 0 ? items.ToArray()[picked - 1] : null;
            if (picked == 0)
            {
                var resourceJson = await TryCreateHubInteractive();
                resource = JToken.Parse(resourceJson);
            }

            _values.Reset("service.resource.name", resource["name"].Value<string>());
            _values.Reset("service.resource.id", resource["id"].Value<string>());
            _values.Reset("service.resource.group.name", resource["resource_group"].Value<string>());
            _values.Reset("service.region.location", resource["location"].Value<string>());
        }

        private async Task<string> TryCreateHubInteractive()
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CREATE AZURE AI RESOURCE`\n");

            var subscription = _values.GetOrDefault("init.service.subscription", "");
            var location = await AzCliConsoleGui.PickRegionLocationAsync(true, null, false);
            var group = await AzCliConsoleGui.PickOrCreateResourceGroup(true, subscription, location.Name);
            var name = DemandAskPrompt("Name: ");
            var displayName = _values.Get("service.resource.display.name", true) ?? name;
            var description = _values.Get("service.resource.description", true) ?? name;

            Console.Write("*** CREATING ***");
            var json = PythonSDKWrapper.CreateResource(_values, subscription, group.Name, name, location.Name, displayName, description);

            Console.WriteLine("\r*** CREATED ***  ");

            var parsed = !string.IsNullOrEmpty(json) ? JToken.Parse(json) : null;
            return parsed["hub"].ToString();
        }

        private async Task DoInitProject(bool interactive)
        {
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // TODO: Add back non-interactive mode support

            ConsoleHelpers.WriteLineWithHighlight($"\n`AZURE AI PROJECT`\n");
            Console.Write("\rProject: *** Loading choices ***");

            var subscription = _values.GetOrDefault("init.service.subscription", "");
            var resourceId = _values.GetOrDefault("service.resource.id", null);
            var groupName = _values.GetOrDefault("service.resource.group.name", null);

            var json = PythonSDKWrapper.ListProjects(_values, subscription);
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
            var width = Math.Max(choices.Max(x => x.Length) + 4, 29);

            var normal = new Colors(ConsoleColor.White, ConsoleColor.Blue);
            var selected = new Colors(ConsoleColor.White, ConsoleColor.Red);

            Console.Write("\rProject: ");
            var picked = ListBoxPicker.PickIndexOf(choices.ToArray(), width, 30, normal, selected);
            if (picked < 0)
            {
                throw new ApplicationException($"CANCELED: No project selected");
            }

            Console.WriteLine($"\rProject: {choices[picked]}");
            var project = picked > 0 ? itemJTokens[picked - 1] : null;
            if (picked == 0)
            {
                var projectJson = TryCreateProjectInteractive();
                project = JToken.Parse(projectJson);
            } 

            var projectName = project["name"].Value<string>();
            var projectId = project["id"].Value<string>();

            _values.Reset("service.project.name", projectName);
            _values.Reset("service.project.id", projectId);
           
            ConsoleHelpers.WriteLineWithHighlight($"\n`AZURE AI PROJECT CONNECTIONS`\n");

            var openAiEndpoint = _values.GetOrDefault("service.openai.endpoint", null);
            var openAiKey = _values.GetOrDefault("service.openai.key", null);

            var connectionName = "Azure-OpenAI";
            Console.WriteLine($"Connection: {connectionName}");
            Console.Write("*** CREATING ***");
            var connectionType = "azure_open_ai";
            var connectionJson = PythonSDKWrapper.CreateConnection(_values, subscription, groupName, projectName, connectionName, connectionType, openAiEndpoint, openAiKey);
            Console.WriteLine("\r*** CREATED ***  ");
            Console.WriteLine();

            var searchEndpoint = _values.GetOrDefault("service.search.endpoint", null);
            var searchKey = _values.GetOrDefault("service.search.key", null);

            connectionName = "Default_CognitiveSearch";
            Console.WriteLine($"Connection: {connectionName}");
            Console.Write("*** CREATING ***");
            connectionType = "cognitive_search";
            connectionJson = PythonSDKWrapper.CreateConnection(_values, subscription, groupName, projectName, connectionName, connectionType, searchEndpoint, searchKey);
            Console.WriteLine("\r*** CREATED ***  ");

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

        private string TryCreateProjectInteractive()
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CREATE AZURE AI PROJECT`\n");

            var subscription = _values.GetOrDefault("init.service.subscription", "");
            var resourceId = _values.GetOrDefault("service.resource.id", "");
            var group = _values.GetOrDefault("service.resource.group.name", "");
            var location = _values.GetOrDefault("service.region.location", "");

            var name = DemandAskPrompt("Name: ");
            var displayName = _values.Get("service.project.display.name", true) ?? name;
            var description = _values.Get("service.project.description", true) ?? name;

            Console.Write("*** CREATING ***");
            var json = PythonSDKWrapper.CreateProject(_values, subscription, group, resourceId, name, location, displayName, description);

            Console.WriteLine("\r*** CREATED ***  ");

            var parsed = !string.IsNullOrEmpty(json) ? JToken.Parse(json) : null;
            return parsed["project"].ToString();
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

        private async Task<(string, string, string, string, string)> GetRegionAndKey(bool interactive, string subscriptionFilter, string regionFilter, string groupFilter, string resourceFilter, string kind, string sku, bool agreeTerms)
        {
            var subscriptionId = await AzCliConsoleGui.PickSubscriptionIdAsync(interactive, subscriptionFilter);

            ConsoleHelpers.WriteLineWithHighlight($"\n`{Program.SERVICE_RESOURCE_DISPLAY_NAME_ALL_CAPS}`");
            var regionLocation = new AzCli.AccountRegionLocationInfo(); // await AzCliConsoleGui.PickRegionLocationAsync(interactive, regionFilter);
            var resource = await AzCliConsoleGui.PickOrCreateCognitiveResource(interactive, subscriptionId, regionLocation.Name, groupFilter, resourceFilter, kind, sku, agreeTerms);

            var deployment = await AzCliConsoleGui.AiResourceDeploymentPicker.PickOrCreateDeployment(interactive, subscriptionId, resource, null);

            var keys = await AzCliConsoleGui.LoadCognitiveServicesResourceKeys(subscriptionId, resource);
            return (subscriptionId, resource.RegionLocation, resource.Endpoint, deployment.Name, keys.Key1);
        }

        private static void ConfigServiceResource(string subscriptionId, string region, string endpoint, string deployment, string key)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CONFIG {Program.SERVICE_RESOURCE_DISPLAY_NAME_ALL_CAPS}`");
            Console.WriteLine();

            if (Program.InitConfigsSubscription)
            {
                ConfigSet("@subscription", subscriptionId, $"Subscription: {subscriptionId}");
            }

            if (Program.InitConfigsEndpoint)
            {
                ConfigSet("@endpoint", endpoint, $"    Endpoint: {endpoint}");
            }

            ConfigSet("@deployment", deployment, $"  Deployment: {deployment}");
            ConfigSet("@region", region, $"      Region: {region}");
            ConfigSet("@key", key, $"         Key: {key.Substring(0, 4)}****************************");
        }

        private static void ConfigSearchResource(string endpoint, string key)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CONFIG COGNITIVE SEARCH RESOURCE`");
            Console.WriteLine();

            ConfigSet("@search.endpoint", endpoint, $"Endpoint: {endpoint}");
            ConfigSet("@search.key", key, $"     Key: {key.Substring(0, 4)}****************************");
        }

        private static void ConfigSet(string atFile, string setValue, string message)
        {
            Console.Write($"*** SETTING *** {message}");
            ConfigSet(atFile, setValue);
            Console.WriteLine($"\r  *** SET ***   {message}");
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

        private static void ThrowPromptNotAnsweredApplicationException()
        {
            throw new ApplicationException($"CANCELED: No input provided.");
        }

        private readonly bool _quiet = false;
        private readonly bool _verbose = false;
    }
}
