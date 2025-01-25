To add a new `ai speech transcribe` command that supports all the options mentioned in the SDE instructions, the following steps and changes would be required:

### 1. Command Parsing

- **Define the new command**:
  - Add `speech.transcribe` to the `_commands` array in `SpeechCommandParser`:
    ```csharp
    ("speech.transcribe", true),
    ```

- **Create a new command parser**:
  - Implement the `TranscribeCommandParser` class similar to `RecognizeCommandParser`:
    ```csharp
    public class TranscribeCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommand("transcribe", transcribeCommandParsers, tokens, values);
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("transcribe", transcribeCommandParsers, tokens, values);
        }

        public static IEnumerable<INamedValueTokenParser> GetCommandParsers()
        {
            return transcribeCommandParsers;
        }

        #region private data

        private static INamedValueTokenParser[] transcribeCommandParsers = {

            new RequiredValidValueNamedValueTokenParser(null, "x.command", "11", "transcribe"),

            new ExpectOutputTokenParser(),
            new DiagnosticLogTokenParser(),
            new CommonNamedValueTokenParsers(),

            new ExpandFileNameNamedValueTokenParser(),

            new SpeechConfigServiceConnectionTokenParser(),
            new TrueFalseNamedValueTokenParser("service.config.content.logging.enabled", "00011;00110"),

            new TrueFalseNamedValueTokenParser("--embedded", "embedded.config.embedded", "001"),
            new Any1ValueNamedValueTokenParser("--embeddedModelKey", "embedded.config.model.key", "0011"),
            new Any1ValueNamedValueTokenParser("--embeddedModelPath", "embedded.config.model.path", "0011"),

            new Any1ValueNamedValueTokenParser("--languages", "source.language.config", "100;010"),
            new RequiredValidValueNamedValueTokenParser("--profanity", "service.output.config.profanity.option", "00010", "masked;raw;removed"),
            new TrueFalseNamedValueTokenParser("service.output.config.word.level.timing", "000101"),

            new Any1or2ValueNamedValueTokenParser("--property", "config.string.property", "001"),
            new AtFileOrListNamedValueTokenParser("--properties", "config.string.properties", "001"),

            new Any1ValueNamedValueTokenParser(null, "audio.input.id.url", "0011"),

            new Any1ValueNamedValueTokenParser("--id", "audio.input.id", "001"),
            new Any1PinnedNamedValueTokenParser("--url", "audio.input.file", "001", "file", "audio.input.type"),
            new ExpandFileNameNamedValueTokenParser("--urls", "audio.input.files", "001", "audio.input.file"),
            new NamedValueTokenParser("--format", "audio.input.format", "001", "1", "any;mp3;ogg;flac;alaw;opus", null, "file", "audio.input.type"),
            new Any1PinnedNamedValueTokenParser(null, "audio.input.microphone.geometry", "0001", "microphone", "audio.input.type"),
            new OptionalWithDefaultNamedValueTokenParser(null, "audio.input.microphone.device", "0010", "microphone", "audio.input.type"),

            new AtFileOrListNamedValueTokenParser("--phrases", "grammar.phrase.list", "011"),
            new Any1ValueNamedValueTokenParser(null, "grammar.recognition.factor.phrase", "0110"),

            new Any1ValueNamedValueTokenParser(null, "luis.key", "11"),
            new Any1ValueNamedValueTokenParser(null, "luis.region", "11"),
            new Any1ValueNamedValueTokenParser(null, "luis.appid", "11"),
            new Any1or2ValueNamedValueTokenParser(null, "luis.intent", "11"),
            new TrueFalseNamedValueTokenParser("--allintents", "luis.allintents", "01"),

            new ConnectDisconnectNamedValueTokenParser(),

            new Any1ValueNamedValueTokenParser(null, "usp.speech.config", "011"),
            new Any1ValueNamedValueTokenParser(null, "usp.speech.context", "011"),

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
    ```

### 2. Command Execution

- **Add the command execution logic**:
  - In the `SpeechCommand` class, add a case for `speech.transcribe` to dispatch to the new `TranscribeCommand`:
    ```csharp
    case "speech.transcribe":
        new TranscribeCommand(_values).RunCommand();
        break;
    ```

- **Implement the `TranscribeCommand` class**:
  - Create a new `TranscribeCommand` class similar to `RecognizeCommand`:
    ```csharp
    public class TranscribeCommand : Command
    {
        public TranscribeCommand(ICommandValues values) : base(values)
        {
        }

        public bool RunCommand()
        {
            Transcribe();
            return _values.GetOrDefault("passed", true);
        }

        private void Transcribe()
        {
            StartCommand();

            // Prepare the HTTP request for the REST API
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, GetEndpointUrl());
            request.Headers.Add("Ocp-Apim-Subscription-Key", GetSubscriptionKey());

            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(File.ReadAllBytes(GetAudioFile())), "audio", Path.GetFileName(GetAudioFile()));
            content.Add(new StringContent(GetDefinitionJson()), "definition");

            request.Content = content;

            var response = client.SendAsync(request).Result;
            if (!response.IsSuccessStatusCode)
            {
                _values.AddThrowError("ERROR:", $"Transcription failed with status code {response.StatusCode}");
            }

            var jsonResponse = response.Content.ReadAsStringAsync().Result;
            ProcessResponse(jsonResponse);

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private string GetEndpointUrl()
        {
            var region = _values["service.config.region"];
            return $"https://{region}.api.cognitive.microsoft.com/speechtotext/transcriptions:transcribe?api-version=2024-11-15";
        }

        private string GetSubscriptionKey()
        {
            return _values["service.config.key"];
        }

        private string GetAudioFile()
        {
            return _values["audio.input.file"];
        }

        private string GetDefinitionJson()
        {
            var locales = _values.GetOrDefault("source.language.config", "en-US");
            var diarization = _values.GetOrDefault("diarization.config", "{}");
            var channels = _values.GetOrDefault("audio.input.channels", "[]");
            var profanityFilterMode = _values.GetOrDefault("service.output.config.profanity.option", "Masked");

            return JsonSerializer.Serialize(new
            {
                locales = locales.Split(';'),
                diarization = JsonSerializer.Deserialize<object>(diarization),
                channels = JsonSerializer.Deserialize<object>(channels),
                profanityFilterMode
            });
        }

        private void ProcessResponse(string jsonResponse)
        {
            var responseObj = JsonSerializer.Deserialize<TranscriptionResponse>(jsonResponse);
            var text = string.Join(" ", responseObj.CombinedPhrases.Select(p => p.Text));
            _output!.EnsureOutputAll("transcription.result", text);
            _output!.CheckOutput();
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

    public class TranscriptionResponse
    {
        public int DurationMilliseconds { get; set; }
        public List<CombinedPhrase> CombinedPhrases { get; set; }
        public List<Phrase> Phrases { get; set; }
    }

    public class CombinedPhrase
    {
        public string Text { get; set; }
    }

    public class Phrase
    {
        public int OffsetMilliseconds { get; set; }
        public int DurationMilliseconds { get; set; }
        public string Text { get; set; }
        public List<Word> Words { get; set; }
        public string Locale { get; set; }
        public float Confidence { get; set; }
    }

    public class Word
    {
        public string Text { get; set; }
        public int OffsetMilliseconds { get; set; }
        public int DurationMilliseconds { get; set; }
    }
    ```

### 3. Additional Configuration and Error Handling

- **Add error handling in `TranscribeCommand`**:
  - Handle different error codes and provide appropriate messages.
  - Ensure that detailed error information is logged and displayed.

### Conclusion

By following these steps, you can introduce a new `ai speech transcribe` command that leverages the Azure AI Speech services for transcription, incorporating all the specified options such as locales, diarization, channels, and profanity filtering. This implementation ensures compatibility with the existing command structure and practices used for the `ai speech recognize` command.

