//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    class SpeechCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("speech", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("speech.recognize", true),
            ("speech.synthesize", true),
            ("speech.intent", true),
            ("speech.translate", true),
            ("speech.batch", true),
            ("speech.csr", true),
            ("speech.profile", true),
            ("speech.speaker", true),
        };

        private static readonly string[] _partialCommands = {
            "speech"
        };

        private static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();
            switch (commandName)
            {
                case "speech.recognize": return speechRecognizeParsers;
                case "speech.synthesize": return speechSynthesizeParsers;
                case "speech.intent": return speechIntentParsers;
                case "speech.translate": return speechTranslateParsers;
                case "speech.batch": return speechBatchParsers;
                case "speech.csr": return speechCsrParsers;
                case "speech.profile": return speechProfileParsers;
                case "speech.speaker": return speechSpeakerParsers;
            }

            return null;
        }

        #region private data

        public class CommonSpeechNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonSpeechNamedValueTokenParsers() : base(

                    new NamedValueTokenParser(null, "x.command", "11", "1"),

                    new CommonNamedValueTokenParsers(),
                    new NamedValueTokenParser(null, "x.command.expand.file.name", "11111", "1"),

                    new ExpectConsoleOutputTokenParser(),
                    new DiagnosticLogTokenParser()

                )
            {
            }
        }

        private static INamedValueTokenParser[] speechRecognizeParsers = {

            new CommonSpeechNamedValueTokenParsers()

        };

        private static INamedValueTokenParser[] speechSynthesizeParsers = {

            new CommonSpeechNamedValueTokenParsers()

        };

        private static INamedValueTokenParser[] speechIntentParsers = {

            new CommonSpeechNamedValueTokenParsers()

        };

        private static INamedValueTokenParser[] speechTranslateParsers = {

            new CommonSpeechNamedValueTokenParsers()

        };

        private static INamedValueTokenParser[] speechBatchParsers = {

            new CommonSpeechNamedValueTokenParsers()

        };

        private static INamedValueTokenParser[] speechCsrParsers = {

            new CommonSpeechNamedValueTokenParsers()

        };

        private static INamedValueTokenParser[] speechProfileParsers = {

            new CommonSpeechNamedValueTokenParsers()

        };

        private static INamedValueTokenParser[] speechSpeakerParsers = {

            new CommonSpeechNamedValueTokenParsers()

        };

        #endregion
    }
}
