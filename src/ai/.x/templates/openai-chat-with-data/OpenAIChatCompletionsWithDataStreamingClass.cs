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
    public <#= ClassName #>(
        string systemPrompt, string openAIKey, string openAIEndpoint, string openAIDeploymentName, string searchEndpoint, string searchApiKey, string searchIndexName, string embeddingsEndpoint)
    {
        _systemPrompt = systemPrompt;
        _client = string.IsNullOrEmpty(openAIKey)
            ? new OpenAIClient(new Uri(openAIEndpoint), new DefaultAzureCredential())
            : new OpenAIClient(new Uri(openAIEndpoint), new AzureKeyCredential(openAIKey));

        var queryType = AzureCognitiveSearchQueryType.VectorSimpleHybrid; // Use VectorSimpleHybrid to get the best of both vector and keyword types.
        var extensionConfig = new AzureCognitiveSearchChatExtensionConfiguration()
        {
            SearchEndpoint = new Uri(searchEndpoint),
            Key = searchApiKey,
            IndexName = searchIndexName,
            QueryType = queryType,
            EmbeddingEndpoint = new Uri(embeddingsEndpoint),
            EmbeddingKey = openAIKey,
        };
        _options = new ChatCompletionsOptions()
        {
            DeploymentName = openAIDeploymentName,

            AzureExtensionsOptions = new()
            {
                Extensions = { extensionConfig }
            }
        };
        ClearConversation();
    }

    public void ClearConversation()
    {
        _options.Messages.Clear();
        _options.Messages.Add(new ChatRequestSystemMessage(_systemPrompt));
    }

    public async Task<string> GetChatCompletionsStreamingAsync(string userPrompt, Action<StreamingChatCompletionsUpdate> callback = null)
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

        _options.Messages.Add(new ChatRequestAssistantMessage(responseContent));
        return responseContent;
    }

    private string _systemPrompt;
    private OpenAIClient _client;
    private ChatCompletionsOptions _options;
}