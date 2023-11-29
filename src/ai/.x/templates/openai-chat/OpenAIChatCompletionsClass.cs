<#@ template hostspecific="true" #>
<#@ output extension=".cs" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
using Azure.AI.OpenAI;
using Azure.Identity;
using System;

public class <#= ClassName #>
{
    private OpenAIClient client;

    public <#= ClassName #>()
    {
        string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "... or insert your endpoint here ...";
        client = new OpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
    }

    public string GetChatbotResponse(string deploymentName, string prompt)
    {
        var options = new ChatCompletionsOptions()
        {
            DeploymentName = deploymentName,
        };

        var response = client.GetChatCompletions(options);
        return response.Value.Choices[0].Message.Content;
    }
}