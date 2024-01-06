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

public class Program
{
    public static async Task Main(string[] args)
    {
        var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? "<#= OPENAI_ENDPOINT #>";
        var azureApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "<#= OPENAI_API_KEY #>";
        var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT") ?? "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>";
        var systemPrompt = Environment.GetEnvironmentVariable("AZURE_OPENAI_SYSTEM_PROMPT") ?? "<#= AZURE_OPENAI_SYSTEM_PROMPT #>";

        var factory = new FunctionFactory();
        factory.AddFunctions(typeof(ChatCompletionsCustomFunctions));
        
        var chat = new <#= ClassName #>(systemPrompt, endpoint, azureApiKey, deploymentName, factory);

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