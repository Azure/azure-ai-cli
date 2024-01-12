<#@ template hostspecific="true" #>
<#@ output extension=".cs" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
using System;

public class Program
{
    public static async Task Main(string[] args)
    {
        var azureOpenApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY") ?? "<#= AZURE_OPENAI_KEY #>";
        var azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "<#= AZURE_OPENAI_ENDPOINT #>";
        var azureOpenAIDeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT") ?? "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>";
        var systemPrompt = Environment.GetEnvironmentVariable("AZURE_OPENAI_SYSTEM_PROMPT") ?? "<#= AZURE_OPENAI_SYSTEM_PROMPT #>";

		var chat = new <#= ClassName #>(systemPrompt, azureOpenApiKey, azureOpenAIEndpoint, azureOpenAIDeploymentName);

        while (true)
        {
            Console.Write("User: ");
            var userPrompt = Console.ReadLine();
            if (string.IsNullOrEmpty(userPrompt) || userPrompt == "exit") break;

            Console.Write("\nAssistant: ");
            var response = await chat.GetChatCompletionsStreamingAsync(userPrompt, update =>
                Console.Write(update.ContentUpdate)
            );
            Console.WriteLine("\n");
        }
    }
}