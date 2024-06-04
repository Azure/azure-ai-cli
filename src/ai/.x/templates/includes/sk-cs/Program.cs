//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Microsoft.SemanticKernel;
{{if {_IS_WITH_DATA_TEMPLATE}}}
using Microsoft.SemanticKernel.Connectors.OpenAI;
{{endif}}

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        {{@include sk-cs/environment_vars.cs}}

        {{if {_IS_WITH_DATA_TEMPLATE}}}
        #pragma warning disable SKEXP0010
        var dataConfig = new AzureOpenAIChatCompletionWithDataConfig
        {
            CompletionModelId = AZURE_OPENAI_CHAT_DEPLOYMENT!,
            CompletionEndpoint = AZURE_OPENAI_ENDPOINT!,
            CompletionApiKey = AZURE_OPENAI_API_KEY!,
            CompletionApiVersion = AZURE_OPENAI_API_VERSION!,
            DataSourceEndpoint = AZURE_AI_SEARCH_ENDPOINT!,
            DataSourceApiKey = AZURE_AI_SEARCH_KEY!,
            DataSourceIndex = AZURE_AI_SEARCH_INDEX_NAME!
        };
		{{endif}}

        // Create a kernel with the Azure OpenAI chat completion service
        var builder = Kernel.CreateBuilder();
		{{if {_IS_WITH_DATA_TEMPLATE}}}
        builder.AddAzureOpenAIChatCompletion(dataConfig);

        var AZURE_OPENAI_EMBEDDING_ENDPOINT = $"{AZURE_OPENAI_ENDPOINT!.Trim('/')}/openai/deployments/{AZURE_OPENAI_EMBEDDING_DEPLOYMENT}/embeddings?api-version={AZURE_OPENAI_API_VERSION}";
        builder.AddAzureOpenAITextEmbeddingGeneration(
            AZURE_OPENAI_EMBEDDING_DEPLOYMENT,
            AZURE_OPENAI_EMBEDDING_ENDPOINT,
            AZURE_OPENAI_API_KEY!);

		{{else}}
        builder.AddAzureOpenAIChatCompletion(AZURE_OPENAI_CHAT_DEPLOYMENT!, AZURE_OPENAI_ENDPOINT!, AZURE_OPENAI_API_KEY!);
		{{endif}}
        {{if {_IS_WITH_FUNCTIONS_TEMPLATE}}}
        builder.Plugins.AddFromType<SemanticKernelCustomFunctions>();
        {{endif}}
        var kernel = builder.Build();
		{{if {_IS_WITH_DATA_TEMPLATE}}}
        #pragma warning restore SKEXP0010
		{{endif}}

        // Create the streaming chat completions helper
        var chat = new {ClassName}(AZURE_OPENAI_SYSTEM_PROMPT!, kernel);

        // Loop until the user types 'exit'
        while (true)
        {
            // Get user input
            Console.Write("User: ");
            var userPrompt = Console.ReadLine();
            if (string.IsNullOrEmpty(userPrompt) || userPrompt == "exit") break;

            // Get the response
            Console.Write("\nAssistant: ");
            await chat.GetStreamingChatMessageContentsAsync(userPrompt, (content) =>
                Console.Write(content.Content)
            );

            Console.WriteLine("\n");
        }

        return 0;
    }
}