<#@ template hostspecific="true" #>
<#@ output extension=".cs" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
<#@ parameter type="System.String" name="OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_AI_SEARCH_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_AI_SEARCH_KEY" #>
<#@ parameter type="System.String" name="AZURE_AI_SEARCH_INDEX_NAME" #>
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class <#= ClassName #>
{
    private OpenAIClient client;

    private ChatCompletionsOptions options;

    public <#= ClassName #>()
    {
        var openAIEndpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? "<#= OPENAI_ENDPOINT #>";
        var openAIDeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT") ?? "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>";
        var searchEndpoint = Environment.GetEnvironmentVariable("AZURE_AI_SEARCH_ENDPOINT") ?? "<#= AZURE_AI_SEARCH_ENDPOINT #>";
        var searchApiKey = Environment.GetEnvironmentVariable("AZURE_AI_SEARCH_KEY") ?? "<#= AZURE_AI_SEARCH_KEY #>";
        var searchIndexName = Environment.GetEnvironmentVariable("AZURE_AI_SEARCH_INDEX_NAME") ?? "<#= AZURE_AI_SEARCH_INDEX_NAME #>";

        client = new OpenAIClient(new Uri(openAIEndpoint), new DefaultAzureCredential());

        var contosoExtensionConfig = new AzureCognitiveSearchChatExtensionConfiguration()
        {
            SearchEndpoint = new Uri(searchEndpoint),
            Key = searchApiKey,
            IndexName = searchIndexName,
        };

        options = new()
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

    public async Task<ChatResponseMessage> ChatUsingYourOwnData(string userPrompt, Action<ChatResponseMessage> callback = null)
    {
        options.Messages.Add(new ChatRequestUserMessage(userPrompt));

        var response = await client.GetChatCompletionsAsync(options);
        var responseContent = response.Value.Choices[0].Message;

        if(callback != null)
        {
            callback(responseContent);
        }
        options.Messages.Add(new ChatRequestAssistantMessage(responseContent));
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

            var response = await chat.ChatUsingYourOwnData(userPrompt, responseContent =>
                {
                    Console.WriteLine($"Assistant: {responseContent.Content}");
                    // Console.WriteLine("Citations and other information:");

                    // foreach (var contextMessage in responseContent.AzureExtensionsContext.Messages)
                    // {
                    //     Console.WriteLine($"Assistant: {contextMessage.Content}");
                    // }
                }
            );
            Console.WriteLine("\n");
        }
    }
}