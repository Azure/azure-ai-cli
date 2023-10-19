//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
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
using Azure.Core.Diagnostics;
using Azure.Core.Pipeline;
using Azure.Core;
using System.Diagnostics.Tracing;
using Microsoft.CognitiveServices.Speech;

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

            var interactive = _values.GetOrDefault("chat.input.interactive", false);
            if (interactive)
            {
                ChatInteractively().Wait();
            }
            else
            {
                ChatNonInteractively().Wait();
            }

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private async Task ChatInteractively()
        {
            var chatTextHandler = await GetChatTextHandler();

            var speechInput = _values.GetOrDefault("chat.speech.input", false);
            var userPrompt = _values["chat.message.user.prompt"];
            userPrompt = userPrompt.ReplaceValues(_values);

            Console.WriteLine("Press ENTER for more options.\n");

            while (true)
            {
                DisplayUserChatPrompt();

                var text = ReadLineOrSimulateInput(ref userPrompt);
                if (text.ToLower() == "")
                {
                    text = PickInteractiveContextMenu(speechInput);
                    if (text == null) continue;
                    if (text == "speech")
                    {
                        text = await GetSpeechInputAsync();
                    }

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(text);
                }

                if (text.ToLower() == "stop") break;
                if (text.ToLower() == "quit") break;
                if (text.ToLower() == "exit") break;

                var task = chatTextHandler(text);
                WaitForStopOrCancel(task);

                if (_canceledEvent.WaitOne(0)) break;
            }

            Console.ResetColor();
        }

        private async Task ChatNonInteractively()
        {
            var chatTextHandler = await GetChatTextHandler();

            var userPrompt = _values["chat.message.user.prompt"];
            if (string.IsNullOrEmpty(userPrompt))
            {
                _values.AddThrowError(
                    "ERROR:", $"Cannot start chat; option missing!\n",
                        "TRY:", $"{Program.Name} chat --interactive",
                                $"{Program.Name} chat --user PROMPT",
                                "",
                        "SEE:", $"{Program.Name} help chat");
            }
            userPrompt = userPrompt.ReplaceValues(_values);

            DisplayUserChatPrompt();
            var text = userPrompt;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(text);

            var task = chatTextHandler(text);
            WaitForStopOrCancel(task);

            Console.ResetColor();
        }

        private async Task<Func<string, Task>> GetChatTextHandler()
        {
            var function = FunctionToken.Data().GetOrDefault(_values);
            return function != null
                ? await GetChatFunctionTextHandler(function)
                : await GetNormalChatTextHandler();
        }

        private async Task<Func<string, Task>> GetChatFunctionTextHandler(string function)
        {
            return await Task.Run(() => {
                var handler = (string text) => {

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("assistant");

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(": ");

                    Console.ForegroundColor = ConsoleColor.Gray;

                    var parameters = $"{{\"question\": \"{text}\"}}";
                    var output = PythonRunner.RunEmbeddedPythonScript(_values, "function_call", "--function", function, "--parameters", parameters);

                    output = output.Replace("\n", "\n           ");
                    Console.WriteLine(output);

                    return Task.CompletedTask;
                };
                return handler;
            });
        }

        private async Task<Func<string, Task>> GetNormalChatTextHandler()
        {
            var kernel = CreateSemanticKernel(out var acsIndex);
            if (kernel != null) await StoreMemoryAsync(kernel, acsIndex);

            var client = CreateOpenAIClient(out var deployment);
            var options = CreateChatCompletionOptions();

            var handler = async (string text) =>
            {
                var relevantMemories = await SearchMemoryAsync(kernel, acsIndex, text);
                if (relevantMemories != null)
                {
                    text = UpdateUserInputWithSearchResultInfo(text, relevantMemories);
                }

                await GetChatCompletionsAsync(client, deployment, options, text);
            };
            return handler;
        }

        private static string ReadLineOrSimulateInput(ref string inputToSimulate)
        {
            var simulate = !string.IsNullOrEmpty(inputToSimulate);

            var input = simulate ? inputToSimulate : Console.ReadLine();
            inputToSimulate = null;

            if (simulate) Console.WriteLine(input);

            return input;
        }

        private SpeechConfig CreateSpeechConfig()
        {
            var existing = FileHelpers.DemandFindFileInDataPath("speech.key", _values, "speech.key");
            var key = FileHelpers.ReadAllText(existing, Encoding.Default);
            existing = FileHelpers.DemandFindFileInDataPath("speech.region", _values, "speech.region");
            var region = FileHelpers.ReadAllText(existing, Encoding.Default);

            return SpeechConfig.FromSubscription(key, region);
        }

        private async Task<string> GetSpeechInputAsync()
        {
            Console.Write("\r");
            DisplayUserChatPrompt();
            Console.ForegroundColor = ConsoleColor.DarkGray;

            var text = "(listening)";
            Console.Write($"{text} ...");
            var lastTextDisplayed = text;

            var config = CreateSpeechConfig();
            var recognizer = new SpeechRecognizer(config);
            recognizer.Recognizing += (s, e) =>
            {
                Console.Write("\r");
                DisplayUserChatPrompt();
                Console.ForegroundColor = ConsoleColor.DarkGray;

                Console.Write($"{e.Result.Text} ...");
                if (e.Result.Text.Length < lastTextDisplayed.Length) Console.Write(new string(' ', lastTextDisplayed.Length - e.Result.Text.Length));
                lastTextDisplayed = text;
            };

            var result = await recognizer.RecognizeOnceAsync();

            Console.Write("\r");
            DisplayUserChatPrompt();
            Console.Write(new string(' ', result.Text.Length + 4));

            Console.Write("\r");
            DisplayUserChatPrompt();

            return result.Text;
        }

        private static string PickInteractiveContextMenu(bool allowSpeechInput)
        {
            if (Console.CursorTop > 0)
            {
                Console.SetCursorPosition(11, Console.CursorTop - 1);
            }

            var choices = allowSpeechInput
                ? new string[] { "speech", "---", "reset conversation", "exit" }
                : new string[] { "reset conversation", "exit" };
            var select = allowSpeechInput ? 0 : choices.Length - 1;
            return ListBoxPicker.PickString(choices, 20, choices.Length + 2, new Colors(ConsoleColor.White, ConsoleColor.Blue), new Colors(ConsoleColor.White, ConsoleColor.Red), select);
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

            Console.ForegroundColor = ConsoleColor.Gray;

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

            var systemPrompt = _values.GetOrDefault("chat.message.system.prompt", DefaultSystemPrompt);
            options.Messages.Add(new ChatMessage(ChatRole.System, systemPrompt.ReplaceValues(_values)));

            var textFile = _values["chat.message.history.text.file"];
            if (!string.IsNullOrEmpty(textFile)) AddChatMessagesFromTextFile(options, textFile);

            var maxTokens = _values["chat.options.max.tokens"];
            var temperature = _values["chat.options.temperature"];
            var frequencyPenalty = _values["chat.options.frequency.penalty"];
            var presencePenalty = _values["chat.options.presence.penalty"];

            options.MaxTokens = TryParse(maxTokens, _defaultMaxTokens);
            options.Temperature = TryParse(temperature, _defaultTemperature);
            options.FrequencyPenalty = TryParse(frequencyPenalty, _defaultFrequencyPenalty);
            options.PresencePenalty = TryParse(presencePenalty, _defaultPresencePenalty);

            var stop = _values["chat.options.stop.sequence"];
            if (!string.IsNullOrEmpty(stop))
            {
                var stops = stop.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
                stops.ForEach(s => options.StopSequences.Add(s));
            }

            AddAzureExtensionOptions(options);

            return options;
        }

        private void AddAzureExtensionOptions(ChatCompletionsOptions options)
        {
            var indexName = SearchIndexNameToken.Data().GetOrDefault(_values);
            var indexOk = !string.IsNullOrEmpty(indexName);
            if (!indexOk) return;

            var searchKey = _values["service.config.search.api.key"];
            var searchEndpoint = _values["service.config.search.endpoint.uri"];
            var searchOk = !string.IsNullOrEmpty(searchKey) && !string.IsNullOrEmpty(searchEndpoint);

            var embeddingEndpoint = GetEmbeddingsDeploymentEndpoint();
            var embeddingsKey = _values["service.config.key"];
            var embeddingsOk = embeddingEndpoint != null && !string.IsNullOrEmpty(embeddingsKey);

            if (!searchOk)
            {
                _values.AddThrowError("ERROR:", $"Creating Azure AI Search extension; requires search key and endpoint.");
            }
            else if (!embeddingsOk)
            {
                _values.AddThrowError("ERROR:", $"Creating Azure AI Search extension; requires embedding key, endpoint, and deployment.");
            }

            var queryType = QueryTypeFrom(_values["service.config.search.query.type"]) ?? AzureCognitiveSearchQueryType.Vector;

            options.AzureExtensionsOptions = new()
            {
                Extensions =
                {
                    new AzureCognitiveSearchChatExtensionConfiguration(
                            AzureChatExtensionType.AzureCognitiveSearch,
                            new Uri(searchEndpoint),
                            new AzureKeyCredential(searchKey),
                            indexName)
                    {
                        QueryType = queryType,
                        EmbeddingEndpoint = embeddingEndpoint,
                        EmbeddingKey = new AzureKeyCredential(embeddingsKey),
                        DocumentCount = 16,
                    }
                }
            };
        }

        private static AzureCognitiveSearchQueryType? QueryTypeFrom(string queryType)
        {
            if (string.IsNullOrEmpty(queryType)) return null;

            return queryType.ToLower() switch
            {
                "semantic" => AzureCognitiveSearchQueryType.Semantic,
                "simple" => AzureCognitiveSearchQueryType.Simple,
                "vector" => AzureCognitiveSearchQueryType.Vector,

                "hybrid"
                or "simplehybrid"
                or "simple-hybrid"
                or "vectorsimplehybrid"
                or "vector-simple-hybrid" => AzureCognitiveSearchQueryType.VectorSimpleHybrid,

                "semantichybrid"
                or "semantic-hybrid"
                or "vectorsemantichybrid"
                or "vector-semantic-hybrid" => AzureCognitiveSearchQueryType.VectorSemanticHybrid,

                _ => throw new ArgumentException($"Invalid query type: {queryType}")
            };
        }

        private Uri GetEmbeddingsDeploymentEndpoint()
        {
            var embeddingsEndpoint = ConfigEndpointUriToken.Data().GetOrDefault(_values);
            var embeddingsDeployment = SearchEmbeddingModelDeploymentNameToken.Data().GetOrDefault(_values);

            var baseOk = !string.IsNullOrEmpty(embeddingsEndpoint) && !string.IsNullOrEmpty(embeddingsDeployment);
            var pathOk = embeddingsEndpoint.Contains("embeddings?") && embeddingsEndpoint.Contains("api-version=");

            if (baseOk && !pathOk)
            {
                var apiVersion = GetOpenAIClientVersionNumber();
                embeddingsEndpoint = $"{embeddingsEndpoint.Trim('/')}/openai/deployments/{embeddingsDeployment}/embeddings?api-version={apiVersion}";
                pathOk = true;
            }

            return baseOk && pathOk ? new Uri(embeddingsEndpoint) : null;
        }

        public static string GetOpenAIClientVersionNumber()
        {
            var latest = ((OpenAIClientOptions.ServiceVersion[])Enum.GetValues(typeof(OpenAIClientOptions.ServiceVersion))).MaxBy(i => (int)i);
            var latestVersion = latest.ToString().ToLower().Replace("_", "-").Substring(1);
            return latestVersion;
        }

        private void AddChatMessagesFromTextFile(ChatCompletionsOptions options, string textFile)
        {
            var existing = FileHelpers.DemandFindFileInDataPath(textFile, _values, "chat history");
            var text = FileHelpers.ReadAllText(existing, Encoding.Default);

            var lines = text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(x => x.Trim())
                .ToList();

            var first = lines.FirstOrDefault();
            var role = UpdateRole(ref first);

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                role = UpdateRole(ref line, role);

                if (role == ChatRole.System || role == ChatRole.User)
                {
                    line = line.ReplaceValues(_values);
                }

                if (i == 0 && role == ChatRole.System && FirstMessageIsDefaultSystemPrompt(options, role))
                {
                    options.Messages.First().Content = line;
                    continue;
                }

                options.Messages.Add(new ChatMessage(role, line));
            }
        }

        private ChatRole UpdateRole(ref string line, ChatRole? currentRole = null)
        {
            var lower = line.ToLower();
            if (lower.StartsWith("system:"))
            {
                line = line.Substring(7).Trim();
                return ChatRole.System;
            }
            else if (lower.StartsWith("user:"))
            {
                line = line.Substring(5).Trim();
                return ChatRole.User;
            }
            else if (lower.StartsWith("assistant:"))
            {
                line = line.Substring(10).Trim();
                return ChatRole.Assistant;
            }
            return currentRole ?? ChatRole.System;
        }

        private static bool FirstMessageIsDefaultSystemPrompt(ChatCompletionsOptions options, ChatRole role)
        {
            return options.Messages.Count() == 1
                && options.Messages.First().Role == ChatRole.System
                && options.Messages.First().Content == DefaultSystemPrompt;
        }

        private OpenAIClient CreateOpenAIClient(out string deployment)
        {
            var key = _values["service.config.key"];
            var host = _values["service.config.host"];
            var region = _values["service.config.region"];
            var endpoint = ConfigEndpointUriToken.Data().GetOrDefault(_values);
            var tokenValue = _values["service.config.token.value"];

            deployment = ConfigDeploymentToken.Data().GetOrDefault(_values);

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
                _azureEventSourceListener = new AzureEventSourceListener((e, message) => EventSourceAiLoggerLog(e, message), System.Diagnostics.Tracing.EventLevel.Verbose);

                var options = new OpenAIClientOptions();
                options.Diagnostics.IsLoggingContentEnabled = true;
                options.Diagnostics.IsLoggingEnabled = true;

                return new OpenAIClient(
                    new Uri(endpoint!),
                    new AzureKeyCredential(key!),
                    options
                    );
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

        private void WaitForStopOrCancel(Task task)
        {
            var interval = 100;

            while (!task.Wait(interval))
            {
                if (_stopEvent.WaitOne(0)) break;
                if (_canceledEvent.WaitOne(0)) break;
            }
        }

        private void EventSourceAiLoggerLog(EventWrittenEventArgs e, string message)
        {
            message = message.Replace("\r", "\\r").Replace("\n", "\\n");
            switch (e.Level)
            {
                case EventLevel.Error:
                    AI.DBG_TRACE_ERROR(message, 0, e.EventSource.Name, e.EventName);
                    break;

                case EventLevel.Warning:
                    AI.DBG_TRACE_WARNING(message, 0, e.EventSource.Name, e.EventName);
                    break;

                case EventLevel.Informational:
                    AI.DBG_TRACE_INFO(message, 0, e.EventSource.Name, e.EventName);
                    break;

                default:
                case EventLevel.Verbose:
                    AI.DBG_TRACE_VERBOSE(message, 0, e.EventSource.Name, e.EventName); break;
            }
        }

        private void StartCommand()
        {
            CheckPath();
            // CheckChatInput();
            LogHelpers.EnsureStartLogFile(_values);

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

            LogHelpers.EnsureStopLogFile(_values);
            // _output.CheckOutput();
            // _output.StopOutput();

            _stopEvent.Set();
        }

        private SpinLock _lock = null;
        private AzureEventSourceListener _azureEventSourceListener;

        // OutputHelper _output = null;
        // DisplayHelper _display = null;

        private int TryParse(string? s, int defaultValue)
        {
            return !string.IsNullOrEmpty(s) && int.TryParse(s, out var parsed) ? parsed : defaultValue;
        }

        private float TryParse(string? s, float defaultValue)
        {
            return !string.IsNullOrEmpty(s) && float.TryParse(s, out var parsed) ? parsed : defaultValue;
        }

        private IKernel? CreateSemanticKernel(out string acsIndex)
        {
            var key = _values["service.config.key"];
            var endpoint = ConfigEndpointUriToken.Data().GetOrDefault(_values);
            var deployment = SearchEmbeddingModelDeploymentNameToken.Data().GetOrDefault(_values);

            acsIndex = SearchIndexNameToken.Data().GetOrDefault(_values);
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

        public const string DefaultSystemPrompt = "You are an AI assistant that helps people find information regarding Azure AI.";

        private const int _defaultMaxTokens = 800;
        private const float _defaultTemperature = 0.7f;
        private const float _defaultFrequencyPenalty = 0.0f;
        private const float _defaultPresencePenalty = 0.0f;
        private const float _defaultTopP = 0.95f;
    }
}
