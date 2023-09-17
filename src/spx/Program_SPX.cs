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
using Microsoft.CognitiveServices.Speech;

namespace Azure.AI.Details.Common.CLI
{
    public partial class Program
    {
        #region name data
        public const string Name = "spx";
        public const string DisplayName = "Azure Speech CLI";
        public const string WarningBanner = null;
        #endregion

        #region assembly data
        public const string Exe = "spx.exe";
        public const string Dll = "spx.dll";
        public static Type ResourceAssemblyType = typeof(Program);
        public static Type BindingAssemblySdkType = typeof(SpeechConfig);
        #endregion

        #region init command data
        public const string SERVICE_RESOURCE_DISPLAY_NAME_ALL_CAPS = "SPEECH RESOURCE";
        public const string CognitiveServiceResourceKind = "SpeechServices";
        public const string CognitiveServiceResourceSku = "S0";
        public static bool InitConfigsEndpoint = false;
        public static bool InitConfigsSubscription = false;
        #endregion

        #region help command data
        public const string HelpCommandTokens = "init;config;csr;batch;synthesize;recognize;dialog;intent;transcribe;translate;speaker;profile;webjob;run";
        #endregion

        #region config command data
        public static string ConfigScopeTokens = $"init;csr;batch;synthesize;recognize;dialog;intent;transcribe;translate;speaker;profile;webjob;run;*";
        #endregion

        #region zip option data
        public static string[] ZipIncludes = 
        {
            "LICENSE.txt",
            "THIRD_PARTY_NOTICE.txt",
            "Microsoft.CognitiveServices.Speech.core.dll",
            "Microsoft.CognitiveServices.Speech.extension.audio.sys.dll",
            "Microsoft.CognitiveServices.Speech.extension.kws.dll",
            "Microsoft.CognitiveServices.Speech.extension.kws.ort.dll",
            "Microsoft.CognitiveServices.Speech.extension.lu.dll",
            "Microsoft.CognitiveServices.Speech.extension.codec.dll",
            "Microsoft.CognitiveServices.Speech.extension.silk_codec.dll"
        };
        #endregion

        public static bool DispatchRunCommand(ICommandValues values)
        {
            var command = values.GetCommand();
            var root = command.Split('.').FirstOrDefault();

            return root switch {
                "init" => (new InitCommand(values)).RunCommand(),
                "csr" => (new CustomSpeechRecognitionCommand(values)).RunCommand(),
                "batch" => (new BatchCommand(values)).RunCommand(),
                "synthesize" => (new SynthesizeCommand(values)).RunCommand(),
                "recognize" => (new RecognizeCommand(values)).RunCommand(),
                "dialog" => (new DialogCommand(values)).RunCommand(),
                "speaker" => (new ProfileCommand(values)).RunCommand(),
                "profile" => (new ProfileCommand(values)).RunCommand(),
                "intent" => (new IntentCommand(values)).RunCommand(),
                "transcribe" => (new TranscribeConversationCommand(values)).RunCommand(),
                "translate" => (new TranslateCommand(values)).RunCommand(),
                "webjob" => (new WebJobCommand(values)).RunCommand(),
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
                "csr" => CustomSpeechRecognitionCommandParser.ParseCommand(tokens, values),
                "batch" => BatchCommandParser.ParseCommand(tokens, values),
                "synthesize" => SynthesizeCommandParser.ParseCommand(tokens, values),
                "recognize" => RecognizeCommandParser.ParseCommand(tokens, values),
                "dialog" => DialogCommandParser.ParseCommand(tokens, values),
                "intent" => IntentCommandParser.ParseCommand(tokens, values),
                "speaker" => ProfileCommandParser.ParseCommand(tokens, values),
                "profile" => ProfileCommandParser.ParseCommand(tokens, values),
                "transcribe" => TranscribeConversationCommandParser.ParseCommand(tokens, values),
                "translate" => TranslateCommandParser.ParseCommand(tokens, values),
                "webjob" => WebJobCommandParser.ParseCommand(tokens, values),
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
                "csr" => CustomSpeechRecognitionCommandParser.ParseCommandValues(tokens, values),
                "batch" => BatchCommandParser.ParseCommandValues(tokens, values),
                "synthesize" => SynthesizeCommandParser.ParseCommandValues(tokens, values),
                "recognize" => RecognizeCommandParser.ParseCommandValues(tokens, values),
                "dialog" => DialogCommandParser.ParseCommandValues(tokens, values),
                "intent" => IntentCommandParser.ParseCommandValues(tokens, values),
                "speaker" => ProfileCommandParser.ParseCommandValues(tokens, values),
                "profile" => ProfileCommandParser.ParseCommandValues(tokens, values),
                "transcribe" => TranscribeConversationCommandParser.ParseCommandValues(tokens, values),
                "translate" => TranslateCommandParser.ParseCommandValues(tokens, values),
                "webjob" => WebJobCommandParser.ParseCommandValues(tokens, values),
                "run" => RunJobCommandParser.ParseCommandValues(tokens, values),
                _ => false
            };
        }

        public static bool DisplayKnownErrors(ICommandValues values, Exception ex)
        {
            if (ex.Message.Contains("SPXERR_INVALID_HEADER"))
            {
                var file = values.GetOrDefault("audio.input.file", "audio input");

                ErrorHelpers.WriteLineMessage(
                       "ERROR:", $"Invalid file format: \"{file}\"",
                         "TRY:", $"{Program.Name} {values.GetCommandForDisplay()} --file \"{file}\" --format ANY",
                    "SEE ALSO:", $"{Program.Name} help find topic format",
                                 $"{Program.Name} help find topic input",
                                 $"{Program.Name} help documentation\n");

                values.Reset("x.verbose", "false");
                return true;
            }
            else if (ex.Message.Contains("SPXERR_MIC_NOT_AVAILABLE"))
            {
                ErrorHelpers.WriteLineMessage(
                       "ERROR:", $"Could not access or open the microphone (SPXERR_MIC_NOT_AVAILABLE)", "",
                        "NOTE:", $"Ensure the microphone is plugged in, working properly, and permissions enabled.");

                values.Reset("x.verbose", "false");
                return true;
            }
            else if (ex.Message.Contains("SPXERR_MIC_ERROR"))
            {
                ErrorHelpers.WriteLineMessage(
                       "ERROR:", $"Could not open or read from the microphone (SPXERR_MIC_ERROR)", "",
                        "NOTE:", $"Ensure the microphone is plugged in and working properly.");

                values.Reset("x.verbose", "false");
                return true;
            }
            return false;
        }
    }
}
