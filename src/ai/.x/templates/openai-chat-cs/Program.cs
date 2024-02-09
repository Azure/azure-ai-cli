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
    public static void Main(string[] args)
    {
        var openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "<#= AZURE_OPENAI_ENDPOINT #>";
        var openAIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY") ?? "<#= AZURE_OPENAI_KEY #>";
        var openAIChatDeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT") ?? "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>";
        var openAISystemPrompt = Environment.GetEnvironmentVariable("AZURE_OPENAI_SYSTEM_PROMPT") ?? "<#= AZURE_OPENAI_SYSTEM_PROMPT #>";

		var chat = new <#= ClassName #>(openAIEndpoint, openAIKey, openAIChatDeploymentName, openAISystemPrompt);

        while (true)
        {
            Console.Write("User: ");
            var userPrompt = Console.ReadLine();
            if (string.IsNullOrEmpty(userPrompt) || userPrompt == "exit") break;

            var response = chat.GetChatCompletion(userPrompt);
            Console.WriteLine($"\nAssistant: {response}\n");
        }
    }
}