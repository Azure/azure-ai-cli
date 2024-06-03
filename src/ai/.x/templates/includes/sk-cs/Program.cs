//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Microsoft.SemanticKernel;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        {{@include sk-cs/environment_vars.cs}}

        // Create a kernel with the Azure OpenAI chat completion service
        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(AZURE_OPENAI_CHAT_DEPLOYMENT!, AZURE_OPENAI_ENDPOINT!, AZURE_OPENAI_API_KEY!);
        {{if {_IS_WITH_FUNCTIONS_TEMPLATE}}}
        builder.Plugins.AddFromType<SemanticKernelCustomFunctions>();
        {{endif}}
        var kernel = builder.Build();

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