//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AI.Details.Common.CLI
{
    public class SpeechCommand : Command
    {
        internal SpeechCommand(ICommandValues values) : base(values)
        {
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", true);
        }

        internal bool RunCommand()
        {
            try
            {
                RunSpeechCommand();
            }
            catch (WebException ex)
            {
                ConsoleHelpers.WriteLineError($"\n  ERROR: {ex.Message}");
                JsonHelpers.PrintJson(HttpHelpers.ReadWriteJson(ex.Response, _values, "speech"));
            }

            return _values.GetOrDefault("passed", true);
        }

        private bool RunSpeechCommand()
        {
            DoCommand(_values.GetCommand());
            return _values.GetOrDefault("passed", true);
        }

        private void DoCommand(string command)
        {
            CheckPath();

            var check = string.Join(".", command
                .Split('.')
                .Take(2)
                .ToArray());

            switch (check)
            {
                case "speech.synthesize":
                    new SynthesizeCommand(_values).RunCommand();
                    break;

                case "speech.recognize":
                    new RecognizeCommand(_values).RunCommand();
                    break;

                case "speech.intent":
                    new IntentCommand(_values).RunCommand();
                    break;

                case "speech.translate":
                    new TranslateCommand(_values).RunCommand();
                    break;

                case "speech.batch":
                    new BatchCommand(_values).RunCommand();
                    break;

                case "speech.csr":
                    new CustomSpeechRecognitionCommand(_values).RunCommand();
                    break;

                case "speech.profile":
                case "speech.speaker":
                    new ProfileCommand(_values).RunCommand();
                    break;

                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
            }
        }

        private bool _quiet = false;
        private bool _verbose = false;
    }
}
