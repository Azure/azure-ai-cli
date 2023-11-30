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
    private OpenAIClient client;
    private ChatCompletionsOptions options;

    public <#= ClassName #>()
    {
        var key = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "<#= OPENAI_API_KEY #>";
        var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? "<#= OPENAI_ENDPOINT #>";
        var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT") ?? "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>";
        var systemPrompt = Environment.GetEnvironmentVariable("AZURE_OPENAI_SYSTEM_PROMPT") ?? "<#= AZURE_OPENAI_SYSTEM_PROMPT #>";

        client = string.IsNullOrEmpty(key)
            ? new OpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
            : new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));

        options = new ChatCompletionsOptions();
        options.DeploymentName = deploymentName;
        options.Messages.Add(new ChatMessage(ChatRole.System, systemPrompt));
    }

    public string GetChatCompletion(string userPrompt)
    {
        options.Messages.Add(new ChatMessage(ChatRole.User, userPrompt));

        var response = client.GetChatCompletions(options);
        var responseContent = response.Value.Choices[0].Message.Content;
        options.Messages.Add(new ChatMessage(ChatRole.Assistant, responseContent));

        return responseContent;
    }

    public static void Main(string[] args)
    {
        var chat = new <#= ClassName #>();

        while (true)
        {
            Console.Write("User: ");
            var userPrompt = Console.ReadLine();
            if (string.IsNullOrEmpty(userPrompt) || userPrompt == "exit") break;

            var response = chat.GetChatCompletion(userPrompt);
            Console.WriteLine($"Assistant: {response}");
        }
    }
}