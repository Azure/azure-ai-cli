//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI;
using OpenAI.Assistants;

class Program
{
    static async Task Main(string[] args)
    {
        var assistantId = Environment.GetEnvironmentVariable("ASSISTANT_ID") ?? "<insert your OpenAI assistant ID here>";
        var threadId = args.Length > 0 ? args[0] : null;

        // Validate environment variables
        var openAIAPIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "<insert your Azure OpenAI API key here>";
        var openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "<insert your Azure OpenAI endpoint here>";
        
        if (string.IsNullOrEmpty(openAIAPIKey) || openAIAPIKey.StartsWith("<insert") ||
            string.IsNullOrEmpty(openAIEndpoint) || openAIEndpoint.StartsWith("<insert") ||
            string.IsNullOrEmpty(assistantId) || assistantId.StartsWith("<insert"))
        {
            Console.WriteLine("To use Azure OpenAI, set the following environment variables:");
            Console.WriteLine("  ASSISTANT_ID\n  AZURE_OPENAI_API_KEY\n  AZURE_OPENAI_ENDPOINT");
            Environment.Exit(1);
        }

        // Initialize OpenAI Client
        var client = string.IsNullOrEmpty(openAIAPIKey)
            ? new AzureOpenAIClient(new Uri(openAIEndpoint), new DefaultAzureCredential())
            : new AzureOpenAIClient(new Uri(openAIEndpoint), new AzureKeyCredential(openAIAPIKey));

        {{if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}
        // Register custom functions
        var factory = new FunctionFactory();
        factory.AddFunctions(typeof(OpenAIChatCompletionsCustomFunctions));

        {{endif}}
        // Create the helper class
        var assistant = new {ClassName}(client, assistantId{{if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}, factory{{endif}});

        // Create or retrieve thread
        if (string.IsNullOrEmpty(threadId))
        {
            await assistant.CreateThreadAsync();
        }
        else
        {
            await assistant.RetrieveThreadAsync(threadId);
            await assistant.GetThreadMessagesAsync((role, content) => 
            {
                Console.WriteLine($"{char.ToUpper(role[0]) + role.Substring(1)}: {content}\n");
            });
        }

        // User interaction loop
        while (true)
        {
            Console.Write("User: ");
            var userPrompt = Console.ReadLine();
            if (string.IsNullOrEmpty(userPrompt) || userPrompt == "exit") break;

            Console.Write("\nAssistant: ");
            {{if {_IS_OPENAI_ASST_STREAMING_TEMPLATE}}}
            await assistant.GetResponseAsync(userPrompt, content => {
                Console.Write(content);
            });
            {{else}}
            var response = await assistant.GetResponseAsync(userPrompt);
            Console.WriteLine($"{response}\n");
            {{endif}}
        }

        Console.WriteLine($"Bye! (ThreadId: {assistant.Thread?.Id})");
    }
}