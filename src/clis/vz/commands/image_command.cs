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
using Newtonsoft.Json.Linq;
using Azure.Core;
using Azure.AI.Vision.ImageAnalysis;
using Azure.AI.Vision.Common;
using Azure.AI.Vision.Common.Diagnostics.Logging;

namespace Azure.AI.Details.Common.CLI
{
    public class AccessTokenCredential : TokenCredential
    {
        public AccessToken AccessToken;
        public event EventHandler<EventArgs> OnGetToken;

        public AccessTokenCredential(AccessToken accessToken)
        {
            AccessToken = accessToken;
        }

        public AccessTokenCredential(string token, DateTimeOffset expires)
        {
            AccessToken = new AccessToken(token, expires);
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            OnGetToken.Invoke(this, null);
            return AccessToken;
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            OnGetToken.Invoke(this, null);
            return new ValueTask<AccessToken>(AccessToken);
        }
    }

    public class ImageCommand : Command
    {
        internal ImageCommand(ICommandValues values) : base(values)
        {
           _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", true);
        }

        internal bool RunCommand()
        {
            try
            {
                RunImageCommand();
            }
            catch (WebException ex)
            {
                FileHelpers.LogException(_values, ex);
                ConsoleHelpers.WriteLineError($"\n  ERROR: {ex.Message}");
                JsonHelpers.PrintJson(HttpHelpers.ReadWriteJson(ex.Response, _values, "batch"));
            }

            return _values.GetOrDefault("passed", true);
        }

        private bool RunImageCommand()
        {
            DoCommand(_values.GetCommand());
            return _values.GetOrDefault("passed", true);
        }

        private void DoCommand(string command)
        {
            CheckPath();

            switch (command)
            {
                case "image.analyze": DoImageAnalyze(false); break;
                case "image.read": DoImageRead(); break;

                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
            }
        }

        private void DoImageAnalyze(bool repeatedly)
        {
            StartCommand();

            var analyzer = CreateImageAnalyzer();
            analyzer.Analyzed += Analyzed;

            // analyzer.SessionStarted += SessionStarted;
            // analyzer.SessionStopped += SessionStopped;

            if (_camera) { Console.WriteLine("Capturing (camera)..."); }

            while (true)
            {
                var task = analyzer.AnalyzeAsync();
                WaitForOnceStopCancelOrKey(analyzer, task);

                if (task.IsCompleted)
                {
                    var result = task.Result;
                    if (result.Reason == ImageAnalysisResultReason.Error)
                    {
                        var errorDetails = ImageAnalysisErrorDetails.FromResult(result);
                        Console.WriteLine($"reason={result.Reason}");
                        Console.WriteLine($"code={errorDetails.ErrorCode}");
                        Console.WriteLine($"message={errorDetails.Message}");
                        Console.WriteLine();
                    }
                }

                // if (_disconnect) AnalyzerConnectionDisconnect(analyzer);

                if (!repeatedly) break;
                if (_canceledEvent.WaitOne(0)) break;
                if (_camera && Console.KeyAvailable) break;
            }

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private void DoImageRead()
        {
            Console.WriteLine("DoImageRead not yet implemted...");
        }

        private ImageAnalyzer CreateImageAnalyzer()
        {
            var serviceOptions = CreateVisionServiceOptions();
            var visionSource = CreateVisionSource();
            var options = CreateImageAnalysisOptions();
            var analyzer = new ImageAnalyzer(serviceOptions, visionSource, options);

            // _disposeAfterStop.Add(visionSource);
            _disposeAfterStop.Add(analyzer);

            // _output!.EnsureCachePropertyCollection("recognizer", recognizer.Properties);

            return analyzer;
        }

        private VisionServiceOptions CreateVisionServiceOptions()
        {
            var endpoint = _values["service.config.endpoint.uri"];
            var tokenValue = _values["service.config.token.value"];
            var key = _values["service.config.key"];

            // var region = _values["service.config.region"];
            // var host = values["service.config.host"];

            if (string.IsNullOrEmpty(endpoint))
            {
                _values.AddThrowError("ERROR:", $"Creating VisionServiceOptions; requires endpoint.");
            }

            VisionServiceOptions serviceOptions;
            if (!string.IsNullOrEmpty(key))
            {
                serviceOptions = new VisionServiceOptions(endpoint, new AzureKeyCredential(key));
            }
            else if (!string.IsNullOrEmpty(tokenValue))
            {
                var expires = DateTimeOffset.UtcNow.AddMinutes(10);
                var tokenCredential = new AccessTokenCredential(tokenValue, expires);
                serviceOptions = new VisionServiceOptions(endpoint, tokenCredential);
            }
            else
            {
                serviceOptions = null;
                _values.AddThrowError("ERROR:", $"Creating VisionServiceOptions; requires one of: key or token.");
            }

            // var stringProperty = values.GetOrEmpty("config.string.property");
            // if (!string.IsNullOrEmpty(stringProperty)) ConfigHelpers.SetStringProperty(config, stringProperty);

            // var stringProperties = values.GetOrEmpty("config.string.properties");
            // if (!string.IsNullOrEmpty(stringProperties)) ConfigHelpers.SetStringProperties(config, stringProperties);

            return serviceOptions;
        }

        private VisionSource CreateVisionSource()
        {
            var input = _values["vision.input.type"];
            var file = _values["vision.input.file"];
            var device = _values["vision.input.camera.device"];

            VisionSource source;
            if (input == "camera" || string.IsNullOrEmpty(input))
            {
                if (!string.IsNullOrEmpty(device))
                {
                    var camera = VisionCamera.GetCameras().Where(x => x.Description == device);
                    source = VisionSource.FromCamera(camera.First());
                }
                else
                {
                    source = VisionSource.FromDefaultCaptureDevice();
                }
            }
            else if (input == "file" && !string.IsNullOrEmpty(file))
            {
                file = FileHelpers.DemandFindFileInDataPath(file, _values, "vision input");
                source = VisionSource.FromFile(file);
            }
            else
            {
                _values.AddThrowError("WARNING:", $"'vision.input.type={input}' NOT YET IMPLEMENTED!!");
                return null;
            }

            return source;
        }

        private ImageAnalysisOptions CreateImageAnalysisOptions()
        {
            var featuresRequested = _values.GetOrEmpty("vision.image.visual.features");
            var features = string.IsNullOrEmpty(featuresRequested)
                ? GetDefaultImageAnalysisFeatures()
                : GetImageAnalysisFeatures(featuresRequested);

            var language = _values.GetOrDefault("source.language.config", "en");
            var options = new ImageAnalysisOptions()
            {
                Language = language,
                Features = features
            };
            return options;
        }

        private static ImageAnalysisFeature GetDefaultImageAnalysisFeatures()
        {
            return ImageAnalysisFeature.Tags
                 | ImageAnalysisFeature.Objects
                 | ImageAnalysisFeature.People
                 | ImageAnalysisFeature.CropSuggestions
                 | ImageAnalysisFeature.Caption
                 | ImageAnalysisFeature.Text;
        }

        private ImageAnalysisFeature GetImageAnalysisFeatures(string requested)
        {
            ImageAnalysisFeature features = 0;

            var items = requested.Split(new char[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in items)
            {
                if (!Enum.TryParse<ImageAnalysisFeature>(item.Trim(), true, out ImageAnalysisFeature feature))
                {
                    _values.AddThrowError(
                        "ERROR:", $"'{item}' is not a valid visual feature.",
                          "SEE:", $"{Program.Name} help image analyze feature");
                }

                features |= feature;
            }
            return features;
        }

        private void Analyzed(object? sender, ImageAnalysisEventArgs e)
        {
            _display!.DisplayAnalyzed(e);
            // _output!.Recognized(e);
            _lock!.ExitReaderLockOnce(ref _expectAnalyzed);
        }

        private void CheckVisionInput()
        {
            var id = _values["vision.input.id"];
            var device = _values["vision.input.camera.device"];
            var input = _values["vision.input.type"];
            var file = _values["vision.input.file"];
            var url = "";

            if (!string.IsNullOrEmpty(file) && file.StartsWith("http"))
            {
                file = DownloadInputFile(url = file, "vision.input.file", "vision input");
            }

            if (string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(url))
            {
                id = GetIdFromInputUrl(url, "vision.input.id");
            }

            if (string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(file))
            {
                id = GetIdFromVisionInputFile(input, file);
            }

            if (string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(id))
            {
                input = GetVisionInputFromId(id);
            }

            if (input == "file" && string.IsNullOrEmpty(file) && !string.IsNullOrEmpty(id))
            {
                file = GetVisionInputFileFromId(id);
            }

            _camera = (input == "camera" || string.IsNullOrEmpty(input));
        }

        private string GetIdFromVisionInputFile(string input, string file)
        {
            string id;
            if (input == "camera" || string.IsNullOrEmpty(input))
            {
                id = "camera";
            }
            else if (input == "file" && !string.IsNullOrEmpty(file))
            {
                var existing = FileHelpers.DemandFindFileInDataPath(file, _values, "vision input");
                id = Path.GetFileNameWithoutExtension(existing);
            }
            else
            {
                id = "error";
            }

            _values.Add("vision.input.id", id);
            return id;
        }

        private string GetVisionInputFromId(string id)
        {
            string input;
            if (id == "camera")
            {
                input = "camera";
            }
            else if (FileHelpers.FileExistsInDataPath(id, _values) || FileHelpers.FileExistsInDataPath(id + ".png", _values) || FileHelpers.FileExistsInDataPath(id + ".jpg", _values))
            {
                input = "file";
            }
            else if (_values.Contains("vision.input.id.url"))
            {
                input = "file";
            }
            else
            {
                _values.AddThrowError("ERROR:", $"Cannot find vision input file: \"{id}.png\"");
                return null;
            }

            _values.Add("vision.input.type", input);
            return input;
        }

        private string GetVisionInputFileFromId(string id)
        {
            string file;
            var existing = FileHelpers.FindFileInDataPath(id, _values);
            if (existing == null) existing = FileHelpers.FindFileInDataPath(id + ".png", _values);
            if (existing == null) existing = FileHelpers.FindFileInDataPath(id + ".jpg", _values);

            if (existing == null)
            {
                var url = _values["vision.input.id.url"];
                if (!string.IsNullOrEmpty(url))
                {
                    url = url.Replace("{id}", id);
                    existing = HttpHelpers.DownloadFileWithRetry(url);
                }
            }

            file = existing;
            _values.Add("vision.input.file", file);
            return file;
        }

        private void WaitForOnceStopCancelOrKey(ImageAnalyzer analyzer, Task<ImageAnalysisResult> task)
        {
            var interval = 100;

            while (!task.Wait(interval))
            {
                if (_stopEvent.WaitOne(0)) break;
                if (_canceledEvent.WaitOne(0)) break;
                if (_camera && Console.KeyAvailable)
                {
                    // analyzer.StopContinuousRecognitionAsync().Wait();
                    break;
                }
            }
        }

        public void EnsureStartLogFile()
        {
            var log = _values["diagnostics.config.log.file"];
            if (!string.IsNullOrEmpty(log))
            {
                var id = _values.GetOrEmpty("vision.input.id");
                if (log.Contains("{id}")) log = log.Replace("{id}", id);

                var pid = Process.GetCurrentProcess().Id.ToString();
                if (log.Contains("{pid}")) log = log.Replace("{pid}", pid);

                var time = DateTime.Now.ToFileTime().ToString();
                if (log.Contains("{time}")) log = log.Replace("{time}", time);

                var runTime = _values.GetOrEmpty("x.run.time");
                if (log.Contains("{run.time}")) log = log.Replace("{run.time}", runTime);

                log = FileHelpers.GetOutputDataFileName(log, _values);
                FileLogger.Start(log);
            }
        }

        public void EnsureStopLogFile()
        {
            var log = _values["diagnostics.config.log.file"];
            if (!string.IsNullOrEmpty(log))
            {
                FileLogger.Stop();
            }
        }

        private void StartCommand()
        {
            CheckPath();
            CheckVisionInput();
            EnsureStartLogFile();

            _display = new DisplayHelper(_values);

            // _output = new OutputHelper(_values);
            // _output!.StartOutput();

            // var id = _values["vision.input.id"];
            // _output!.EnsureOutputAll("vision.input.id", id);
            // _output!.EnsureOutputEach("vision.input.id", id);
            // _output!.EnsureCacheProperty("vision.input.id", id);

            // var file = _values["vision.input.file"];
            // _output!.EnsureCacheProperty("vision.input.file", file);

            _lock = new SpinLock();
            _lock.StartLock();

            _expectAnalyzed = 0;
            // _expectSessionStopped = 0;
            // _expectDisconnected = 0;
        }

        private void StopCommand()
        {
            _lock!.StopLock(5000);

            // This shouldn't really be here, but since there is no session stopped event it hast to be here
            _stopEvent.Set();

            // _output!.CheckOutput();
            // _output!.StopOutput();
        }

        private bool _quiet = false;
        private bool _verbose = false;

        private bool _camera = false;
        // private bool _connect = false;
        // private bool _disconnect = false;

        private SpinLock? _lock = null;
        private int _expectAnalyzed = 0;
        // private int _expectSessionStopped = 0;
        // private int _expectDisconnected = 0;

        // OutputHelper? _output = null;
        DisplayHelper? _display = null;
    }
}
