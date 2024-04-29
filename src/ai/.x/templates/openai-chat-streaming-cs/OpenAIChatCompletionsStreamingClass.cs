//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using System;

public class {ClassName}
{
    public {ClassName}(string openAIEndpoint, string openAIAPIKey, string openAIChatDeploymentName, string openAISystemPrompt)
    {
        _openAISystemPrompt = openAISystemPrompt;

        _client = string.IsNullOrEmpty(openAIAPIKey)
            ? new OpenAIClient(new Uri(openAIEndpoint), new DefaultAzureCredential())
            : new OpenAIClient(new Uri(openAIEndpoint), new AzureKeyCredential(openAIAPIKey));

        _options = new ChatCompletionsOptions();
        _options.DeploymentName = openAIChatDeploymentName;

        ClearConversation();
    }

    public void ClearConversation()
    {
        _options.Messages.Clear();
        _options.Messages.Add(new ChatRequestSystemMessage(_openAISystemPrompt));
    }

    public async Task<string> GetChatCompletionsStreamingAsync(string userPrompt, Action<StreamingChatCompletionsUpdate>? callback = null)
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
            if (callback != null) callback(update);
        }

        _options.Messages.Add(new ChatRequestAssistantMessage(responseContent));
        return responseContent;
    }

    private string _openAISystemPrompt;
    private ChatCompletionsOptions _options;
    private OpenAIClient _client;
}