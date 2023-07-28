//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Dialog;

namespace Azure.AI.Details.Common.CLI
{
    public class RecognizeCommandBase : Command
    {
        protected void StartCommand()
        {
            CheckPath();
            CheckAudioInput();

            _display = new DisplayHelper(_values);

            _output = new OutputHelper(_values);
            _output.StartOutput();

            var id = _values["audio.input.id"];
            _output.EnsureOutputAll("audio.input.id", id);
            _output.EnsureOutputEach("audio.input.id", id);
            _output.EnsureCacheProperty("audio.input.id", id);

            var file = _values["audio.input.file"];
            _output.EnsureCacheProperty("audio.input.file", file);

            _lock = new SpinLock();
            _lock.StartLock();

            _expectRecognized = 0;
            _expectSessionStopped = 0;
            _expectDisconnected = 0;
        }

        protected void StopCommand()
        {
            _lock.StopLock(5000);

            _output.CheckOutput();
            _output.StopOutput();
        }

        protected KeywordRecognitionModel LoadKeywordModel()
        {
            var fileName = _values["recognize.keyword.file"];
            var existing = FileHelpers.DemandFindFileInDataPath(fileName, _values, "keyword model");

            var keywordModel = KeywordRecognitionModel.FromFile(existing);
            return keywordModel;
        }

        protected void WaitForKeywordCancelKeyOrTimeout(SpeechRecognizer recognizer, int timeout)
        {
            WaitForKeywordCancelKeyOrTimeoutInternal(_canceledEvent, timeout);
            recognizer.StopKeywordRecognitionAsync();
        }

        protected void WaitForKeywordCancelKeyOrTimeout(DialogServiceConnector connector, ManualResetEvent keywordRecognizedEvent, int timeout)
        {
            WaitForKeywordCancelKeyOrTimeoutInternal(_canceledEvent, keywordRecognizedEvent, timeout);
            connector.StopKeywordRecognitionAsync();
        }

        protected void WaitForOnceStopCancelOrKey(SpeechRecognizer recognizer, Task<SpeechRecognitionResult> task)
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

        protected void WaitForOnceStopCancelOrKey(DialogServiceConnector connector, Task<SpeechRecognitionResult> task)
        {
            var interval = 100;

            while (!task.Wait(interval))
            {
                if (_stopEvent.WaitOne(0)) break;
                if (_canceledEvent.WaitOne(0)) break;
                if (_microphone && Console.KeyAvailable)
                {
                    connector.StopListeningAsync().Wait();
                    break;
                }
            }
        }

        #region Event Handlers
        protected void SessionStarted(object sender, SessionEventArgs e)
        {
            _lock.EnterReaderLockOnce(ref _expectSessionStopped);
            _stopEvent.Reset();

            _display.DisplaySessionStarted(e);
            _output.SessionStarted(e);
        }

        protected void SessionStopped(object sender, SessionEventArgs e)
        {
            _display.DisplaySessionStopped(e);
            _output.SessionStopped(e);

            _stopEvent.Set();
            _lock.ExitReaderLockOnce(ref _expectSessionStopped);
        }

        protected void Recognizing(object sender, SpeechRecognitionEventArgs e)
        {
            _lock.EnterReaderLockOnce(ref _expectRecognized);

            _display.DisplayRecognizing(e);
            _output.Recognizing(e);
        }

        protected void Recognized(object sender, SpeechRecognitionEventArgs e)
        {
            _display.DisplayRecognized(e);
            _output.Recognized(e);

            _lock.ExitReaderLockOnce(ref _expectRecognized);
        }

        protected void Canceled(object sender, SpeechRecognitionCanceledEventArgs e)
        {
            _display.DisplayCanceled(e);
            _output.Canceled(e);
            _canceledEvent.Set();
        }
        #endregion

        #region audio input checks
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

        private string GetIdFromAudioInputFile(string input, string file)
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

        private string GetAudioInputFromId(string id)
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

        private string GetAudioInputFileFromId(string id)
        {
            string file;
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

            file = existing;
            _values.Add("audio.input.file", file);
            return file;
        }
        #endregion

        #region private backing methods

        private void WaitForKeywordCancelKeyOrTimeoutInternal(ManualResetEvent canceledEvent, int timeout)
        {
            var interval = 100;

            while (timeout > 0)
            {
                timeout -= interval;
                if (canceledEvent.WaitOne(interval)) break;
                if (_microphone && Console.KeyAvailable)
                {
                    break;
                }
            }
        }

        private void WaitForKeywordCancelKeyOrTimeoutInternal(ManualResetEvent canceledEvent, ManualResetEvent keywordRecognizedEvent, int timeout)
        {
            var interval = 100;

            while (timeout > 0)
            {
                timeout -= interval;
                if (canceledEvent.WaitOne(interval)) break;
                if (keywordRecognizedEvent.WaitOne(0)) break;
                if (_microphone && Console.KeyAvailable) break;
            }
        }
        #endregion

        protected bool _microphone = false;
        protected bool _connect = false;
        protected bool _disconnect = false;

        protected SpinLock _lock = null;
        protected int _expectRecognized = 0;
        protected int _expectSessionStopped = 0;
        protected int _expectDisconnected = 0;

        OutputHelper _output = null;
        DisplayHelper _display = null;
    }
}
