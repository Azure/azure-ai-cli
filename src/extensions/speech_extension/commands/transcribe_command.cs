using System.Text;
using System.Text.Json;
using System.Net;
using System.Net.Http.Headers;

namespace Azure.AI.Details.Common.CLI
{
    public class TranscribeCommand : Command
    {
        public TranscribeCommand(ICommandValues values) : base(values)
        {
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", false);
        }

        public bool RunCommand()
        {
            try
            {
                Transcribe();
            }
            catch (WebException ex)
            {
                FileHelpers.LogException(_values, ex);
                ConsoleHelpers.WriteLineError($"\n  ERROR: {ex.Message}");
                JsonHelpers.PrintJson(HttpHelpers.ReadWriteJson(ex.Response, _values, "transcribe"));
            }

            return _values.GetOrDefault("passed", true);
        }

        private void Transcribe()
        {
            StartCommand();

            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(FileHelpers.ReadAllBytes(GetAudioFile())), "audio", Path.GetFileName(GetAudioFile()));
            content.Add(new StringContent(GetDefinitionJson()), "definition");

            var request = CreateRequestMessage();
            request.Content = content;

            CheckWriteOutputRequest(request, content);

            var client = new HttpClient();
            var response = client.SendAsync(request).Result;
            var json = ReadWritePrintJson(response);
            ProcessResponse(json);

            StopCommand();
            DeleteTemporaryFiles();
        }

        private HttpRequestMessage CreateRequestMessage()
         {
            var region = _values["service.config.region"];
            var url = $"https://{region}.api.cognitive.microsoft.com/speechtotext/transcriptions:transcribe?api-version=2024-11-15";

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Ocp-Apim-Subscription-Key", GetSubscriptionKey());

            return request;
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
            var profanityFilterMode = _values.GetOrDefault("transcribe.profanity.filter.mode", "masked");
            var channels = _values.GetOrDefault("audio.input.channels", "[]");

            var diarization = diarizationEnabled ? new { maxSpeakers, enabled = true } : null;

            return JsonSerializer.Serialize(new
            {
                locales = locales.Split(';'),
                diarization,
                channels = JsonSerializer.Deserialize<object>(channels),
                profanityFilterMode
            });
        }

        private void CheckWriteOutputRequest(HttpRequestMessage request, MultipartFormDataContent content)
        {
            var output = _values.GetOrEmpty("transcribe.output.request.file");
            if (!string.IsNullOrEmpty(output))
            {
                var fileName = FileHelpers.GetOutputDataFileName(output, _values);
                HttpHelpers.WriteOutputRequest(request, fileName, content);
            }
        }

        private string ReadWritePrintJson(HttpResponseMessage response)
        {
            var json = HttpHelpers.ReadWriteJson(response, _values, "transcribe");
            if (!_quiet && _verbose) JsonHelpers.PrintJson(json);
            return json;
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

            if (!_quiet) Console.WriteLine(withSpeakerInfo);

            _output!.EnsureOutputAll("transcription.result.text", withSpeakerInfo);
                    
            var srtFileName = _values.GetOrEmpty("output.srt.file.name");
            if (!string.IsNullOrEmpty(srtFileName))
            {
                GenerateSrtFile(jsonResponse, srtFileName);
            }

            var vttFileName = _values.GetOrEmpty("output.vtt.file.name");
            if (!string.IsNullOrEmpty(vttFileName))
            {
                GenerateVttFile(jsonResponse, vttFileName);
            }
        }

        private void StartCommand()
        {
            CheckPath();
            CheckAudioInput();

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
        }

        private void StopCommand()
        {
            _lock!.StopLock(5000);

            _output!.CheckOutput();
            _output!.StopOutput();
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
        }

        private string GetIdFromAudioInputFile(string? input, string file)
        {
            string id;
            if (input == "file" && !string.IsNullOrEmpty(file))
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
            if (FileHelpers.FileExistsInDataPath(id, _values) || FileHelpers.FileExistsInDataPath(id + ".wav", _values))
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

        private void GenerateSrtFile(string transcriptionJson, string srtFileName)
        {
            var json = JsonDocument.Parse(transcriptionJson);

            var sb = new StringBuilder();
            int sequence = 1;

            foreach (var phrase in json.RootElement.GetProperty("phrases").EnumerateArray())
            {
                var startTime = TimeSpan.FromMilliseconds(phrase.GetProperty("offsetMilliseconds").GetInt32());
                var duration = TimeSpan.FromMilliseconds(phrase.GetProperty("durationMilliseconds").GetInt32());
                var endTime = startTime + duration;

                sb.AppendLine($"{sequence}");
                sb.AppendLine($"{startTime:hh\\:mm\\:ss\\,fff} --> {endTime:hh\\:mm\\:ss\\,fff}");
                sb.AppendLine(phrase.GetProperty("text").GetString());
                sb.AppendLine();

                sequence++;
            }

            FileHelpers.WriteAllText(srtFileName, sb.ToString().Trim(), Encoding.UTF8);
        }

        private void GenerateVttFile(string transcriptionJson, string vttFileName)
        {
            var json = JsonDocument.Parse(transcriptionJson);

            var sb = new StringBuilder("WEBVTT\n");

            foreach (var phrase in json.RootElement.GetProperty("phrases").EnumerateArray())
            {
                var startTime = TimeSpan.FromMilliseconds(phrase.GetProperty("offsetMilliseconds").GetInt32());
                var duration = TimeSpan.FromMilliseconds(phrase.GetProperty("durationMilliseconds").GetInt32());
                var endTime = startTime + duration;

                sb.AppendLine();
                sb.AppendLine($"{startTime:hh\\:mm\\:ss\\.fff} --> {endTime:hh\\:mm\\:ss\\.fff}");
                sb.AppendLine(phrase.GetProperty("text").GetString());
            }

            FileHelpers.WriteAllText(vttFileName, sb.ToString().Trim(), Encoding.UTF8);
        }

        OutputHelper? _output = null;

        private SpinLock? _lock = null;
        private bool _quiet = false;
        private bool _verbose = false;
    }
}
