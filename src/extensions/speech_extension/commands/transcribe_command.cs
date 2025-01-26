using System.Text.Json;

namespace Azure.AI.Details.Common.CLI
{
    public class TranscribeCommand : Command
    {
        public TranscribeCommand(ICommandValues values) : base(values) { }

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
            content.Add(new ByteArrayContent(FileHelpers.ReadAllBytes(GetAudioFile())), "audio", Path.GetFileName(GetAudioFile()));
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
            var file = _values["audio.input.file"];
            var existing = FileHelpers.DemandFindFileInDataPath(file, _values, "audio input");
            return existing;
        }

        private string GetDefinitionJson()
        {
            var locales = _values.GetOrDefault("source.language.config", "en-US");
            var diarizationEnabled = _values.GetOrDefault("transcribe.diarization", false);
            var maxSpeakers = _values.GetOrDefault("transcribe.diarization.max.speakers", 1);
            var channels = _values.GetOrDefault("audio.input.channels", "[]");
            var profanityFilterMode = _values.GetOrDefault("service.output.config.profanity.option", "Masked");

            var diarization = diarizationEnabled ? new { maxSpeakers, enabled = true } : null;

            return JsonSerializer.Serialize(new
            {
                locales = locales.Split(';'),
                diarization,
                channels = JsonSerializer.Deserialize<object>(channels),
                profanityFilterMode
            });
        }

        private void ProcessResponse(string jsonResponse)
        {
            if (Program.Debug) Console.WriteLine(jsonResponse);

            var json = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

            var transcription = string.Join("\n", json
                .GetProperty("combinedPhrases")
                .EnumerateArray()
                .Select(p => p.GetProperty("text").GetString())
                .ToArray());

            var phrases = json.GetProperty("phrases").EnumerateArray();
            var hasSpeakerInfo = phrases.Any(p => p.TryGetProperty("speaker", out var speaker) && speaker.GetInt32() > 0);
            var withSpeakerInfo = hasSpeakerInfo
                ? string.Join("\n", phrases.Select(p => {
                    var speaker = p.GetProperty("speaker").GetInt32();
                    var text = p.GetProperty("text").GetString();
                    return $"Speaker {speaker}: {text}";
                    }))
                : transcription;

            Console.WriteLine(withSpeakerInfo);

            _output!.EnsureOutputAll("transcription.result", withSpeakerInfo);
            _output!.CheckOutput();
        }

        private void CheckAudioInput()
        {
            var id = _values["audio.input.id"];
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
