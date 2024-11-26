{{if {_IS_SPEECH_TO_TEXT_WITH_TRANSLATION}}}
  {{set _RECOGNITION_EVENT_ARGS="TranslationRecognitionEventArgs"}}
  {{set _RECOGNITION_CANCELED_EVENT_ARGS="TranslationRecognitionCanceledEventArgs"}}
{{else}}
  {{set _RECOGNITION_EVENT_ARGS="SpeechRecognitionEventArgs"}}
  {{set _RECOGNITION_CANCELED_EVENT_ARGS="SpeechRecognitionCanceledEventArgs"}}
{{endif}}
using System;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
{{if {_IS_SPEECH_TO_TEXT_WITH_TRANSLATION}}}
using Microsoft.CognitiveServices.Speech.Translation;
{{endif}}

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Connection and configuration details required
        var speechKey = Environment.GetEnvironmentVariable("AZURE_AI_SPEECH_KEY") ?? "{AZURE_AI_SPEECH_KEY}";
        var speechRegion = Environment.GetEnvironmentVariable("AZURE_AI_SPEECH_REGION") ?? "{AZURE_AI_SPEECH_REGION}";
        var speechLanguage = "en-US"; // BCP-47 language code
{{if {_IS_SPEECH_TO_TEXT_WITH_TRANSLATION}}}
        var targetLanguages = new string[] { "de", "fr" };
{{endif}}
{{if {_IS_SPEECH_TO_TEXT_WITH_FILE}}}
        var inputFileName = args.Length == 1 ? args[0] : "audio.wav";
{{else if {_IS_SPEECH_TO_TEXT_WITH_TRANSLATION}}}
        var inputFileName = args.Length == 1 ? args[0] : null;
{{endif}}

{{if {_IS_SPEECH_TO_TEXT_WITH_FILE} || {_IS_SPEECH_TO_TEXT_WITH_TRANSLATION}}}
        // Check to see if the input file exists
    {{if {_IS_SPEECH_TO_TEXT_WITH_TRANSLATION}}}
        if (inputFileName != null && !File.Exists(inputFileName))
    {{else}}
        if (!File.Exists(inputFileName))
    {{endif}}
        {
            Console.WriteLine($"ERROR: Cannot find audio input file: {inputFileName}");
            return 1;
        }

{{endif}}
{{if {_IS_SPEECH_TO_TEXT_WITH_TRANSLATION}}}
        // Create instances of a speech translation config and audio config
        var config = SpeechTranslationConfig.FromSubscription(speechKey, speechRegion);
        var audioConfig = inputFileName != null
            ? AudioConfig.FromWavFileInput(inputFileName)
            : AudioConfig.FromDefaultMicrophoneInput();
{{else}}
        // Create instances of a speech config, source language config, and audio config
        var config = SpeechConfig.FromSubscription(speechKey, speechRegion);
        var sourceLanguageConfig = SourceLanguageConfig.FromLanguage(speechLanguage);
    {{if {_IS_SPEECH_TO_TEXT_WITH_FILE}}}
        var audioConfig = AudioConfig.FromWavFileInput(inputFileName);
    {{else}}
        var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
    {{endif}}
{{endif}}
{{if {_IS_SPEECH_TO_TEXT_WITH_TRANSLATION}}}

        // Set the source and target language(s) for translation
        config.SpeechRecognitionLanguage = speechLanguage;
        foreach (var targetLanguage in targetLanguages)
        {
            config.AddTargetLanguage(targetLanguage);
        }
{{endif}}

        // Create the speech recognizer from the above configuration information
{{if {_IS_SPEECH_TO_TEXT_WITH_TRANSLATION}}}
        using (var recognizer = new TranslationRecognizer(config, audioConfig))
{{else}}
        using (var recognizer = new SpeechRecognizer(config, sourceLanguageConfig, audioConfig))
{{endif}}
        {
{{if {_IS_SPEECH_TO_TEXT_CONTINUOUS}}}
            // Subscribe to the Recognizing and Recognized events. As the user speaks individual
            // utterances, intermediate recognition results are sent to the Recognizing event,
            // and the final recognition results are sent to the Recognized event.
            recognizer.Recognizing += (s, e) => HandleRecognizingEvent(e);
            recognizer.Recognized += (s, e) => HandleRecognizedEvent(e);

            // Create a task completion source to wait for the session to stop. This is needed in
            // console apps to prevent the main thread from terminating while the recognition is
            // running asynchronously on a separate background thread.
            var sessionStoppedNoError = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Subscribe to SessionStarted and SessionStopped events. These events are useful for
            // logging the start and end of a speech recognition session. In console apps, this is
            // used to allow the application to block the main thread until recognition is complete.
            recognizer.SessionStarted += (s, e) => HandleSessionStartedEvent(e);
            recognizer.SessionStopped += (s, e) => HandleSessionStoppedEvent(e, sessionStoppedNoError);

            // Subscribe to the Canceled event, which indicates that the recognition operation
            // was stopped/canceled, likely due to an error of some kind.
            recognizer.Canceled += (s, e) => HandleCanceledEvent(e, sessionStoppedNoError);

            // Allow the user to press ENTER to stop recognition
            Task.Run(() =>
            {
                while (Console.ReadKey().Key != ConsoleKey.Enter) { }
                recognizer.StopContinuousRecognitionAsync();
            });

            // Start speech recognition
            await recognizer.StartContinuousRecognitionAsync();
            Console.WriteLine("Listening; press ENTER to stop ...\n");

            // Wait for the session to stop. The Task will not complete until the recognition
            // session stops, and the result will indicate whether the session completed
            // or was canceled.
            return await sessionStoppedNoError.Task ? 0 : 1;
{{else}}
            Console.WriteLine("Listening ...\n");

            // Start speech recognition, and return after a single utterance is recognized. The end of a
            // single utterance is determined by listening for silence at the end or until a maximum of 15
            // seconds of audio is processed.
            var result = await recognizer.RecognizeOnceAsync();

            // Check the result
            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                Console.WriteLine($"RECOGNIZED: {result.Text}");
            }
            else if (result.Reason == ResultReason.NoMatch)
            {
                Console.WriteLine("NOMATCH: Speech could not be recognized.");
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = CancellationDetails.FromResult(result);
                Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                    Console.WriteLine("CANCELED: Did you update the subscription info?");
                }
            }

            return 0;
{{endif}}
        }
    }

{{if {_IS_SPEECH_TO_TEXT_CONTINUOUS}}}
    private static void HandleRecognizingEvent({_RECOGNITION_EVENT_ARGS} e)
    {
        Console.WriteLine($"RECOGNIZING: {e.Result.Text}");
    {{if {_IS_SPEECH_TO_TEXT_WITH_TRANSLATION}}}
        foreach (var lang in e.Result.Translations.Keys)
        {
            Console.WriteLine($"TRANSLATING into '{lang}': {e.Result.Translations[lang]}");
        }
        Console.WriteLine();
    {{endif}}
    }

    private static void HandleRecognizedEvent({_RECOGNITION_EVENT_ARGS} e)
    {
    {{if {_IS_SPEECH_TO_TEXT_WITH_TRANSLATION}}}
        if (e.Result.Reason == ResultReason.TranslatedSpeech && e.Result.Text.Length != 0)
        {
            Console.WriteLine($"RECOGNIZED: {e.Result.Text}");
            foreach (var lang in e.Result.Translations.Keys)
            {
                Console.WriteLine($"TRANSLATED into '{lang}': {e.Result.Translations[lang]}");
            }
            Console.WriteLine();
        }
        else if (e.Result.Reason == ResultReason.RecognizedSpeech && e.Result.Text.Length != 0)
    {{else}}
        if (e.Result.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrEmpty(e.Result.Text))
    {{endif}}
        {
    {{if {_IS_SPEECH_TO_TEXT_WITH_TRANSLATION}}}
            Console.WriteLine($"RECOGNIZED: {e.Result.Text} (text could not be translated)");
    {{else}}
            Console.WriteLine($"RECOGNIZED: {e.Result.Text}\n");
    {{endif}}
        }
        else if (e.Result.Reason == ResultReason.NoMatch)
        {
            Console.WriteLine("NOMATCH: Speech could not be recognized.\n");
        }
    }

    private static void HandleSessionStartedEvent(SessionEventArgs e)
    {
        Console.WriteLine($"SESSION STARTED: {e.SessionId}.\n");
    }

    private static void HandleSessionStoppedEvent(SessionEventArgs e, TaskCompletionSource<bool> sessionStoppedNoError)
    {
        Console.WriteLine($"SESSION STOPPED: {e.SessionId}.");
        sessionStoppedNoError.TrySetResult(true); // Set the result so the main thread can exit
    }

    private static void HandleCanceledEvent({_RECOGNITION_CANCELED_EVENT_ARGS} e, TaskCompletionSource<bool> sessionStoppedNoError)
    {
        Console.WriteLine($"CANCELED: Reason={e.Reason}");

        // Check the CancellationReason for more detailed information.
        if (e.Reason == CancellationReason.EndOfStream)
        {
            Console.WriteLine("CANCELED: End of the audio stream was reached.");
        }
        else if (e.Reason == CancellationReason.Error)
        {
            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
            Console.WriteLine("CANCELED: Did you update the subscription info?");
        }

        // Set the task completion source result so the main thread can exit
        sessionStoppedNoError.TrySetResult(e.Reason != CancellationReason.Error);
    }
{{endif}}
}