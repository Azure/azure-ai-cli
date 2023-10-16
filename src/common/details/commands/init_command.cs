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
                case "init.openai": await DoInitOpenAiCommand(); break;
                case "init.search": await DoInitSearchCommand(); break;
                case "init.resource": await DoInitResourceCommand(); break;
                case "init.project": await DoInitProjectCommand(); break;

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
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // TODO: Add back non-interactive mode support

            ConsoleHelpers.WriteLineWithHighlight("AI INIT\n\n  The `ai init` command initializes (create, select, or attach to) AI Projects and services.\n");

            var existing = FileHelpers.FindFileInDataPath("config.json", _values);
            if (existing != null)
            {
                await DoInitRootVerifyConfig(existing);
            }
            else
            {
                await DoInitRootMenuPick();
            }
        }

        private Task DoInitRootVerifyConfig(string fileName)
        {
            throw new NotImplementedException();
        }

        private async Task DoInitRootMenuPick()
        {
            Console.WriteLine("  Choose between initializing:");
            ConsoleHelpers.WriteLineWithHighlight("  - AI Project resource: Recommended when using `Azure AI Studio` and/or connecting to multiple AI services.");
            Console.WriteLine("  - Standalone resources: Recommended when building simple solutions connecting to a single AI service.");
            Console.WriteLine();

            var label = "  Task";
            Console.Write($"{label}: ");
            var choiceToPart = new Dictionary<string, string>
            {
                ["INIT: a new AI Project"] = "init-root-project-new",
                ["  or: an existing AI Project"] = "init-root-project-pick",
                ["  or: standalone service resources"] = "init-root-standalone-select-or-create",
            };
            var partToLabelDisplay = new Dictionary<string, string>()
            {
                ["init-root-project-new"] = "New AI Project",
                ["init-root-project-pick"] = "Existing AI Project",
                ["init-root-standalone-select-or-create"] = "Standalone service resources",
            };

            var choices = choiceToPart.Keys.ToArray();
            var picked = ListBoxPicker.PickIndexOf(choices.ToArray());
            if (picked < 0)
            {
                Console.WriteLine($"\r{label}: (canceled)");
                return;
            }

            var part = choiceToPart[choices[picked]];
            var display = partToLabelDisplay[part];

            ConsoleHelpers.WriteLineWithHighlight($"\r`AI INIT: {display.ToUpper()}`\n");

            // HACK
            if (part == "init-root-standalone-select-or-create")
            {
                part = "openai;search";
            }

            var interactive = true;
            await DoInitServiceParts(interactive, part.Split(';').ToArray());

            await Task.CompletedTask;
        }

        private async Task DoInitServiceCommand()
        {
            var interactive = _values.GetOrDefault("init.service.interactive", true);
            await DoInitService(interactive);
        }

        private async Task DoInitOpenAiCommand()
        {
            var interactive = _values.GetOrDefault("init.service.interactive", true);
            await DoInitServiceParts(interactive, "openai");
        }

        private async Task DoInitSearchCommand()
        {
            var interactive = _values.GetOrDefault("init.service.interactive", true);
            await DoInitServiceParts(interactive, "openai", "search");
        }

        private async Task DoInitResourceCommand()
        {
            var interactive = _values.GetOrDefault("init.service.interactive", true);
            await DoInitServiceParts(interactive, "resource");
        }

        private async Task DoInitProjectCommand()
        {
            var interactive = _values.GetOrDefault("init.service.interactive", true);
            await DoInitServiceParts(interactive, "openai", "search", "resource", "project");
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
                ["Azure OpenAI + AI Search"] = "openai;search",
                ["Azure AI Resource + Project"] = "resource;project",
                ["Azure AI Resource + Project + OpenAI"] = "openai;resource;project",
                ["Azure AI Resource + Project + OpenAI + AI Search"] = "openai;search;resource;project",
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

                    "init-root-project-pick" => DoInitProject(interactive), // TODO: Replace with new flow
                    "init-root-project-new" => DoInitProject(interactive), // TODO: Replace with new flow

                    _ => throw new ApplicationException($"WARNING: NOT YET IMPLEMENTED")
                };
                await task;
            }
        }

        private async Task DoInitOpenAi(bool interactive)
        {
            var subscriptionFilter = SubscriptionToken.Data().GetOrDefault(_values, "");
            var regionFilter = _values.GetOrDefault("init.service.resource.region.name", "");
            var groupFilter = _values.GetOrDefault("init.service.resource.group.name", "");
            var resourceFilter = _values.GetOrDefault("init.service.cognitiveservices.resource.name", "");
            var kind = _values.GetOrDefault("init.service.cognitiveservices.resource.kind", Program.CognitiveServiceResourceKind);
            var sku = _values.GetOrDefault("init.service.cognitiveservices.resource.sku", Program.CognitiveServiceResourceSku);
            var yes = _values.GetOrDefault("init.service.cognitiveservices.terms.agree", false);

            var subscriptionId = await AzCliConsoleGui.PickSubscriptionIdAsync(interactive, subscriptionFilter);
            var resource = await AzCliConsoleGui.InitAndConfigOpenAiResource(interactive, subscriptionId, regionFilter, groupFilter, resourceFilter, kind, sku, yes);

            SubscriptionToken.Data().Set(_values, subscriptionId);
            _values.Reset("service.resource.region.name", resource.RegionLocation);
            _values.Reset("service.openai.endpoint", resource.Endpoint);
            _values.Reset("service.openai.key", resource.Key);
            _values.Reset("service.openai.resource.id", resource.Id);
            ResourceNameToken.Data().Set(_values, resource.Name);
            ResourceGroupNameToken.Data().Set(_values, resource.Group);
        }

        private async Task DoInitSearch(bool interactive)
        {
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // TODO: Add back non-interactive mode support

            var subscription = SubscriptionToken.Data().GetOrDefault(_values, "");
            var location = _values.GetOrDefault("service.resource.region.name", "");
            var groupName = ResourceGroupNameToken.Data().GetOrDefault(_values, "");

            var smartName = ResourceNameToken.Data().GetOrDefault(_values);
            var smartNameKind = smartName != null && smartName.Contains("openai") ? "openai" : "oai";

            var resource = await AzCliConsoleGui.InitAndConfigCogSearchResource(subscription, location, groupName, smartName, smartNameKind);

            _values.Reset("service.search.endpoint", resource.Endpoint);
            _values.Reset("service.search.key", resource.Key);
        }

        private async Task DoInitHub(bool interactive)
        {
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // TODO: Add back non-interactive mode support

            var subscription = SubscriptionToken.Data().GetOrDefault(_values, "");
            if (string.IsNullOrEmpty(subscription))
            {
                subscription = await AzCliConsoleGui.PickSubscriptionIdAsync(interactive);
                _values.Reset("init.service.subscription", subscription);
            }

            var aiHubResource = await AiSdkConsoleGui.PickOrCreateAiHubResource(_values, subscription);
        }

        private async Task DoInitProject(bool interactive)
        {
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // TODO: Add back non-interactive mode support

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
