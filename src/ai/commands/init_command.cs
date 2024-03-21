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
using Azure.AI.Details.Common.CLI.ConsoleGui;
using Azure.AI.Details.Common.CLI.Telemetry;
using Azure.AI.Details.Common.CLI.Telemetry.Events;

namespace Azure.AI.Details.Common.CLI
{
    public class InitCommand : Command
    {
        public InitCommand(ICommandValues values) : base(values)
        {
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

            var interactive = _values.GetOrDefault("init.service.interactive", true);
            var runId = _values.GetOrAdd("telemetry.init.run_id", Guid.NewGuid);

            switch (command)
            {
                // case "init": await DoInitServiceCommand(); break;
                case "init": await DoInitRootAsync(); break;

                case "init.resource": await DoInitRootHubResource(interactive); break;

                case "init.project": await DoInitRootProject(interactive, true, true); break;
                case "init.project.new": await DoInitRootProject(interactive, true, false); break;
                case "init.project.select": await DoInitRootProject(interactive, false, true); break;

                case "init.aiservices": await DoInitRootCognitiveServicesAIServicesKind(interactive); break;
                case "init.cognitiveservices": await DoInitRootCognitiveServicesCognitiveServicesKind(interactive); break;

                case "init.openai":
                case "init.openai.chat": await DoInitRootOpenAi(interactive, false, false, true, true, true, true); break;
                case "init.openai.embeddings": await DoInitRootOpenAi(interactive, true, true, false, false, true, true); break;
                case "init.openai.evaluations": await DoInitRootOpenAi(interactive, true, true, true, true, false, false); break;
                case "init.openai.deployments": await DoInitRootOpenAi(interactive, false, true, false, true, false, true); break;

                case "init.search": await DoInitRootSearch(interactive); break;
                case "init.speech": await DoInitRootSpeech(interactive); break;
                case "init.vision": await DoInitRootVision(interactive); break;

                // POST-IGNITE: TODO: add ability to init deployments
                // TODO: ensure that deployments in "openai" flow can be skipped

                default:
                    _values.AddThrowError("WARNING", $"Unknown command '{command}'");
                    break;
            }

            var outputEnvFile = _values.GetOrDefault("init.output.env.file", false);
            if (outputEnvFile)
            {
                var fileName = ".env";

                var env = ConfigEnvironmentHelpers.GetEnvironment(_values);
                var fqn = ConfigEnvironmentHelpers.SaveEnvironment(env, fileName);

                Console.WriteLine($"\n{fileName} (saved at '{fqn}')\n");
                ConfigEnvironmentHelpers.PrintEnvironment(env);
            }

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private async Task DoInitRootAsync()
        {
            var interactive = _values.GetOrDefault("init.service.interactive", true);
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // POST-IGNITE: TODO: Add back non-interactive mode support

            ConsoleHelpers.WriteLineWithHighlight("`AI INIT`\n\n  Initializes (creates, selects, or attaches to) AI Projects and services.\n");

            var existing = FileHelpers.FindFileInDataPath("config.json", _values);
            if (existing != null)
            {
                await DoInitRootVerifyConfigFileAsync(interactive, existing);
            }
            else
            {
                await DoInitRootMenuPick();
            }
        }

        private async Task DoInitRootVerifyConfigFileAsync(bool interactive, string fileName)
        {
            string detail = null;

            bool? useSaved = await Program.Telemetry.WrapAsync<bool?>(async () =>
            {
                bool success = ParseConfigJson(fileName, out string subscription, out string groupName, out string projectName);
                if (success)
                {
                    var (hubName, openai, search) = await VerifyProjectAsync(interactive, subscription, groupName, projectName);
                    if (openai != null && search != null)
                    {
                        bool? useSaved = await DoInitRootConfirmVerifiedProjectResources(
                            interactive, subscription, projectName, hubName, openai.Value, search.Value);

                        detail = useSaved.HasValue
                            ? useSaved == true ? "saved_config" : "something_else"
                            : null;

                        return useSaved; 
                    }
                    else
                    {
                        detail = "invalid_config";
                        return null;
                    }
                }
                else
                {
                    detail = "invalid_json";
                    return null;
                }
            },
            (outcome, useSaved, ex, duration) => new VerifySavedConfigTelemetryEvent()
            {
                Outcome = useSaved == null ? Outcome.Failed : outcome,
                Detail = detail,
                DurationInMs = duration.TotalMilliseconds,
                Error = ex?.Message,
            })
            .ConfigureAwait(false);

            if (useSaved != true)
            {
                await DoInitRootMenuPick();
            }
        }

        private async Task<(string, AzCli.CognitiveServicesResourceInfo?, AzCli.CognitiveSearchResourceInfo?)> VerifyProjectAsync(bool interactive, string subscriptionId, string groupName, string projectName)
        {
            Console.WriteLine($"  PROJECT: {projectName}");

            var validated = await AzCliConsoleGui.ValidateSubscriptionAsync(interactive, subscriptionId);
            if (validated == null)
            {
                return (null, null, null);
            }

            ConsoleHelpers.WriteLineWithHighlight("\n  `ATTACHED SERVICES AND RESOURCES`\n");

            var message = "    Validating...";
            Console.Write(message);

            var (hubName, openai, search) = await AiSdkConsoleGui.VerifyResourceConnections(_values, validated?.Id, groupName, projectName);
            if (openai != null && search != null)
            {
                Console.Write($"\r{new string(' ', message.Length)}\r");
                return (hubName, openai, search);
            }
            else
            {
                ConsoleHelpers.WriteLineWithHighlight($"\r{message} `#e_;WARNING: Configuration could not be validated!`");
                Console.WriteLine();
                return (null, null, null);
            }
        }

        private async Task<bool?> DoInitRootConfirmVerifiedProjectResources(bool interactive, string subscription, string projectName, string resourceName, AzCli.CognitiveServicesResourceInfo openaiResource, AzCli.CognitiveSearchResourceInfo searchResource)
        {
            ConsoleHelpers.WriteLineWithHighlight($"    AI RESOURCE: {resourceName}");
            ConsoleHelpers.WriteLineWithHighlight($"    AI SEARCH RESOURCE: {searchResource.Name}");
            ConsoleHelpers.WriteLineWithHighlight($"    AZURE OPENAI RESOURCE: {openaiResource.Name}");

            // TODO: If there's a way to get the deployments, get them, and do this... Print correct stuff here... 
            // ConsoleHelpers.WriteLineWithHighlight($"    AZURE OPENAI DEPLOYMENT (CHAT): {{chat-deployment-name}}                `#e_;<== work in progress`");
            // ConsoleHelpers.WriteLineWithHighlight($"    AZURE OPENAI DEPLOYMENT (EMBEDDING): {{embedding-deployment-name}}    `#e_;<== work in progress`");
            // ConsoleHelpers.WriteLineWithHighlight($"    AZURE OPENAI DEPLOYMENT (EVALUATION): {{evaluation-deployment-name}}    `#e_;<== work in progress`");

            Console.WriteLine();
            var label = "  Initialize";
            Console.Write($"{label}: ");

            var choices = new string[] { $"PROJECT: {projectName}", $"     OR: (Initialize something else)" };
            int picked = ListBoxPicker.PickIndexOf(choices.ToArray());

            if (picked < 0)
            {
                Console.WriteLine($"\r{label}: CANCELED (no selection)");
                return null;
            }
            else if (picked > 0)
            {
                Console.Write(new string(' ', label.Length + 2) + "\r");
                Console.WriteLine("  Initializing something else...\n");
                return false;
            }
            else
            {
                Console.Write("\r" + new string(' ', label.Length + 2) + "\r");

                await AzCliConsoleGui.PickOrCreateAndConfigCognitiveServicesOpenAiKindResourceDeployments(null, "AZURE OPENAI RESOURCE", interactive, subscription, openaiResource);

                var searchKeys = await AzCliConsoleGui.LoadSearchResourceKeys(subscription, searchResource);
                ConfigSetHelpers.ConfigSearchResource(searchResource.Endpoint, searchKeys.Key1);

                ConfigSetHelpers.ConfigSet("@subscription", subscription);
                ConfigSetHelpers.ConfigSet("@project", projectName);
                ConfigSetHelpers.ConfigSet("@group", openaiResource.Group);

                return true;
            }
        }

        private bool ParseConfigJson(string fileName, out string subscription, out string groupName, out string projectName)
        {
            subscription = groupName = projectName = null;

            var json = FileHelpers.ReadAllHelpText(fileName, Encoding.UTF8);
            try
            {
                var config = JsonDocument.Parse(json).RootElement;
                subscription = config.GetPropertyStringOrNull("subscription_id");
                groupName = config.GetPropertyStringOrNull("resource_group");
                projectName = config.GetPropertyStringOrNull("project_name");
                projectName ??= config.GetPropertyStringOrNull("workspace_name");
            }
            catch (Exception)
            {
                return false;
            }

            return !string.IsNullOrEmpty(subscription) && !string.IsNullOrEmpty(groupName) && !string.IsNullOrEmpty(projectName);
        }

        private async ValueTask DoInitRootMenuPick()
        {
            var interactive = true;

            Console.WriteLine("  Choose between initializing:");
            ConsoleHelpers.WriteLineWithHighlight("  - AI Project: Recommended when using `Azure AI Studio` and/or connecting to multiple AI services.");
            Console.WriteLine("  - Standalone resources: Recommended when building simple solutions connecting to a single AI service.");
            Console.WriteLine();

            var label = "  Initialize";
            Console.Write($"{label}: ");

            var choices = new[]
            {
                new { DisplayName = "New AI Project", Value = "init-root-project-create", Metadata = "new" },
                new { DisplayName = "Existing AI Project", Value = "init-root-project-select", Metadata = "existing" },
                new { DisplayName = "Standalone resources", Value = "init-root-standalone-select-or-create", Metadata = "standalone" },
            };

            int selected = -1;

            var outcome = Program.Telemetry.Wrap(() =>
            {
                selected = ListBoxPicker.PickIndexOf(choices.Select(e => e.DisplayName).ToArray());
                if (selected < 0)
                {
                    Console.WriteLine($"\r{label}: CANCELED (no selection)");
                    return Outcome.Canceled;
                }

                Console.Write($"\r{label.Trim()}: {choices.ElementAtOrDefault(selected)?.DisplayName}\n");
                _values.Reset("telemetry.init.run_type", choices.ElementAtOrDefault(selected)?.Metadata);

                return Outcome.Success;
            },
            (outcome, ex, timeTaken) => choices.ElementAtOrDefault(selected)?.Metadata == "standalone"
                ? null
                : new InitTelemetryEvent(InitStage.Choice)
                {
                    Outcome = outcome,
                    RunId = _values.GetOrDefault("telemetry.init.run_id", null),
                    RunType = _values.GetOrDefault("telemetry.init.run_type", null),
                    DurationInMs = timeTaken.TotalMilliseconds,
                    Error  = ex?.Message
                });

            if (outcome == Outcome.Success)
            {
                await DoInitServiceParts(interactive, choices.ElementAtOrDefault(selected)?.Value);
            }
        }

        private async Task DoInitStandaloneResources(bool interactive)
        {
            Console.WriteLine("  Standalone resources:");
            Console.WriteLine("  - Azure AI Services (v2): Includes Azure Speech, Vision, and OpenAI");
            Console.WriteLine("  - Azure AI Services (v1): Includes Azure Speech, Vision, Language, and Search");
            Console.WriteLine("  - Azure OpenAI: Provides access to OpenAI's powerful language models.");
            Console.WriteLine("  - Azure Search: Provides keyword, vector, and hybrid search capabilities.");
            Console.WriteLine("  - Azure Speech: Provides speech recognition, synthesis, and translation.");
            Console.WriteLine();

            var label = "  Initialize";
            Console.Write($"{label}: ");

            var choices = new[]
            {
                new { DisplayName = "Azure AI Services (v2)", Value = "init-root-cognitiveservices-ai-services-kind-create-or-select", Metadata = "aiservices" },
                new { DisplayName = "Azure AI Services (v1)", Value = "init-root-cognitiveservices-cognitiveservices-kind-create-or-select", Metadata ="cognitiveservices" },
                new { DisplayName = "Azure OpenAI", Value = "init-root-openai-create-or-select", Metadata = "openai" },
                new { DisplayName = "Azure Search", Value = "init-root-search-create-or-select", Metadata = "search" },
                new { DisplayName = "Azure Speech", Value = "init-root-speech-create-or-select", Metadata = "speech" }
            };

            int picked = -1;
            var outcome = Program.Telemetry.Wrap(() =>
                {
                    picked = ListBoxPicker.PickIndexOf(choices.Select(e => e.DisplayName).ToArray());
                    if (picked < 0)
                    {
                        Console.WriteLine("\rInitialize: CANCELED (no selection)");
                        return Outcome.Canceled;
                    }

                    Console.WriteLine($"\rInitialize: {choices.ElementAtOrDefault(picked)?.DisplayName}");
                    return Outcome.Success;
                },
                (outcome, ex, timeTaken) => new InitTelemetryEvent(InitStage.Choice)
                {
                    Outcome = outcome,
                    RunId = _values.GetOrDefault("telemetry.init.run_id", null),
                    RunType = _values.GetOrDefault("telemetry.init.run_type", null),
                    Selected = choices.ElementAtOrDefault(picked)?.Metadata,
                    DurationInMs = timeTaken.TotalMilliseconds,
                    Error  = ex?.Message
                });

            if (outcome == Outcome.Success)
            {
                await DoInitServiceParts(true, choices.ElementAtOrDefault(picked)?.Value);
            }
        }

        private async Task DoInitServiceParts(bool interactive, params string[] operations)
        {
            foreach (var operation in operations)
            {
                if (Program.Debug) Console.WriteLine($"OPERATION: {operation}");

                var task = operation switch
                {
                    "init-root-project-select-or-create" => DoInitRootProject(interactive, true, true),
                    "init-root-project-select" => DoInitRootProject(interactive, false, true),
                    "init-root-project-create" => DoInitRootProject(interactive, true, false),

                    "project-select-or-create" => DoInitProject(interactive, true, true),
                    "project-select" => DoInitProject(interactive, false, true),
                    "project-create" => DoInitProject(interactive, true, false),

                    "init-root-standalone-select-or-create" => DoInitStandaloneResources(interactive),
                    "init-root-cognitiveservices-ai-services-kind-create-or-select" => DoInitRootCognitiveServicesAIServicesKind(interactive),
                    "init-root-cognitiveservices-cognitiveservices-kind-create-or-select" => DoInitRootCognitiveServicesCognitiveServicesKind(interactive),
                    "init-root-openai-create-or-select" => DoInitRootOpenAi(interactive),
                    "init-root-search-create-or-select" => DoInitRootSearch(interactive),
                    "init-root-speech-create-or-select" => DoInitRootSpeech(interactive),

                    "subscription" => DoInitSubscriptionId(interactive),
                    "cognitiveservices-ai-services-kind" => DoInitCognitiveServicesAIServicesKind(interactive),
                    "cognitiveservices-cognitiveservices-kind" => DoInitCognitiveServicesCognitiveServicesKind(interactive),
                    "openai" => DoInitOpenAi(interactive),
                    "search" => DoInitSearch(interactive),

                    _ => throw new ApplicationException($"WARNING: NOT YET IMPLEMENTED")
                };
                await task;
            }
        }

        private Task<string> DoInitSubscriptionId(bool interactive)
        {
            return Program.Telemetry.WrapAsync(
                async () =>
                {
                    var subscriptionFilter = SubscriptionToken.Data().GetOrDefault(_values, "");
                    string subscriptionId = await AzCliConsoleGui.PickSubscriptionIdAsync(interactive, interactive, subscriptionFilter);
                    SubscriptionToken.Data().Set(_values, subscriptionId);
                    return subscriptionId;
                },
                (outcome, subscriptionId, ex, timeTaken) => new InitTelemetryEvent(InitStage.Subscription)
                {
                    Outcome = outcome,
                    //Selected = subscriptionId, // TODO PRIVACY REVIEW: can include this?
                    RunId = _values.GetOrDefault("telemetry.init.run_id", null),
                    RunType = _values.GetOrDefault("telemetry.init.run_type", null),
                    DurationInMs = timeTaken.TotalMilliseconds,
                    Error = ex?.Message
                });
        }

        private async Task DoInitRootHubResource(bool interactive)
        {
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // POST-IGNITE: TODO: Add back non-interactive mode support

            await DoInitSubscriptionId(interactive);
            await DoInitHubResource(interactive);
        }

        private Task DoInitHubResource(bool interactive)
        {
            return Program.Telemetry.WrapAsync(
                () =>
                {
                    var subscription = SubscriptionToken.Data().GetOrDefault(_values, "");
                    return AiSdkConsoleGui.PickOrCreateAiHubResource(_values, subscription);
                },
                (outcome, res, ex, timeTaken) => new InitTelemetryEvent(InitStage.Resource)
                {
                    Outcome = outcome,
                    RunId = _values.GetOrDefault("telemetry.init.run_id", null),
                    RunType = _values.GetOrDefault("telemetry.init.run_type", null),
                    Selected = res.Item2 ? "new" : "existing",
                    DurationInMs = timeTaken.TotalMilliseconds,
                    Error = ex?.Message
                });
        }

        private async Task DoInitRootProject(bool interactive, bool allowCreate = true, bool allowPick = true)
        {
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // POST-IGNITE: TODO: Add back non-interactive mode support

            await DoInitSubscriptionId(interactive);
            await DoInitProject(interactive, allowCreate, allowPick);
        }

        private async Task DoInitProject(bool interactive, bool allowCreate = true, bool allowPick = true, bool allowSkipSearch = true)
        {
            if (allowCreate)
            {
                await DoInitHubResource(interactive);
            }

            var subscription = SubscriptionToken.Data().GetOrDefault(_values, "");
            var resourceId = ResourceIdToken.Data().GetOrDefault(_values, null);
            var groupName = ResourceGroupNameToken.Data().GetOrDefault(_values);

            var openAiEndpoint = _values.GetOrDefault("service.openai.endpoint", null);
            var openAiKey = _values.GetOrDefault("service.openai.key", null);
            var searchEndpoint = _values.GetOrDefault("service.search.endpoint", null);
            var searchKey = _values.GetOrDefault("service.search.key", null);

            // Special case for "existing" AI project. The UI skips presenting the user with a choice of AI hub resource
            // since they directly choose a project. So we add back this missing telemetry event here
            if (!allowCreate && allowPick)
            {
                Program.Telemetry.LogEvent(new InitTelemetryEvent(InitStage.Resource)
                {
                    Outcome = Outcome.Success,
                    Selected = "project",
                    DurationInMs = 0,
                    RunId = _values.GetOrDefault("telemetry.init.run_id", null),
                    RunType = _values.GetOrDefault("telemetry.init.run_type", null)
                });
            }

            bool createdNew = false;
            AiHubProjectInfo project = Program.Telemetry.Wrap(
                () => AiSdkConsoleGui.PickOrCreateAiHubProject(allowCreate, allowPick, _values, subscription, resourceId, out createdNew),
                (outcome, project, ex, timeTaken) => new InitTelemetryEvent(InitStage.Project)
                {
                    Outcome = outcome,
                    Selected = createdNew ? "new" : "existing",
                    RunId = _values.GetOrDefault("telemetry.init.run_id", null),
                    RunType = _values.GetOrDefault("telemetry.init.run_type", null),
                    DurationInMs = timeTaken.TotalMilliseconds,
                    Error = ex?.Message
                });

            // TODO FIXME: There was a bug in the what used to one method call that was split into two. Namely when allowCreate == true and allowPick == false,
            //             createdNew would not be correctly to true. The ConfigAiHubProject relies on this bug so restore this broken behaviour here.
            //             This will be fixed in a future re-factor/simplification of this code
            if (allowCreate && !allowPick)
            {
                createdNew = false;
            }

            await AiSdkConsoleGui.ConfigAiHubProject(_values, project, createdNew, allowSkipSearch, subscription, resourceId, groupName, openAiEndpoint, openAiKey, searchEndpoint, searchKey);

            ProjectNameToken.Data().Set(_values, project.Name);
            _values.Reset("service.project.id", project.Id);
        }

        private async Task DoInitRootOpenAi(bool interactive, bool skipChat = false, bool allowSkipChat = true, bool skipEmbeddings = false, bool allowSkipEmbeddings = true, bool skipEvaluations = false, bool allowSkipEvaluations = true)
        {
            // if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // POST-IGNITE: TODO: Add back non-interactive mode support

            await DoInitSubscriptionId(interactive);
            await DoInitOpenAi(interactive, skipChat, allowSkipChat, skipEmbeddings, allowSkipEmbeddings, skipEvaluations, allowSkipEvaluations);
        }

        private async Task DoInitOpenAi(bool interactive, bool skipChat = false, bool allowSkipChat = true, bool skipEmbeddings = false, bool allowSkipEmbeddings = true, bool skipEvaluations = false, bool allowSkipEvaluations = true)
        {
            var subscriptionId = SubscriptionToken.Data().GetOrDefault(_values, "");
            var regionFilter = _values.GetOrEmpty("init.service.resource.region.name");
            var groupFilter = _values.GetOrEmpty("init.service.resource.group.name");
            var resourceFilter = _values.GetOrEmpty("init.service.cognitiveservices.resource.name");
            var kind = _values.GetOrDefault("init.service.cognitiveservices.resource.kind", "OpenAI;AIServices");
            var sku = _values.GetOrDefault("init.service.cognitiveservices.resource.sku", Program.CognitiveServiceResourceSku);
            var yes = _values.GetOrDefault("init.service.cognitiveservices.terms.agree", false);

            var chatDeploymentFilter = _values.GetOrEmpty("init.chat.model.deployment.name");
            var embeddingsDeploymentFilter = _values.GetOrEmpty("init.embeddings.model.deployment.name");
            var evaluationsDeploymentFilter = _values.GetOrEmpty("init.evaluation.model.deployment.name");

            var chatModelFilter = _values.GetOrEmpty("init.chat.model.name");
            var embeddingsModelFilter = _values.GetOrEmpty("init.embeddings.model.name");
            var evaluationsModelFilter = _values.GetOrEmpty("init.evaluation.model.name");
            
            var resource = await AzCliConsoleGui.PickOrCreateAndConfigCognitiveServicesOpenAiKindResource(_values, interactive, subscriptionId, regionFilter, groupFilter, resourceFilter, kind, sku, yes, skipChat, allowSkipChat, skipEmbeddings, allowSkipEmbeddings, skipEvaluations, allowSkipEvaluations, chatDeploymentFilter, embeddingsDeploymentFilter, evaluationsDeploymentFilter, chatModelFilter, embeddingsModelFilter, evaluationsModelFilter);
            _values.Reset("service.openai.deployments.picked", "true");

            SubscriptionToken.Data().Set(_values, subscriptionId);
            _values.Reset("service.resource.region.name", resource.RegionLocation);
            _values.Reset("service.openai.endpoint", resource.Endpoint);
            _values.Reset("service.openai.key", resource.Key);
            _values.Reset("service.openai.resource.id", resource.Id);
            _values.Reset("service.openai.resource.kind", resource.Kind);
            ResourceNameToken.Data().Set(_values, resource.Name);
            ResourceGroupNameToken.Data().Set(_values, resource.Group);
        }

        private async Task DoInitRootCognitiveServicesAIServicesKind(bool interactive)
        {
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // POST-IGNITE: TODO: Add back non-interactive mode support

            await DoInitSubscriptionId(interactive);
            await DoInitCognitiveServicesAIServicesKind(interactive);
        }

        private async Task DoInitCognitiveServicesAIServicesKind(bool interactive)
        {
            var subscriptionId = SubscriptionToken.Data().GetOrDefault(_values);
            var regionFilter = _values.GetOrEmpty("init.service.resource.region.name");
            var groupFilter = _values.GetOrEmpty("init.service.resource.group.name");
            var resourceFilter = _values.GetOrEmpty("init.service.cognitiveservices.resource.name");
            var kind = _values.GetOrDefault("init.service.cognitiveservices.resource.kind", "AIServices");
            var sku = _values.GetOrDefault("init.service.cognitiveservices.resource.sku", Program.CognitiveServiceResourceSku);
            var yes = _values.GetOrDefault("init.service.cognitiveservices.terms.agree", false);

            var resource = await AzCliConsoleGui.PickOrCreateAndConfigCognitiveServicesAiServicesKindResource(_values, interactive, subscriptionId, regionFilter, groupFilter, resourceFilter, kind, sku, yes);

            SubscriptionToken.Data().Set(_values, subscriptionId);
            _values.Reset("service.resource.region.name", resource.RegionLocation);
            _values.Reset("service.openai.endpoint", resource.Endpoint);
            _values.Reset("service.openai.key", resource.Key);
            _values.Reset("service.openai.resource.id", resource.Id);
            _values.Reset("service.openai.resource.kind", resource.Kind);
            ResourceNameToken.Data().Set(_values, resource.Name);
            ResourceGroupNameToken.Data().Set(_values, resource.Group);
        }

        private async Task DoInitRootCognitiveServicesCognitiveServicesKind(bool interactive)
        {
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // POST-IGNITE: TODO: Add back non-interactive mode support

            await DoInitSubscriptionId(interactive);
            await DoInitCognitiveServicesCognitiveServicesKind(interactive);
        }

        private async Task DoInitCognitiveServicesCognitiveServicesKind(bool interactive)
        {
            var subscriptionId = SubscriptionToken.Data().GetOrDefault(_values, "");
            var regionFilter = _values.GetOrEmpty("init.service.resource.region.name");
            var groupFilter = _values.GetOrEmpty("init.service.resource.group.name");
            var resourceFilter = _values.GetOrEmpty("init.service.cognitiveservices.resource.name");
            var kind = _values.GetOrDefault("init.service.cognitiveservices.resource.kind", "CognitiveServices");
            var sku = _values.GetOrDefault("init.service.cognitiveservices.resource.sku", Program.CognitiveServiceResourceSku);
            var yes = _values.GetOrDefault("init.service.cognitiveservices.terms.agree", false);

            var resource = await AzCliConsoleGui.PickOrCreateAndConfigCognitiveServicesCognitiveServicesKindResource(interactive, subscriptionId, regionFilter, groupFilter, resourceFilter, kind, sku, yes);

            SubscriptionToken.Data().Set(_values, subscriptionId);
            _values.Reset("services.region", resource.RegionLocation);
            _values.Reset("services.endpoint", resource.Endpoint);
            _values.Reset("services.key", resource.Key);
            ResourceNameToken.Data().Set(_values, resource.Name);
            ResourceGroupNameToken.Data().Set(_values, resource.Group);
        }

        private async Task DoInitRootSearch(bool interactive)
        {
            if (!interactive) ThrowInteractiveNotSupportedApplicationException(); // POST-IGNITE: TODO: Add back non-interactive mode support

            await DoInitSubscriptionId(interactive);
            await DoInitSearch(interactive, false);
        }

        private async Task DoInitSearch(bool interactive, bool allowSkipSearch = true)
        {
            var subscription = SubscriptionToken.Data().GetOrDefault(_values, "");
            var location = _values.GetOrEmpty("service.resource.region.name");
            var groupName = ResourceGroupNameToken.Data().GetOrDefault(_values, "");

            var smartName = ResourceNameToken.Data().GetOrDefault(_values);
            var smartNameKind = smartName != null && smartName.Contains("openai") ? "openai" : "oai";

            var resource = await AzCliConsoleGui.PickOrCreateAndConfigCogSearchResource(allowSkipSearch, subscription, location, groupName, smartName, smartNameKind);

            _values.Reset("service.search.endpoint", resource?.Endpoint);
            _values.Reset("service.search.key", resource?.Key);
        }

        private async Task DoInitRootSpeech(bool interactive)
        {
            await DoInitSubscriptionId(interactive);
            await DoInitSpeech(interactive);
        }
        private async Task DoInitRootVision(bool interactive)
        {
            await DoInitSubscriptionId(interactive);
            await DoInitVision(interactive);
        }

        private async Task DoInitSpeech(bool interactive)
        {
            var subscriptionId = SubscriptionToken.Data().GetOrDefault(_values, "");
            var regionFilter = _values.GetOrEmpty("init.service.resource.region.name");
            var groupFilter = _values.GetOrEmpty("init.service.resource.group.name");
            var resourceFilter = _values.GetOrEmpty("init.service.cognitiveservices.resource.name");
            var kind = _values.GetOrDefault("init.service.cognitiveservices.resource.kind", "SpeechServices");
            var sku = _values.GetOrDefault("init.service.cognitiveservices.resource.sku", "S0");
            var yes = _values.GetOrDefault("init.service.cognitiveservices.terms.agree", false);

            var resource = await AzCliConsoleGui.PickOrCreateAndConfigCognitiveServicesSpeechServicesKindResource(interactive, subscriptionId, regionFilter, groupFilter, resourceFilter, kind, sku, yes);

            SubscriptionToken.Data().Set(_values, subscriptionId);
        }

        private async Task DoInitVision(bool interactive)
        {
            var subscriptionId = SubscriptionToken.Data().GetOrDefault(_values, "");
            var regionFilter = _values.GetOrEmpty("init.service.resource.region.name");
            var groupFilter = _values.GetOrEmpty("init.service.resource.group.name");
            var resourceFilter = _values.GetOrEmpty("init.service.cognitiveservices.resource.name");
            var kind = _values.GetOrDefault("init.service.cognitiveservices.resource.kind", "ComputerVision");
            var sku = _values.GetOrDefault("init.service.cognitiveservices.resource.sku", "S0");
            var yes = _values.GetOrDefault("init.service.cognitiveservices.terms.agree", false);

            var resource = await AzCliConsoleGui.PickOrCreateAndConfigCognitiveServicesComputerVisionKindResource(interactive, subscriptionId, regionFilter, groupFilter, resourceFilter, kind, sku, yes);

            SubscriptionToken.Data().Set(_values, subscriptionId);
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
