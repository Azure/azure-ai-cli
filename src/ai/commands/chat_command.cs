//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Azure.AI.Details.Common.CLI.ConsoleGui;
using Azure.AI.Details.Common.CLI.Extensions.HelperFunctions;
using Azure.AI.OpenAI;
using Azure.Core.Diagnostics;
using Microsoft.CognitiveServices.Speech;
using System;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Scriban;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using Azure.AI.OpenAI.Chat;
using OpenAI.Assistants;

#pragma warning disable AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace Azure.AI.Details.Common.CLI
{
    public class ChatCommand : Command
    {
        internal ChatCommand(ICommandValues values) : base(values)
        {
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", true);
        }

        internal bool RunCommand()
        {
            try
            {
                DoCommand(_values.GetCommand());
            }
            catch (AggregateException ex)
            {
                ex.Handle(x => {
                    var msg = x.Message.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    ConsoleHelpers.WriteLineError($"\n  ERROR: {msg}");
                    return true;
                });
                _values.Reset("passed", "false");
            }

            return _values.GetOrDefault("passed", true);
        }

        private void DoCommand(string command)
        {
            StartCommand();

            switch (command)
            {
                case "chat": DoChat(); break;

                case "chat.assistant": HelpCommandParser.DisplayHelp(_values); break;
                case "chat.assistant.create": DoChatAssistantCreate().Wait(); break;
                case "chat.assistant.delete": DoChatAssistantDelete().Wait(); break;
                case "chat.assistant.get": DoChatAssistantGet().Wait(); break;
                case "chat.assistant.list": DoChatAssistantList().Wait(); break;

                case "chat.assistant.vector-store": HelpCommandParser.DisplayHelp(_values); break;
                case "chat.assistant.vector-store.create": HelpCommandParser.DisplayHelp(_values); break;
                case "chat.assistant.vector-store.delete": HelpCommandParser.DisplayHelp(_values); break;
                case "chat.assistant.vector-store.get": HelpCommandParser.DisplayHelp(_values); break;
                case "chat.assistant.vector-store.list": HelpCommandParser.DisplayHelp(_values); break;

                case "chat.assistant.file": HelpCommandParser.DisplayHelp(_values); break;
                case "chat.assistant.file.upload": DoChatAssistantFileUpload().Wait(); break;
                case "chat.assistant.file.list": DoChatAssistantFileList().Wait(); break;
                case "chat.assistant.file.delete": DoChatAssistantFileDelete().Wait(); break;

                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
            }

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private void DoChat()
        {
            var interactive = _values.GetOrDefault("chat.input.interactive", false);
            if (interactive)
            {
                ChatInteractively().Wait();
            }
            else
            {
                ChatNonInteractively().Wait();
            }
        }

        private async Task ChatInteractively()
        {
            var chatTextHandler = await GetChatTextHandlerAsync(interactive: true);

            var speechInput = _values.GetOrDefault("chat.speech.input", false);
            var userPrompt = _values["chat.message.user.prompt"];

            if (!_quiet) Console.WriteLine("Press ENTER for more options.\n");

            while (true)
            {
                DisplayUserChatPromptLabel();

                var text = ReadLineOrSimulateInput(ref userPrompt, "exit");
                if (text.ToLower() == "")
                {
                    text = PickInteractiveContextMenu(speechInput);
                    if (text == null) continue;

                    var fromSpeech = false;
                    if (text == "speech")
                    {
                        text = await GetSpeechInputAsync();
                        fromSpeech = true;
                    }

                    DisplayUserChatPromptText(text, fromSpeech);
                }

                if (text.ToLower() == "stop") break;
                if (text.ToLower() == "quit") break;
                if (text.ToLower().Trim('.') == "exit") break;

                var task = chatTextHandler(text);
                WaitForStopOrCancel(task);

                if (_canceledEvent.WaitOne(0)) break;
            }

            Console.ResetColor();
        }

        private async Task ChatNonInteractively()
        {
            var chatTextHandler = await GetChatTextHandlerAsync(interactive: false);

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

            DisplayUserChatPromptLabel();
            var text = userPrompt;
            DisplayUserChatPromptText(text);

            var task = chatTextHandler(text);
            WaitForStopOrCancel(task);

            Console.ResetColor();
        }

        private async Task<Func<string, Task>> GetChatTextHandlerAsync(bool interactive)
        {
            var parameterFile = InputChatParameterFileToken.Data().GetOrDefault(_values);
            if (!string.IsNullOrEmpty(parameterFile)) SetValuesFromParameterFile(parameterFile);

            var assistantId = _values["chat.assistant.id"];
            var assistantIdOk = !string.IsNullOrEmpty(assistantId);

            return assistantIdOk
                ? await GetAssistantsAPITextHandlerAsync(interactive, assistantId)
                : GetChatCompletionsTextHandler(interactive);
        }

        private async Task<Func<string, Task>> GetAssistantsAPITextHandlerAsync(bool interactive, string assistantId)
        {
            var threadId = _values.GetOrDefault("chat.thread.id", null);

            var client = CreateAssistantClient();
            var thread = await CreateOrGetAssistantThread(client, threadId);

            var options = new RunCreationOptions();
            var factory = CreateFunctionFactoryWithRunOptions(options);

            return async (string text) =>
            {
                if (interactive && text.ToLower() == "reset")
                {
                    // ClearMessageHistory(messages);
                    return;
                }

                await GetAssistantsAPIResponseAsync(client, assistantId, thread, options, factory, text);
            };
        }

        private Func<string, Task> GetChatCompletionsTextHandler(bool interactive)
        {
            var client = CreateOpenAIClient(out var deployment);
            var chatClient = client.GetChatClient(deployment);

            var options = CreateChatCompletionOptions(out var messages);
            var funcContext = CreateChatCompletionsFunctionFactoryAndCallContext(messages, options);

            return async (string text) =>
            {
                if (interactive && text.ToLower() == "reset")
                {
                    ClearMessageHistory(messages);
                    return;
                }

                await GetChatCompletionsAsync(chatClient, messages, options, funcContext, text);
            };
        }

        private static string ReadLineOrSimulateInput(ref string inputToSimulate, string defaultOnEndOfRedirectedInput = null)
        {
            var simulate = !string.IsNullOrEmpty(inputToSimulate);

            var input = simulate ? inputToSimulate : ConsoleHelpers.ReadLineOrDefault("", defaultOnEndOfRedirectedInput);
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
            DisplayUserChatPromptLabel();
            Console.ForegroundColor = ColorHelpers.MapColor(ConsoleColor.DarkGray);

            var text = "(listening)";
            Console.Write($"{text} ...");
            var lastTextDisplayed = text;

            var config = CreateSpeechConfig();
            var recognizer = new SpeechRecognizer(config);
            recognizer.Recognizing += (s, e) =>
            {
                Console.Write("\r");
                DisplayUserChatPromptLabel();
                Console.ForegroundColor = ColorHelpers.MapColor(ConsoleColor.DarkGray);

                Console.Write($"{e.Result.Text} ...");
                if (e.Result.Text.Length < lastTextDisplayed.Length) Console.Write(new string(' ', lastTextDisplayed.Length - e.Result.Text.Length));
                lastTextDisplayed = text;
            };

            var result = await recognizer.RecognizeOnceAsync();

            Console.Write("\r");
            DisplayUserChatPromptLabel();
            Console.Write(new string(' ', result.Text.Length + 4));

            Console.Write("\r");
            DisplayUserChatPromptLabel();

            return result.Text;
        }

        private string PickInteractiveContextMenu(bool allowSpeechInput)
        {
            if (Console.CursorTop > 0)
            {
                var x = _quiet ? 0 : 11;
                Console.SetCursorPosition(x, Console.CursorTop - 1);
            }

            var choices = allowSpeechInput
                ? new string[] { "speech", "---", "reset conversation", "exit" }
                : new string[] { "reset conversation", "exit" };
            var select = allowSpeechInput ? 0 : choices.Length - 1;

            var choice = ListBoxPicker.PickString(choices, 20, choices.Length + 2, new Colors(ConsoleColor.White, ConsoleColor.Blue), new Colors(ConsoleColor.White, ConsoleColor.Red), select);
            return choice switch
            {
                "speech" => "speech",
                "exit" => "exit",
                _ => "reset"
            };
        }

        private void DisplayUserChatPromptLabel()
        {
            if (!_quiet)
            {
                Console.Write('\r');
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("user");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("@");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write("CHAT");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(": ");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
        }

        private void DisplayUserChatPromptText(string text, bool fromSpeech = false)
        {
            if (_quiet && !fromSpeech) return;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(text);
        }

        private void DisplayAssistantPromptLabel()
        {
            if (!_quiet)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("assistant");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(": ");
                Console.ForegroundColor = ColorHelpers.MapColor(ConsoleColor.Gray);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        private void DisplayAssistantPromptTextStreaming(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            // do "tab" indentation when not quiet
            Console.Write(!_quiet
                ? text.Replace("\n", "\n           ")
                : text);
        }

        private void DisplayAssistantPromptTextStreamingDone()
        {
            Console.WriteLine("\n");
        }

        private void DisplayAssistantFunctionCall(string functionName, string functionArguments, string result)
        {
            if (!_quiet && _verbose)
            {
               Console.ForegroundColor = ConsoleColor.Green;
               Console.Write("\rassistant-function");
               Console.ForegroundColor = ConsoleColor.White;
               Console.Write(": ");
               Console.ForegroundColor = ColorHelpers.MapColor(ConsoleColor.DarkGray);
               Console.WriteLine($"{functionName}({functionArguments}) = {result}");

               DisplayAssistantPromptLabel();
            }
        }

        public async Task GetAssistantsAPIResponseAsync(AssistantClient assistantClient, string assistantId, AssistantThread thread, RunCreationOptions options, HelperFunctionFactory factory, string userInput)
        {
            await assistantClient.CreateMessageAsync(thread, [ userInput ]);
            _ = CheckWriteChatHistoryOutputFileAsync(assistantClient, thread);

            DisplayAssistantPromptLabel();

            var assistant = await assistantClient.GetAssistantAsync(assistantId);
            var stream = assistantClient.CreateRunStreamingAsync(thread, assistant.Value, options);

            string contentComplete = string.Empty;
            ThreadRun? run = null;
            List<ToolOutput> toolOutputs = [];
            do
            {
                await foreach (var update in stream)
                {
                    if (update is MessageContentUpdate contentUpdate)
                    {
                        contentComplete += contentUpdate.Text;
                        DisplayAssistantPromptTextStreaming(contentUpdate.Text);
                    }
                    else if (update is RunUpdate runUpdate)
                    {
                        run = runUpdate;
                    }
                    else if (update is RequiredActionUpdate requiredActionUpdate)
                    {
                        if (factory.TryCallFunction(requiredActionUpdate.FunctionName, requiredActionUpdate.FunctionArguments, out var result))
                        {
                            DisplayAssistantFunctionCall(requiredActionUpdate.FunctionName, requiredActionUpdate.FunctionArguments, result);
                            DisplayAssistantPromptLabel();
                            toolOutputs.Add(new ToolOutput(requiredActionUpdate.ToolCallId, result));
                        }
                    }

                    if (run?.Status.IsTerminal == true)
                    {
                        DisplayAssistantPromptTextStreamingDone();
                        CheckWriteChatAnswerOutputFile(contentComplete);
                    }
                }

                if (toolOutputs.Count > 0 && run != null)
                {
                    stream = assistantClient.SubmitToolOutputsToRunStreamingAsync(run, toolOutputs);
                    toolOutputs.Clear();
                }
            }
            while (run?.Status.IsTerminal == false);

            await CheckWriteChatHistoryOutputFileAsync(assistantClient, thread);
        }

        private async Task<string> GetChatCompletionsAsync(ChatClient client, List<ChatMessage> messages, ChatCompletionOptions options, HelperFunctionCallContext functionCallContext, string text)
        {
            var requestMessage = new UserChatMessage(text);
            messages.Add(requestMessage);
            CheckWriteChatHistoryOutputFile(messages);

            DisplayAssistantPromptLabel();

            string contentComplete = string.Empty;
            while (true)
            {
                var response = client.CompleteChatStreamingAsync(messages, options);
                await foreach (var update in response)
                {
                    functionCallContext.CheckForUpdate(update);

                    CheckChoiceFinishReason(update.FinishReason);

                    var content = update.ContentUpdate.ToText();
                    if (update.FinishReason == ChatFinishReason.ContentFilter)
                    {
                        content = $"{content}\nWARNING: Content filtered!";
                    }

                    if (content == null) continue;

                    contentComplete += content;
                    DisplayAssistantPromptTextStreaming(content);
                }

                if (functionCallContext.TryCallFunctions(contentComplete, (name, args, result) => DisplayAssistantFunctionCall(name, args, result)))
                {
                    functionCallContext.Clear();
                    CheckWriteChatHistoryOutputFile(messages);
                    continue;
                }

                DisplayAssistantPromptTextStreamingDone();
                CheckWriteChatAnswerOutputFile(contentComplete);

                var currentContent = new AssistantChatMessage(contentComplete);
                messages.Add(currentContent);
                
                CheckWriteChatHistoryOutputFile(messages);

                return contentComplete;
            }
        }

        private void CheckWriteChatAnswerOutputFile(string completeResponse)
        {
            var outputAnswerFile = OutputChatAnswerFileToken.Data().GetOrDefault(_values);
            if (!string.IsNullOrEmpty(outputAnswerFile))
            {
                var fileName = FileHelpers.GetOutputDataFileName(outputAnswerFile, _values);
                FileHelpers.WriteAllText(fileName, completeResponse, Encoding.UTF8);
            }
        }

        private async Task CheckWriteChatHistoryOutputFileAsync(AssistantClient client, AssistantThread thread)
        {
            var outputHistoryFile = OutputChatHistoryFileToken.Data().GetOrDefault(_values);
            if (!string.IsNullOrEmpty(outputHistoryFile))
            {
                var fileName = FileHelpers.GetOutputDataFileName(outputHistoryFile, _values);
                await thread.SaveChatHistoryToFileAsync(client, fileName);
            }
        }

        private void CheckWriteChatHistoryOutputFile(IList<ChatMessage> messages)
        {
            var outputHistoryFile = OutputChatHistoryFileToken.Data().GetOrDefault(_values);
            if (!string.IsNullOrEmpty(outputHistoryFile))
            {
                var fileName = FileHelpers.GetOutputDataFileName(outputHistoryFile, _values);
                messages.SaveChatHistoryToFile(fileName);
            }
        }

        private void ClearMessageHistory(List<ChatMessage> messages)
        {
            messages.RemoveRange(1, messages.Count - 1);
            CheckWriteChatHistoryOutputFile(messages);

            DisplayAssistantPromptLabel();
            DisplayAssistantPromptTextStreaming("I've reset the conversation. How can I help you today?");
            DisplayAssistantPromptTextStreamingDone();
        }

        private ChatCompletionOptions CreateChatCompletionOptions(out List<ChatMessage> messages)
        {
            var systemPrompt = _values.GetOrDefault("chat.message.system.prompt", DefaultSystemPrompt);

            messages = new List<ChatMessage>();
            messages.Add(ChatMessage.CreateSystemMessage(systemPrompt));

            var textFile = _values["chat.message.history.text.file"];
            var jsonFile = InputChatHistoryJsonFileToken.Data().GetOrDefault(_values);

            if(!string.IsNullOrEmpty(jsonFile) && !string.IsNullOrEmpty(textFile))
            {
                _values.AddThrowError("chat.message.history.text.file", "chat.message.history.json.file", "Only one of these options can be specified");
            }

            if (!string.IsNullOrEmpty(jsonFile)) messages.ReadChatHistoryFromFile(jsonFile);
            if (!string.IsNullOrEmpty(textFile)) AddChatMessagesFromTextFile(messages, textFile);

            var maxTokens = _values["chat.options.max.tokens"];
            var temperature = _values["chat.options.temperature"];
            var frequencyPenalty = _values["chat.options.frequency.penalty"];
            var presencePenalty = _values["chat.options.presence.penalty"];

            var options = new ChatCompletionOptions()
            {
                MaxTokens = TryParse(maxTokens, null),
                Temperature = TryParse(temperature, _defaultTemperature),
                FrequencyPenalty = TryParse(frequencyPenalty, _defaultFrequencyPenalty),
                PresencePenalty = TryParse(presencePenalty, _defaultPresencePenalty),
            };

            var stop = _values["chat.options.stop.sequence"];
            if (!string.IsNullOrEmpty(stop))
            {
                var stops = stop.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
                stops.ForEach(s => options.StopSequences.Add(s));
            }

            AddAzureExtensionOptions(options);

            return options;
        }

        private void AddAzureExtensionOptions(ChatCompletionOptions options)
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

            var queryType = QueryTypeFrom(_values["service.config.search.query.type"]) ?? DataSourceQueryType.VectorSimpleHybrid;

            options.AddDataSource(new AzureSearchChatDataSource()
            {
                Authentication = DataSourceAuthentication.FromApiKey(searchKey),
                Endpoint = new Uri(searchEndpoint),
                IndexName = indexName,
                QueryType = queryType,
                VectorizationSource = DataSourceVectorizer.FromEndpoint(embeddingEndpoint, DataSourceAuthentication.FromApiKey(embeddingsKey))
            });
        }

        private static DataSourceQueryType? QueryTypeFrom(string queryType)
        {
            if (string.IsNullOrEmpty(queryType)) return null;

            return queryType.ToLower() switch
            {
                "semantic" => DataSourceQueryType.Semantic,
                "simple" => DataSourceQueryType.Simple,
                "vector" => DataSourceQueryType.Vector,

                "hybrid"
                or "simplehybrid"
                or "simple-hybrid"
                or "vectorsimplehybrid"
                or "vector-simple-hybrid" => DataSourceQueryType.VectorSimpleHybrid,

                "semantichybrid"
                or "semantic-hybrid"
                or "vectorsemantichybrid"
                or "vector-semantic-hybrid" => DataSourceQueryType.VectorSemanticHybrid,

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
            var latest = ((AzureOpenAIClientOptions.ServiceVersion[])Enum.GetValues(typeof(AzureOpenAIClientOptions.ServiceVersion))).MaxBy(i => (int)i);
            var latestVersion = latest.ToString().ToLower().Replace("_", "-").Substring(1);
            return latestVersion;
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
                // _azureEventSourceListener = new AzureEventSourceListener((e, message) => EventSourceHelpers.EventSourceAiLoggerLog(e, message), System.Diagnostics.Tracing.EventLevel.Verbose);

                var options = new AzureOpenAIClientOptions();
                //options.Diagnostics.IsLoggingContentEnabled = true;
                //options.Diagnostics.IsLoggingEnabled = true;

                return new AzureOpenAIClient(
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

        private AssistantClient CreateAssistantClient()
        {
            var client = CreateOpenAIClient(out var deployment);
            return client.GetAssistantClient();
        }

        private async Task<AssistantThread> CreateOrGetAssistantThread(AssistantClient client, string threadId)
        {
            return string.IsNullOrEmpty(threadId)
                ? await CreateAssistantThread(client)
                : await GetAssistantThread(client, threadId);
        }

        private static async Task<AssistantThread> CreateAssistantThread(AssistantClient client)
        {
            var result = await client.CreateThreadAsync();
            return result.Value;
        }

        private async Task<AssistantThread> GetAssistantThread(AssistantClient client, string threadId)
        {
            var result = await client.GetThreadAsync(threadId);
            var thread = result.Value;

            await foreach (var message in client.GetMessagesAsync(thread, ListOrder.OldestFirst))
            {
                var content = string.Join("", message.Content.Select(c => c.Text));
                var isUser = message.Role == MessageRole.User;
                var isAssistant = message.Role == MessageRole.Assistant;

                if (isUser)
                {
                    DisplayUserChatPromptLabel();
                    DisplayUserChatPromptText(content);
                }

                if (isAssistant)
                {
                    DisplayAssistantPromptLabel();
                    DisplayAssistantPromptTextStreaming(content);
                    DisplayAssistantPromptTextStreamingDone();
                }
            }

            return thread;
        }

        private HelperFunctionFactory CreateFunctionFactoryWithRunOptions(RunCreationOptions options)
        {
            var factory = CreateFunctionFactory();
            foreach (var tool in factory.GetToolDefinitions())
            {
                options.ToolsOverride.Add(tool);
            }

            return factory;
        }

        private HelperFunctionCallContext CreateChatCompletionsFunctionFactoryAndCallContext(IList<ChatMessage> messages, ChatCompletionOptions options)
        {
            var factory = CreateFunctionFactory();
            foreach (var tool in factory.GetChatTools())
            {
                options.Tools.Add(tool);
            }

            return new HelperFunctionCallContext(factory, messages);
        }

        private HelperFunctionFactory CreateFunctionFactory()
        {
            var customFunctions = _values.GetOrDefault("chat.custom.helper.functions", null);
            var useCustomFunctions = !string.IsNullOrEmpty(customFunctions);
            var useBuiltInFunctions = _values.GetOrDefault("chat.built.in.helper.functions", false);

            return useCustomFunctions && useBuiltInFunctions
                ? CreateFunctionFactoryForCustomFunctions(customFunctions) + CreateFunctionFactoryWithBuiltinFunctions()
                : useCustomFunctions
                    ? CreateFunctionFactoryForCustomFunctions(customFunctions)
                    : useBuiltInFunctions
                        ? CreateFunctionFactoryWithBuiltinFunctions()
                        : CreateFunctionFactoryWithNoFunctions();
        }

        private HelperFunctionFactory CreateFunctionFactoryForCustomFunctions(string customFunctions)
        {
            var factory = new HelperFunctionFactory();

            var patterns = customFunctions.Split(new char[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var pattern in patterns)
            {
                var files = FileHelpers.FindFiles(pattern, _values);
                if (files.Count() == 0)
                {
                    files = FileHelpers.FindFilesInOsPath(pattern);
                }

                foreach (var file in files)
                {
                    if (Program.Debug) Console.WriteLine($"Trying to load custom functions from: {file}");
                    var assembly = TryCatchHelpers.TryCatchNoThrow<Assembly>(() => Assembly.LoadFrom(file), null, out var ex);
                    if (assembly != null) factory.AddFunctions(assembly);
                }
            }

            return factory;
        }

        private HelperFunctionFactory CreateFunctionFactoryWithBuiltinFunctions()
        {
            return new HelperFunctionFactory(typeof(FileHelperFunctions).Assembly);
        }

        private HelperFunctionFactory CreateFunctionFactoryWithNoFunctions()
        {
            return new HelperFunctionFactory();
        }

        private void CheckChoiceFinishReason(ChatFinishReason? reason)
        {
            if (reason == null) return;

            if (reason == ChatFinishReason.ContentFilter)
            {
                if (!_quiet) ConsoleHelpers.WriteLineWithHighlight("#e_;WARNING: Content filtered!");
            }
            if (reason == ChatFinishReason.Length)
            {
                _values.AddThrowError("ERROR:", $"exceeded token limit!",
                                        "TRY:", $"{Program.Name} chat --max-tokens TOKENS");
            }
        }

        private ChatMessageRole UpdateRole(ref string line, ChatMessageRole? currentRole = null)
        {
            var lower = line.ToLower();
            if (lower.StartsWith("system:"))
            {
                line = line.Substring(7).Trim();
                return ChatMessageRole.System;
            }
            else if (lower.StartsWith("user:"))
            {
                line = line.Substring(5).Trim();
                return ChatMessageRole.User;
            }
            else if (lower.StartsWith("assistant:"))
            {
                line = line.Substring(10).Trim();
                return ChatMessageRole.Assistant;
            }
            return currentRole ?? ChatMessageRole.System;
        }

        private void SetValuesFromParameterFile(string parameterFile)
        {
            var existing = FileHelpers.DemandFindFileInDataPath(parameterFile, _values, "chat parameter");
            var text = FileHelpers.ReadAllText(existing, Encoding.Default);
            string[] sections = text.Split("---\n");
            if (sections.Length < 2)
            {
                sections = text.Split("---\r\n");
            }
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            if (sections.Length > 2)
            {
                var promptyFrontMatterText = sections[1];
                var chatTemplate = sections[2];
                var obj = deserializer.Deserialize<PromptyFrontMatter>(promptyFrontMatterText);
                Dictionary<string, object> templateInputs = ParsePromptySample(obj.Sample ?? "sample.json");
                string parsedText;
                switch (obj.Template.ToLower())
                {
                    // default assumption for templating language is jinja2
                    case "jinja2":
                    case "":
                        parsedText = ParseJinja(chatTemplate, templateInputs);
                        break;
                    default:
                        parsedText = "";
                        _values.AddThrowError("ERROR:", $"parsing {parameterFile}; {obj.Template} parser not implemented.");
                        break;
                }
                if (!String.IsNullOrEmpty(parsedText))
                {
                    // set values

                    if (!string.IsNullOrEmpty(obj.Model.AzureDeployment)) ConfigDeploymentToken.Data().Set(_values, obj.Model.AzureDeployment);
                    if (!string.IsNullOrEmpty(obj.Model.AzureEndpoint)) ConfigEndpointUriToken.Data().Set(_values, obj.Model.AzureEndpoint);
                    string systemPrompt = "";
                    string[] separators = { "\n", "\r\n"};
                    var textlines = parsedText.Split(separators, StringSplitOptions.None);
                    if (textlines[0].ToLower().Equals("system:"))
                    {
                        parsedText = "";
                        var systemPromptEnded = false;
                        foreach (var line in textlines[1..])
                        {
                            if (string.IsNullOrEmpty(line)) systemPromptEnded = true;
                            if (systemPromptEnded)
                            {
                                parsedText += $"{line}\n";
                            }
                            else
                            {
                                systemPrompt += $"{line}\n";
                            }
                        };
                        if (!string.IsNullOrEmpty(systemPrompt)) _values.Reset("chat.message.system.prompt", systemPrompt);
                    }
                    if (!string.IsNullOrEmpty(parsedText)) _values.Reset("chat.message.user.prompt", parsedText);

                    if (obj.Parameters != null)
                    {
                        var modelParams = obj.Parameters;
                        if (modelParams.Temperature != 0) _values.Reset("chat.options.temperature", modelParams.Temperature.ToString());
                        if (modelParams.MaxTokens != 0) _values.Reset("chat.options.max.tokens", modelParams.MaxTokens.ToString());
                        if (modelParams.FrequencyPenalty != 0) _values.Reset("chat.options.frequency.penalty", modelParams.FrequencyPenalty.ToString());
                        if (modelParams.PresencePenalty != 0) _values.Reset("chat.options.presence.penalty", modelParams.PresencePenalty.ToString());
                        if (modelParams.Stop != null) _values.Reset("chat.options.stop.sequence", modelParams.Stop.ToString());
                    }
                }
            }
            else
            { 
                _values.AddThrowError("ERROR:", $"parsing {parameterFile}; unable to parse, incorrect yaml format.");
            }
        }

        private string ParseJinja(string chatTemplate, Dictionary<string, object> inputs)
        {
            var template = Template.Parse(chatTemplate);
            return template.Render(inputs);
        }

        private Dictionary<string, object> ParsePromptySample(object sample)
        {
            var dictionary = new Dictionary<string, object>();
            var inputString = sample as string;
            if (inputString == null)
            {
                var inputIter = sample as IDictionary<object, object>;

                foreach (var kv in inputIter)
                {
                    dictionary.Add(kv.Key.ToString(), kv.Value);
                }
            }
            else
            {
                var existing = FileHelpers.DemandFindFileInDataPath(inputString, _values, "chat parameter");
                var text = FileHelpers.ReadAllText(existing, Encoding.Default);
                if (inputString.EndsWith(".json"))
                {
                    dictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(text);
                }
                else
                {
                    dictionary = text
                        .Split(Environment.NewLine)
                        .Select(part => part.Split(':'))
                        .Where(part => part.Length == 2)
                        .ToDictionary(sp => sp[0], sp => sp[1] as object);
                }
            }
            return dictionary;
        }

        private void AddChatMessagesFromTextFile(IList<ChatMessage> messages, string textFile)
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

                if (role == ChatMessageRole.System || role == ChatMessageRole.User)
                {
                    line = line.ReplaceValues(_values);
                }

                if (i == 0 && role == ChatMessageRole.System && FirstMessageIsDefaultSystemPrompt(messages, role))
                {
                    messages[0] = new SystemChatMessage(line);
                    continue;
                }

                messages.Add(role == ChatMessageRole.System
                    ? new SystemChatMessage(line)
                    : role == ChatMessageRole.User
                        ? new UserChatMessage(line)
                        : new AssistantChatMessage(line));
            }
        }

        private static bool FirstMessageIsDefaultSystemPrompt(IList<ChatMessage> messages, ChatMessageRole role)
        {
            var message = messages.FirstOrDefault() as SystemChatMessage;
            return message != null && message.Content.FirstOrDefault()?.Text == DefaultSystemPrompt;
        }

        private static string ConvertMessagesToJson(IList<ChatMessage> messages)
        {
            var sb = new StringBuilder();
            sb.Append("[");
            foreach (var message in messages)
            {
                var user = message as UserChatMessage;
                var system = message as SystemChatMessage;
                var assistant = message as AssistantChatMessage;
                var role = system != null ? "system"
                    : assistant != null ? "assistant"
                    : user != null ? "user"
                    : null;

                var contentParts = system?.Content ?? user?.Content ?? assistant?.Content;
                var content = contentParts.ToText().Replace("\\", "\u005C").Replace("\"", "");

                var ok = !string.IsNullOrEmpty(content);
                if (!ok) continue;

                if (sb.Length > 1) sb.Append(",");

                sb.Append($"{{\"role\": \"{role}\", \"content\": \"{content}\"}}");
            }
            sb.Append("]");
            var theDict = $"{{ \"messages\": {sb.ToString()} }}";

            if (Program.Debug) Console.WriteLine(theDict);
            return theDict;
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

        private async Task<bool> DoChatAssistantCreate()
        {
            var name = _values["chat.assistant.create.name"];
            var deployment = ConfigDeploymentToken.Data().GetOrDefault(_values);
            var instructions = InstructionsToken.Data().GetOrDefault(_values);

            if (string.IsNullOrEmpty(name))
            {
                _values.AddThrowError("ERROR:", $"Creating assistant; requires name.");
            }
            else if (string.IsNullOrEmpty(deployment))
            {
                _values.AddThrowError("ERROR:", $"Creating assistant; requires deployment.");
            }
            else if (string.IsNullOrEmpty(instructions))
            {
                _values.AddThrowError("ERROR:", $"Creating assistant; requires instructions.");
            }

            var message = $"Creating assistant ({name}) ...";
            if (!_quiet) Console.WriteLine(message);

            var codeInterpreter = CodeInterpreterToken.Data().GetOrDefault(_values, false);
            var fileIds = FileIdOptionXToken.GetOptions(_values).ToList();
            fileIds.AddRange(FileIdsOptionXToken.GetOptions(_values));

            DemandKeyAndEndpoint(out var key, out var endpoint);
            string assistantId = await OpenAIAssistantHelpers.CreateAssistant(key, endpoint, name, deployment, instructions, codeInterpreter, fileIds);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var fi = new FileInfo(ConfigSetHelpers.ConfigSet("assistant.id", assistantId));
            if (!_quiet) Console.WriteLine($"{fi.Name} (saved at {fi.DirectoryName})\n\n  {assistantId}");

            return true;
        }

        private async Task<bool> DoChatAssistantDelete()
        {
            var id = _values["chat.assistant.id"];
            if (string.IsNullOrEmpty(id))
            {
                _values.AddThrowError("ERROR:", $"Deleting assistant; requires id.");
            }

            var message = $"Deleting assistant ({id}) ...";
            if (!_quiet) Console.WriteLine(message);

            DemandKeyAndEndpoint(out var key, out var endpoint);
            await OpenAIAssistantHelpers.DeleteAssistant(key, endpoint, id);

            if (!_quiet) Console.WriteLine($"{message} Done!");
            return true;
        }

        private async Task<bool> DoChatAssistantGet()
        {
            var id = _values["chat.assistant.id"];
            if (string.IsNullOrEmpty(id))
            {
                _values.AddThrowError("ERROR:", $"Deleting assistant; requires id.");
            }

            var message = $"Getting assistant ({id}) ...";
            if (!_quiet) Console.WriteLine(message);

            DemandKeyAndEndpoint(out var key, out var endpoint);
            var json = await OpenAIAssistantHelpers.GetAssistantJson(key, endpoint, id);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var ok = !string.IsNullOrEmpty(json);
            if (ok) Console.WriteLine(json);

            return true;
        }

        private async Task<bool> DoChatAssistantList()
        {
            var message = $"Listing assistants ...";
            if (!_quiet) Console.WriteLine(message);

            DemandKeyAndEndpoint(out var key, out var endpoint);
            var assistants = await OpenAIAssistantHelpers.ListAssistants(key, endpoint);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            if (assistants.Count() == 0)
            {
                Console.WriteLine("No assistants found.");
            }
            else
            {
                Console.WriteLine("Assistants:\n");
                foreach (var assistant in assistants)
                {
                    var assistantId = assistant.Key;
                    var assistantName = assistant.Value;
                    if (string.IsNullOrEmpty(assistantName)) assistantName = "(no name)";
                    Console.WriteLine($"  {assistantName} ({assistantId})");
                }
            }

            return true;
        }

        private async Task<bool> DoChatAssistantFileUpload()
        {
            var file = _values["assistant.upload.file"];
            if (string.IsNullOrEmpty(file))
            {
                _values.AddThrowError("ERROR:", $"Uploading assistant file; requires file.");
            }

            var existing = FileHelpers.DemandFindFileInDataPath(file, _values, "assistant file");

            var message = $"Uploading assistant file ({file}) ...";
            if (!_quiet) Console.WriteLine(message);

            DemandKeyAndEndpoint(out var key, out var endpoint);
            var (uploadedId, uploadedName) = await OpenAIAssistantHelpers.UploadAssistantFile(key, endpoint, existing);

            if (!_quiet) Console.WriteLine($"{message} Done!\n\n  {uploadedName} ({uploadedId})\n");

            return true;
        }

        private async Task<bool> DoChatAssistantFileList()
        {
            var message = $"Listing assistant files ...";
            if (!_quiet) Console.WriteLine(message);

            DemandKeyAndEndpoint(out var key, out var endpoint);
            var files = await OpenAIAssistantHelpers.ListAssistantFiles(key, endpoint);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            if (files.Count() == 0)
            {
                Console.WriteLine("No files found.");
            }
            else
            {
                Console.WriteLine("Assistant files:\n");
                foreach (var file in files)
                {
                    var fileId = file.Key;
                    var fileName = file.Value;
                    if (string.IsNullOrEmpty(fileName)) fileName = "(no name)";
                    Console.WriteLine($"  {fileName} ({fileId})");
                }
            }

            return true;
        }

        private async Task<bool> DoChatAssistantFileDelete()
        {
            var id = _values["chat.assistant.file.id"];
            if (string.IsNullOrEmpty(id))
            {
                _values.AddThrowError("ERROR:", $"Deleting assistant file; requires file id.");
            }

            var message = $"Deleting assistant file ({id}) ...";
            if (!_quiet) Console.WriteLine(message);

            DemandKeyAndEndpoint(out var key, out var endpoint);
            await OpenAIAssistantHelpers.DeleteAssistantFile(key, endpoint, id);

            if (!_quiet) Console.WriteLine($"{message} Done!");
            return true;
        }

        private void DemandKeyAndEndpoint(out string key, out string endpoint)
        {
            key = _values["service.config.key"];
            endpoint = ConfigEndpointUriToken.Data().GetOrDefault(_values);
            if (string.IsNullOrEmpty(endpoint))
            {
                _values.AddThrowError("ERROR:", $"Creating AssistantsClient; requires endpoint.");
            }
        }

        private void StartCommand()
        {
            CheckPath();
            // CheckChatInput();
            LogHelpers.EnsureStartLogFile(_values);

            // _display = new DisplayHelper(_values);

            // _output = new OutputHelper(_values);
            // _output!.StartOutput();

            // var id = _values["chat.input.id"];
            // _output!.EnsureOutputAll("chat.input.id", id);
            // _output!.EnsureOutputEach("chat.input.id", id);

            _lock = new SpinLock();
            _lock.StartLock();
        }

        private void StopCommand()
        {
            _lock!.StopLock(5000);

            LogHelpers.EnsureStopLogFile(_values);
            // _output!.CheckOutput();
            // _output!.StopOutput();

            _stopEvent.Set();
        }

        private SpinLock? _lock = null;
        private readonly bool _quiet = false;
        private readonly bool _verbose = false;

        private static AzureEventSourceListener _azureEventSourceListener;

        // OutputHelper? _output = null;
        // DisplayHelper? _display = null;

#nullable enable
        private int? TryParse(string? s, int? defaultValue)
        {
            return !string.IsNullOrEmpty(s) && int.TryParse(s, out var parsed) ? parsed : defaultValue;
        }

        private float TryParse(string? s, float defaultValue)
        {
            return !string.IsNullOrEmpty(s) && float.TryParse(s, out var parsed) ? parsed : defaultValue;
        }

        public const string DefaultSystemPrompt = "You are an AI assistant that helps people find information regarding Azure AI.";

        private const float _defaultTemperature = 0.7f;
        private const float _defaultFrequencyPenalty = 0.0f;
        private const float _defaultPresencePenalty = 0.0f;
    }
}
