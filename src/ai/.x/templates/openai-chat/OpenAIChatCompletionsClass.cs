<#@ template hostspecific="true" #>
<#@ output extension=".cs" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
<#@ parameter type="System.String" name="OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="OPENAI_API_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
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

    public static void Main(string[] args)
    {
        var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? "<#= OPENAI_ENDPOINT #>";
        var azureApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "<#= OPENAI_API_KEY #>";
        var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT") ?? "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>";
        var systemPrompt = Environment.GetEnvironmentVariable("AZURE_OPENAI_SYSTEM_PROMPT") ?? "<#= AZURE_OPENAI_SYSTEM_PROMPT #>";
		
		var chat = new <#= ClassName #>(systemPrompt, endpoint, azureApiKey, deploymentName);

        while (true)
        {
            Console.Write("User: ");
            var userPrompt = Console.ReadLine();
            if (string.IsNullOrEmpty(userPrompt) || userPrompt == "exit") break;

            var response = chat.GetChatCompletion(userPrompt);
            Console.WriteLine($"\nAssistant: {response}\n");
        }
    }

    private string _systemPrompt;
    private ChatCompletionsOptions _options;
    private OpenAIClient _client;
}