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
using System.Collections.Generic;
using System.Net;

namespace Azure.AI.Details.Common.CLI
{
    public class SynthesizeCommand : Command
    {
        public SynthesizeCommand(ICommandValues values)
        {
            _values = values.ReplaceValues();
        }

        public bool RunCommand()
        {
            var listVoices = _values.GetOrDefault("synthesizer.list.voices", false);
            if (listVoices) return DoListVoices();

            Synthesize();
            return _values.GetOrDefault("passed", true);
        }

        private bool DoListVoices()
        {
            string url = GetVoiceNameListUrl();
            var downloaded = HttpHelpers.DownloadFileWithRetry(url, "Downloading voice name list...", _values);

            var content = FileHelpers.ReadAllText(downloaded, Encoding.Default);
            Console.WriteLine("Voice names:");
            JsonHelpers.PrintJson(content);

            return !string.IsNullOrEmpty(content);
        }

        private string GetVoiceNameListUrl()
        {
            var host = _values["service.config.host"];
            var region = _values["service.config.region"];
            var endpoint = _values["service.config.endpoint.uri"];

            var url = !string.IsNullOrEmpty(endpoint)
                ? endpoint
                : !string.IsNullOrEmpty(host)
                    ? $"{host}/cognitiveservices/voices/list"
                    : !string.IsNullOrEmpty(region)
                        ? $"https://{region}.tts.speech.microsoft.com/cognitiveservices/voices/list"
                        : null;
            return url;
        }

        private void Synthesize()
        {
            StartCommand();

            var kind = _values["synthesizer.input.type"];
            switch (kind)
            {
                case "":
                case null:
                case "interactive":
                    // SynthesizeInteractive(false);
                    // break;
                    
                case "interactive+":
                    SynthesizeInteractive(true);
                    break;

                case "text":
                    SynthesizeText();
                    break;

                case "text.file":
                    SynthesizeTextFile();
                    break;

                case "ssml":
                    SynthesizeSsml();
                    break;

                case "ssml.file":
                    SynthesizeSsmlFile();
                    break;
            }

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private void SynthesizeInteractive(bool repeatedly = false)
        {
            SpeechSynthesizer synthesizer = CreateSpeechSynthesizer();

            synthesizer.SynthesisStarted += SynthesisStarted;
            synthesizer.Synthesizing += Synthesizing;
            synthesizer.SynthesisCompleted += SynthesisCompleted;
            synthesizer.SynthesisCanceled += SynthesisCanceled;
            var wordboundary = _values["config.metadata.wordBoundaryEnabled"];
            if (!string.IsNullOrWhiteSpace(wordboundary) && wordboundary == "true")
            {
                synthesizer.WordBoundary += SynthesisWordBoundary;
            }

            while (true)
            {
                Console.Write("Enter text: ");
                var text = ConsoleHelpers.ReadLineOrDefault("", "exit");

                if (text.ToLower() == "") break;
                if (text.ToLower() == "stop") break;
                if (text.ToLower() == "quit") break;
                if (text.ToLower() == "exit") break;

                var task = synthesizer.SpeakTextAsync(text);
                WaitForStopOrCancel(synthesizer, task);

                if (!repeatedly) break;
                if (_canceledEvent.WaitOne(0)) break;
            }
        }

        private void SynthesizeText()
        {
            var text = _values.GetOrEmpty("synthesizer.input.text");
            SynthesizeText(text);
        }

        private void SynthesizeTextFile()
        {
            var fileName = _values.GetOrEmpty("synthesizer.input.text.file");
            var existing = FileHelpers.DemandFindFileInDataPath(fileName, _values, "text input");
            var text = FileHelpers.ReadAllText(existing, Encoding.Default);
            SynthesizeText(text);
        }

        private void SynthesizeText(string text)
        {
            SpeechSynthesizer synthesizer = CreateSpeechSynthesizer();

            synthesizer.SynthesisStarted += SynthesisStarted;
            synthesizer.Synthesizing += Synthesizing;
            synthesizer.SynthesisCompleted += SynthesisCompleted;
            synthesizer.SynthesisCanceled += SynthesisCanceled;
            var wordboundary = _values["config.metadata.wordBoundaryEnabled"];
            if (!string.IsNullOrWhiteSpace(wordboundary) && wordboundary == "true")
            {
                synthesizer.WordBoundary += SynthesisWordBoundary;
            }

            var task = synthesizer.SpeakTextAsync(text);
            WaitForStopOrCancel(synthesizer, task);
        }

        private void SynthesizeSsml()
        {
            var ssml = _values.GetOrEmpty("synthesizer.input.ssml");
            SynthesizeSsml(ssml);
        }

        private void SynthesizeSsmlFile()
        {
            var fileName = _values.GetOrDefault("synthesizer.input.ssml.file", _values.GetOrEmpty("synthesizer.input.text.file"));
            var existing = FileHelpers.DemandFindFileInDataPath(fileName, _values, "ssml input");
            var content = FileHelpers.ReadAllText(existing, Encoding.Default);

            var isText = !content.TrimStart().StartsWith("<");
            if (isText)
            {
                SynthesizeText(content);
            }
            else
            {
                SynthesizeSsml(content);
            }
        }

        private void SynthesizeSsml(string ssml)
        {
            SpeechSynthesizer synthesizer = CreateSpeechSynthesizer();

            synthesizer.SynthesisStarted += SynthesisStarted;
            synthesizer.Synthesizing += Synthesizing;
            synthesizer.SynthesisCompleted += SynthesisCompleted;
            synthesizer.SynthesisCanceled += SynthesisCanceled;
            var wordboundary = _values["config.metadata.wordBoundaryEnabled"];
            if (!string.IsNullOrWhiteSpace(wordboundary) && wordboundary == "true")
            {
                synthesizer.WordBoundary += SynthesisWordBoundary;
            }

            var task = synthesizer.SpeakSsmlAsync(ssml);
            WaitForStopOrCancel(synthesizer, task);
        }

        private SpeechSynthesizer CreateSpeechSynthesizer()
        {
            SpeechConfig config = CreateSpeechConfig();
            AudioConfig audioConfig = CreateAudioConfig();

            var synthesizer = audioConfig != null
                ? new SpeechSynthesizer(config, audioConfig)
                : new SpeechSynthesizer(config);

            _disposeAfterStop.Add(audioConfig);
            _disposeAfterStop.Add(synthesizer);

            // _output.EnsureCachePropertyCollection("synthesizer", synthesizer.Properties);

            return synthesizer;
        }

        private SpeechConfig CreateSpeechConfig()
        {
            var key = _values["service.config.key"];
            var host = _values["service.config.host"];
            var region = _values["service.config.region"];
            var endpoint = _values["service.config.endpoint.uri"];
            var tokenValue = _values["service.config.token.value"];

            if (_values.Contains("embedded.config.embedded"))
            {
                key = "UNUSED";
                region = "UNUSED";
            }

            if (string.IsNullOrEmpty(endpoint) && string.IsNullOrEmpty(region) && string.IsNullOrEmpty(host))
            {
                _values.AddThrowError("ERROR:", $"Creating SpeechConfig; requires one of: region, endpoint, or host.");
            }
            else if (!string.IsNullOrEmpty(region) && string.IsNullOrEmpty(tokenValue) && string.IsNullOrEmpty(key))
            {
                _values.AddThrowError("ERROR:", $"Creating SpeechConfig; use of region requires one of: key or token.");
            }

            SpeechConfig config = null;
            if (!string.IsNullOrEmpty(endpoint))
            {
                config = string.IsNullOrEmpty(key)
                    ? SpeechConfig.FromEndpoint(new Uri(endpoint))
                    : SpeechConfig.FromEndpoint(new Uri(endpoint), key);
            }
            else if (!string.IsNullOrEmpty(host))
            {
                config = string.IsNullOrEmpty(key)
                    ? SpeechConfig.FromHost(new Uri(host))
                    : SpeechConfig.FromHost(new Uri(host), key);
            }
            else // if (!string.IsNullOrEmpty(region))
            {
                config = string.IsNullOrEmpty(tokenValue)
                    ? SpeechConfig.FromSubscription(key, region)
                    : SpeechConfig.FromAuthorizationToken(tokenValue, region);
            }

            if (!string.IsNullOrEmpty(tokenValue))
            {
                config.AuthorizationToken = tokenValue;
            }

            var format = _values["audio.output.format"];
            if (!string.IsNullOrEmpty(format)) config.SetSpeechSynthesisOutputFormat(AudioOutputHelpers.OutputFormatFrom(format));

            SetSpeechConfigProperties(config);
            return config;
        }

        private void SetSpeechConfigProperties(SpeechConfig config)
        {
            ConfigHelpers.SetupLogFile(config, _values);

            var voice = _values["synthesizer.output.voice.name"];
            if (!string.IsNullOrEmpty(voice)) config.SpeechSynthesisVoiceName = voice;

            var language = _values["target.language.config"];
            if (!string.IsNullOrEmpty(language)) config.SpeechSynthesisLanguage = language;

            var proxyHost = _values["service.config.proxy.host"];
            if (!string.IsNullOrEmpty(proxyHost)) config.SetProxy(proxyHost, _values.GetOrDefault("service.config.proxy.port", 80));

            var endpointId = _values["service.config.endpoint.id"];
            if (!string.IsNullOrEmpty(endpointId)) config.EndpointId = endpointId;

            // var needDetailedText = _output.NeedsLexicalText() || _output.NeedsItnText();
            // if (needDetailedText) config.OutputFormat = OutputFormat.Detailed;

            // var profanity = _values["service.output.config.profanity.option"];
            // if (profanity == "removed") config.SetProfanity(ProfanityOption.Removed);
            // if (profanity == "masked") config.SetProfanity(ProfanityOption.Masked);
            // if (profanity == "raw") config.SetProfanity(ProfanityOption.Raw);

            // var contentLogging = _values.GetOrDefault("service.config.content.logging.enabled", false);
            // if (contentLogging) config.EnableAudioLogging();

            var trafficType = _values.GetOrDefault("service.config.endpoint.traffic.type", "spx");
            config.SetServiceProperty("traffictype", trafficType, ServicePropertyChannel.UriQueryParameter);

            var endpointParam = _values.GetOrEmpty("service.config.endpoint.query.string");
            if (!string.IsNullOrEmpty(endpointParam)) ConfigHelpers.SetEndpointParams(config, endpointParam);

            var httpHeader = _values.GetOrEmpty("service.config.endpoint.http.header");
            if (!string.IsNullOrEmpty(httpHeader)) SetHttpHeaderProperty(config, httpHeader);

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
            // Use embedded (offline) text-to-speech engine.
            config.SetProperty("SPEECH-SynthesisBackend", "offline");
            // The device neural voices only support 24kHz and the offline engine has no ability to resample
            config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm);

            var modelKey = _values.GetOrEmpty("embedded.config.model.key");
            config.SetProperty("SPEECH-SynthesisModelKey", modelKey);

            var modelPath = _values.GetOrEmpty("embedded.config.model.path");
            var modelXmlFileFullPath = Path.GetFullPath(Path.Combine(modelPath, "Tokens.xml"));
            if (!File.Exists(modelXmlFileFullPath))
            {
                _values.AddThrowError(
                    "ERROR:", $"Missing or invalid speech synthesis model path!", "",
                      "USE:", $"{Program.Name} synthesize --embedded --embeddedModelPath PATH [...]");
            }
            config.SetProperty("SPEECH-SynthesisOfflineDataPath", modelPath);
        }

        private static void SetHttpHeaderProperty(SpeechConfig config, string httpHeader)
        {
            string name = "", value = "";
            if (StringHelpers.SplitNameValue(httpHeader, out name, out value)) config.SetServiceProperty(name, value, ServicePropertyChannel.HttpHeader);
        }

        private void CheckNotYetImplementedConfigProperties()
        {
            var notYetImplemented = 
                ";config.token.type;config.token.password;config.token.username" +
                ";synthesizer.property";

            foreach (var key in notYetImplemented.Split(';'))
            {
                var value = _values[key];
                if (!string.IsNullOrEmpty(value))
                {
                    _values.AddThrowError("WARNING:", $"'{key}={value}' NOT YET IMPLEMENTED!!");
                }
            }
        }

        private void CheckSynthesizerInput()
        {
            var id = _values["synthesizer.input.id"];
            var device = _values["audio.input.microphone.device"];
            var input = _values["synthesizer.input.type"];

            var fileValueDisplayName = _values.Contains("synthesizer.input.text.file") ? "text file" : "ssml file";
            var fileValueName = _values.Contains("synthesizer.input.text.file") ? "synthesizer.input.text.file" : "synthesizer.input.ssml.file";
            var file = _values.GetOrDefault(fileValueName, "");
            var url = "";

            if (!string.IsNullOrEmpty(file) && file.StartsWith("http"))
            {
                file = DownloadInputFile(url = file, fileValueName, fileValueDisplayName);
            }

            if (string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(url))
            {
                id = GetIdFromInputUrl(url, "synthesizer.input.id");
            }

            if (string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(file))
            {
                id = GetIdFromInputFile(input, file, "synthesizer.input.id", fileValueDisplayName);
            }

            if (string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(id))
            {
                input = GetInputFromId(id);
            }

            if (input.EndsWith("file") && string.IsNullOrEmpty(file) && !string.IsNullOrEmpty(id))
            {
                file = GetInputFileFromId(id);
            }

            // _interactive = (input == "microphone" || string.IsNullOrEmpty(input));
        }

        private string GetIdFromInputFile(string input, string file, string idValueName, string fileValueDisplayName)
        {
            string id;
            if (input == "speaker" || string.IsNullOrEmpty(input))
            {
                id = "speaker";
            }
            else if (input.EndsWith("file") && !string.IsNullOrEmpty(file))
            {
                var existing = FileHelpers.DemandFindFileInDataPath(file, _values, fileValueDisplayName);
                id = Path.GetFileNameWithoutExtension(existing);
            }
            else
            {
                id = "error";
            }

            _values.Add(idValueName, id);
            return id;
        }

        private string GetInputFromId(string id)
        {
            string input;
            if (id == "speaker")
            {
                input = "speaker";
            }
            else if (FileHelpers.FileExistsInDataPath(id + ".txt", _values))
            {
                input = "text.file";
            }
            else if (FileHelpers.FileExistsInDataPath(id, _values) ||
                     FileHelpers.FileExistsInDataPath(id + ".ssml", _values))
            {
                input = "ssml.file";
            }
            else if (_values.Contains("synthesizer.input.id.url"))
            {
                input = "ssml.file";
            }
            else
            {
                _values.AddThrowError("ERROR:", $"Cannot find synthesis input file: \"{id}.txt\" or \"{id}.ssml\"");
                return null;
            }

            _values.Add("synthesizer.input.type", input);
            return input;
        }

        private string GetInputFileFromId(string id)
        {
            string file;
            var existing = FileHelpers.FindFileInDataPath(id, _values);
            if (existing == null) existing = FileHelpers.FindFileInDataPath(id + ".txt", _values);
            if (existing == null) existing = FileHelpers.FindFileInDataPath(id + ".ssml", _values);

            if (existing == null)
            {
                var url = _values["synthesizer.input.id.url"];
                if (!string.IsNullOrEmpty(url))
                {
                    url = url.Replace("{id}", id);
                    existing = HttpHelpers.DownloadFileWithRetry(url);
                }
            }

            file = existing;
            _values.Add(existing.EndsWith(".txt") ? "synthesizer.input.text.file" : "synthesizer.input.ssml.file", file);
            return file;
        }

        private AudioConfig CreateAudioConfig()
        {
            var output = _values["audio.output.type"];
            var file = _values["audio.output.file"];

            AudioConfig audioConfig = null;
            if (output == "speaker" || string.IsNullOrEmpty(output))
            {
                audioConfig = AudioOutputHelpers.CreateAudioConfigForSpeaker();
            }
            else if (output == "file" && !string.IsNullOrEmpty(file))
            {
                file = ReplaceFileNameValues(file, "synthesizer.input.id");
                audioConfig = AudioOutputHelpers.CreateAudioConfigForFile(file);
            }
            else
            {
                _values.AddThrowError("WARNING:", $"'audio.output.type={output}' NOT YET IMPLEMENTED!!");
            }

            return audioConfig;
        }

        private void SynthesisStarted(object sender, SpeechSynthesisEventArgs e)
        {
            _lock.EnterReaderLockOnce(ref _expectSynthesisCompleted);
            _stopEvent.Reset();

            _display.DisplaySynthesisStarted(e);
            _output.SynthesisStarted(e);
        }

        private void Synthesizing(object sender, SpeechSynthesisEventArgs e)
        {
            _display.DisplaySynthesizing(e);
            _output.Synthesizing(e);
        }

        private void SynthesisCompleted(object sender, SpeechSynthesisEventArgs e)
        {
            _display.DisplaySynthesisCompleted(e);
            _output.SynthesisCompleted(e);

            _stopEvent.Set();
            _lock.ExitReaderLockOnce(ref _expectSynthesisCompleted);
        }

        private void SynthesisCanceled(object sender, SpeechSynthesisEventArgs e)
        {
            _display.DisplaySynthesisCanceled(e);
            _output.SynthesisCanceled(e);
            _canceledEvent.Set();
        }

        private void SynthesisWordBoundary(object sender, SpeechSynthesisWordBoundaryEventArgs e)
        {
            _display.DisplaySynthesisWordBoundary(e);
            _output.SynthesisWordBoundary(e);
        }

        private void WaitForStopOrCancel(SpeechSynthesizer synthesizer, Task<SpeechSynthesisResult> task)
        {
            var interval = 100;

            while (!task.Wait(interval))
            {
                if (_stopEvent.WaitOne(0)) break;
                if (_canceledEvent.WaitOne(0)) break;
            }
        }

        private void StartCommand()
        {
            CheckPath();
            CheckSynthesizerInput();

            _display = new DisplayHelper(_values);

            _output = new OutputHelper(_values);
            _output.StartOutput();

            var id = _values["synthesizer.input.id"];
            _output.EnsureOutputAll("synthesizer.input.id", id);
            _output.EnsureOutputEach("synthesizer.input.id", id);

            _lock = new SpinLock();
            _lock.StartLock();

            _expectSynthesisCompleted = 0;
        }

        private void StopCommand()
        {
            _lock.StopLock(5000);

            _output.CheckOutput();
            _output.StopOutput();
        }

        private SpinLock _lock = null;
        private int _expectSynthesisCompleted = 0;

        OutputHelper _output = null;
        DisplayHelper _display = null;
    }
}
