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
using Microsoft.CognitiveServices.Speech.Translation;
using Microsoft.CognitiveServices.Speech.Transcription;
using System.Collections.Generic;

namespace Azure.AI.Details.Common.CLI
{
    public class DisplayHelper
    {
        public DisplayHelper(ICommandValues values)
        {
            _values = values;
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = values.GetOrDefault("x.verbose", false);
        }

        public void DisplayConnected(ConnectionEventArgs e)
        {
            if (_quiet) return;
            Console.WriteLine("Connection CONNECTED...");
        }

        public void DisplayDisconnected(ConnectionEventArgs e)
        {
            if (_quiet) return;
            Console.WriteLine("Connection DISCONNECTED.");
        }

        public void DisplayMessageReceived(ConnectionMessageEventArgs e)
        {
            if (_quiet || !_verbose) return;
            Console.WriteLine("MESSAGE RECEIVED: {0}", e.Message.Path);
        }

        public void DisplaySessionStarted(SessionEventArgs e)
        {
            if (_quiet) return;
            Console.WriteLine("SESSION STARTED: {0}\n", e.SessionId);
        }

        public void DisplaySessionStopped(SessionEventArgs e)
        {
            if (_quiet) return;
            Console.WriteLine("SESSION STOPPED: {0}", e.SessionId);
        }

        public void DisplayRecognizing(SpeechRecognitionEventArgs e)
        {
            if (_quiet) return;
            Console.WriteLine($"RECOGNIZING: {e.Result.Text}");
        }

        public void DisplayRecognized(SpeechRecognitionEventArgs e)
        {
            if (_quiet) return;

            var result = e.Result;
            if (result.Reason == ResultReason.RecognizedKeyword && result.Text.Length != 0)
            {
                Console.WriteLine($"RECOGNIZED: {result.Text}");
                Console.WriteLine();
            }
            else if (result.Reason == ResultReason.RecognizedSpeech && result.Text.Length != 0)
            {
                Console.WriteLine($"RECOGNIZED: {result.Text}");
                Console.WriteLine();
            }
            else if (result.Reason == ResultReason.NoMatch && _verbose)
            {
                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                Console.WriteLine();
            }
        }

        public void DisplayCanceled(SpeechRecognitionCanceledEventArgs e)
        {
            if (_quiet) return;

            var cancellation = CancellationDetails.FromResult(e.Result);

            var normal = cancellation.Reason == CancellationReason.EndOfStream;
            if (normal && !_verbose) return;

            Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

            if (cancellation.Reason == CancellationReason.Error)
            {
                Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                Console.WriteLine($"CANCELED: Did you update the subscription info?");
            }

            Console.WriteLine();
        }
        
        public void DisplayRecognizing(IntentRecognitionEventArgs e)
        {
            if (_quiet) return;
            Console.WriteLine($"RECOGNIZING: {e.Result.Text}");
        }

        public void DisplayRecognized(IntentRecognitionEventArgs e)
        {
            if (_quiet) return;

            var result = e.Result;
            if (result.Reason == ResultReason.RecognizedIntent && result.Text.Length != 0)
            {
                Console.WriteLine($"RECOGNIZED: {result.Text} ({result.IntentId})");
                Console.WriteLine();
            }
            else if (result.Reason == ResultReason.RecognizedSpeech && result.Text.Length != 0)
            {
                Console.WriteLine($"RECOGNIZED: {result.Text}");
                Console.WriteLine();
            }
            else if (result.Reason == ResultReason.NoMatch && _verbose)
            {
                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                Console.WriteLine();
            }
        }
        public void DisplayCanceled(IntentRecognitionCanceledEventArgs e)
        {
            if (_quiet) return;

            var cancellation = CancellationDetails.FromResult(e.Result);

            var normal = cancellation.Reason == CancellationReason.EndOfStream;
            if (normal && !_verbose) return;

            Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

            if (cancellation.Reason == CancellationReason.Error)
            {
                Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                Console.WriteLine($"CANCELED: Did you update the subscription info?");
            }

            Console.WriteLine();
        }

        public void DisplayTranscribing(ConversationTranscriptionEventArgs e)
        {
            if (_quiet) return;
            Console.WriteLine($"TRANSCRIBING: {e.Result.Text}");
        }

        public void DisplayTranscribed(ConversationTranscriptionEventArgs e)
        {
            if (_quiet) return;

            var result = e.Result;
            if (result.Reason == ResultReason.RecognizedSpeech && result.Text.Length != 0)
            {
                Console.WriteLine($"TRANSCRIBED: {result.Text} (UserId={result.UserId})");
                Console.WriteLine();
            }
            else if (result.Reason == ResultReason.NoMatch && _verbose)
            {
                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                Console.WriteLine();
            }
        }

        public void DisplayCanceled(ConversationTranscriptionCanceledEventArgs e)
        {
            if (_quiet) return;

            var cancellation = CancellationDetails.FromResult(e.Result);

            var normal = cancellation.Reason == CancellationReason.EndOfStream;
            if (normal && !_verbose) return;

            Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

            if (cancellation.Reason == CancellationReason.Error)
            {
                Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                Console.WriteLine($"CANCELED: Did you update the subscription info?");
            }

            Console.WriteLine();
        }

        public void DisplayRecognizing(TranslationRecognitionEventArgs e)
        {
            if (_quiet) return;

            bool notSingle = e.Result.Translations.Count != 1;
            if (notSingle) Console.WriteLine($"RECOGNIZING: {e.Result.Text}");

            foreach (var lang in e.Result.Translations.Keys)
            {
                Console.WriteLine(notSingle
                    ? $"TRANSLATING into '{lang}': {e.Result.Translations[lang]}"
                    : $"TRANSLATING into '{lang}': {e.Result.Translations[lang]} (from '{e.Result.Text}')");
            }

            if (e.Result.Translations.Count > 1) Console.WriteLine();
        }

        public void DisplayRecognized(TranslationRecognitionEventArgs e)
        {
            if (_quiet) return;

            var result = e.Result;
            if (result.Reason == ResultReason.TranslatedSpeech && result.Text.Length != 0)
            {
                bool notSingle = e.Result.Translations.Count != 1;
                if (notSingle) Console.WriteLine($"RECOGNIZED: {e.Result.Text}");

                foreach (var lang in e.Result.Translations.Keys)
                {
                    Console.WriteLine(notSingle
                        ? $"TRANSLATED into '{lang}': {e.Result.Translations[lang]}"
                        : $"TRANSLATED into '{lang}': {e.Result.Translations[lang]} (from '{e.Result.Text}')");
                }

                Console.WriteLine();
            }
            else if (result.Reason == ResultReason.RecognizedSpeech && result.Text.Length != 0)
            {
                Console.WriteLine($"RECOGNIZED: {result.Text} (text could not be translated)");
                Console.WriteLine();
            }
            else if (result.Reason == ResultReason.NoMatch && _verbose)
            {
                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                Console.WriteLine();
            }
        }

        public void DisplayCanceled(TranslationRecognitionCanceledEventArgs e)
        {
            if (_quiet) return;

            var cancellation = CancellationDetails.FromResult(e.Result);

            var normal = cancellation.Reason == CancellationReason.EndOfStream;
            if (normal && !_verbose) return;

            Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

            if (cancellation.Reason == CancellationReason.Error)
            {
                Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                Console.WriteLine($"CANCELED: Did you update the subscription info?");
            }

            Console.WriteLine();
        }

        public void DisplaySynthesisStarted(SpeechSynthesisEventArgs e)
        {
            if (_quiet) return;
            Console.WriteLine("SYNTHESIS STARTED: {0}\n", e.Result.ResultId);
        }

        public void DisplaySynthesizing(SpeechSynthesisEventArgs e)
        {
            if (_quiet) return;

            var result = e.Result;
            if (result.Reason == ResultReason.SynthesizingAudio)
            {
                Console.WriteLine($"SYNTHESIZING: audio.length={e.Result.AudioData.Length}...");
            }
            else
            {
                Console.WriteLine($"SYNTHESIZING: reason={result.Reason}");
            }
        }

        public void DisplaySynthesisCompleted(SpeechSynthesisEventArgs e)
        {
            if (_quiet) return;
            Console.WriteLine("\nSYNTHESIS COMPLETED: {0}\n", e.Result.ResultId);
        }

        internal void DisplaySynthesisCanceled(SpeechSynthesisEventArgs e)
        {
            if (_quiet) return;

            var cancellation = SpeechSynthesisCancellationDetails.FromResult(e.Result);
            Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

            if (cancellation.Reason == CancellationReason.Error)
            {
                Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                Console.WriteLine($"CANCELED: Did you update the subscription info?");
            }

            Console.WriteLine();
        }

        private ICommandValues _values;
        private bool _quiet;
        private bool _verbose;
    }
}
