using OpenAI.Chat;
using System;

public class Program
{
    public static bool Debug { get; set; } = false;

    public static async Task Main(string[] args)
    {
        Debug = args.Contains("--debug") || args.Contains("debug");

        var openAIAPIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "<insert your OpenAI API key here>";
        var openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "<insert your OpenAI endpoint here>";
        var openAIChatDeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_REALTIME_DEPLOYMENT") ?? "<insert your OpenAI chat deployment name here>";
        var openAISystemPrompt = Environment.GetEnvironmentVariable("AZURE_OPENAI_SYSTEM_PROMPT") ?? "You are a helpful AI assistant.";

        var factory = new FunctionFactory();
        factory.AddFunctions(typeof(OpenAIRealtimeChatStreamingCustomFunctions));

        var microphone = new MicrophoneAudioInputStream();
        var speaker = new SpeakerAudioOutputStream();
        var conversation = new {ClassName}(openAIAPIKey, openAIEndpoint, openAIChatDeploymentName, openAISystemPrompt, factory, microphone, speaker);

        await conversation.StartSessionAsync();

        var lastRole = string.Empty;
        await conversation.GetSessionUpdatesAsync((role, content) => {
            var isUser = role.ToLower() == "user";
            role = isUser ? "User" : "Assistant";
            if (role != lastRole)
            {
                if (!string.IsNullOrEmpty(lastRole)) Console.WriteLine();
                Console.Write($"{role}: ");
                lastRole = role;
            }
            Console.Write(content);
        });
    }
}
