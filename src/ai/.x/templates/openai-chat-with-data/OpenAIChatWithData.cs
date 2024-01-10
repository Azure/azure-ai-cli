<#@ template hostspecific="true" #>
<#@ output extension=".cs" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
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
    public <#= ClassName #>(string openAIKey, string openAIEndpoint, string openAIDeploymentName, string searchEndpoint, string searchApiKey, string searchIndexName)
    {
        _client = string.IsNullOrEmpty(openAIKey)
            ? new OpenAIClient(new Uri(openAIEndpoint), new DefaultAzureCredential())
            : new OpenAIClient(new Uri(openAIEndpoint), new AzureKeyCredential(openAIKey));

        var extensionConfig = new AzureCognitiveSearchChatExtensionConfiguration()
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
                Extensions = { extensionConfig }
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
            if (callback != null)
            {
                callback(update);
            }
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

    private OpenAIClient _client;
    private ChatCompletionsOptions _options;
}