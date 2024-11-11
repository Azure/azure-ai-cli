using NAudio.SoundFont;
using OpenAI.Chat;
using System;

public class Program
{
    public static bool Debug { get; set; } = false;

    public static async Task Main(string[] args)
    {
        Debug = args.Contains("--debug") || args.Contains("debug");

        var name = "Quincy";
        var language = "English";
        var openAIAPIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "<insert your OpenAI API key here>";
        var openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "<insert your OpenAI endpoint here>";
        var openAIChatDeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_REALTIME_DEPLOYMENT") ?? "<insert your OpenAI chat deployment name here>";
        var openAISystemPrompt = Environment.GetEnvironmentVariable("AZURE_OPENAI_SYSTEM_PROMPT") ??
            $"You are a helpful AI assistant, named {name}. You always listen and respond in {language}." +
            "You speak in a friendly manner, but you're also very concise. You don't ask me what I want all the time, you just do the right thing." +
            "If there's nothing to say, but super brief. Don't keep telling me things like, 'I'm hear if you need anything', or 'how can i assist you today'... just be chill.";

        var factory = new FunctionFactory();
        factory.AddFunctions(typeof(OpenAIRealtimeChatStreamingCustomFunctions));

        var audioSourceController = new AudioSourceController();
        audioSourceController.DisplayOutput += AudioSourceController_DisplayOutput;
        audioSourceController.SoundOutput += AudioSourceController_SoundOutput;
        var audioSourceControlStream = new AudioSourceControlStream(audioSourceController);

        var speaker = new SpeakerAudioOutputStream();
        var conversation = new OpenAIRealtimeConversationSessionHelperClass(openAIAPIKey, openAIEndpoint, openAIChatDeploymentName, openAISystemPrompt, factory, audioSourceController, audioSourceControlStream, speaker);

        Console.WriteLine($"Hi, I'm {name}! Say my name or press the spacebar to talk to me, or 'X' to exit.\n\n");
        await conversation.StartSessionAsync();

        _ = Task.Run(() =>
        {
            while (true)
            {
                var state = audioSourceController.State;

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.X)
                    {
                        Environment.Exit(0);
                    }
                    else if (key.Key == ConsoleKey.D)
                    {
                        Debug = !Debug;
                        ConsoleWrite($"Debug mode: {Debug}\n\n");
                    }
                    else if (key.Key == ConsoleKey.M)
                    {
                        audioSourceController.ToggleMute();
                    }
                    else if (key.Key == ConsoleKey.Spacebar || key.Key == ConsoleKey.Enter)
                    {
                        switch (state)
                        {
                            case AudioSourceState.Off:
                            case AudioSourceState.KeywordArmed:
                                speaker.ClearPlayback();
                                audioSourceController.TransitionToOpenMic();
                                break;

                            case AudioSourceState.OpenMic:
                                if (audioSourceController.IsMuted)
                                {
                                    speaker.ClearPlayback();
                                    audioSourceController.UnMute();
                                }
                                else
                                {
                                    audioSourceController.TransitionToOff();
                                }
                                break;
                        }
                    }
                    else if (key.Key == ConsoleKey.K)
                    {
                        switch (state)
                        {
                            case AudioSourceState.Off:
                            case AudioSourceState.OpenMic:
                                audioSourceController.TransitionToKeywordArmed();
                                break;

                            case AudioSourceState.KeywordArmed:
                                audioSourceController.TransitionToOff();
                                break;
                        }
                    }
                }
            }
        });

        var lastRole = string.Empty;
        await conversation.GetSessionUpdatesAsync((role, content) => {
            var isUser = role.ToLower() == "user";
            role = isUser ? "User" : "Assistant";
            if (role != lastRole)
            {
                if (!string.IsNullOrEmpty(lastRole)) ConsoleWrite("\n\n");
                ConsoleWrite($"{role}: ");
                lastRole = role;
            }
            ConsoleWrite(content);
        });
    }

    private static void AudioSourceController_SoundOutput(object? sender, SoundEventArgs e)
    {
        e.PlaySound();
    }

    private static void AudioSourceController_DisplayOutput(object? sender, string e)
    {
        var message = $"[{e}]";

        var addSpaces = Console.WindowWidth - message.Length;
        if (addSpaces > 0)
        {
            message += new string(' ', addSpaces);
        }

        var y = Math.Max(Console.CursorTop - 1, 0);
        ConsoleWrite(message, true, 0, y);
    }

    private static void ConsoleWrite(string message, bool savePos = false, int atX = -1, int atY = -1)
    {
        // if (!savePos && message.Contains("\n"))
        // {
        //     var spaces = new string(' ', 25);
        //     var x = Console.WindowWidth - spaces.Length;
        //     ConsoleWrite(spaces, true, x);
        // }

        lock (_syncLock)
        {
            var x = Console.CursorLeft;
            var y = Console.CursorTop;
            var fg = Console.ForegroundColor;
            var bg = Console.BackgroundColor;

            if (savePos)
            {
                if (atX >= 0 && atY >= 0)
                {
                    Console.SetCursorPosition(atX, atY);
                }
                else if (atX >= 0)
                {
                    Console.SetCursorPosition(atX, y);
                }
                else if (atY >= 0)
                {
                    Console.SetCursorPosition(x, atY);
                }
                Console.ForegroundColor = ConsoleColor.DarkBlue;
            }

            Console.Write(message);

            if (savePos)
            {
                Console.SetCursorPosition(x, y);
                Console.ForegroundColor = fg;
                Console.BackgroundColor = bg;
            }
        }
    }

    private static object _syncLock = new();
}