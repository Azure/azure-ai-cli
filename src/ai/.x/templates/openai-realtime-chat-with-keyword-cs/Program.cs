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
            "If there's nothing to say, be super brief. Don't keep telling me things like, 'I'm hear if you need anything', or 'how can i assist you today'... just be chill.";

        var factory = new FunctionFactory();
        factory.AddFunctions(typeof(OpenAIRealtimeChatStreamingCustomFunctions));

        var speaker = new SpeakerAudioOutputStream();
        var audioSourceController = new AudioSourceController();
        var audioSourceControlStream = new AudioSourceControlStream(audioSourceController);
        ConsoleHelpers.HandleAudioEvents(audioSourceController);

        var conversation = new OpenAIRealtimeConversationSessionHelperClass(openAIAPIKey, openAIEndpoint, openAIChatDeploymentName, openAISystemPrompt, factory, audioSourceController, audioSourceControlStream, speaker);

        ConsoleHelpers.Write($"Hi, I'm {name}! Say my name or press the spacebar to talk to me, or 'X' to exit.\n\n\nUser: ");
        ConsoleHelpers.Write("[...]", ConsoleColor.DarkBlue, true, 0, Math.Max(Console.CursorTop - 1, 0));
        _ = ConsoleHelpers.HandleKeysAsync(audioSourceController, speaker);

        await conversation.StartSessionAsync();

        await conversation.GetSessionUpdatesAsync((role, content) =>
        {
            ConsoleHelpers.EnsureWriteRoleAndContent(role, content);
        });
    }
}
