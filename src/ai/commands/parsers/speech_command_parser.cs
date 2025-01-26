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
            ("speech.transcribe", true),
            ("speech.translate", true),
            ("speech.batch.list", true),
            ("speech.batch.download", true),
            ("speech.batch.transcription.create", true),
            ("speech.batch.transcription.update", true),
            ("speech.batch.transcription.delete", true),
            ("speech.batch.transcription.status", true),
            ("speech.batch.transcription.download", true),
            ("speech.batch.transcription.list", false),
            ("speech.batch.transcription.onprem.create", true),
            ("speech.batch.transcription.onprem.status", true),
            ("speech.batch.transcription.onprem.delete", true),
            ("speech.batch.transcription.onprem.endpoints", true),
            ("speech.batch.transcription.onprem.list", false),
            ("speech.batch.transcription", true),
            ("speech.batch", true),
            ("speech.csr.list", true),
            ("speech.csr.download", true),                          
            ("speech.csr.project.create", true),
            ("speech.csr.project.update", true),
            ("speech.csr.project.delete", true),
            ("speech.csr.project.status", true),
            ("speech.csr.project.list", false),
            ("speech.csr.project", true),
            ("speech.csr.dataset.create", true),
            ("speech.csr.dataset.upload", true),
            ("speech.csr.dataset.update", true),
            ("speech.csr.dataset.delete", true),
            ("speech.csr.dataset.list", false),
            ("speech.csr.dataset.status", true),
            ("speech.csr.dataset.download", true),
            ("speech.csr.dataset", true),
            ("speech.csr.model.create", true),
            ("speech.csr.model.update", true),
            ("speech.csr.model.delete", true),
            ("speech.csr.model.list", false),
            ("speech.csr.model.status", true),
            ("speech.csr.model.download", true),
            ("speech.csr.model.copy", true),
            ("speech.csr.model", true),
            ("speech.csr.evaluation.create", true),
            ("speech.csr.evaluation.list", false),
            ("speech.csr.evaluation.show", true),
            ("speech.csr.evaluation.status", true),
            ("speech.csr.evaluation.delete", true),
            ("speech.csr.evaluation", true),
            ("speech.csr.endpoint.create", true),
            ("speech.csr.endpoint.update", true),
            ("speech.csr.endpoint.delete", true),
            ("speech.csr.endpoint.list", false),
            ("speech.csr.endpoint.status", true),
            ("speech.csr.endpoint.download", true),
            ("speech.csr.endpoint", true),
            ("speech.csr", true),
            ("speech.profile.list", false),
            ("speech.profile.create", true),
            ("speech.profile.status", true),
            ("speech.profile.enroll", true),
            ("speech.profile.delete", true),
            ("speech.profile", true),
            ("speech.speaker.identify", true),
            ("speech.speaker.verify", true),
            ("speech.speaker", true)
        };

        private static readonly string[] _partialCommands = {
            "speech.batch.transcription",
            "speech.batch",
            "speech.csr.project",
            "speech.csr.dataset",
            "speech.csr.model",
            "speech.csr.evaluation",
            "speech.csr.endpoint",
            "speech.csr",
            "speech.profile",
            "speech.speaker",
            "speech"
        };

        private static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var check = string.Join(".", values.GetCommand()
                .Split('.')
                .Take(2)
                .ToArray());

            switch (check)
            {
                case "speech.recognize": return RecognizeCommandParser.GetCommandParsers();
                case "speech.synthesize": return SynthesizeCommandParser.GetCommandParsers();
                case "speech.intent": return IntentCommandParser.GetCommandParsers();
                case "speech.transcribe": return TranscribeCommandParser.GetCommandParsers();
                case "speech.translate": return TranslateCommandParser.GetCommandParsers();
                case "speech.batch": return BatchCommandParser.GetCommandParsers(values);
                case "speech.csr": return CustomSpeechRecognitionCommandParser.GetCommandParsers(values);
                case "speech.profile": return ProfileCommandParser.GetCommandParsers(values);
                case "speech.speaker": return ProfileCommandParser.GetCommandParsers(values);
            }

            return null;
        }

        #region private data

        public class CommonSpeechNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonSpeechNamedValueTokenParsers() : base(

                    new Any1ValueNamedValueTokenParser(null, "x.command", "11"),

                    new CommonNamedValueTokenParsers(),
                    new IniFileNamedValueTokenParser(),
                    new ExpandFileNameNamedValueTokenParser(),

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