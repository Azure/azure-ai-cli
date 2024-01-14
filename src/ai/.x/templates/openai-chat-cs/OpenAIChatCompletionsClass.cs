<#@ template hostspecific="true" #>
<#@ output extension=".cs" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using System;

public class <#= ClassName #>
{
    public <#= ClassName #>(string openAIEndpoint, string openAIKey, string openAIChatDeploymentName, string openAISystemPrompt)
    {
        _openAISystemPrompt = openAISystemPrompt;

        _client = string.IsNullOrEmpty(openAIKey)
            ? new OpenAIClient(new Uri(openAIEndpoint), new DefaultAzureCredential())
            : new OpenAIClient(new Uri(openAIEndpoint), new AzureKeyCredential(openAIKey));

        _options = new ChatCompletionsOptions();
        _options.DeploymentName = openAIChatDeploymentName;

        ClearConversation();
    }

    public void ClearConversation()
    {
        _options.Messages.Clear();
        _options.Messages.Add(new ChatRequestSystemMessage(_openAISystemPrompt));
    }

    public string GetChatCompletion(string userPrompt)
    {
        _options.Messages.Add(new ChatRequestUserMessage(userPrompt));

        var response = _client.GetChatCompletions(_options);
        var responseContent = response.Value.Choices[0].Message.Content;

        _options.Messages.Add(new ChatRequestAssistantMessage(responseContent));
        return responseContent;
    }

    private string _openAISystemPrompt;
    private ChatCompletionsOptions _options;
    private OpenAIClient _client;
}