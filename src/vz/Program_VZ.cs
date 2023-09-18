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

namespace Azure.AI.Details.Common.CLI
{
    public partial class Program
    {
        #region name data
        public const string Name = "vz";
        public const string DisplayName = "Azure Vision CLI";
        public const string WarningBanner = null;
        #endregion

        #region assembly data
        public const string Exe = "vz.exe";
        public const string Dll = "vz.dll";
        public static Type ResourceAssemblyType = typeof(Program);
        public static Type BindingAssemblySdkType = typeof(Program); // typeof(VisionServiceOptions);
        #endregion

        #region init command data
        public const string SERVICE_RESOURCE_DISPLAY_NAME_ALL_CAPS = "VISION RESOURCE";
        public const string CognitiveServiceResourceKind = "ComputerVision";
        public const string CognitiveServiceResourceSku = "S1";
        public static bool InitConfigsEndpoint = true;
        public static bool InitConfigsSubscription = false;
        #endregion

        #region help command data
        public const string HelpCommandTokens = "init;config;image;person;face;run";
        #endregion

        #region config command data
        public static string ConfigScopeTokens = $"init;image;person;face;run;*";
        #endregion

        #region zip option data
        public static string[] ZipIncludes = 
        {
            "LICENSE.txt",
            "THIRD_PARTY_NOTICE.txt",
            "Azure-AI-Vision-Native.dll",
            "Azure-AI-Vision-Extension-Image.dll",
            "Azure-AI-Vision-Input-Device.dll",
            "Azure-AI-Vision-Input-File.dll",
            "Azure.AI.Vision.dll",
            "Azure.Core.dll",
            "Azure.Identity.dll",
            "turbojpeg.dll",
            "Vision_Core.dll",
            "Vision_Media.dll",
            "Vision_Media_VideoIngester.dll",
        };
        #endregion

        public static bool DispatchRunCommand(ICommandValues values)
        {
            var command = values.GetCommand();
            var root = command.Split('.').FirstOrDefault();

            return root switch {
                "init" => (new InitCommand(values)).RunCommand(),
                "image" => (new ImageCommand(values)).RunCommand(),
                "face" => (new FaceCommand(values)).RunCommand(),
                "person" => (new PersonCommand(values)).RunCommand(),
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
                "image" => ImageCommandParser.ParseCommand(tokens, values),
                "face" => FaceCommandParser.ParseCommand(tokens, values),
                "person" => PersonCommandParser.ParseCommand(tokens, values),
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
                "image" => ImageCommandParser.ParseCommandValues(tokens, values),
                "face" => FaceCommandParser.ParseCommandValues(tokens, values),
                "person" => PersonCommandParser.ParseCommandValues(tokens, values),
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
