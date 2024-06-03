//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
{{if {_IS_WITH_FUNCTIONS_TEMPLATE}}}
using Microsoft.SemanticKernel.Connectors.OpenAI;    
{{endif}}

public class {ClassName}
{
    public {ClassName}(string systemPrompt, Kernel kernel)
    {
        _systemPrompt = systemPrompt;
        _kernel = kernel;
        _history = new ChatHistory(_systemPrompt);
        _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
    }

    public void ClearConversation()
    {
        _history.RemoveRange(1, _history.Count);
    }

    public async Task<string> GetStreamingChatMessageContentsAsync(string userPrompt, Action<StreamingChatMessageContent>? callback = null)
    {
        _history.AddUserMessage(userPrompt);

        {{if {_IS_WITH_FUNCTIONS_TEMPLATE}}}
        // Automatically invoke kernel functions
        var settings = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };

        {{endif}}
        // Get the response content by iterating over the streaming chat messages
        var responseContent = string.Empty;
        {{if {_IS_WITH_FUNCTIONS_TEMPLATE}}}
        var response = _chatCompletionService.GetStreamingChatMessageContentsAsync(_history, settings, _kernel);
        {{else}}
        var response = _chatCompletionService.GetStreamingChatMessageContentsAsync(_history);
        {{endif}}
        await foreach (var content in response)
        {
            if (!string.IsNullOrEmpty(content.Content))
            {
                responseContent += content.Content;
                if (callback != null) callback(content);
            }
        }

        _history.AddAssistantMessage(responseContent);
        return responseContent;
    }

    private string _systemPrompt;
    private Kernel _kernel;
    private ChatHistory _history;
    private IChatCompletionService _chatCompletionService;
}