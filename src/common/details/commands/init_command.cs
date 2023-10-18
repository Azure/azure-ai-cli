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
        public InitCommand(ICommandValues values)
        {
            _values = values.ReplaceValues();
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", true);
        }

        public bool RunCommand()
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
            StartCommand();

            DisplayInitServiceBanner();

            CheckPath();

            var interactive = _values.GetOrDefault("init.service.interactive", true);
            switch (command)
            {
                // case "init": await DoInitServiceCommand(); break;
                case "init": await DoInitRoot(); break;
                case "init.openai": await DoInitRootOpenAi(interactive); break;
                case "init.search": await DoInitRootSearch(interactive); break;
                case "init.speech": await DoInitRootSpeech(interactive); break;
                case "init.project": await DoInitRootProject(interactive); break;
                case "init.resource": await DoInitResourceCommand(); break;

                // POST-IGNITE: TODO: add ability to init deployments
                // TODO: ensure that deployments in "openai" flow can be skipped

                default:
                    _values.AddThrowError("WARNING", $"Unknown command '{command}'");
                    break;
            }

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private async Task DoInitRoot()
        {
            var interactive = _values.GetOrDefault("init.service.interactive", true);
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // POST-IGNITE: TODO: Add back non-interactive mode support

            ConsoleHelpers.WriteLineWithHighlight("`AI INIT`\n\n  Initializes (creates, selects, or attaches to) AI Projects and services.\n");

            var existing = FileHelpers.FindFileInDataPath("config.json", _values);
            if (existing != null)
            {
                await DoInitRootVerifyConfig(interactive, existing);
            }
            else
            {
                await DoInitRootMenuPick();
            }
        }

        private async Task DoInitRootVerifyConfig(bool interactive, string fileName)
        {
            if (await VerifyConfigGood(interactive, fileName))
            {
                await DoInitRootConfirmVerifiedConfig(fileName);
            }
            else
            {
                await DoInitRootMenuPick();
            }
        }

        private async Task<bool> VerifyConfigGood(bool interactive, string fileName)
        {
            ParseConfigJson(fileName, out string subscription, out string groupName, out string projectName);

            Console.WriteLine($"  PROJECT: {projectName}");
            var validated = await AzCliConsoleGui.ValidateSubscriptionAsync(interactive, subscription, "  SUBSCRIPTION");

            ConsoleHelpers.WriteLineWithHighlight("\n  `ATTACHED SERVICES AND RESOURCES`\n");

            var message = "    Validating...";
            Console.Write(message);

            if (await VerifyConfigGood(subscription, groupName, projectName))
            {
                Console.Write(new string(' ', message.Length + 2) + "\r");
                return true;
            }
            else
            {
                ConsoleHelpers.WriteLineWithHighlight($"\r{message} `#e_;WARNING: Configuration could not be validated!`");
                Console.WriteLine();
                return false;
            }
        }

        private async Task<bool> VerifyConfigGood(string subscription, string groupName, string projectName)
        {
            Thread.Sleep(4000); // TODO: Actually verify the config.json is good
            await Task.CompletedTask;
            return Environment.GetEnvironmentVariable("AZURE_AI_CLI_INIT_PRETEND_VALID") == "true";
        }

        private async Task DoInitRootConfirmVerifiedConfig(string fileName)
        {
            ParseConfigJson(fileName, out string subscription, out string groupName, out string projectName);

            // TODO: Print correct stuff here... 
            ConsoleHelpers.WriteLineWithHighlight("    AI RESOURCE: {resource-name}                                     `#e_;<== work in progress`");
            ConsoleHelpers.WriteLineWithHighlight("    AI SEARCH RESOURCE: {search-resource-name}                       `#e_;<== work in progress`");
            Console.WriteLine();
            ConsoleHelpers.WriteLineWithHighlight("    OPEN AI RESOURCE: {openai-resource-name}                         `#e_;<== work in progress`");
            ConsoleHelpers.WriteLineWithHighlight("    OPEN AI DEPLOYMENT (CHAT): {chat-deployment-name}                `#e_;<== work in progress`");
            ConsoleHelpers.WriteLineWithHighlight("    OPEN AI DEPLOYMENT (EMBEDDINGS): {embeddings-deployment-name}    `#e_;<== work in progress`");
            ConsoleHelpers.WriteLineWithHighlight("    OPEN AI DEPLOYMENT (EVALUATION): {evaluation-deployment-name}    `#e_;<== work in progress`");

            Console.WriteLine();
            var label = "  Initialize";
            Console.Write($"{label}: ");

            var choices = new string[] { $"PROJECT: {projectName}", $"     OR: (Initialize something else)" };
            var picked = ListBoxPicker.PickIndexOf(choices.ToArray());
            if (picked < 0)
            {
                Console.WriteLine($"\r{label}: CANCELED (no selection)");
                return;
            }
            else if (picked > 0)
            {
                Console.Write(new string(' ', label.Length + 2) + "\r");
                Console.WriteLine("  Initializing something else...\n");
                await DoInitRootMenuPick();
                return;
            }

            ConsoleHelpers.WriteLineWithHighlight($"\r`INIT FROM PROJECT {projectName.ToUpper()}`\n");

            // TODO: Setup all the local datastore configuration 

            _values.AddThrowError("WARNING", "NOT IMPLEMENTED YET");
        }

        private bool ParseConfigJson(string fileName, out string subscription, out string groupName, out string projectName)
        {
            subscription = groupName = projectName = null;

            var json = FileHelpers.ReadAllHelpText(fileName, Encoding.UTF8);
            try
            {
                var config = JObject.Parse(json);
                subscription = config.ContainsKey("subscription_id") ? config["subscription_id"].ToString() : null;
                groupName = config.ContainsKey("resource_group") ? config["resource_group"].ToString() : null;
                projectName = config.ContainsKey("project_name") ? config["project_name"].ToString() : null;
            }
            catch (Exception ex)
            {
                return false;
                // _values.AddThrowError("ERROR", $"Unable to parse config.json: {ex.Message}");
            }

            return !string.IsNullOrEmpty(subscription) && !string.IsNullOrEmpty(groupName) && !string.IsNullOrEmpty(projectName);
        }

        private async Task DoInitRootMenuPick()
        {
            var interactive = true;

            Console.WriteLine("  Choose between initializing:");
            ConsoleHelpers.WriteLineWithHighlight("  - AI Project resource: Recommended when using `Azure AI Studio` and/or connecting to multiple AI services.");
            Console.WriteLine("  - Standalone resources: Recommended when building simple solutions connecting to a single AI service.");
            Console.WriteLine();

            var label = "  Initialize";
            Console.Write($"{label}: ");
            var choiceToPart = new Dictionary<string, string>
            {
                ["AI Project resource"] = "init-root-project-hack",             // TODO: Replace with new flows below | | |
                // ["New AI Project"] = "init-root-project-new",                //                                    v v v
                // ["Existing AI Project"] = "init-root-project-pick",
                ["Standalone resources"] = "init-root-standalone-select-or-create",
            };
            var partToLabelDisplay = new Dictionary<string, string>()
            {
                ["init-root-project-hack"] = "AI Project resource",
                // ["init-root-project-new"] = "New AI Project",
                // ["init-root-project-pick"] = "Existing AI Project",
                ["init-root-standalone-select-or-create"] = null
            };

            var choices = choiceToPart.Keys.ToArray();
            var picked = ListBoxPicker.PickIndexOf(choices.ToArray());
            if (picked < 0)
            {
                Console.WriteLine($"\r{label}: CANCELED (no selection)");
                return;
            }

            var part = choiceToPart[choices[picked]];
            var display = partToLabelDisplay[part];

            Console.Write(display == null
                ? new string(' ', label.Length + 2) + "\r"
                : $"\r{label.Trim()}: {display}\n");
            await DoInitServiceParts(interactive, part.Split(';').ToArray());
        }

        private async Task DoInitResourceCommand()
        {
            var interactive = _values.GetOrDefault("init.service.interactive", true);
            await DoInitServiceParts(interactive, "resource");
        }

        private async Task DoInitStandaloneResources(bool interactive)
        {
            var label = "  Initialize";
            Console.Write($"{label}: ");
            var choiceLookup = new Dictionary<string, string>
            {
                ["Azure OpenAI"] = "init-root-openai-create-or-select",
                ["Azure Search"] = "init-root-search-create-or-select",
                ["Azure Speech"] = "init-root-speech-create-or-select"
            };

            var choices = choiceLookup.Keys.ToArray();

            var picked = ListBoxPicker.PickIndexOf(choices.ToArray());
            if (picked < 0)
            {
                Console.WriteLine("\rInitialize: CANCELED (no selection)");
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
                if (Program.Debug) Console.WriteLine($"OPERATION: {operation}");

                var task = operation switch
                {
                    "init-root-project-hack" => DoInitRootProject(interactive),         // TODO: Replace with new flows below  | | |
                    "init-root-project-pick" => DoInitRootProject(interactive),         //                                     v v v
                    "init-root-project-new" => DoInitRootProject(interactive),

                    "init-root-standalone-select-or-create" => DoInitStandaloneResources(interactive),
                    "init-root-openai-create-or-select" => DoInitRootOpenAi(interactive),
                    "init-root-search-create-or-select" => DoInitRootSearch(interactive),
                    "init-root-speech-create-or-select" => DoInitRootSpeech(interactive),

                    "openai" => DoInitOpenAi(interactive),
                    "search" => DoInitSearch(interactive),
                    "resource" => DoInitHub(interactive),
                    "project" => DoInitProject(interactive),

                    _ => throw new ApplicationException($"WARNING: NOT YET IMPLEMENTED")
                };
                await task;
            }
        }

        private async Task DoInitRootProject(bool interactive)
        {
            var subscriptionFilter = SubscriptionToken.Data().GetOrDefault(_values, "");
            var subscriptionId = await AzCliConsoleGui.PickSubscriptionIdAsync(interactive, interactive, subscriptionFilter);
            SubscriptionToken.Data().Set(_values, subscriptionId);

            await DoInitServiceParts(interactive, "openai", "search", "resource", "project");
        }

        private async Task DoInitProject(bool interactive)
        {
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // POST-IGNITE: TODO: Add back non-interactive mode support

            var subscription = SubscriptionToken.Data().GetOrDefault(_values, "");
            var resourceId = _values.GetOrDefault("service.resource.id", null);
            var groupName = ResourceGroupNameToken.Data().GetOrDefault(_values);

            var openAiEndpoint = _values.GetOrDefault("service.openai.endpoint", null);
            var openAiKey = _values.GetOrDefault("service.openai.key", null);
            var searchEndpoint = _values.GetOrDefault("service.search.endpoint", null);
            var searchKey = _values.GetOrDefault("service.search.key", null);

            var project = AiSdkConsoleGui.InitAndConfigAiHubProject(_values, subscription, resourceId, groupName, openAiEndpoint, openAiKey, searchEndpoint, searchKey);

            ProjectNameToken.Data().Set(_values, project.Name);
            _values.Reset("service.project.id", project.Id);
        }

        private async Task DoInitRootOpenAi(bool interactive)
        {
            var subscriptionFilter = SubscriptionToken.Data().GetOrDefault(_values, "");
            var subscriptionId = await AzCliConsoleGui.PickSubscriptionIdAsync(interactive, interactive, subscriptionFilter);
            SubscriptionToken.Data().Set(_values, subscriptionId);

            await DoInitOpenAi(interactive);
        }

        private async Task DoInitOpenAi(bool interactive)
        {
            var subscriptionId = SubscriptionToken.Data().GetOrDefault(_values, "");
            var regionFilter = _values.GetOrDefault("init.service.resource.region.name", "");
            var groupFilter = _values.GetOrDefault("init.service.resource.group.name", "");
            var resourceFilter = _values.GetOrDefault("init.service.cognitiveservices.resource.name", "");
            var kind = _values.GetOrDefault("init.service.cognitiveservices.resource.kind", "OpenAI");
            var sku = _values.GetOrDefault("init.service.cognitiveservices.resource.sku", Program.CognitiveServiceResourceSku);
            var yes = _values.GetOrDefault("init.service.cognitiveservices.terms.agree", false);

            var resource = await AzCliConsoleGui.InitAndConfigOpenAiResource(interactive, subscriptionId, regionFilter, groupFilter, resourceFilter, kind, sku, yes);

            SubscriptionToken.Data().Set(_values, subscriptionId);
            _values.Reset("service.resource.region.name", resource.RegionLocation);
            _values.Reset("service.openai.endpoint", resource.Endpoint);
            _values.Reset("service.openai.key", resource.Key);
            _values.Reset("service.openai.resource.id", resource.Id);
            ResourceNameToken.Data().Set(_values, resource.Name);
            ResourceGroupNameToken.Data().Set(_values, resource.Group);
        }

        private async Task DoInitRootSearch(bool interactive)
        {
            var subscriptionFilter = SubscriptionToken.Data().GetOrDefault(_values, "");
            var subscriptionId = await AzCliConsoleGui.PickSubscriptionIdAsync(interactive, interactive, subscriptionFilter);
            SubscriptionToken.Data().Set(_values, subscriptionId);

            await DoInitSearch(interactive);
        }

        private async Task DoInitSearch(bool interactive)
        {
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // POST-IGNITE: TODO: Add back non-interactive mode support

            var subscription = SubscriptionToken.Data().GetOrDefault(_values, "");
            var location = _values.GetOrDefault("service.resource.region.name", "");
            var groupName = ResourceGroupNameToken.Data().GetOrDefault(_values, "");

            var smartName = ResourceNameToken.Data().GetOrDefault(_values);
            var smartNameKind = smartName != null && smartName.Contains("openai") ? "openai" : "oai";

            var resource = await AzCliConsoleGui.InitAndConfigCogSearchResource(subscription, location, groupName, smartName, smartNameKind);

            _values.Reset("service.search.endpoint", resource.Endpoint);
            _values.Reset("service.search.key", resource.Key);
        }

        private async Task DoInitRootSpeech(bool interactive)
        {
            var subscriptionFilter = SubscriptionToken.Data().GetOrDefault(_values, "");
            var subscriptionId = await AzCliConsoleGui.PickSubscriptionIdAsync(interactive, interactive, subscriptionFilter);
            SubscriptionToken.Data().Set(_values, subscriptionId);

            await DoInitSpeech(interactive);
        }

        private async Task DoInitSpeech(bool interactive)
        {
            var subscriptionId = SubscriptionToken.Data().GetOrDefault(_values, "");
            var regionFilter = _values.GetOrDefault("init.service.resource.region.name", "");
            var groupFilter = _values.GetOrDefault("init.service.resource.group.name", "");
            var resourceFilter = _values.GetOrDefault("init.service.cognitiveservices.resource.name", "");
            var kind = _values.GetOrDefault("init.service.cognitiveservices.resource.kind", "SpeechServices");
            var sku = _values.GetOrDefault("init.service.cognitiveservices.resource.sku", "S0");
            var yes = _values.GetOrDefault("init.service.cognitiveservices.terms.agree", false);

            var resource = await AzCliConsoleGui.InitAndConfigSpeechResource(interactive, subscriptionId, regionFilter, groupFilter, resourceFilter, kind, sku, yes);

            SubscriptionToken.Data().Set(_values, subscriptionId);
        }

        private async Task DoInitHub(bool interactive)
        {
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // POST-IGNITE: TODO: Add back non-interactive mode support

            var subscription = SubscriptionToken.Data().GetOrDefault(_values, "");
            if (string.IsNullOrEmpty(subscription))
            {
                subscription = await AzCliConsoleGui.PickSubscriptionIdAsync(interactive, interactive);
                _values.Reset("init.service.subscription", subscription);
            }

            var aiHubResource = await AiSdkConsoleGui.PickOrCreateAiHubResource(_values, subscription);
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

        private static void ThrowInteractiveNotSupportedApplicationException()
        {
            throw new ApplicationException("WARNING: Non-interactive mode not supported");
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

        private SpinLock _lock = null;
        private readonly bool _quiet = false;
        private readonly bool _verbose = false;
    }
}
