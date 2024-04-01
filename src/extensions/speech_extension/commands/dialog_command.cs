//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Bot.Schema;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Dialog;

namespace Azure.AI.Details.Common.CLI
{
    // TODO: should support continuous
    // TODO: should support connect/disconnect
    // TODO: find other things it should support by comparing with RecognizeCommand
    
    public enum DialogType
    {
        Bot, CustomCommands
    }

    public class DialogCommand : RecognizeCommandBase
    {
        internal DialogCommand(ICommandValues values) : base(values)
        {
            _activityReceivedTimeout = _values.GetOrDefault("recognize.timeout", _microphone ? 10000 : int.MaxValue);
        }

        internal bool RunCommand()
        {
            Recognize(_values["recognize.method"]);
            return _values.GetOrDefault("passed", true);
        }

        private void Recognize(string? recognize)
        {
            StartCommand();

            switch (recognize)
            {
                case "":
                case null:
                case "continuous": RecognizeContinuous(); break;

                case "once": RecognizeOnce(); break;

                case "keyword": RecognizeKeyword(); break;
            }

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private void RecognizeOnce()
        {
            if (_connector == null) _connector = CreateDialogServiceConnector();

            if (_microphone) { PrintToConsole("Listening..."); }

            var task = _connector.ListenOnceAsync();
            WaitForOnceStopCancelOrKey(_connector, task);

            PrintToConsole("Checking for activity from backend; Press ENTER to skip...");
            WaitForExpectingInputActivityOrTimeout(_activityReceivedTimeout);
            HandleExpectingInputActivityEvent();
        }

        private void RecognizeContinuous()
        {
            if (_connector == null) _connector = CreateDialogServiceConnector();

            while (true)
            {
                if (_microphone) { PrintToConsole("Listening; press ENTER to stop ...\n"); }
                var task = _connector.ListenOnceAsync();
                WaitForOnceStopCancelOrKey(_connector, task);

                if (_canceledEvent.WaitOne(0)) break;
                if (_microphone && Console.KeyAvailable) break;
                if (_microphone && task.Result.Text == "Stop listening.") break;
            }

            PrintToConsole("Checking for activity from backend; Press ENTER to skip...");
            WaitForExpectingInputActivityOrTimeout(_activityReceivedTimeout);
            HandleExpectingInputActivityEvent();
        }

        private void RecognizeKeyword()
        {
            if (_connector == null) _connector = CreateDialogServiceConnector();

            var keywordModel = LoadKeywordModel();
            _connector.StartKeywordRecognitionAsync(keywordModel).Wait();
                
            if (_microphone) { PrintToConsole("Listening for keyword; Press ENTER to stop...");  }
            var timeout = _values.GetOrDefault("recognize.timeout", int.MaxValue);
            WaitForKeywordCancelKeyOrTimeout(_connector, _keywordRecognizedEvent, timeout);

            PrintToConsole("Checking for activity from backend; Press ENTER to skip...");
            WaitForExpectingInputActivityOrTimeout(_activityReceivedTimeout);
            HandleExpectingInputActivityEvent();
        }

        private DialogServiceConnector CreateDialogServiceConnector()
        {
            var config = CreateDialogServiceConfig();
            var audioConfig = ConfigHelpers.CreateAudioConfig(_values);

            DialogServiceConnector connector = new DialogServiceConnector(config, audioConfig);

            connector.SessionStarted += SessionStarted;
            connector.SessionStopped += SessionStopped;
            connector.Recognizing += Recognizing;
            connector.Recognized += Recognized;
            connector.ActivityReceived += ActivityReceived;
            connector.Canceled += Canceled;

            _disposeAfterStop.Add(audioConfig);
            _disposeAfterStop.Add(connector);

            // _output!.EnsureCachePropertyCollection("recognizer", connector.Properties);

            return connector;
        }

        private DialogServiceConfig CreateDialogServiceConfig(string? key = null, string? region = null, string? botId = null, string? ccAppId = null)
        {
            key = string.IsNullOrEmpty(key) ? _values["service.config.key"] : key;
            region = string.IsNullOrEmpty(region) ? _values["service.config.region"] : region;
            var tokenValue = _values["service.config.token.value"];

            if (!string.IsNullOrEmpty(region) && string.IsNullOrEmpty(tokenValue) && string.IsNullOrEmpty(key))
            {
                _values.AddThrowError("ERROR:", $"Creating DialogServiceConfig; requires one of: key or token.");
            }

            var dialogType = _values.GetCommand() == "dialog.customcommands"
                                ? DialogType.CustomCommands
                                : DialogType.Bot;
            DialogServiceConfig config;

            if (dialogType == DialogType.Bot)
            {
                botId = string.IsNullOrEmpty(botId) ? _values["dialog.bot.id"] : botId;
                botId = string.IsNullOrEmpty(botId) ? string.Empty : botId;

                config = string.IsNullOrEmpty(tokenValue)
                    ? BotFrameworkConfig.FromSubscription(key, region, botId)
                    : BotFrameworkConfig.FromAuthorizationToken(tokenValue, region, botId);
            }
            else
            {
                ccAppId = string.IsNullOrEmpty(ccAppId) ? _values["dialog.customcommands.appid"] : ccAppId;

                if (string.IsNullOrEmpty(ccAppId))
                {
                    _values.AddThrowError("ERROR:", $"Creating CustomCommandsConfig; appId is required.");
                }

                config = string.IsNullOrEmpty(tokenValue)
                    ? CustomCommandsConfig.FromSubscription(ccAppId, key, region)
                    : CustomCommandsConfig.FromAuthorizationToken(ccAppId, tokenValue, region);
            }

            return config;
        }

        private new void Recognized(object? sender, SpeechRecognitionEventArgs e)
        {
            base.Recognized(sender, e);

            if (e.Result.Reason == ResultReason.RecognizedKeyword)
            {
                _connector!.StopKeywordRecognitionAsync().Wait();
                _keywordRecognizedEvent.Set();
            }
        }

        private void ActivityReceived(object? sender, ActivityReceivedEventArgs e)
        {
            var activity = JsonSerializer.Deserialize<Activity>(e.Activity);
            if (e.HasAudio && activity?.Speak != null)
            {
                // Audio playback workaround pending Speech SDK work to include
                // automatic audio rendering in the DialogServiceConnector.
                PrintToConsole("Playing back audio from received activity via `spx synthesize`");

                if (_ssmlRegex.IsMatch(activity.Speak))
                {
                    _values.Reset("synthesizer.input.type", "ssml");
                    _values.Reset("synthesizer.input.ssml", activity.Speak);
                }
                else
                {
                    _values.Reset("synthesizer.input.type", "text");
                    _values.Reset("synthesizer.input.text", activity.Speak);
                }

                (new SynthesizeCommand(_values)).RunCommand();

                PrintToConsole("Audio playback complete.");
            }

            if (activity?.Type == ActivityTypes.Message && 
                activity.InputHint == InputHints.ExpectingInput)
            {
                // Signal to listen for multi-turn input
                _expectingInputActivityEvent.Set();
            }
        }

        private void WaitForExpectingInputActivityOrTimeout(int timeout)
        {
           var interval = 100;

            while (timeout > 0)
            {
                timeout -= interval;
                if (_expectingInputActivityEvent.WaitOne(interval)) break;
                if (_microphone && Console.KeyAvailable) break;
            }
        }

        private void HandleExpectingInputActivityEvent()
        {
            if (_expectingInputActivityEvent.WaitOne(0))
            {
                _expectingInputActivityEvent.Reset();
                RecognizeOnce();
            }
        }

        private void PrintToConsole(string text)
        {
            Console.WriteLine($"DIALOG: {text}");
        }

        private DialogServiceConnector? _connector;
        private Regex _ssmlRegex = new Regex("(<speak)(.*|\\s*)*(</speak>)");

        private ManualResetEvent _keywordRecognizedEvent = new ManualResetEvent(false);
        private ManualResetEvent _expectingInputActivityEvent = new ManualResetEvent(false);
        private int _activityReceivedTimeout;

    }
}
