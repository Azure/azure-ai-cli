<#@ template hostspecific="true" #>
<#@ output extension=".cs" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
<#@ parameter type="System.String" name="OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="OPENAI_API_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using System;

public class <#= ClassName #>
{
    public <#= ClassName #>()
    {
        var openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "<#= OPENAI_API_KEY #>";
        var openAIEndpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? "<#= OPENAI_ENDPOINT #>";
        var openAIDeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT") ?? "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>";
        var systemPrompt = Environment.GetEnvironmentVariable("AZURE_OPENAI_SYSTEM_PROMPT") ?? "<#= AZURE_OPENAI_SYSTEM_PROMPT #>";

        _client = string.IsNullOrEmpty(openAIKey)
            ? new OpenAIClient(new Uri(openAIEndpoint), new DefaultAzureCredential())
            : new OpenAIClient(new Uri(openAIEndpoint), new AzureKeyCredential(openAIKey));

        _options = new ChatCompletionsOptions();
        _options.DeploymentName = openAIDeploymentName;
        _options.Messages.Add(new ChatRequestSystemMessage(systemPrompt));
    }

    public async Task<string> GetChatCompletionsStreamingAsync(string userPrompt, Action<StreamingChatCompletionsUpdate> callback = null)
    {
        _options.Messages.Add(new ChatRequestUserMessage(userPrompt));

        var responseContent = string.Empty;
        var response = await _client.GetChatCompletionsStreamingAsync(_options);
        await foreach (var update in response.EnumerateValues())
        {
            if (callback != null)
            {
                callback(update);
            }

            var content = update.ContentUpdate;
            if (update.FinishReason == CompletionsFinishReason.ContentFiltered)
            {
                content = $"{content}\nWARNING: Content filtered!";
            }
            else if (update.FinishReason == CompletionsFinishReason.TokenLimitReached)
            {
                content = $"{content}\nERROR: Exceeded token limit!";
            }

            if (string.IsNullOrEmpty(content)) continue;

            responseContent += content;
        }

        _options.Messages.Add(new ChatRequestAssistantMessage(responseContent));
        return responseContent;
    }

    public static async Task Main(string[] args)
    {
        var chat = new <#= ClassName #>();

        while (true)
        {
            Console.Write("User: ");
            var userPrompt = Console.ReadLine();
            if (string.IsNullOrEmpty(userPrompt) || userPrompt == "exit") break;

            Console.Write("\nAssistant: ");
            var response = await chat.GetChatCompletionsStreamingAsync(userPrompt, update =>
                Console.Write(update.ContentUpdate)
            );
            Console.WriteLine("\n");
        }
    }
    private OpenAIClient _client;
    private ChatCompletionsOptions _options;
}
