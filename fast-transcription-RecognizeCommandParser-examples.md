## src\ai\commands\parsers\speech_command_parser.cs

Modified: 28 minutes ago
Size: 7 KB

```csharp
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

```

## src\extensions\speech_extension\commands\parsers\recognize_command_parser.cs

Modified: 28 minutes ago
Size: 10 KB

```csharp
//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    public class RecognizeCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommand("recognize", recognizeCommandParsers, tokens, values);
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("recognize", recognizeCommandParsers, tokens, values);
        }

        public static IEnumerable<INamedValueTokenParser> GetCommandParsers()
        {
            return recognizeCommandParsers;
        }

        #region private data

        private static INamedValueTokenParser[] recognizeCommandParsers = {

            new RequiredValidValueNamedValueTokenParser(null, "x.command", "11", "recognize"),

            new ExpectOutputTokenParser(),
            new DiagnosticLogTokenParser(),
            new CommonNamedValueTokenParsers(),

            new ExpandFileNameNamedValueTokenParser(),

            new SpeechConfigServiceConnectionTokenParser(),
            new TrueFalseNamedValueTokenParser("service.config.content.logging.enabled", "00011;00110"),

            new TrueFalseNamedValueTokenParser("--embedded", "embedded.config.embedded", "001"),
            new Any1ValueNamedValueTokenParser("--embeddedModelKey", "embedded.config.model.key", "0011"),
            new Any1ValueNamedValueTokenParser("--embeddedModelPath", "embedded.config.model.path", "0011"),

            // new Any1ValueNamedValueTokenParser("--target", "target.language.config", "100"),
            new Any1ValueNamedValueTokenParser("--languages", "source.language.config", "100;010"),
            new RequiredValidValueNamedValueTokenParser("--profanity", "service.output.config.profanity.option", "00010", "masked;raw;removed"),
            new TrueFalseNamedValueTokenParser("service.output.config.word.level.timing", "000101"),

            new Any1or2ValueNamedValueTokenParser("--property", "config.string.property", "001"),
            new AtFileOrListNamedValueTokenParser("--properties", "config.string.properties", "001"),

            new Any1ValueNamedValueTokenParser(null, "audio.input.id.url", "0011"),

            new Any1ValueNamedValueTokenParser("--id", "audio.input.id", "001"),
            new Any1PinnedNamedValueTokenParser("--url", "audio.input.file", "001", "file", "audio.input.type"),
            new ExpandFileNameNamedValueTokenParser("--urls", "audio.input.files", "001", "audio.input.file"),
            new NamedValueTokenParser("--format",     "audio.input.format", "001", "1", "any;mp3;ogg;flac;alaw;opus", null, "file", "audio.input.type"),
            new Any1PinnedNamedValueTokenParser(null, "audio.input.microphone.geometry", "0001", "microphone", "audio.input.type"),
            new OptionalWithDefaultNamedValueTokenParser(null, "audio.input.microphone.device", "0010", "microphone", "audio.input.type"),
            // new Any1PinnedNamedValueTokenParser(null, "audio.input.push.stream.file", "00100;01100", "push", "audio.input.type"),
            // new Any1PinnedNamedValueTokenParser(null, "audio.input.pull.stream.file", "00100;01100", "pull", "audio.input.type"),
            // new RequiredValidValueNamedValueTokenParser(null, "audio.input.type", "011", "file;files;microphone;push;pull;blob"),
            new RequiredValidValueNamedValueTokenParser(null, "audio.input.type", "011", "file;files;microphone"),
            new NamedValueTokenParser(null,           "audio.input.file", "010", "1", null, "audio.input.file", "file", "audio.input.type"),
            new Any1ValueNamedValueTokenParser("--rtf", "audio.input.real.time.factor", "00110"),
            new Any1ValueNamedValueTokenParser("--fast", "audio.input.fast.lane", "0010"),

            new AtFileOrListNamedValueTokenParser("--phrases", "grammar.phrase.list", "011"),
            new Any1ValueNamedValueTokenParser(null, "grammar.recognition.factor.phrase", "0110"),

            new Any1ValueNamedValueTokenParser(null, "luis.key", "11"),
            new Any1ValueNamedValueTokenParser(null, "luis.region", "11"),
            new Any1ValueNamedValueTokenParser(null, "luis.appid", "11"),
            new Any1or2ValueNamedValueTokenParser(null, "luis.intent", "11"),
            new TrueFalseNamedValueTokenParser("--allintents", "luis.allintents", "01"),
            // new Any1or2ValueNamedValueTokenParser("--pattern", "intent.pattern", "01;10"),
            // new IniFileNamedValueTokenParser(),

            new ConnectDisconnectNamedValueTokenParser(),

            new Any1ValueNamedValueTokenParser(null, "usp.speech.config", "011"),
            new Any1ValueNamedValueTokenParser(null, "usp.speech.context", "011"),

            // new Any1or2ValueNamedValueTokenParser(null, "recognizer.property", "11"),
            new Any1PinnedNamedValueTokenParser(null, "recognize.keyword.file", "010", "keyword", "recognize.method"),
            new Any1ValueNamedValueTokenParser(null, "recognize.timeout", "01"),
            new RequiredValidValueNamedValueTokenParser("--recognize", "recognize.method", "10", "keyword;continuous;once+;once;rest;intent"),
            new PinnedNamedValueTokenParser("--continuous", "recognize.method", "10", "continuous"),
            new PinnedNamedValueTokenParser("--once+", "recognize.method", "10", "once+"),
            new PinnedNamedValueTokenParser("--once", "recognize.method", "10", "once"),
            new PinnedNamedValueTokenParser("--rest", "recognize.method", "10", "rest"),

            new IniFileNamedValueTokenParser(),

            new NamedValueTokenParser(null, "wer.sr.url", "101", "1", null, "wer.sr.url"),
            new Any1ValueNamedValueTokenParser(null, "transcript.lexical.text", "110"),
            new Any1ValueNamedValueTokenParser(null, "transcript.itn.text", "110"),
            new Any1ValueNamedValueTokenParser(null, "transcript.text", "10"),

            new Any1or2ValueNamedValueTokenParser(null, "check.sr.transcript.text.wer", "10001", "check.sr.transcript.text.wer", "true", "output.all.recognizer.recognized.result.text"),
            new Any1or2ValueNamedValueTokenParser(null, "check.sr.transcript.itn.text.wer", "100101", "check.sr.transcript.itn.text.wer", "true", "output.all.recognizer.recognized.result.itn.text"),
            new Any1or2ValueNamedValueTokenParser(null, "check.sr.transcript.lexical.text.wer", "100101", "check.sr.transcript.lexical.text.wer", "true", "output.all.recognizer.recognized.result.lexical.text"),

            new AtFileOrListWithPinnedValueNamedValueTokenParser(null, "check.sr.transcript.text.in", "10011", "check.sr.transcript.text.in", "true", "output.all.recognizer.recognized.result.text"),
            new NamedValueTokenParser(null, "check.sr.transcript.text.contains", "10011", "1", null, "check.sr.transcript.text.contains", "true", "output.all.recognizer.recognized.result.text"),
            new AtFileOrListWithPinnedValueNamedValueTokenParser(null, "check.sr.transcript.text.not.in", "100111", "check.sr.transcript.text.not.in", "true", "output.all.recognizer.recognized.result.text"),
            new NamedValueTokenParser(null, "check.sr.transcript.text.not.contains", "100111", "1", null, "check.sr.transcript.text.not.contains", "true", "output.all.recognizer.recognized.result.text"),
            new Any1or2ValueNamedValueTokenParser(null, "check.sr.transcript.text", "1001", "check.sr.transcript.text", "true", "output.all.recognizer.recognized.result.text"),

            new AtFileOrListWithPinnedValueNamedValueTokenParser(null, "check.sr.transcript.itn.text.in", "100101", "check.sr.transcript.itn.text.in", "true", "output.all.recognizer.recognized.result.itn.text"),
            new NamedValueTokenParser(null, "check.sr.transcript.itn.text.contains", "100101", "1", null, "check.sr.transcript.itn.text.contains", "true", "output.all.recognizer.recognized.result.itn.text"),
            new AtFileOrListWithPinnedValueNamedValueTokenParser(null, "check.sr.transcript.itn.text.not.in", "1001011", "check.sr.transcript.itn.text.not.in", "true", "output.all.recognizer.recognized.result.itn.text"),
            new NamedValueTokenParser(null, "check.sr.transcript.itn.text.not.contains", "1001011", "1", null, "check.sr.transcript.itn.text.not.contains", "true", "output.all.recognizer.recognized.result.itn.text"),
            new Any1or2ValueNamedValueTokenParser(null, "check.sr.transcript.itn.text", "10010", "check.sr.transcript.itn.text", "true", "output.all.recognizer.recognized.result.itn.text"),

            new AtFileOrListWithPinnedValueNamedValueTokenParser(null, "check.sr.transcript.lexical.text.in", "100101", "check.sr.transcript.lexical.text.in", "true", "output.all.recognizer.recognized.result.lexical.text"),
            new NamedValueTokenParser(null, "check.sr.transcript.lexical.text.contains", "100101", "1", null, "check.sr.transcript.lexical.text.contains", "true", "output.all.recognizer.recognized.result.lexical.text"),
            new AtFileOrListWithPinnedValueNamedValueTokenParser(null, "check.sr.transcript.lexical.text.not.in", "1001011", "check.sr.transcript.lexical.text.not.in", "true", "output.all.recognizer.recognized.result.lexical.text"),
            new NamedValueTokenParser(null, "check.sr.transcript.lexical.text.not.contains", "1001011", "1", null, "check.sr.transcript.lexical.text.not.contains", "true", "output.all.recognizer.recognized.result.lexical.text"),
            new Any1or2ValueNamedValueTokenParser(null, "check.sr.transcript.lexical.text", "10010", "check.sr.transcript.lexical.text", "true", "output.all.recognizer.recognized.result.lexical.text"),

            new NamedValueTokenParser(null, "check.jmes.verbose.failures", "1010", "1;0", "true;false", null, "false"),
            new Any1ValueNamedValueTokenParser(null, "check.jmes", "10"),

            new TrueFalseNamedValueTokenParser("output.overwrite", "11"),
            new TrueFalseNamedValueTokenParser("output.audio.input.id", "1101;1011"),

            new OutputBatchRecognizerTokenParser(),
            new OutputSrtVttRecognizerTokenParser(),

            new OutputAllRecognizerEventTokenParser(),
            new OutputEachRecognizerEventTokenParser()
        };

        #endregion
    }
}

```
