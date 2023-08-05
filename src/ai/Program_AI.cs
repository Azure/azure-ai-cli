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
        #endregion

        #region help command data
        public const string HelpCommandTokens = "init;config;chat;complete;wizard;run";
        #endregion

        #region config command data
        public static string ConfigScopeTokens = $"init;chat;eval;speech;vision;language;search;service;tool;wizard;run;*";
        #endregion

        #region zip option data
        public static string[] ZipIncludes = 
        {
            // "LICENSE.txt",
            // "THIRD_PARTY_NOTICE.txt",
            "Azure.Core.dll",
            "Azure.Identity.dll",
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
                "complete" => (new CompleteCommand(values)).RunCommand(),
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
                "speech" => SpeechCommandParser.ParseCommand(tokens, values),
                "vision" => VisionCommandParser.ParseCommand(tokens, values),
                "complete" => CompleteCommandParser.ParseCommand(tokens, values),
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
                "speech" => SpeechCommandParser.ParseCommandValues(tokens, values),
                "vision" => VisionCommandParser.ParseCommandValues(tokens, values),
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
