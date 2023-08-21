//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
// using Azure.AI.OpenAI;

namespace Azure.AI.Details.Common.CLI
{
    public partial class Program
    {
        #region name data
        public const string Name = "ai";
        public const string DisplayName = "Azure AI CLI";
        public const string WarningBanner = "`#e_;This PRIVATE PREVIEW version may change at any time.\nSee: https://aka.ms/azure-ai-cli-private-preview";
        #endregion

        #region assembly data
        public const string Exe = "ai.exe";
        public const string Dll = "ai.dll";
        public static Type BindingAssemblySdkType = typeof(Program); // typeof(OpenAIClient);
        #endregion

        #region init command data
        public const string SERVICE_RESOURCE_DISPLAY_NAME_ALL_CAPS = "AI RESOURCE";
        public const string CognitiveServiceResourceKind = "OpenAI";
        public const string CognitiveServiceResourceSku = "s0";
        public static bool InitConfigsEndpoint = true;
        public static bool InitConfigsSubscription = true;
        #endregion

        #region help command data
        public const string HelpCommandTokens = "wizard;init;config;chat;speech;vision;language;search;service;tool;samples;code;eval;run";
        #endregion

        #region config command data
        public static string ConfigScopeTokens = $"wizard;init;chat;speech;vision;language;search;service;tool;samples;code;eval;run;*";
        #endregion

        #region zip option data
        public static string[] ZipIncludes = 
        {
            "LICENSE.txt",
            // "THIRD_PARTY_NOTICE.txt",
            "Azure.Core.dll",
            "Azure.Identity.dll",
            "Azure.AI.OpenAI.dll",
            "Azure.Core.dll",
            "Azure.Search.Documents.dll",
            "JmePath.Net.Parser.dll",
            "JmesPath.Net.dll",
            "Microsoft.Bcl.AsyncInterfaces.dll",
            "Microsoft.Bcl.HashCode.dll",
            "Microsoft.Extensions.Logging.Abstractions.dll",
            "Microsoft.SemanticKernel.Abstractions.dll",
            "Microsoft.SemanticKernel.Connectors.AI.OpenAI.dll",
            "Microsoft.SemanticKernel.Connectors.Memory.AzureCognitiveSearch.dll",
            "Microsoft.SemanticKernel.Core.dll",
            "Microsoft.SemanticKernel.dll",
            "Microsoft.SemanticKernel.Planning.ActionPlanner.dll",
            "Microsoft.SemanticKernel.Planning.SequentialPlanner.dll",
            "Microsoft.SemanticKernel.Skills.Core.dll",
            "Newtonsoft.Json.dll",
            "System.Diagnostics.DiagnosticSource.dll",
            "System.Interactive.Async.dll",
            "System.Linq.Async.dll",
            "System.Memory.Data.dll",
            "System.Text.Json.dll"
        };
        #endregion

        public static bool DispatchRunCommand(ICommandValues values)
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
                "samples" => (new SamplesCommand(values)).RunCommand(),
                "code" => (new CodeCommand(values)).RunCommand(),
                "eval" => (new EvalCommand(values)).RunCommand(),
                "wizard" => (new ScenarioWizardCommand(values)).RunCommand(),
                "run" => (new RunJobCommand(values)).RunCommand(),
                _ => false
            };
        }

        public static bool DispatchParseCommand(INamedValueTokens tokens, ICommandValues values)
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
                "samples" => SamplesCommandParser.ParseCommand(tokens, values),
                "code" => CodeCommandParser.ParseCommand(tokens, values),
                "wizard" => ScenarioWizardCommandParser.ParseCommand(tokens, values),
                "run" => RunJobCommandParser.ParseCommand(tokens, values),
                _ => false
            };
        }

        public static bool DispatchParseCommandValues(INamedValueTokens tokens, ICommandValues values)
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
                "samples" => SamplesCommandParser.ParseCommandValues(tokens, values),
                "code" => CodeCommandParser.ParseCommandValues(tokens, values),
                "complete" => CompleteCommandParser.ParseCommandValues(tokens, values),
                "wizard" => ScenarioWizardCommandParser.ParseCommandValues(tokens, values),
                "run" => RunJobCommandParser.ParseCommandValues(tokens, values),
                _ => false
            };
        }

        private static bool DisplayKnownErrors(ICommandValues values, Exception ex)
        {
            return false;
        }
    }
}
