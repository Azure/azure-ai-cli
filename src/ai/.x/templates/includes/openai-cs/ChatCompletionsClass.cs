//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Azure;
using OpenAI;
using OpenAI.Chat;
using Azure.AI.OpenAI;
{{if {_IS_WITH_DATA_TEMPLATE}}}
using Azure.AI.OpenAI.Chat;
{{endif}}
using Azure.Identity;
using System;

{{if {_IS_WITH_DATA_TEMPLATE}}}
#pragma warning disable AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

{{endif}}
public class {ClassName}
{
{{if {_IS_WITH_DATA_TEMPLATE}}}
    public OpenAIChatCompletionsWithDataStreamingClass(string openAIEndpoint, string openAIAPIKey, string openAIChatDeploymentName, string openAISystemPrompt, string searchEndpoint, string searchApiKey, string searchIndexName, string embeddingsEndpoint)
{{else}}
    public {ClassName}(string openAIEndpoint, string openAIAPIKey, string openAIChatDeploymentName, string openAISystemPrompt)
{{endif}}
    {
        _openAISystemPrompt = openAISystemPrompt;

        _client = string.IsNullOrEmpty(openAIAPIKey)
            ? new AzureOpenAIClient(new Uri(openAIEndpoint), new DefaultAzureCredential())
            : new AzureOpenAIClient(new Uri(openAIEndpoint), new AzureKeyCredential(openAIAPIKey));

        _chatClient = _client.GetChatClient(openAIChatDeploymentName);
        _messages = new List<ChatMessage>();

{{if {_IS_WITH_DATA_TEMPLATE}}}
        _options = new();
        _options.AddDataSource(new AzureSearchChatDataSource()
        {
            Authentication = DataSourceAuthentication.FromApiKey(searchApiKey),
            Endpoint = new Uri(searchEndpoint),
            IndexName = searchIndexName,
            QueryType = DataSourceQueryType.VectorSimpleHybrid, // Use VectorSimpleHybrid to get the best vector and keyword search query types.
            VectorizationSource = DataSourceVectorizer.FromEndpoint(new Uri(embeddingsEndpoint), DataSourceAuthentication.FromApiKey(openAIAPIKey))
        });

{{endif}}
        ClearConversation();
    }

    public void ClearConversation()
    {
        _messages.Clear();
        _messages.Add(ChatMessage.CreateSystemMessage(_openAISystemPrompt));
    }

{{if {_IS_OPENAI_CHAT_STREAMING_TEMPLATE}}}
    public async Task<string> GetChatCompletionsStreamingAsync(string userPrompt, Action<StreamingChatCompletionUpdate>? callback = null)
{{else}}
    public string GetChatCompletion(string userPrompt)
{{endif}}
    {
        _messages.Add(ChatMessage.CreateUserMessage(userPrompt));

{{if {_IS_OPENAI_CHAT_STREAMING_TEMPLATE}}}
        var responseContent = string.Empty;
  {{if {_IS_WITH_DATA_TEMPLATE}}}
        var response = _chatClient.CompleteChatStreamingAsync(_messages, _options);
  {{else}}
        var response = _chatClient.CompleteChatStreamingAsync(_messages);
  {{endif}}
        await foreach (var update in response)
        {
            var content = string.Join("", update.ContentUpdate
                .Where(x => x.Kind == ChatMessageContentPartKind.Text)
                .Select(x => x.Text)
                .ToList());

            if (update.FinishReason == ChatFinishReason.ContentFilter)
            {
                content = $"{content}\nWARNING: Content filtered!";
            }

            if (string.IsNullOrEmpty(content)) continue;

            responseContent += content;
            if (callback != null) callback(update);
        }
{{else}}
        var response = _chatClient.CompleteChat(_messages);
        var responseContent = string.Join("", response.Value.Content
            .Where(x => x.Kind == ChatMessageContentPartKind.Text)
            .Select(x => x.Text)
            .ToList());
{{endif}}

        _messages.Add(ChatMessage.CreateAssistantMessage(responseContent));
        return responseContent;
    }

    private string _openAISystemPrompt;
{{if {_IS_WITH_DATA_TEMPLATE}}}
    private ChatCompletionOptions _options;
{{endif}}
    private OpenAIClient _client;
    private ChatClient _chatClient;
    private List<ChatMessage> _messages;
}