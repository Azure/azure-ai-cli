//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Azure.AI.CLI.Clients.AzPython;
using Azure.AI.CLI.Common.Clients;
using Azure.AI.Details.Common.CLI.Telemetry;
using Azure.AI.Details.Common.CLI.Telemetry.Events;

namespace Azure.AI.Details.Common.CLI
{
    public class AiProgram
    {
        static async Task<int> Main(string[] args)
        {
            IProgramData data = null;
            Stopwatch stopwatch = new Stopwatch();
            int exitCode = int.MinValue;

            try
            {
                bool isDebug = args.Length > 0 && args[0] == "debug";
                if (isDebug)
                {
                    Console.WriteLine($"StopWatch: Started at {DateTime.Now}");
                }

                stopwatch.Start();

                data = new AiProgramData();
                exitCode = Program.Main(data, args);
                stopwatch.Stop();

                if (isDebug)
                {
                    Console.WriteLine($"StopWatch: Stopped at {DateTime.Now} ({GetStopWatchElapsedAsString(stopwatch.Elapsed)})");
                }

                return exitCode;
            }
            catch (Exception)
            {
                exitCode = -1;
                throw;
            }
            finally
            {
                if (data?.Telemetry != null)
                {
                    data.Telemetry.LogEvent(new ExitedTelemetryEvent()
                    {
                        ExitCode = exitCode,
                        Elapsed = stopwatch.Elapsed
                    });

                    await data.Telemetry.DisposeAsync()
                        .ConfigureAwait(false);
                }
            }
        }

        static string GetStopWatchElapsedAsString(TimeSpan elapsed)
        {
            var elapsedMilliseconds = elapsed.TotalMilliseconds;
            var elapsedSeconds = elapsed.TotalSeconds;
            var elapsedMinutes = elapsed.TotalMinutes;
            var elapsedHours = elapsed.TotalHours;

            var elapsedString = elapsedSeconds < 1 ? $"{elapsedMilliseconds} ms"
                              : elapsedMinutes < 1 ? $"{elapsedSeconds:0.00} sec"
                              : elapsedHours < 1 ? $"{elapsedMinutes:0.00} min"
                              : $"{elapsedHours:0.00} hr";

            return elapsedString;
        }
    }

    public class AiProgramData : IProgramData
    {
        private readonly Lazy<ITelemetry> _telemetry;

        public AiProgramData()
        {
            _telemetry = new Lazy<ITelemetry>(
                () => TelemetryHelpers.InstantiateFromConfig(this),
                System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

            LoginManager = new AzConsoleLoginManager(TelemetryUserAgent);
            var azCliClient = new AzCliClient(
                LoginManager,
                () => Values?.GetOrDefault("init.service.interactive", true) ?? true,
                TelemetryUserAgent);

            SubscriptionsClient = azCliClient;
            CognitiveServicesClient = azCliClient;
            SearchClient = azCliClient;
        }

        #region name data
        public string Name => "ai";
        public string DisplayName => "Azure AI CLI";
        public string WarningBanner => "`#e_;This PUBLIC PREVIEW version may change at any time.\nSee: https://aka.ms/azure-ai-cli-public-preview";
        public string TelemetryUserAgent => "ai-cli 0.0.1";
        #endregion

        #region assembly data
        public string Exe => "ai.exe";
        public string Dll => "ai.dll";
        public Type ResourceAssemblyType => typeof(AiProgramData);
        public Type BindingAssemblySdkType => typeof(AiProgramData);
        #endregion

        #region init command data
        public string SERVICE_RESOURCE_DISPLAY_NAME_ALL_CAPS => "AZURE OPENAI RESOURCE";
        public string CognitiveServiceResourceKind => "OpenAI";
        public string CognitiveServiceResourceSku => "s0";
        public bool InitConfigsEndpoint => true;
        public bool InitConfigsSubscription => true;
        #endregion

        #region help command data
        public string HelpCommandTokens => "wizard;dev;test;init;config;chat;speech;vision;language;search;service;tool;samples;eval;run";
        #endregion

        #region config command data
        public string ConfigScopeTokens => $"wizard;dev;test;init;chat;speech;vision;language;search;service;tool;samples;eval;run;*";
        #endregion

        #region zip option data
        public string[] ZipIncludes => new string[]
        {
            "LICENSE.txt",
            // "THIRD_PARTY_NOTICE.txt",
            "Azure.Core.dll",
            "Azure.Identity.dll",
            "Azure.AI.CLI.Common.dll",
            "Azure.AI.CLI.Extensions.HelperFunctions.dll",
            "Azure.AI.CLI.Extensions.Templates.dll",
            "Azure.AI.OpenAI.dll",
            "Azure.Core.dll",
            "Azure.Search.Documents.dll",
            "JmePath.Net.Parser.dll",
            "JmesPath.Net.dll",
            "Microsoft.Bcl.AsyncInterfaces.dll",
            "Microsoft.Bcl.HashCode.dll",
            "Microsoft.CognitiveServices.Speech.core.dll",
            "Microsoft.CognitiveServices.Speech.csharp.dll",
            "Microsoft.CognitiveServices.Speech.extension.audio.sys.dll",
            "Microsoft.CognitiveServices.Speech.extension.kws.dll",
            "Microsoft.CognitiveServices.Speech.extension.kws.ort.dll",
            "Microsoft.CognitiveServices.Speech.extension.lu.dll",
            "Microsoft.CognitiveServices.Speech.extension.codec.dll",
            "Microsoft.CognitiveServices.Speech.extension.silk_codec.dll",
            "Microsoft.Extensions.Logging.Abstractions.dll",
            "Microsoft.SemanticKernel.Abstractions.dll",
            "Microsoft.SemanticKernel.Connectors.AI.OpenAI.dll",
            "Microsoft.SemanticKernel.Connectors.Memory.AzureCognitiveSearch.dll",
            "Microsoft.SemanticKernel.Core.dll",
            "Microsoft.SemanticKernel.dll",
            "Microsoft.SemanticKernel.Planning.ActionPlanner.dll",
            "Microsoft.SemanticKernel.Planning.SequentialPlanner.dll",
            "Microsoft.SemanticKernel.Skills.Core.dll",
            "System.Diagnostics.DiagnosticSource.dll",
            "System.Interactive.Async.dll",
            "System.Linq.Async.dll",
            "System.Memory.Data.dll",
            "System.Text.Json.dll"
        };
        #endregion

        public bool DispatchRunCommand(ICommandValues values)
        {
            var command = values.GetCommand();
            var root = command.Split('.').FirstOrDefault();

            return root switch {
                "init" => (new InitCommand(values)).RunCommand(),
                "chat" => (new ChatCommand(values)).RunCommand(),
                "speech" => (new SpeechCommand(values)).RunCommand(),
                "vision" => (new VisionCommand(values)).RunCommand(),
                "language" => (new LanguageCommand(values)).RunCommand(),
                "search" => (new SearchCommand(values)).RunCommand(),
                "service" => (new ServiceCommand(values)).RunCommand(),
                "tool" => (new ToolCommand(values)).RunCommand(),
                "eval" => (new EvalCommand(values)).RunCommand(),
                "wizard" => (new ScenarioWizardCommand(values)).RunCommand(),
                "dev" => (new DevCommand(values)).RunCommand(),
                "test" => (new TestCommand(values)).RunCommand(),
                "run" => (new RunJobCommand(values)).RunCommand(),
                "version" => (new VersionCommand(values)).RunCommand(),
                "update" => (new VersionCommand(values)).RunCommand(),
                _ => false
            };
        }

        public bool DispatchParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            var command = values.GetCommand(null) ?? tokens.PeekNextToken();
            var root = command.Split('.').FirstOrDefault();

            return root switch
            {
                "help" => HelpCommandParser.ParseCommand(tokens, values),
                "init" => InitCommandParser.ParseCommand(tokens, values),
                "config" => ConfigCommandParser.ParseCommand(tokens, values),
                "chat" => ChatCommandParser.ParseCommand(tokens, values),
                "eval" => EvalCommandParser.ParseCommand(tokens, values),
                "speech" => SpeechCommandParser.ParseCommand(tokens, values),
                "vision" => VisionCommandParser.ParseCommand(tokens, values),
                "language" => LanguageCommandParser.ParseCommand(tokens, values),
                "search" => SearchCommandParser.ParseCommand(tokens, values),
                "service" => ServiceCommandParser.ParseCommand(tokens, values),
                "tool" => ToolCommandParser.ParseCommand(tokens, values),
                "wizard" => ScenarioWizardCommandParser.ParseCommand(tokens, values),
                "version" => VersionCommandParser.ParseCommand(tokens, values),
                "update" => UpdateCommandParser.ParseCommand(tokens, values),
                "dev" => DevCommandParser.ParseCommand(tokens, values),
                "test" => TestCommandParser.ParseCommand(tokens, values),
                "run" => RunJobCommandParser.ParseCommand(tokens, values),
                _ => false
            };
        }

        public bool DispatchParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            var root = values.GetCommandRoot();
            return root switch
            {
                "init" => InitCommandParser.ParseCommandValues(tokens, values),
                "config" => ConfigCommandParser.ParseCommandValues(tokens, values),
                "chat" => ChatCommandParser.ParseCommandValues(tokens, values),
                "eval" => EvalCommandParser.ParseCommandValues(tokens, values),
                "speech" => SpeechCommandParser.ParseCommandValues(tokens, values),
                "vision" => VisionCommandParser.ParseCommandValues(tokens, values),
                "language" => LanguageCommandParser.ParseCommandValues(tokens, values),
                "search" => SearchCommandParser.ParseCommandValues(tokens, values),
                "service" => ServiceCommandParser.ParseCommandValues(tokens, values),
                "tool" => ToolCommandParser.ParseCommandValues(tokens, values),
                "wizard" => ScenarioWizardCommandParser.ParseCommandValues(tokens, values),
                "dev" => DevCommandParser.ParseCommandValues(tokens, values),
                "test" => TestCommandParser.ParseCommandValues(tokens, values),
                "run" => RunJobCommandParser.ParseCommandValues(tokens, values),
                "version" => VersionCommandParser.ParseCommandValues(tokens, values),
                "update" => UpdateCommandParser.ParseCommandValues(tokens, values),
                _ => false
            };
        }

        public bool DisplayKnownErrors(ICommandValues values, Exception ex)
        {
            var message = ex.Message;

            if (message.Contains("az login"))
            {
                ErrorHelpers.WriteLineMessage(
                       "ERROR:", $"Azure CLI credential not found.",
                                 "",
                         "TRY:", $"az login",
                                 "",
                         "SEE:", "https://docs.microsoft.com/cli/azure/authenticate-azure-cli");

                values.Reset("x.verbose", "false");
                return true;
            }

            if (message.Contains("refresh token") && message.Contains("expired"))
            {
                ErrorHelpers.WriteLineMessage(
                       "ERROR:", $"Refresh token expired.",
                                 "",
                         "TRY:", $"az login");
                values.Reset("x.verbose", "false");
                return true;
            }

            return false;
        }

        public IEventLoggerHelpers EventLoggerHelpers => new AiEventLoggerHelpers();
        public ITelemetry Telemetry => _telemetry.Value;

        public ILoginManager LoginManager { get; }
        public ISubscriptionsClient SubscriptionsClient { get; }
        public ICognitiveServicesClient CognitiveServicesClient { get; }
        public ISearchClient SearchClient { get; }
        public ICommandValues Values { get; } = new CommandValues();
    }
}
