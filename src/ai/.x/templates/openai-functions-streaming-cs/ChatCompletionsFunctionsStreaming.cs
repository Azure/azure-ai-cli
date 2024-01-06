<#@ template hostspecific="true" #>
<#@ output extension=".cs" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using System;

public class <#= ClassName #>
{
    public <#= ClassName #>(string systemPrompt, string endpoint, string azureApiKey, string deploymentName, FunctionFactory factory)
    {
        _systemPrompt = systemPrompt;
        _functionFactory = factory;

        _client = string.IsNullOrEmpty(azureApiKey)
            ? new OpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
            : new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(azureApiKey));

        _options = new ChatCompletionsOptions();
        _options.DeploymentName = deploymentName;

        foreach (var function in _functionFactory.GetFunctionDefinitions())
        {
            _options.Functions.Add(function);
            // _options.Tools.Add(new ChatCompletionsFunctionToolDefinition(function));
        }

        _functionCallContext = new(_functionFactory, _options.Messages);
        ClearConversation();
    }

    public void ClearConversation()
    {
        _options.Messages.Clear();
        _options.Messages.Add(new ChatRequestSystemMessage(_systemPrompt));
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

                if (callback != null) callback(update);
                responseContent += content;
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

    private string _systemPrompt;
    private FunctionFactory _functionFactory;
    private FunctionCallContext _functionCallContext;
    private ChatCompletionsOptions _options;
    private OpenAIClient _client;
}
