using System;
using System.Text;
using System.Security.Cryptography;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Diagnostics;
using Azure.AI.OpenAI;
using System.Runtime.InteropServices;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Check to see if the keyword file exists
        if (!File.Exists(keywordFileName))
        {
            Console.WriteLine($"ERROR: Cannot find keyword file: {keywordFileName}");
            return 1;
        }

        // Check to see if the input file exists
        var inputFileName = args.Length == 1 ? args[0] : null;
        if (inputFileName != null && !File.Exists(inputFileName))
        {
            Console.WriteLine($"ERROR: Cannot find audio input file: {inputFileName}");
            return 1;
        }

        // Create instances of a speech config, source language config, and audio config
        var config = SpeechConfig.FromSubscription(speechKey, speechRegion);
        var sourceLanguageConfig = SourceLanguageConfig.FromLanguage(speechInputLanguage);
        var audioConfig = inputFileName != null
            ? AudioConfig.FromWavFileInput(inputFileName)
            : AudioConfig.FromDefaultMicrophoneInput();

        // Create the speech recognizer and the keyword model
        var recognizer = new SpeechRecognizer(config, sourceLanguageConfig, audioConfig);
        var keywordModel = KeywordRecognitionModel.FromFile(keywordFileName);

        // Hook up the Canceled event handler (whcih will set the task completion source when the session stops)
        var sessionStoppedNoError = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        recognizer.Canceled += (s, e) => HandleCanceledEvent(e, sessionStoppedNoError);

        // Hook up the Recognizing and Recognized event handlers
        recognizer.Recognizing += (s, e) => HandleRecognizingEvent(e);
        recognizer.Recognized += (s, e) => HandleRecognizedEvent(e);

        // Start the keyword recognition and wait for the user to press ENTER to stop
        _ = Task.Run(async () =>
        {
            await recognizer.StartKeywordRecognitionAsync(keywordModel);
            Console.WriteLine("Listening for keyword; press ENTER to stop ...\n");

            while (Console.ReadKey().Key != ConsoleKey.Enter) { }
            _ = recognizer.StopContinuousRecognitionAsync();
        });

        // Tell the user that the assistant is ready
        await Speak($"Hello! I'm ready when you are. Just say my name, {keywordName}, to get started.");

        // Wait for the session to stop. The Task will not complete until the recognition
        // session stops, and the result will indicate whether the session completed
        // or was canceled.
        return await sessionStoppedNoError.Task ? 0 : 1;
    }

    private static void HandleCanceledEvent(SpeechRecognitionCanceledEventArgs e, TaskCompletionSource<bool> sessionStoppedNoError)
    {
        if (e.Reason == CancellationReason.Error)
        {
            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
            Console.WriteLine($"CANCELED: Did you update the subscription info?");
        }

        sessionStoppedNoError.TrySetResult(e.Reason != CancellationReason.Error);
    }

    private static void HandleRecognizingEvent(SpeechRecognitionEventArgs e)
    {
        _ = EnsureStopSynthesizer(); // Stop speaking whenever the recognizer hears something...
        PrintPartialRecognitionText(e.Result.Text);
    }

    private static void HandleRecognizedEvent(SpeechRecognitionEventArgs e)
    {
        _ = EnsureStopSynthesizer(); // Stop speaking whenever the recognizer hears something...

        if (e.Result.Reason == ResultReason.RecognizedKeyword && !string.IsNullOrEmpty(e.Result.Text))
        {
            PrintPartialRecognitionText(e.Result.Text);
        }
        else if (e.Result.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrEmpty(e.Result.Text))
        {
            HandleSpeechRecognizedEvent(e);
        }
        else if (e.Result.Reason == ResultReason.NoMatch)
        {
            PrintFullRecognitionText("(NO MATCH)");
        }
    }

    private static async void HandleSpeechRecognizedEvent(SpeechRecognitionEventArgs e)
    {
        // Print the full recognition text and the role label
        PrintFullRecognitionText(e.Result.Text);
        PrintRoleLabel("assistant");

        // Prepare the lambda for each content update
        var contentNotYetSpokenAccumulator = new StringBuilder();
        var onlyHandleIfThisGeneration = GetSynthesizerGeneration();
        var eachUpdateAction = new Action<StreamingChatCompletionsUpdate>((update) =>
        {
            if (onlyHandleIfThisGeneration == GetSynthesizerGeneration())
            {
                HandleContentUpdate(update, contentNotYetSpokenAccumulator);
            }
        });

        // Get the chat completions and wait for the response to complete
        var responseComplete = chat.GetChatCompletionsStreamingAsync(e.Result.Text, eachUpdateAction);
        await responseComplete;

        // Speak any remaining content that hasn't been spoken yet
        SpeakThePartsBetweenTheSentinels(contentNotYetSpokenAccumulator.ToString());
        Console.WriteLine("\n");
    }

    private static void HandleContentUpdate(StreamingChatCompletionsUpdate update, StringBuilder contentNotYetSpokenAccumulator)
    {
        if (update.ContentUpdate == null) return;

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(update.ContentUpdate);

        contentNotYetSpokenAccumulator.Append(update.ContentUpdate);

        var accumulator = contentNotYetSpokenAccumulator.ToString();
        if (accumulator.Contains(sentinel))
        {
            contentNotYetSpokenAccumulator.Clear();
            SpeakThePartsBetweenTheSentinels(accumulator);
        }
    }

    private static void SpeakThePartsBetweenTheSentinels(string text)
    {
        var parts = text.Split(sentinel);
        foreach (var part in parts)
        {
            Speak(part).Wait(1000);
        }
    }

    private static async Task Speak(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        text = text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        if (synthesizer == null)
        {
            synthesizer = InitSynthesizer();
        }

        string ssml = ConvertToSsml(text, speechOutputVoiceName, speechOutputVoiceRate);
        await synthesizer.SpeakSsmlAsync(ssml);
    }

    private static SpeechSynthesizer InitSynthesizer()
    {
        var config = SpeechConfig.FromSubscription(speechKey, speechRegion);
        config.SpeechSynthesisVoiceName = speechOutputVoiceName;

        var synthesizer = new SpeechSynthesizer(config);

        var generation = GetSynthesizerGeneration();
        synthesizer.SynthesisStarted += (s, e) => HandleSynthesisStarted(e, generation);
        synthesizer.SynthesisCompleted += (s, e) => HandleSynthesisCompleted(e, generation);

        return synthesizer;
    }

    private static async Task EnsureStopSynthesizer()
    {
        if (synthesizer != null && IsSpeaking())
        {
            var task = synthesizer.StopSpeakingAsync();
            synthesizer = null;
            synthesizerGeneration++;
            synthesizerSpeakingCount = 0;
            await task;
        }
    }

    private static int GetSynthesizerGeneration()
    {
        return synthesizerGeneration;
    }

    private static bool IsSpeaking()
    {
        return synthesizerSpeakingCount > 0;
    }

    private static void HandleSynthesisStarted(SpeechSynthesisEventArgs e, int generation)
    {
        if (generation == GetSynthesizerGeneration())
        {
            synthesizerSpeakingCount++;
        }
    }

    private static void HandleSynthesisCompleted(SpeechSynthesisEventArgs e, int generation)
    {
        if (generation == GetSynthesizerGeneration())
        {
            synthesizerSpeakingCount--;
        }
    }

    private static string ConvertToSsml(string text, string name, string rate)
    {
        return "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:mstts=\"http://www.w3.org/2001/mstts\" xml:lang=\"en-US\">" +
                    $"<voice name=\"{name}\">" +
                        $"<prosody rate=\"{rate}\">" +
                            "<mstts:silence  type=\"Leading-exact\" value=\"0ms\"/>" +
                            text +
                            "<mstts:silence  type=\"Tailing-exact\" value=\"0ms\"/>" +
                        "</prosody>" +
                    "</voice>" +
               "</speak>";
    }

    private static int PrintRoleLabel(string role)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write($"\r{role}: ");
        return role.Length + 2;
    }

    private static void PrintPartialRecognitionText(string text)
    {
        if (!Console.IsOutputRedirected)
        {
            var printed = PrintRoleLabel("user");

            Console.ForegroundColor = uiSupports256Colors ? ConsoleColor.DarkGray : ConsoleColor.Gray;

            var widthRemaining = Console.WindowWidth - printed;
            var textToLong = text.Length + 3 > widthRemaining;

            Console.Write(textToLong
                ? "..." + text.Substring(text.Length - widthRemaining + 3)
                : text);
        }
    }

    private static void PrintFullRecognitionText(string text)
    {
        PrintRoleLabel("user");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(text);
        Console.WriteLine();
    }

    private static bool TerminalSupports256Colors()
    {
        return Environment.GetEnvironmentVariable("TERM")?.Contains("256") == true;
    }

    private static bool IsWindows()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    private static OpenAIChatCompletionsFunctionsStreamingClass GetChat()
    {
        var updatedSystemPrompt = openAISystemPrompt +
            "\n\nYour name is " + keywordName + "." +
            "\n\nPlease follow the given instructions for generating all responses:" +
            "\n* You must only generate SSML fragments." +
            "\n* The user will never see the SSML you generate, only hear the synthesized speech." +
            "\n* Unless the user asks you about SSML, never reveal that you are using SSML." +
            "\n* The SSML fragments must be balanced and well-formed, meaning that each opening tag must have a corresponding closing tag." +
            "\n* The SSML fragments must not contain <speak> or <voice> tags." +
            "\n* The SSML fragments may only contain <p>, <emphasis>, and <prosody> tags." +
            "\n* The SSML prosody volume should be a number between 0% and 100% (default is 100%)." +
            "\n* The SSML prosody rate should be a number between -50% and +10%." +
            "\n* The SSML prosody pitch and contour attributes should never be used." +
            "\n* If you use \"&\" or \"<\" or \">\", you must escape them as \"&amp;\", \"&lt;\", \"&gt;\" respectively." +
            "\n* Ensure the narrative is positive, upbeat, and cheerful." +
            "\n* Use a friendly and engaging tone. Provide more emphasis than usual on important parts." +
            "\n* Break your response into separate SSML fragments at sentence boundaries." +
            "\n* You **MUST NOT** put more than one sentence in a single SSML fragment." +
            "\n* If a sentence becomes too long, make the SSML fragment shorter by breaking at comma boundaries." +
            "\n* If you are speaking about code, you need to read the code aloud, using words, such as open curly brace and period, one line at a time." +
            "\n* Between SSML fragments, use a sentinel of \"" + sentinel + "\".";

        var factory = new FunctionFactory();
        factory.AddFunctions(typeof(OpenAIChatCompletionsCustomFunctions));

        return new OpenAIChatCompletionsFunctionsStreamingClass(openAIEndpoint, openAIAPIKey, openAIChatDeploymentName, updatedSystemPrompt, factory);
    }

    private static string keywordName = "Quincy";
    private static string keywordFileName = "keyword.table";
    private static string speechInputLanguage = "en-US"; // BCP-47 language code
    private static string speechOutputVoiceName = "en-US-AndrewNeural"; // "en-US-AvaMultilingualNeural";
    private static string speechOutputVoiceRate = "+20%";

    private static string speechKey = Environment.GetEnvironmentVariable("AZURE_AI_SPEECH_KEY") ?? "<insert your Speech Service API key here>";
    private static string speechRegion = Environment.GetEnvironmentVariable("AZURE_AI_SPEECH_REGION") ?? "<insert your Speech Service region here>";
    private static string openAIAPIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "<insert your OpenAI API key here>";
    private static string openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "<insert your OpenAI endpoint here>";
    private static string openAIChatDeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT") ?? "<insert your OpenAI chat deployment name here>";
    private static string openAISystemPrompt = Environment.GetEnvironmentVariable("AZURE_OPENAI_SYSTEM_PROMPT") ?? "You are a helpful AI assistant.";

    private static bool uiSupports256Colors = IsWindows() || TerminalSupports256Colors();

    private static OpenAIChatCompletionsFunctionsStreamingClass chat = GetChat();
    private const string sentinel = "\u001E";

    private static SpeechSynthesizer? synthesizer = null;
    private static int synthesizerGeneration = 0;
    private static int synthesizerSpeakingCount = 0;
}