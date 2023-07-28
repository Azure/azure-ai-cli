//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using Azure.AI.OpenAI;
using Azure.AI.Details.Common.CLI.ConsoleGui;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Memory.AzureCognitiveSearch;
using Microsoft.SemanticKernel.Memory;

namespace Azure.AI.Details.Common.CLI
{
    public class ChatCommand : Command
    {
        internal ChatCommand(ICommandValues values)
        {
            _values = values.ReplaceValues();
        }

        internal bool RunCommand()
        {
            Chat();
            return _values.GetOrDefault("passed", true);
        }

        private void Chat()
        {
            StartCommand();

            var kind = _values["chat.input.type"];
            switch (kind)
            {
                case "":
                case null:
                case "interactive":
                    // SynthesizeInteractive(false);
                    // break;
                    
                case "interactive+":
                    ChatInteractively(true).Wait();
                    break;

                // TODO: Add support for other input types
            }

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private async Task ChatInteractively(bool repeatedly = false)
        {
            var kernel = CreateSemanticKernel(out var acsIndex);
            if (kernel != null) await StoreMemoryAsync(kernel, acsIndex);

            var client = CreateOpenAIClient(out var deployment);
            var options = CreateChatCompletionOptions();

            while (true)
            {
                DisplayUserChatPrompt();
                var text = Console.ReadLine();

                if (text.ToLower() == "")
                {
                    text = PickInteractiveContextMenu();
                    if (text == null) continue;

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(text);
                }

                if (text.ToLower() == "stop") break;
                if (text.ToLower() == "quit") break;
                if (text.ToLower() == "exit") break;

                var relevantMemories = await SearchMemoryAsync(kernel, acsIndex, text);
                if (relevantMemories != null)
                {
                text = UpdateUserInputWithSearchResultInfo(text, relevantMemories);
                }

                var task = GetChatCompletionsAsync(client, deployment, options, text);
                WaitForStopOrCancel(task);

                if (!repeatedly) break;
                if (_canceledEvent.WaitOne(0)) break;
            }

            Console.ResetColor();
        }

        private static string PickInteractiveContextMenu()
        {
            if (Console.CursorTop > 0)
            {
                Console.SetCursorPosition(11, Console.CursorTop - 1);
            }

            var choices = new string[] { "exit", "reset conversation" };
            return ListBoxPicker.PickString(choices, 20, 4, new Colors(ConsoleColor.White, ConsoleColor.Blue), new Colors(ConsoleColor.White, ConsoleColor.Red));
        }

        private static void DisplayUserChatPrompt()
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("user");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("@");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("CHAT");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(": ");
        }

        private async Task<Response<StreamingChatCompletions>> GetChatCompletionsAsync(OpenAIClient client, string deployment, ChatCompletionsOptions options, string text)
        {
            options.Messages.Add(new ChatMessage(ChatRole.User, text));

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("assistant");

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(": ");

            Console.ForegroundColor = ConsoleColor.White;

            var response = await client.GetChatCompletionsStreamingAsync(deployment, options);

            var completeResponse = string.Empty;
            await foreach (var choice in response.Value.GetChoicesStreaming())
            {
                await foreach (var message in choice.GetMessageStreaming())
                {
                    var str = message.Content;
                    if (string.IsNullOrEmpty(str)) continue;

                    completeResponse = completeResponse + str;

                    str = str.Replace("\n", "\n           ");
                    Console.Write(str);
                }
                Console.WriteLine();
            }

            options.Messages.Add(new ChatMessage(ChatRole.Assistant, completeResponse));
            Console.WriteLine();

            return response;
        }

        private ChatCompletionsOptions CreateChatCompletionOptions()
        {
            var options = new ChatCompletionsOptions();
            options.Messages.Add(new ChatMessage(ChatRole.System, DefaultSystemPrompt));

            // messages.ToList().ForEach(m => options.Messages.Add(m));

            // options.MaxTokens = TryParse(maxTokens, _defaultMaxTokens);
            // options.Temperature = TryParse(temperature, _defaultTemperature);
            // options.FrequencyPenalty = TryParse(frequencyPenalty, _defaultFrequencyPenalty);
            // options.PresencePenalty = TryParse(presencePenalty, _defaultPresencePenalty);

            // if (!string.IsNullOrEmpty(stop))
            // {
            //     var stops = stop.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
            //     stops.ForEach(s => options.StopSequences.Add(s));
            // }

            return options;
        }

        private OpenAIClient CreateOpenAIClient(out string deployment)
        {
            var key = _values["service.config.key"];
            var host = _values["service.config.host"];
            var region = _values["service.config.region"];
            var endpoint = _values["service.config.endpoint.uri"];
            var tokenValue = _values["service.config.token.value"];

            deployment = _values["service.config.deployment"];

            if (string.IsNullOrEmpty(endpoint) && string.IsNullOrEmpty(region) && string.IsNullOrEmpty(host))
            {
                _values.AddThrowError("ERROR:", $"Creating OpenAIClient; requires one of: region, endpoint, or host.");
            }
            else if (!string.IsNullOrEmpty(region) && string.IsNullOrEmpty(tokenValue) && string.IsNullOrEmpty(key))
            {
                _values.AddThrowError("ERROR:", $"Creating OpenAIClient; use of region requires one of: key or token.");
            }
            else if (string.IsNullOrEmpty(deployment))
            {
                _values.AddThrowError("ERROR:", $"Creating OpenAIClient; requires deployment.");
            }

            if (!string.IsNullOrEmpty(endpoint))
            {
                return new OpenAIClient(
                    new Uri(endpoint!),
                    new AzureKeyCredential(key!));
            }
            else if (!string.IsNullOrEmpty(host))
            {
                _values.AddThrowError("ERROR:", $"Creating OpenAIClient; Not-yet-implemented create from host.");
                return null;
            }
            else // if (!string.IsNullOrEmpty(region))
            {
                _values.AddThrowError("ERROR:", $"Creating OpenAIClient; Not-yet-implemented create from region.");
                return null;
            }
        }

        private void WaitForStopOrCancel(Task<Response<StreamingChatCompletions>> task)
        {
            var interval = 100;

            while (!task.Wait(interval))
            {
                if (_stopEvent.WaitOne(0)) break;
                if (_canceledEvent.WaitOne(0)) break;
            }
        }

        private void StartCommand()
        {
            CheckPath();
            // CheckChatInput();

            // _display = new DisplayHelper(_values);

            // _output = new OutputHelper(_values);
            // _output.StartOutput();

            // var id = _values["chat.input.id"];
            // _output.EnsureOutputAll("chat.input.id", id);
            // _output.EnsureOutputEach("chat.input.id", id);

            _lock = new SpinLock();
            _lock.StartLock();
        }

        private void StopCommand()
        {
            _lock.StopLock(5000);

            // _output.CheckOutput();
            // _output.StopOutput();

            _stopEvent.Set();
        }

        private SpinLock _lock = null;

        // OutputHelper _output = null;
        // DisplayHelper _display = null;


        private IKernel? CreateSemanticKernel(out string acsIndex)
        {
           var key = _values["service.config.key"];
           var endpoint = _values["service.config.endpoint.uri"];
           var deployment = _values["service.config.embeddings.deployment"];

           acsIndex = _values["service.config.embeddings.index.name"];
           if (acsIndex == null) return null;

           if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(deployment))
           {
               return null;
           }

           var acsKey = _values["service.config.acs.key"];
           var acsEndpoint = _values["service.config.acs.endpoint.uri"];
           var acsOk = !string.IsNullOrEmpty(acsKey) && !string.IsNullOrEmpty(acsEndpoint);

           IMemoryStore store = acsOk
               ? new AzureCognitiveSearchMemoryStore(acsEndpoint, acsKey)
               : new VolatileMemoryStore();

           var kernelWithACS = Kernel.Builder
               .WithAzureTextEmbeddingGenerationService(deployment, endpoint, key)
               .WithMemoryStorage(store)
               .Build();

           return kernelWithACS;
        }

        private static async Task StoreMemoryAsync(IKernel kernel, string index)
        {
           Console.Write("Storing files in semantic memory...");
           var githubFiles = SampleData();
           foreach (var entry in githubFiles)
           {
               await kernel.Memory.SaveReferenceAsync(
                   collection: index,
                   externalSourceName: "GitHub",
                   externalId: entry.Key,
                   description: entry.Value,
                   text: entry.Value);

               Console.Write(".");
           }

           Console.WriteLine(". Done!\n");
        }

        private static async Task<string?> SearchMemoryAsync(IKernel? kernel, string collection, string text)
        {
           if (kernel == null) return null;
            
           var sb = new StringBuilder();
           var memories = kernel.Memory.SearchAsync(collection, text, limit: 2, minRelevanceScore: 0.5);
           int i = 0;
           await foreach (var memory in memories)
           {
               i++;
               sb.AppendLine($"[{memory.Metadata.Id}]: {memory.Metadata.Description}");
           }

           var result = i > 0 ? sb.ToString().Trim() : null;

           // Console.ForegroundColor = ConsoleColor.DarkGray;
           // Console.WriteLine("Relevant?\n" + result + "\n");
           // Console.ResetColor();

           return result;
        }

        private string UpdateUserInputWithSearchResultInfo(string input, string searchResults)
        {
           var sb = new StringBuilder();
           sb.Append("The below might be relevant information.\n[START INFO]\n");
           sb.Append(searchResults);
           sb.Append("\n[END INFO]");
           sb.Append("\nEach source has a name followed by colon and the actual information, always include the source name for each fact you use in the response. Use square brackets to reference the source, e.g. [info1.txt]. Don't combine sources, list each source separately, e.g. [info1.txt][info2.pdf].");
           sb.Append($"\n{input}");
           return sb.ToString();
        }

        private static Dictionary<string, string> SampleData()
        {
           return new Dictionary<string, string>
           {
               ["https://github.com/microsoft/semantic-kernel/blob/main/README.md"]
                   = "README: Installation, getting started, and how to contribute",
               ["https://github.com/microsoft/semantic-kernel/blob/main/samples/notebooks/dotnet/02-running-prompts-from-file.ipynb"]
                   = "Jupyter notebook describing how to pass prompts from a file to a semantic skill or function",
               ["https://github.com/microsoft/semantic-kernel/blob/main/samples/notebooks/dotnet/00-getting-started.ipynb"]
                   = "Jupyter notebook describing how to get started with the Semantic Kernel",
               ["https://github.com/microsoft/semantic-kernel/tree/main/samples/skills/ChatSkill/ChatGPT"]
                   = "Sample demonstrating how to create a chat skill interfacing with ChatGPT",
               ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/src/SemanticKernel/Memory/VolatileMemoryStore.cs"]
                   = "C# class that defines a volatile embedding store",
               ["https://github.com/microsoft/semantic-kernel/blob/main/samples/dotnet/KernelHttpServer/README.md"]
                   = "README: How to set up a Semantic Kernel Service API using Azure Function Runtime v4",
               ["https://github.com/microsoft/semantic-kernel/blob/main/samples/apps/chat-summary-webapp-react/README.md"]
                   = "README: README associated with a sample chat summary react-based webapp",
           };
        }

        private const string DefaultSystemPrompt = "You are an AI assistant that helps people find information regarding Azure AI.";
    }
}
