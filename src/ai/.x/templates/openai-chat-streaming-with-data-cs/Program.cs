//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using OpenAI.Chat;
using System;

public class Program
{
    public static async Task Main(string[] args)
    {
        var openAIAPIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "{AZURE_OPENAI_API_KEY}";
        var openAIApiVersion = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_VERSION") ?? "{AZURE_OPENAI_API_VERSION}";
        var openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "{AZURE_OPENAI_ENDPOINT}";
        var openAIChatDeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT") ?? "{AZURE_OPENAI_CHAT_DEPLOYMENT}";
        var openAISystemPrompt = Environment.GetEnvironmentVariable("AZURE_OPENAI_SYSTEM_PROMPT") ?? "{AZURE_OPENAI_SYSTEM_PROMPT}";

        var openAIEmbeddingsDeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") ?? "{AZURE_OPENAI_EMBEDDING_DEPLOYMENT}";
        var openAIEmbeddingsEndpoint = $"{openAIEndpoint.Trim('/')}/openai/deployments/{openAIEmbeddingsDeploymentName}/embeddings?api-version={openAIApiVersion}";

        var searchApiKey = Environment.GetEnvironmentVariable("AZURE_AI_SEARCH_KEY") ?? "{AZURE_AI_SEARCH_KEY}";
        var searchEndpoint = Environment.GetEnvironmentVariable("AZURE_AI_SEARCH_ENDPOINT") ?? "{AZURE_AI_SEARCH_ENDPOINT}";
        var searchIndexName = Environment.GetEnvironmentVariable("AZURE_AI_SEARCH_INDEX_NAME") ?? "{AZURE_AI_SEARCH_INDEX_NAME}";

        var chat = new {ClassName}(openAIEndpoint, openAIAPIKey, openAIChatDeploymentName, openAISystemPrompt, searchEndpoint, searchApiKey, searchIndexName, openAIEmbeddingsEndpoint);

        while (true)
        {
            Console.Write("User: ");
            var userPrompt = Console.ReadLine();
            if (string.IsNullOrEmpty(userPrompt) || userPrompt == "exit") break;

            Console.Write("\nAssistant: ");
            var response = await chat.GetChatCompletionsStreamingAsync(userPrompt, update => {
                var text = string.Join("", update.ContentUpdate
                    .Where(x => x.Kind == ChatMessageContentPartKind.Text)
                    .Select(x => x.Text)
                    .ToList());
                Console.Write(text);
            });
            Console.WriteLine("\n");
        }
    }
}