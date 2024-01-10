<#@ template hostspecific="true" #>
<#@ output extension=".cs" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using System;

public class <#= ClassName #>
{
    public <#= ClassName #>(string systemPrompt, string endpoint, string azureApiKey, string deploymentName)
    {
        _systemPrompt = systemPrompt;

        _client = string.IsNullOrEmpty(azureApiKey)
            ? new OpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
            : new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(azureApiKey));

        _options = new ChatCompletionsOptions();
        _options.DeploymentName = deploymentName;
		
        ClearConversation();
    }

    public void ClearConversation()
    {
        _options.Messages.Clear();
        _options.Messages.Add(new ChatRequestSystemMessage(_systemPrompt));
    }

    public string GetChatCompletion(string userPrompt)
    {
        _options.Messages.Add(new ChatRequestUserMessage(userPrompt));

        var response = _client.GetChatCompletions(_options);
        var responseContent = response.Value.Choices[0].Message.Content;

        _options.Messages.Add(new ChatRequestAssistantMessage(responseContent));
        return responseContent;
    }

    private string _systemPrompt;
    private ChatCompletionsOptions _options;
    private OpenAIClient _client;
}