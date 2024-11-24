//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

{{if {_IS_OPENAI_CHAT_STREAMING_TEMPLATE}}}
using OpenAI.Chat;
{{endif}}
using System;

public class Program
{
{{if {_IS_OPENAI_CHAT_STREAMING_TEMPLATE}}}
    public static async Task Main(string[] args)
{{else}}
    public static void Main(string[] args)
{{endif}}
    {
        {{@include openai-cs/environment_vars.cs}}

{{if {_IS_WITH_FUNCTIONS_TEMPLATE}}}
        var factory = new FunctionFactory();
        factory.AddFunctions(typeof(OpenAIChatCompletionsCustomFunctions));
        var chat = new {ClassName}(openAIEndpoint, openAIAPIKey, openAIChatDeploymentName, openAISystemPrompt, factory);
{{else if {_IS_WITH_DATA_TEMPLATE}}}
        var chat = new {ClassName}(openAIEndpoint, openAIAPIKey, openAIChatDeploymentName, openAISystemPrompt, searchEndpoint, searchApiKey, searchIndexName, openAIEmbeddingsEndpoint);
{{else}}
		var chat = new {ClassName}(openAIEndpoint, openAIAPIKey, openAIChatDeploymentName, openAISystemPrompt);
{{endif}}

        while (true)
        {
            Console.Write("User: ");
            var userPrompt = Console.ReadLine();
            if (string.IsNullOrEmpty(userPrompt) || userPrompt == "exit") break;

{{if {_IS_OPENAI_CHAT_STREAMING_TEMPLATE}}}
            Console.Write("\nAssistant: ");
            var response = await chat.GetChatCompletionsStreamingAsync(userPrompt, update => {
                var text = string.Join("", update.ContentUpdate
                    .Where(x => x.Kind == ChatMessageContentPartKind.Text)
                    .Select(x => x.Text)
                    .ToList());
                Console.Write(text);
            });
            Console.WriteLine("\n");
{{else}}
            var response = chat.GetChatCompletion(userPrompt);
            Console.WriteLine($"\nAssistant: {response}\n");
{{endif}}
        }
    }
}