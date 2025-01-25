## src\ai\commands\parsers\speech_command_parser.cs

Modified: 34 minutes ago
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

## src\ai\commands\speech_command.cs

Modified: 34 minutes ago
Size: 2 KB

```csharp
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
                FileHelpers.LogException(_values, ex);
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

```

## src\extensions\speech_extension\commands\parsers\recognize_command_parser.cs

Modified: 34 minutes ago
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

## src\extensions\speech_extension\commands\recognize_command.cs

Modified: 34 minutes ago
Size: 22 KB

```csharp
//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Intent;
using System.Collections.Generic;

namespace Azure.AI.Details.Common.CLI
{
    public class RecognizeCommand : Command
    {
        public RecognizeCommand(ICommandValues values) : base(values)
        {
        }

        public bool RunCommand()
        {
            Recognize(_values["recognize.method"]);
            return _values.GetOrDefault("passed", true);
        }

        private void Recognize(string? recognize)
        {
            switch (recognize)
            {
                case "":
                case null:
                case "continuous": RecognizeContinuous(); break;

                case "once": RecognizeOnce(false); break;
                case "once+": RecognizeOnce(true); break;

                case "keyword": RecognizeKeyword(); break;

                case "rest": RecognizeREST(); break;
            }
        }

        private void RecognizeREST()
        {
            _values.AddThrowError("WARNING:", $"'recognize=rest' NOT YET IMPLEMENTED!!");
        }

        private void RecognizeContinuous()
        {
            StartCommand();

            SpeechRecognizer recognizer = CreateSpeechRecognizer();
            PrepRecognizerConnection(recognizer, true);
            PrepRecognizerGrammars(recognizer);

            recognizer.SessionStarted += SessionStarted;
            recognizer.SessionStopped += SessionStopped;
            recognizer.Recognizing += Recognizing;
            recognizer.Recognized += Recognized;
            recognizer.Canceled += Canceled;

            recognizer.StartContinuousRecognitionAsync().Wait();
            if (_microphone) { Console.WriteLine("Listening; press ENTER to stop ...\n"); }

            var timeout = _values.GetOrDefault("recognize.timeout", int.MaxValue);
            WaitForContinuousStopCancelKeyOrTimeout(recognizer, timeout);

            recognizer.StopContinuousRecognitionAsync().Wait();

            if (_disconnect) RecognizerConnectionDisconnect(recognizer);

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private void RecognizeOnce(bool repeatedly)
        {
            StartCommand();

            SpeechRecognizer recognizer = CreateSpeechRecognizer();
            PrepRecognizerConnection(recognizer, false);
            PrepRecognizerGrammars(recognizer);

            recognizer.SessionStarted += SessionStarted;
            recognizer.SessionStopped += SessionStopped;
            recognizer.Recognizing += Recognizing;
            recognizer.Recognized += Recognized;
            recognizer.Canceled += Canceled;

            if (_microphone) { Console.WriteLine("Listening..."); }

            while (true)
            {
                var task = recognizer.RecognizeOnceAsync();
                WaitForOnceStopCancelOrKey(recognizer, task);

                if (_disconnect) RecognizerConnectionDisconnect(recognizer);

                if (!repeatedly) break;
                if (_canceledEvent.WaitOne(0)) break;
                if (_microphone && Console.KeyAvailable) break;
                if (_microphone && task.Result.Text == "Stop.") break;
                if (_microphone && task.Result.Text == "Stop listening.") break;
            }

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private void RecognizeKeyword()
        {
            StartCommand();

            SpeechRecognizer recognizer = CreateSpeechRecognizer();
            PrepRecognizerConnection(recognizer, false);
            PrepRecognizerGrammars(recognizer);

            recognizer.SessionStarted += SessionStarted;
            recognizer.SessionStopped += SessionStopped;
            recognizer.Recognizing += Recognizing;
            recognizer.Recognized += Recognized;
            recognizer.Canceled += Canceled;

            var keywordModel = LoadKeywordModel();
            recognizer.StartKeywordRecognitionAsync(keywordModel).Wait();
            if (_microphone) { Console.WriteLine("Listening for keyword..."); }

            var timeout = _values.GetOrDefault("recognize.timeout", int.MaxValue);
            WaitForKeywordCancelKeyOrTimeout(recognizer, timeout);

            recognizer.StopKeywordRecognitionAsync().Wait();

            if (_disconnect) RecognizerConnectionDisconnect(recognizer);

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private SpeechRecognizer CreateSpeechRecognizer()
        {
            var config = CreateSpeechConfig();
            var audioConfig = ConfigHelpers.CreateAudioConfig(_values);

            CreateSourceLanguageConfig(config, out var language, out var autoDetect);

            var recognizer = autoDetect != null 
                ? new SpeechRecognizer(config, autoDetect, audioConfig)
                : language != null
                    ? new SpeechRecognizer(config, language, audioConfig)
                    : audioConfig != null
                        ? new SpeechRecognizer(config, audioConfig)
                        : new SpeechRecognizer(config);

            _disposeAfterStop.Add(audioConfig);
            _disposeAfterStop.Add(recognizer);

            _output!.EnsureCachePropertyCollection("recognizer", recognizer.Properties);

            return recognizer;
        }

        private void CreateSourceLanguageConfig(SpeechConfig config, out SourceLanguageConfig? language, out AutoDetectSourceLanguageConfig? autoDetect)
        {
            language = null;
            autoDetect = null;

            var bcp47 = _values["source.language.config"];
            if (string.IsNullOrEmpty(bcp47)) bcp47 = "en-US";

            var endpointId = _values["service.config.endpoint.id"];
            var endpointIdOk = !string.IsNullOrEmpty(endpointId);

            var langs = new List<SourceLanguageConfig>();
            foreach (var item in bcp47.Split(';'))
            {
                langs.Add(endpointIdOk
                    ? SourceLanguageConfig.FromLanguage(item, endpointId)
                    : SourceLanguageConfig.FromLanguage(item));
            }

            if (langs.Count == 1)
            {
                language = langs[0];
            }
            else
            {
                autoDetect = AutoDetectSourceLanguageConfig.FromSourceLanguageConfigs(langs.ToArray());
            }
        }

        private SpeechConfig CreateSpeechConfig(string? key = null, string? region = null)
        {
            SpeechConfig config = ConfigHelpers.CreateSpeechConfig(_values, key, region);
            SetSpeechConfigProperties(config);
            return config;
        }

        private void SetSpeechConfigProperties(SpeechConfig config)
        {
            var proxyHost = _values["service.config.proxy.host"];
            if (!string.IsNullOrEmpty(proxyHost)) config.SetProxy(proxyHost, _values.GetOrDefault("service.config.proxy.port", 80));

            var endpointId = _values["service.config.endpoint.id"];
            if (!string.IsNullOrEmpty(endpointId)) config.EndpointId = endpointId;

            var needDetailedText = _output != null && (_output.NeedsLexicalText() || _output.NeedsItnText());
            if (needDetailedText) config.OutputFormat = OutputFormat.Detailed;

            var profanity = _values.GetOrEmpty("service.output.config.profanity.option").ToLower();
            if (profanity == "removed") config.SetProfanity(ProfanityOption.Removed);
            if (profanity == "masked") config.SetProfanity(ProfanityOption.Masked);
            if (profanity == "raw") config.SetProfanity(ProfanityOption.Raw);

            var wordTimings = _values.GetOrDefault("service.output.config.word.level.timing", false);
            if (wordTimings) config.RequestWordLevelTimestamps();

            var contentLogging = _values.GetOrDefault("service.config.content.logging.enabled", false);
            if (contentLogging) config.EnableAudioLogging();

            var trafficType = _values.GetOrDefault("service.config.endpoint.traffic.type", "spx");
            config.SetServiceProperty("traffictype", trafficType, ServicePropertyChannel.UriQueryParameter);

            var endpointParam = _values.GetOrEmpty("service.config.endpoint.query.string");
            if (!string.IsNullOrEmpty(endpointParam)) ConfigHelpers.SetEndpointParams(config, endpointParam);

            var httpHeader = _values.GetOrEmpty("service.config.endpoint.http.header");
            if (!string.IsNullOrEmpty(httpHeader)) SetHttpHeaderProperty(config, httpHeader);

            var rtf = _values.GetOrDefault("audio.input.real.time.factor", -1);
            if (rtf >= 0) config.SetProperty("SPEECH-AudioThrottleAsPercentageOfRealTime", rtf.ToString());

            var fastLane = _values.GetOrDefault("audio.input.fast.lane", rtf >= 0 ? 0 : -1);
            if (fastLane >= 0) config.SetProperty("SPEECH-TransmitLengthBeforThrottleMs", fastLane.ToString());

            var stringProperty = _values.GetOrEmpty("config.string.property");
            if (!string.IsNullOrEmpty(stringProperty)) ConfigHelpers.SetStringProperty(config, stringProperty);

            var stringProperties = _values.GetOrEmpty("config.string.properties");
            if (!string.IsNullOrEmpty(stringProperties)) ConfigHelpers.SetStringProperties(config, stringProperties);

            var embedded = _values.GetOrDefault("embedded.config.embedded", false);
            if (embedded) SetEmbeddedProperties(config);

            CheckNotYetImplementedConfigProperties();
        }

        private void SetEmbeddedProperties(SpeechConfig config)
        {
            // Turn on embedded (offline) speech recognition.
            config.SetProperty("SPEECH-RecoBackend", "offline");

            var modelKey = _values.GetOrEmpty("embedded.config.model.key");
            config.SetProperty("SPEECH-RecoModelKey", modelKey);

            var modelPath = _values.GetOrEmpty("embedded.config.model.path");
            var modelIniFileFullPath = Path.GetFullPath(Path.Combine(modelPath, "sr.ini"));
            if (!File.Exists(modelIniFileFullPath))
            {
                _values.AddThrowError(
                    "WARNING:", $"Missing or invalid speech recognition model path!", "",
                        "USE:", $"{Program.Name} recognize --embedded --embeddedModelPath PATH [...]");
            }
            config.SetProperty("SPEECH-RecoModelIniFile", modelIniFileFullPath);
        }

        private static void SetHttpHeaderProperty(SpeechConfig config, string httpHeader)
        {
            if (StringHelpers.SplitNameValue(httpHeader, out string name, out string value)) config.SetServiceProperty(name, value, ServicePropertyChannel.HttpHeader);
        }

        private void CheckNotYetImplementedConfigProperties()
        {
            var notYetImplemented = 
                ";config.token.type;config.token.password;config.token.username" +
                ";config.language.target;recognizer.property";

            foreach (var key in notYetImplemented.Split(';'))
            {
                var value = _values[key];
                if (!string.IsNullOrEmpty(value))
                {
                    _values.AddThrowError("WARNING:", $"'{key}={value}' NOT YET IMPLEMENTED!!");
                }
            }
        }

        private void CheckAudioInput()
        {
            var id = _values["audio.input.id"];
            var device = _values["audio.input.microphone.device"];
            var input = _values["audio.input.type"];
            var file = _values["audio.input.file"];
            var url = "";

            if (!string.IsNullOrEmpty(file) && file.StartsWith("http"))
            {
                file = DownloadInputFile(url = file, "audio.input.file", "audio input");
            }

            if (string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(url))
            {
                id = GetIdFromInputUrl(url, "audio.input.id");
            }

            if (string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(file))
            {
                id = GetIdFromAudioInputFile(input, file);
            }

            if (string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(id))
            {
                input = GetAudioInputFromId(id);
            }

            if (input == "file" && string.IsNullOrEmpty(file) && !string.IsNullOrEmpty(id))
            {
                file = GetAudioInputFileFromId(id);
            }

            _microphone = (input == "microphone" || string.IsNullOrEmpty(input));
        }

        private string GetIdFromAudioInputFile(string? input, string file)
        {
            string id;
            if (input == "microphone" || string.IsNullOrEmpty(input))
            {
                id = "microphone";
            }
            else if (input == "file" && !string.IsNullOrEmpty(file))
            {
                var existing = FileHelpers.DemandFindFileInDataPath(file, _values, "audio input");
                id = Path.GetFileNameWithoutExtension(existing);
            }
            else
            {
                id = "error";
            }

            _values.Add("audio.input.id", id);
            return id;
        }

        private string? GetAudioInputFromId(string id)
        {
            string input;
            if (id == "microphone")
            {
                input = "microphone";
            }
            else if (FileHelpers.FileExistsInDataPath(id, _values) || FileHelpers.FileExistsInDataPath(id + ".wav", _values))
            {
                input = "file";
            }
            else if (_values.Contains("audio.input.id.url"))
            {
                input = "file";
            }
            else
            {
                _values.AddThrowError("ERROR:", $"Cannot find audio input file: \"{id}.wav\"");
                return null;
            }

            _values.Add("audio.input.type", input);
            return input;
        }

        private string? GetAudioInputFileFromId(string id)
        {
            var existing = FileHelpers.FindFileInDataPath(id, _values);
            if (existing == null) existing = FileHelpers.FindFileInDataPath(id + ".wav", _values);

            if (existing == null)
            {
                var url = _values["audio.input.id.url"];
                if (!string.IsNullOrEmpty(url))
                {
                    url = url.Replace("{id}", id);
                    existing = HttpHelpers.DownloadFileWithRetry(url);
                }
            }

            var file = existing;
            _values.Add("audio.input.file", file);
            return file;
        }

        private KeywordRecognitionModel LoadKeywordModel()
        {
            var fileName = _values["recognize.keyword.file"]!;
            var existing = FileHelpers.DemandFindFileInDataPath(fileName, _values, "keyword model");

            var keywordModel = KeywordRecognitionModel.FromFile(existing);
            return keywordModel;
        }

        private void PrepRecognizerConnection(SpeechRecognizer recognizer, bool continuous)
        {
            var connection = Connection.FromRecognizer(recognizer);
            _disposeAfterStop.Add(connection);

            connection.Connected += Connected;
            connection.Disconnected += Disconnected;
            connection.MessageReceived += ConnectionMessageReceived;

            var connect = _values["connection.connect"];
            var disconnect = _values["connection.disconnect"];

            _connect = !string.IsNullOrEmpty(connect) && connect == "true";
            _disconnect = !string.IsNullOrEmpty(disconnect) && disconnect == "true";

            if (_connect) RecognizerConnectionConnect(connection, continuous);
            ConnectionHelpers.SetConnectionMessageProperties(connection, _values);
        }

        private void PrepRecognizerGrammars(SpeechRecognizer recognizer)
        {
            var phrases = _values["grammar.phrase.list"];
            if (!string.IsNullOrEmpty(phrases)) PrepRecognizerPhraseListGrammar(recognizer, phrases.Split('\r', '\n', ';'));

            var partialPhraseRecognitionFactor = _values["grammar.recognition.factor.phrase"];
            if (!string.IsNullOrEmpty(partialPhraseRecognitionFactor)) PrepGrammarList(recognizer, double.Parse(partialPhraseRecognitionFactor));
        }

        private void PrepGrammarList(SpeechRecognizer recognizer, double partialPhraseRecognitionFactor)
        {
            var grammarList = GrammarList.FromRecognizer(recognizer);
            grammarList.SetRecognitionFactor(partialPhraseRecognitionFactor, RecognitionFactorScope.PartialPhrase);
        }

        private void PrepRecognizerPhraseListGrammar(SpeechRecognizer recognizer, IEnumerable<string> phrases)
        {
            var grammar = PhraseListGrammar.FromRecognizer(recognizer);
            foreach (var phrase in phrases)
            {
                if (!string.IsNullOrEmpty(phrase))
                {
                    grammar.AddPhrase(phrase);
                }
            }
        }

        private void RecognizerConnectionConnect(Connection connection, bool continuous)
        {
            connection.Open(continuous);
        }

        private void RecognizerConnectionDisconnect(SpeechRecognizer recognizer)
        {
            _lock!.EnterReaderLockOnce(ref _expectDisconnected);

            var connection = Connection.FromRecognizer(recognizer);
            connection.Close();
        }

        private void Connected(object? sender, ConnectionEventArgs e)
        {
            _display!.DisplayConnected(e);
            _output!.Connected(e);
        }

        private void Disconnected(object? sender, ConnectionEventArgs e)
        {
            _display!.DisplayDisconnected(e);
            _output!.Disconnected(e);

            _lock!.ExitReaderLockOnce(ref _expectDisconnected);
        }

        private void ConnectionMessageReceived(object? sender, ConnectionMessageEventArgs e)
        {
            _display!.DisplayMessageReceived(e);
            _output!.ConnectionMessageReceived(e);
        }

        private void SessionStarted(object? sender, SessionEventArgs e)
        {
            _lock!.EnterReaderLockOnce(ref _expectSessionStopped);
            _stopEvent.Reset();

            _display!.DisplaySessionStarted(e);
            _output!.SessionStarted(e);
        }

        private void SessionStopped(object? sender, SessionEventArgs e)
        {
            _display!.DisplaySessionStopped(e);
            _output!.SessionStopped(e);

            _stopEvent.Set();
            _lock!.ExitReaderLockOnce(ref _expectSessionStopped);
        }

        private void Recognizing(object? sender, SpeechRecognitionEventArgs e)
        {
            _lock!.EnterReaderLockOnce(ref _expectRecognized);

            _display!.DisplayRecognizing(e);
            _output!.Recognizing(e);
        }

        private void Recognized(object? sender, SpeechRecognitionEventArgs e)
        {
            _display!.DisplayRecognized(e);
            _output!.Recognized(e);

            _lock!.ExitReaderLockOnce(ref _expectRecognized);
        }

        private void Canceled(object? sender, SpeechRecognitionCanceledEventArgs e)
        {
            _display!.DisplayCanceled(e);
            _output!.Canceled(e);
            _canceledEvent.Set();
        }

        private void WaitForContinuousStopCancelKeyOrTimeout(SpeechRecognizer recognizer, int timeout)
        {
            var interval = 100;

            while (timeout > 0)
            {
                timeout -= interval;
                if (_stopEvent.WaitOne(interval)) break;
                if (_canceledEvent.WaitOne(0)) break;
                if (_microphone && Console.KeyAvailable)
                {
                    recognizer.StopContinuousRecognitionAsync().Wait();
                    break;
                }
            }
        }

        private void WaitForOnceStopCancelOrKey(SpeechRecognizer recognizer, Task<SpeechRecognitionResult> task)
        {
            var interval = 100;

            while (!task.Wait(interval))
            {
                if (_stopEvent.WaitOne(0)) break;
                if (_canceledEvent.WaitOne(0)) break;
                if (_microphone && Console.KeyAvailable)
                {
                    recognizer.StopContinuousRecognitionAsync().Wait();
                    break;
                }
            }
        }
        
        private void WaitForKeywordCancelKeyOrTimeout(SpeechRecognizer recognizer, int timeout)
        {
            var interval = 100;

            while (timeout > 0)
            {
                timeout -= interval;
                if (_canceledEvent.WaitOne(interval)) break;
                if (_microphone && Console.KeyAvailable)
                {
                    recognizer.StopKeywordRecognitionAsync().Wait();
                    break;
                }
            }
        }

        private void StartCommand()
        {
            CheckPath();
            CheckAudioInput();

            _display = new DisplayHelper(_values);

            _output = new OutputHelper(_values);
            _output!.StartOutput();

            var id = _values["audio.input.id"]!;
            _output!.EnsureOutputAll("audio.input.id", id);
            _output!.EnsureOutputEach("audio.input.id", id);
            _output!.EnsureCacheProperty("audio.input.id", id);

            var file = _values["audio.input.file"];
            _output!.EnsureCacheProperty("audio.input.file", file);

            _lock = new SpinLock();
            _lock.StartLock();

            _expectRecognized = 0;
            _expectSessionStopped = 0;
            _expectDisconnected = 0;
        }

        private void StopCommand()
        {
            _lock!.StopLock(5000);

            _output!.CheckOutput();
            _output!.StopOutput();
        }

        private bool _microphone = false;
        private bool _connect = false;
        private bool _disconnect = false;

        private SpinLock? _lock = null;
        private int _expectRecognized = 0;
        private int _expectSessionStopped = 0;
        private int _expectDisconnected = 0;

        OutputHelper? _output = null;
        DisplayHelper? _display = null;
    }
}

```
