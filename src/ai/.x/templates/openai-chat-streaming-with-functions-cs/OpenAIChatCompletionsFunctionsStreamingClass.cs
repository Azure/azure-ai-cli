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
    public {ClassName}(string openAIEndpoint, string openAIAPIKey, string openAIChatDeploymentName, string openAISystemPrompt, FunctionFactory factory)
    {
        _openAISystemPrompt = openAISystemPrompt;
        _functionFactory = factory;

        _client = string.IsNullOrEmpty(openAIAPIKey)
            ? new OpenAIClient(new Uri(openAIEndpoint), new DefaultAzureCredential())
            : new OpenAIClient(new Uri(openAIEndpoint), new AzureKeyCredential(openAIAPIKey));

        _options = new ChatCompletionsOptions();
        _options.DeploymentName = openAIChatDeploymentName;

        foreach (var function in _functionFactory.GetFunctionDefinitions())
        {
            _options.Functions.Add(function);
            // _options.Tools.Add(new ChatCompletionsFunctionToolDefinition(function));
        }

        _functionCallContext = new FunctionCallContext(_functionFactory, _options.Messages);
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
        while (true)
        {
            var response = await _client.GetChatCompletionsStreamingAsync(_options);
            await foreach (var update in response.EnumerateValues())
            {
                _functionCallContext.CheckForUpdate(update);

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

            if (_functionCallContext.TryCallFunction() != null)
            {
                _functionCallContext.Clear();
                continue;
            }

            _options.Messages.Add(new ChatRequestAssistantMessage(responseContent));
            return responseContent;
        }
    }

    private string _openAISystemPrompt;
    private FunctionFactory _functionFactory;
    private FunctionCallContext _functionCallContext;
    private ChatCompletionsOptions _options;
    private OpenAIClient _client;
}