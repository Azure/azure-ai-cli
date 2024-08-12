//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Azure;
using Azure.Identity;
using Azure.AI.Inference;
using System;

public class {ClassName}
{
    public {ClassName}(string aiChatEndpoint, string aiChatAPIKey, string systemPrompt)
    {
        _systemPrompt = systemPrompt;

        _client = string.IsNullOrEmpty(aiChatAPIKey)
            ? new ChatCompletionsClient(new Uri(aiChatEndpoint), new DefaultAzureCredential())
            : new ChatCompletionsClient(new Uri(aiChatEndpoint), new AzureKeyCredential(aiChatAPIKey));
        _messages = new List<ChatRequestMessage>();

        ClearConversation();
    }

    public void ClearConversation()
    {
        _messages.Clear();
        _messages.Add(new ChatRequestSystemMessage(_systemPrompt));
    }

    public async Task<string> GetChatCompletionsStreamingAsync(string userPrompt, Action<StreamingChatCompletionsUpdate>? callback = null)
    {
        _messages.Add(new ChatRequestUserMessage(userPrompt));
        var options = new ChatCompletionsOptions(_messages);

        var responseContent = string.Empty;
        var response = await _client.CompleteStreamingAsync(options);
        await foreach (var update in response)
        {
            var content = update.ContentUpdate;

            if (update.FinishReason == CompletionsFinishReason.ContentFiltered)
            {
                content = $"{content}\nWARNING: Content filtered!";
            }

            if (string.IsNullOrEmpty(content)) continue;

            responseContent += content;
            if (callback != null) callback(update);
        }

        _messages.Add(new ChatRequestAssistantMessage() { Content = responseContent });
        return responseContent;
    }

    private string _systemPrompt;
    private ChatCompletionsClient _client;
    private List<ChatRequestMessage> _messages;
}