<#@ template hostspecific="true" #>
<#@ output extension=".cs" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
<#@ parameter type="System.String" name="OPENAI_API_KEY" #>
<#@ parameter type="System.String" name="OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_AI_SEARCH_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_AI_SEARCH_KEY" #>
<#@ parameter type="System.String" name="AZURE_AI_SEARCH_INDEX_NAME" #>
<#@ parameter type="System.Boolean" name="OPTION_INCLUDE_CITATIONS" #>
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class <#= ClassName #>
{
    public <#= ClassName #>()
    {
        var openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "<#= OPENAI_API_KEY #>";
        var openAIEndpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? "<#= OPENAI_ENDPOINT #>";
        var openAIDeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT") ?? "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>";
        var searchEndpoint = Environment.GetEnvironmentVariable("AZURE_AI_SEARCH_ENDPOINT") ?? "<#= AZURE_AI_SEARCH_ENDPOINT #>";
        var searchApiKey = Environment.GetEnvironmentVariable("AZURE_AI_SEARCH_KEY") ?? "<#= AZURE_AI_SEARCH_KEY #>";
        var searchIndexName = Environment.GetEnvironmentVariable("AZURE_AI_SEARCH_INDEX_NAME") ?? "<#= AZURE_AI_SEARCH_INDEX_NAME #>";

        _client = string.IsNullOrEmpty(openAIKey)
            ? new OpenAIClient(new Uri(openAIEndpoint), new DefaultAzureCredential())
            : new OpenAIClient(new Uri(openAIEndpoint), new AzureKeyCredential(openAIKey));

        var contosoExtensionConfig = new AzureCognitiveSearchChatExtensionConfiguration()
        {
            SearchEndpoint = new Uri(searchEndpoint),
            Key = searchApiKey,
            IndexName = searchIndexName,
        };

        _options = new()
        {
            DeploymentName = openAIDeploymentName,
            Messages =
            {
                new ChatRequestSystemMessage("You are a helpful assistant that answers questions about the Contoso product database."),
                new ChatRequestUserMessage("What are the best-selling Contoso products this month?")
            },

            AzureExtensionsOptions = new()
            {
                Extensions = { contosoExtensionConfig }
            }
        };
    }

    public async Task<string> ChatUsingYourOwnDataStreamingAsync(string userPrompt, Action<StreamingChatCompletionsUpdate> callback = null)
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

        <# if (OPTION_INCLUDE_CITATIONS)
        { #>
        Console.WriteLine("Citations and other information:");
        foreach (var contextMessage in responseContent.AzureExtensionsContext.Messages)
        {
            Console.WriteLine($"Assistant: {contextMessage.Content}");
        }
        <# } #>
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
            var response = await chat.ChatUsingYourOwnDataStreamingAsync(userPrompt, update =>
                Console.Write(update.ContentUpdate)
            );
            Console.WriteLine("\n");
        }
    }

    private OpenAIClient _client;
    private ChatCompletionsOptions _options;
}