using OpenAI.Chat;
using System;

public class Program
{
    public static bool Debug { get; set; } = false;

    public static async Task Main(string[] args)
    {
        Debug = args.Contains("--debug") || args.Contains("debug");

        var speechKey = Environment.GetEnvironmentVariable("AZURE_AI_SPEECH_KEY") ?? "<insert your Speech Service API key here>";
        var speechRegion = Environment.GetEnvironmentVariable("AZURE_AI_SPEECH_REGION") ?? "<insert your Speech Service region here>";
        var speechLanguage = "en-US"; // BCP-47 language code
        var keywordFileName = "keyword.table";

        var openAIAPIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "<insert your OpenAI API key here>";
        var openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "<insert your OpenAI endpoint here>";
        var openAIChatDeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_REALTIME_DEPLOYMENT") ?? "<insert your OpenAI chat deployment name here>";
        var openAISystemPrompt = Environment.GetEnvironmentVariable("AZURE_OPENAI_SYSTEM_PROMPT") ?? "You are a helpful AI assistant.";

        var factory = new FunctionFactory();
        factory.AddFunctions(typeof(OpenAIRealtimeChatStreamingCustomFunctions));
        factory.AddFunctions(typeof(FileHelperFunctions));

        var microphone = new MicrophoneAudioInputStream();
        var speaker = new SpeakerAudioOutputStream();
        var conversation = new OpenAIRealtimeConversationSessionHelperClass(openAIAPIKey, openAIEndpoint, openAIChatDeploymentName, openAISystemPrompt, factory, microphone, speaker);

        await conversation.StartSessionAsync();

        while (true)
        {
            Console.Write("User: ");
            var userPrompt = Console.ReadLine();
            if (string.IsNullOrEmpty(userPrompt) || userPrompt == "exit") break;

            Console.Write("\nAssistant: ");
            var response = await conversation.GetSessionUpdateAsync(userPrompt, update => {
                Console.Write(update);
            });
            Console.WriteLine("\n");
        }
    }
}