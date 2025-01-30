//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Azure.AI.Details.Common.CLI.ConsoleGui;
using Azure.AI.Details.Common.CLI.Extensions.HelperFunctions;
using Azure.AI.Details.Common.CLI.Extensions.Inference;
using Azure.AI.Details.Common.CLI.Extensions.ONNX;
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
using OpenAI.Files;
using OpenAI.VectorStores;
using System.ClientModel.Primitives;
using static Azure.AI.Details.Common.CLI.ConsoleGui.Window;

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
                    if (!msg.StartsWith("ERROR: "))
                    {
                        FileHelpers.LogException(_values, ex);
                        ConsoleHelpers.WriteLineError($"\n  ERROR: {msg}");
                        return true;
                    }

                    throw ex;
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
                case "chat.assistant.update": DoChatAssistantUpdate().Wait(); break;
                case "chat.assistant.delete": DoChatAssistantDelete().Wait(); break;
                case "chat.assistant.get": DoChatAssistantGet().Wait(); break;
                case "chat.assistant.list": DoChatAssistantList().Wait(); break;

                case "chat.assistant.vector-store": HelpCommandParser.DisplayHelp(_values); break;
                case "chat.assistant.vector-store.create": DoChatAssistantVectorStoreCreate().Wait(); break;
                case "chat.assistant.vector-store.update": DoChatAssistantVectorStoreUpdate().Wait(); break;
                case "chat.assistant.vector-store.delete": DoChatAssistantVectorStoreDelete().Wait(); break;
                case "chat.assistant.vector-store.get": DoChatAssistantVectorStoreGet().Wait(); break;
                case "chat.assistant.vector-store.list": DoChatAssistantVectorStoreList().Wait(); break;

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

            CheckOutputThreadId();
        }

        private async Task ChatInteractively()
        {
            if (!_quiet) Console.WriteLine("Press ENTER for more options.\n");

            var chatTextHandler = await GetChatTextHandlerAsync(interactive: true);

            var speechInput = _values.GetOrDefault("chat.speech.input", false);
            var userPrompt = _values["chat.message.user.prompt"];

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

        private void CheckOutputThreadId()
        {
            var threadId = _values.GetOrDefault("chat.thread.id", null);
            if (!string.IsNullOrEmpty(threadId))
            {
                IdHelpers.CheckWriteOutputNameOrId(threadId, _values, "chat.assistant.thread", IdKind.Id);
                if (!_quiet)
                {
                    DisplayAssistantPromptLabel();
                    Console.WriteLine($"Bye!\n\n(ThreadId: {threadId})");
                }
            }
        }

        private async Task<Func<string, Task>> GetChatTextHandlerAsync(bool interactive)
        {
            var parameterFile = InputChatParameterFileToken.Data().GetOrDefault(_values);
            if (!string.IsNullOrEmpty(parameterFile)) SetValuesFromParameterFile(parameterFile);

            var endpointType = ConfigEndpointTypeToken.Data().GetOrDefault(_values);
            var inferenceEndpointOk = endpointType == "inference";
            if (inferenceEndpointOk) return GetInferenceChatTextHandler(interactive);

            var modelPath = ChatModelPathToken.Data().GetOrDefault(_values);
            var modelPathOk = !string.IsNullOrEmpty(modelPath);
            if (modelPathOk) return GetONNXGenAIChatCompletionsTextHandler(interactive, modelPath);

            var assistantId = _values["chat.assistant.id"];
            var assistantIdOk = !string.IsNullOrEmpty(assistantId);
            if (assistantIdOk) return await GetAssistantsAPITextHandlerAsync(interactive, assistantId);

            return GetChatCompletionsTextHandler(interactive);
        }

        private async Task<Func<string, Task>> GetAssistantsAPITextHandlerAsync(bool interactive, string assistantId)
        {
            var threadId = _values.GetOrDefault("chat.thread.id", null);

            var client = CreateAssistantClient();
            var thread = await CreateOrGetAssistantThread(client, threadId);

            _ = CheckWriteChatHistoryOutputFileAsync(fileName => thread.SaveChatHistoryToFileAsync(client, fileName));

            threadId = thread.Id;
            _values.Reset("chat.thread.id", threadId);

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

        private Func<string, Task> GetInferenceChatTextHandler(bool interactive)
        {
            var aiChatEndpoint = _values["service.config.endpoint.uri"];
            var aiChatAPIKey = _values["service.config.key"];

            var systemPrompt = _values.GetOrDefault("chat.message.system.prompt", DefaultSystemPrompt);
            var chatHistoryJsonFile = InputChatHistoryJsonFileToken.Data().GetOrDefault(_values);
            var aiChatModel = ChatModelNameToken.Data().GetOrDefault(_values);
            var chat = new AzureAIInferenceChatCompletionsStreaming(aiChatEndpoint, aiChatAPIKey, aiChatModel, systemPrompt, chatHistoryJsonFile);

            return async (string text) =>
            {
                if (interactive && text.ToLower() == "reset")
                {
                    chat.ClearConversation();
                    return;
                }

                await GetInferenceChatTextHandlerAsync(chat, text);
            };
        }

        private Func<string, Task> GetONNXGenAIChatCompletionsTextHandler(bool interactive, string modelPath)
        {
            var systemPrompt = _values.GetOrDefault("chat.message.system.prompt", DefaultSystemPrompt);
            var chatHistoryJsonFile = InputChatHistoryJsonFileToken.Data().GetOrDefault(_values);
            var chat = new OnnxGenAIChatCompletionsStreamingClass(modelPath, systemPrompt, chatHistoryJsonFile);

            return async (string text) =>
            {
                if (interactive && text.ToLower() == "reset")
                {
                    chat.ClearConversation();
                    return;
                }

                await GetONNXGenAIChatTextHandlerAsync(chat, text);
            };
        }

        private Func<string, Task> GetChatCompletionsTextHandler(bool interactive)
        {
            var client = CreateOpenAIClient(out var deployment);
            var chatClient = client.GetChatClient(deployment);

            var options = CreateChatCompletionOptions(out var messages);
            CheckWriteChatHistoryOutputFile(fileName => messages.SaveChatHistoryToFile(fileName));

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
                Console.Write("\rassistant");
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

            Console.Write(text);
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
            await assistantClient.CreateMessageAsync(thread, MessageRole.User, [ userInput ]);
            _ = CheckWriteChatHistoryOutputFileAsync(fileName => thread.SaveChatHistoryToFileAsync(assistantClient, fileName));

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
                    else if (update is RequiredActionUpdate requiredActionUpdate)
                    {
                        if (factory.TryCallFunction(requiredActionUpdate.FunctionName, requiredActionUpdate.FunctionArguments, out var result))
                        {
                            DisplayAssistantFunctionCall(requiredActionUpdate.FunctionName, requiredActionUpdate.FunctionArguments, result);
                            DisplayAssistantPromptLabel();
                            toolOutputs.Add(new ToolOutput(requiredActionUpdate.ToolCallId, result));
                        }
                    }

                    if (update is RunUpdate runUpdate)
                    {
                        run = runUpdate;
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

            await CheckWriteChatHistoryOutputFileAsync(fileName => thread.SaveChatHistoryToFileAsync(assistantClient, fileName));
        }

        private async Task GetInferenceChatTextHandlerAsync(AzureAIInferenceChatCompletionsStreaming chat, string text)
        {
            CheckWriteChatHistoryOutputFile(fileName => chat.Messages.SaveChatHistoryToFile(fileName));
            DisplayAssistantPromptLabel();

            var response = await chat.GetChatCompletionsStreamingAsync(text, update =>
            {
                var content = update.ContentUpdate;
                DisplayAssistantPromptTextStreaming(content);
            });

            DisplayAssistantPromptTextStreamingDone();
            CheckWriteChatAnswerOutputFile(response);
            CheckWriteChatHistoryOutputFile(fileName => chat.Messages.SaveChatHistoryToFile(fileName));
        }

        private Task GetONNXGenAIChatTextHandlerAsync(OnnxGenAIChatCompletionsStreamingClass chat, string text)
        {
            CheckWriteChatHistoryOutputFile(fileName => chat.Messages.SaveChatHistoryToFile(fileName));
            DisplayAssistantPromptLabel();

            var response = chat.GetChatCompletionStreaming(text, update =>
            {
                DisplayAssistantPromptTextStreaming(update);
            });

            DisplayAssistantPromptTextStreamingDone();
            CheckWriteChatAnswerOutputFile(response);
            CheckWriteChatHistoryOutputFile(fileName => chat.Messages.SaveChatHistoryToFile(fileName));

            return Task.CompletedTask;
        }

        private async Task<string> GetChatCompletionsAsync(ChatClient client, List<ChatMessage> messages, ChatCompletionOptions options, HelperFunctionCallContext functionCallContext, string text)
        {
            var requestMessage = new UserChatMessage(text);
            messages.Add(requestMessage);
            CheckWriteChatHistoryOutputFile(fileName => messages.SaveChatHistoryToFile(fileName));

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
                    CheckWriteChatHistoryOutputFile(fileName => messages.SaveChatHistoryToFile(fileName));
                    continue;
                }

                DisplayAssistantPromptTextStreamingDone();
                CheckWriteChatAnswerOutputFile(contentComplete);

                var currentContent = new AssistantChatMessage(contentComplete);
                messages.Add(currentContent);
                
                CheckWriteChatHistoryOutputFile(fileName => messages.SaveChatHistoryToFile(fileName));

                return contentComplete;
            }
        }

        private void CheckWriteChatAnswerOutputFile(string completeResponse)
        {
            if (!string.IsNullOrEmpty(_outputAnswerFile))
            {
                FileHelpers.WriteAllText(_outputAnswerFile, completeResponse, Encoding.UTF8);
            }

            if (!string.IsNullOrEmpty(_outputAddAnswerFile))
            {
                FileHelpers.AppendAllText(_outputAddAnswerFile, "\n" + completeResponse, Encoding.UTF8);
            }
        }

        private async Task CheckWriteChatHistoryOutputFileAsync(Func<string, Task> saveChatHistoryToFile)
        {
            if (!string.IsNullOrEmpty(_outputChatHistoryFileName))
            {
                await saveChatHistoryToFile(_outputChatHistoryFileName);
            }
        }

        private void CheckWriteChatHistoryOutputFile(Action<string> saveChatHistoryToFile)
        {
            if (!string.IsNullOrEmpty(_outputChatHistoryFileName))
            {
                saveChatHistoryToFile(_outputChatHistoryFileName);
            }
        }

        private void ClearMessageHistory(List<ChatMessage> messages)
        {
            messages.RemoveRange(1, messages.Count - 1);
            CheckWriteChatHistoryOutputFile(fileName => messages.SaveChatHistoryToFile(fileName));

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
                AzureOpenAIClientOptions options = new();
                options.AddPolicy(new LogTrafficEventPolicy(), PipelinePosition.PerCall);

                return new AzureOpenAIClient(
                    new Uri(endpoint!),
                    new AzureKeyCredential(key!),
                    options);
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

            var options = new MessageCollectionOptions() { Order = ListOrder.OldestFirst };
            await foreach (var message in client.GetMessagesAsync(thread, options).GetAllValuesAsync())
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
            var name = _values["chat.assistant.name"];
            var deployment = ConfigDeploymentToken.Data().GetOrDefault(_values);
            var instructions = InstructionsToken.Data().GetOrDefault(_values, "You are a helpful assistant.");

            if (string.IsNullOrEmpty(name))
            {
                _values.AddThrowError("ERROR:", $"Creating assistant; requires name.");
            }
            else if (string.IsNullOrEmpty(deployment))
            {
                _values.AddThrowError("ERROR:", $"Creating assistant; requires deployment.");
            }

            var message = $"Creating assistant ({name}) ...";
            if (!_quiet) Console.WriteLine(message);

            DemandKeyAndEndpoint(out var key, out var endpoint);

            var codeInterpreter = CodeInterpreterTrueFalseToken.Data().GetOrDefault(_values, false);
            var assistant = await OpenAIAssistantHelpers.CreateAssistantAsync(key, endpoint, name, deployment, instructions, codeInterpreter);
            IdHelpers.CheckWriteOutputNameOrId(assistant.Id, _values, "chat", IdKind.Id);
            IdHelpers.CheckWriteOutputNameOrId(assistant.Name, _values, "chat", IdKind.Name);

            var fileIds = FileIdOptionXToken.GetOptions(_values).ToList();
            fileIds.AddRange(FileIdsOptionXToken.GetOptions(_values));
            fileIds.ExtendSplitItems(';');

            var files = FileOptionXToken.GetOptions(_values).ToList();
            files.AddRange(FilesOptionXToken.GetOptions(_values));
            files.ExtendSplitItems(';');

            files = ExpandFindFiles(files);

            var fileSearch = FileSearchTrueFalseToken.Data().GetOrDefault(_values, false);
            if (fileSearch || fileIds.Count() > 0 || files.Count() > 0)
            {
                if (!_quiet) Console.WriteLine("\n  Creating vector store ...");

                var store = await CreateAssistantVectorStoreAsync(key, endpoint, name, fileIds, files);
                var modifyOptions = new AssistantModificationOptions()
                {
                    ToolResources = ToolResourcesFromVectorStoreId(store.Id)
                };
                modifyOptions.DefaultTools.Add(new FileSearchToolDefinition());

                var assistantClient = OpenAIAssistantHelpers.CreateOpenAIAssistantClient(key, endpoint);
                var modified = await assistantClient.ModifyAssistantAsync(assistant, modifyOptions);
                assistant = modified.Value;

                Console.WriteLine();
            }

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            SetAssistantIdConfig(assistant);
            return true;
        }

        private async Task<bool> DoChatAssistantUpdate()
        {
            var id = _values["chat.assistant.id"];
            if (string.IsNullOrEmpty(id))
            {
                _values.AddThrowError(
                      "ERROR:", $"Updating assistant; requires id.",
                                "",
                        "TRY:", $"{Program.Name} chat assistant update --id ID",
                                "",
                        "SEE:", $"{Program.Name} help chat assistant update");
            }

            var name = _values["chat.assistant.name"];
            var nameToDisplay = string.IsNullOrEmpty(name) ? id : name;

            var message = $"Updating assistant ({nameToDisplay}) ...";
            if (!_quiet) Console.WriteLine(message);

            DemandKeyAndEndpoint(out var key, out var endpoint);
            var assistantClient = OpenAIAssistantHelpers.CreateOpenAIAssistantClient(key, endpoint);
            var response = await assistantClient.GetAssistantAsync(id);
            var assistant = response.Value;

            var deployment = ConfigDeploymentToken.Data().GetOrDefault(_values, assistant.Model);
            var instructions = InstructionsToken.Data().GetOrDefault(_values, assistant.Instructions);

            var existingCodeInterpreter = assistant.Tools.FirstOrDefault(t => t is CodeInterpreterToolDefinition) != null;
            var codeInterpreterSpecifiedTrue = CodeInterpreterTrueFalseToken.Data().GetOrDefault(_values, false) == true;
            var codeInterpreterSpecifiedFalse = CodeInterpreterTrueFalseToken.Data().GetOrDefault(_values, true) == false;

            var vectorStoreId = assistant.ToolResources.FileSearch?.VectorStoreIds?.FirstOrDefault();
            var existingVectorStore = !string.IsNullOrEmpty(vectorStoreId);
            var fileSearchSpecifiedFalse = FileSearchTrueFalseToken.Data().GetOrDefault(_values, true) == false;
            var fileSearchSpecifiedTrue = FileSearchTrueFalseToken.Data().GetOrDefault(_values, false) == true;

            var fileIds = FileIdOptionXToken.GetOptions(_values).ToList();
            fileIds.AddRange(FileIdsOptionXToken.GetOptions(_values));
            fileIds.ExtendSplitItems(';');

            var files = FileOptionXToken.GetOptions(_values).ToList();
            files.AddRange(FilesOptionXToken.GetOptions(_values));
            files.ExtendSplitItems(';');
            files = ExpandFindFiles(files);

            var newFilesForVectorStore = fileIds.Count() > 0 || files.Count() > 0;
            var createVectorStore = !existingVectorStore && (newFilesForVectorStore || fileSearchSpecifiedTrue);
            var updateVectorStore = existingVectorStore && newFilesForVectorStore;
            var removeVectorStore = existingVectorStore && fileSearchSpecifiedFalse;

            if (createVectorStore)
            {
                if (!_quiet) Console.WriteLine("\n  Creating vector store ...");

                var store = await CreateAssistantVectorStoreAsync(key, endpoint, name, fileIds, files);
                vectorStoreId = store.Id;
            }
            else if (updateVectorStore)
            {
                if (!_quiet) Console.WriteLine("\n  Updating vector store ...");

                var store = await OpenAIAssistantHelpers.GetVectorStoreAsync(key, endpoint, vectorStoreId);
                store = await UploadFilesToAssistantVectorStore(key, endpoint, fileIds, files, store);
                vectorStoreId = store.Id;
            }
            else if (removeVectorStore)
            {
                Console.WriteLine();
                _values.AddThrowError("ERROR:", $"Removing vector store; not yet implemented.");
            }

            var modifyOptions = new AssistantModificationOptions()
            {
                Name = name ?? assistant.Name,
                Model = deployment ?? assistant.Model,
                Instructions = instructions ?? assistant.Instructions,
                ToolResources = ToolResourcesFromVectorStoreId(vectorStoreId),
            };

            var removeCodeInterpreter = existingCodeInterpreter && codeInterpreterSpecifiedFalse;
            if (removeCodeInterpreter)
            {
                Console.WriteLine();
                _values.AddThrowError("ERROR:", $"Removing code interpreter; not yet implemented.");
            }
            else if (existingCodeInterpreter)
            {
                modifyOptions.DefaultTools.Add(new CodeInterpreterToolDefinition());
            }

            if (existingVectorStore || newFilesForVectorStore)
            {
                modifyOptions.DefaultTools.Add(new FileSearchToolDefinition());
            }

            var modified = await assistantClient.ModifyAssistantAsync(assistant, modifyOptions);
            assistant = modified.Value;

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            SetAssistantIdConfig(assistant);
            return true;
        }

        private async Task<bool> DoChatAssistantDelete()
        {
            var id = _values["chat.assistant.id"];
            if (string.IsNullOrEmpty(id))
            {
                _values.AddThrowError(
                      "ERROR:", $"Deleting assistant; requires id.",
                                "",
                        "TRY:", $"{Program.Name} chat assistant delete --id ID",
                                "",
                        "SEE:", $"{Program.Name} help chat assistant delete");
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
                _values.AddThrowError(
                      "ERROR:", $"Getting assistant; requires id.",
                                "",
                        "TRY:", $"{Program.Name} chat assistant get --id ID",
                                "",
                        "SEE:", $"{Program.Name} help chat assistant get");
            }

            var message = $"Getting assistant ({id}) ...";
            if (!_quiet) Console.WriteLine(message);

            DemandKeyAndEndpoint(out var key, out var endpoint);

            var assistant = await OpenAIAssistantHelpers.GetAssistantAsync(key, endpoint, id);
            PrintAssistant(assistant);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            return true;
        }

        private async Task<bool> DoChatAssistantList()
        {
            var message = $"Listing assistants ...";
            if (!_quiet) Console.WriteLine(message);

            DemandKeyAndEndpoint(out var key, out var endpoint);
            var assistants = await OpenAIAssistantHelpers.ListAssistantsAsync(key, endpoint);
            IdHelpers.CheckWriteOutputNamesOrIds(assistants, _values, "chat", IdKind.Id, (a) => a.Key);
            IdHelpers.CheckWriteOutputNamesOrIds(assistants, _values, "chat", IdKind.Name, (a) => a.Value);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            if (assistants.Count() == 0)
            {
                if (!_quiet) Console.WriteLine("No assistants found.");
            }
            else
            {
                if (!_quiet) Console.WriteLine("Assistants:\n");
                foreach (var assistant in assistants)
                {
                    var assistantId = assistant.Key;
                    var assistantName = assistant.Value;
                    if (string.IsNullOrEmpty(assistantName)) assistantName = "(no name)";
                    if (!_quiet)
                    {
                        Console.WriteLine($"  {assistantName} ({assistantId})");
                    }
                    else
                    {
                        Console.WriteLine(assistantId);
                    }
                }
            }

            return true;
        }

        private async Task<bool> DoChatAssistantVectorStoreCreate()
        {
            DemandKeyAndEndpoint(out var key, out var endpoint);

            var name = _values["chat.assistant.vector.store.name"];
            var nameToDisplay = string.IsNullOrEmpty(name) ? "no name" : name;

            var message = $"Creating assistant vector store ({nameToDisplay}) ...";
            if (!_quiet) Console.WriteLine(message);

            var fileIds = FileIdOptionXToken.GetOptions(_values).ToList();
            fileIds.AddRange(FileIdsOptionXToken.GetOptions(_values));
            fileIds.ExtendSplitItems(';');

            var files = FileOptionXToken.GetOptions(_values).ToList();
            files.AddRange(FilesOptionXToken.GetOptions(_values));
            files.ExtendSplitItems(';');

            files = ExpandFindFiles(files);

            var store = await CreateAssistantVectorStoreAsync(key, endpoint, name, fileIds, files);
            IdHelpers.CheckWriteOutputNameOrId(store.Id, _values, "chat", IdKind.Id);
            IdHelpers.CheckWriteOutputNameOrId(store.Name, _values, "chat", IdKind.Name);

            PrintVectorStore(key, endpoint, store);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ConfigSetHelpers.ConfigSet("vector.store.id", store.Id, print: !_quiet);
            return true;
        }

        private async Task<bool> DoChatAssistantVectorStoreUpdate()
        {
            DemandKeyAndEndpoint(out var key, out var endpoint);

            var id = _values["chat.assistant.vector.store.id"];
            if (string.IsNullOrEmpty(id))
            {
                _values.AddThrowError(
                    "ERROR:", $"Updating assistant vector store; requires id.",
                                "",
                        "TRY:", $"{Program.Name} chat assistant vector-store update --id ID",
                                "",
                        "SEE:", $"{Program.Name} help chat assistant vector-store update");
            }

            var name = _values["chat.assistant.vector.store.name"];
            var nameToDisplay = string.IsNullOrEmpty(name) ? id : name;

            var message = $"Updating assistant vector store ({nameToDisplay}) ...";
            if (!_quiet) Console.WriteLine(message);

            var fileIds = FileIdOptionXToken.GetOptions(_values).ToList();
            fileIds.AddRange(FileIdsOptionXToken.GetOptions(_values));
            fileIds.ExtendSplitItems(';');

            var files = FileOptionXToken.GetOptions(_values).ToList();
            files.AddRange(FilesOptionXToken.GetOptions(_values));
            files.ExtendSplitItems(';');

            files = ExpandFindFiles(files);

            var store = await UpdateAssistantVectorStoreAsync(key, endpoint, id, name, fileIds, files);
            PrintVectorStore(key, endpoint, store);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");
            return true;
        }

        private async Task<bool> DoChatAssistantVectorStoreDelete()
        {
            var id = _values["chat.assistant.vector.store.id"];
            if (string.IsNullOrEmpty(id))
            {
                _values.AddThrowError(
                      "ERROR:", $"Deleting assistant vector store; requires id.",
                                "",
                        "TRY:", $"{Program.Name} chat assistant vector-store delete --id ID",
                                "",
                        "SEE:", $"{Program.Name} help chat assistant vector-store delete");
            }

            var message = $"Deleting assistant vector store ...";
            if (!_quiet) Console.WriteLine(message);

            DemandKeyAndEndpoint(out var key, out var endpoint);
            await OpenAIAssistantHelpers.DeleteAssistantVectorStoreAsync(key, endpoint, id);

            if (!_quiet) Console.WriteLine($"{message} Done!");
            return true;
        }

        private async Task<bool> DoChatAssistantVectorStoreGet()
        {
            var id = _values["chat.assistant.vector.store.id"];
            if (string.IsNullOrEmpty(id))
            {
                _values.AddThrowError(
                      "ERROR:", $"Getting assistant vector store; requires id.",
                                "",
                        "TRY:", $"{Program.Name} chat assistant vector-store get --id ID",
                                "",
                        "SEE:", $"{Program.Name} help chat assistant vector-store get");
            }

            var message = $"Getting assistant vector store ...";
            if (!_quiet) Console.WriteLine(message);

            DemandKeyAndEndpoint(out var key, out var endpoint);
            var store = await OpenAIAssistantHelpers.GetAssistantVectorStoreAsync(key, endpoint, id);
            var json = OpenAIAssistantHelpers.GetAssistantVectorStoreJson(store);

            PrintVectorStore(key, endpoint, store);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            // var ok = !string.IsNullOrEmpty(json);
            // if (ok) Console.WriteLine(json);

            return true;
        }

        private async Task<bool> DoChatAssistantVectorStoreList()
        {
            var message = $"Listing assistant vector stores ...";
            if (!_quiet) Console.WriteLine(message);

            DemandKeyAndEndpoint(out var key, out var endpoint);
            var stores = await OpenAIAssistantHelpers.ListAssistantVectorStoresAsync(key, endpoint);
            IdHelpers.CheckWriteOutputNamesOrIds(stores, _values, "chat", IdKind.Id, (a) => a.Key);
            IdHelpers.CheckWriteOutputNamesOrIds(stores, _values, "chat", IdKind.Name, (a) => a.Value);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            if (stores.Count() == 0)
            {
                if (!_quiet) Console.WriteLine("No vector stores found.");
            }
            else
            {
                if (!_quiet) Console.WriteLine("Vector stores:\n");
                foreach (var store in stores)
                {
                    var storeId = store.Key;
                    var storeName = store.Value;
                    if (string.IsNullOrEmpty(storeName)) storeName = "(no name)";
                    if (!_quiet) 
                    {
                        Console.WriteLine($"  {storeName} ({storeId})");
                    }
                    else
                    {
                        Console.WriteLine(storeId);
                    }
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
            IdHelpers.CheckWriteOutputNameOrId(uploadedId, _values, "chat", IdKind.Id);
            IdHelpers.CheckWriteOutputNameOrId(uploadedName, _values, "chat", IdKind.Name);

            if (!_quiet) Console.WriteLine($"{message} Done!\n\n  {uploadedName} ({uploadedId})\n");

            return true;
        }

        private async Task<bool> DoChatAssistantFileList()
        {
            var message = $"Listing assistant files ...";
            if (!_quiet) Console.WriteLine(message);

            DemandKeyAndEndpoint(out var key, out var endpoint);
            var files = await OpenAIAssistantHelpers.ListAssistantFiles(key, endpoint);
            IdHelpers.CheckWriteOutputNamesOrIds(files, _values, "chat", IdKind.Id, (a) => a.Key);
            IdHelpers.CheckWriteOutputNamesOrIds(files, _values, "chat", IdKind.Name, (a) => a.Value);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            if (files.Count() == 0)
            {
                if (!_quiet) Console.WriteLine("No files found.");
            }
            else
            {
                if (!_quiet) Console.WriteLine("Assistant files:\n");
                foreach (var file in files)
                {
                    var fileId = file.Key;
                    var fileName = file.Value;
                    if (string.IsNullOrEmpty(fileName)) fileName = "(no name)";
                    if (!_quiet)
                    {
                        Console.WriteLine($"  {fileName} ({fileId})");
                    }
                    else
                    {
                        Console.WriteLine($"{fileId}");
                    }
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

        private List<string> ExpandFindFiles(List<string> files)
        {
            if (files.Count() > 0)
            {
                if (!_quiet) Console.Write("\n  Finding files ...");

                files = files.SelectMany(f => FileHelpers.FindFiles(f, _values)).ToList();

                if (!_quiet) Console.WriteLine($"\r  Found {files.Count()} file(s) ...   ");
            }

            return files;
        }

        private static ToolResources ToolResourcesFromVectorStoreId(string vectorStoreId)
        {
            return string.IsNullOrEmpty(vectorStoreId) ? new() : new()
            {
                FileSearch = new()
                {
                    VectorStoreIds = { vectorStoreId }
                },
            };
        }

        private async Task<VectorStore> CreateAssistantVectorStoreAsync(string key, string endpoint, string name, List<string> fileIds, List<string> files)
        {
            var store = await OpenAIAssistantHelpers.CreateAssistantVectorStoreAsync(key, endpoint, name, fileIds);
            return await UploadFilesToAssistantVectorStore(key, endpoint, new(), files, store);
        }

        private async Task<VectorStore> UpdateAssistantVectorStoreAsync(string key, string endpoint, string id, string name, List<string> fileIds, List<string> files)
        {
            var store = await OpenAIAssistantHelpers.UpdateAssistantVectorStoreAsync(key, endpoint, id, name);
            return await UploadFilesToAssistantVectorStore(key, endpoint, fileIds, files, store);
        }

        private async Task<VectorStore> UploadFilesToAssistantVectorStore(string key, string endpoint, List<string> fileIds, List<string> files, VectorStore store)
        {
            var batchFiles = new List<OpenAIFileInfo>();

            if (fileIds.Count() > 0)
            {
                if (!_quiet) Console.WriteLine("\n  Getting files ...\n");

                var fileClient = OpenAIAssistantHelpers.CreateOpenAIFileClient(key, endpoint);
                var foundFiles = await OpenAIAssistantHelpers.GetFilesAsync(fileClient, fileIds,
                    parallelism: 10,
                    callback: (x) =>
                    {
                        Console.WriteLine($"    {x.Filename} ({x.SizeInBytes} byte(s))");
                    });

                batchFiles.AddRange(foundFiles);
            }

            if (files.Count() > 0)
            {
                if (!_quiet) Console.WriteLine("\n  Uploading files ...\n");

                var fileClient = OpenAIAssistantHelpers.CreateOpenAIFileClient(key, endpoint);
                var uploaded = await OpenAIAssistantHelpers.UploadFilesAsync(fileClient, files,
                    parallelism: 10,
                    callback: (x) =>
                    {
                        Console.WriteLine($"    {x.Filename} ({x.SizeInBytes} byte(s))");
                    });

                batchFiles.AddRange(uploaded);
            }

            if (batchFiles.Count() > 0)
            {
                const int maxBatchSize = 50;
                await ProcessBatchFileJobs(key, endpoint, store, batchFiles, maxBatchSize);

                store = await OpenAIAssistantHelpers.GetAssistantVectorStoreAsync(key, endpoint, store.Id);
            }

            return store;
        }

        private static async Task ProcessBatchFileJobs(string key, string endpoint, VectorStore store, List<OpenAIFileInfo> batchFiles, int maxBatchSize)
        {
            var completed = 0;
            var total = 0;

            while (batchFiles.Count > 0)
            {
                Console.Write("\n  Processing vector store files ...");

                var take = Math.Min(maxBatchSize, batchFiles.Count);
                var thisBatch = batchFiles.Take(take).ToList();
                batchFiles.RemoveRange(0, take);

                var batchJob = await ProcessBatchFileJob(key, endpoint, store, thisBatch);
                completed += batchJob.FileCounts.Completed;
                total += batchJob.FileCounts.Total;
            }

            if (total > maxBatchSize)
            {
                Console.WriteLine("\n  Processing vector store files ... Done!\n");
                Console.WriteLine($"    File Completed/Total: {completed} out of {total}\n");
            }
        }

        private static async Task<VectorStoreBatchFileJob> ProcessBatchFileJob(string key, string endpoint, VectorStore store, List<OpenAIFileInfo> batchFiles)
        {
            var batchJob = await OpenAIAssistantHelpers.ProcessBatchFileJob(key, endpoint, store, batchFiles);

            Console.WriteLine("\n");
            Console.WriteLine($"    Batch job: {batchJob.BatchId}");
            Console.WriteLine($"    File Completed/Total: {batchJob.FileCounts.Completed} out of {batchJob.FileCounts.Total}");

            return batchJob;
        }

        private void PrintVectorStore(string key, string endpoint, VectorStore store)
        {
            var id = store.Id;
            var name = store.Name ?? "(no name)";

            Console.WriteLine();
            Console.WriteLine($"  ID: {id}");
            Console.WriteLine($"  Name: {name}");

            var storeClient = OpenAIAssistantHelpers.CreateOpenAIVectorStoreClient(key, endpoint);
            var fileClient = OpenAIAssistantHelpers.CreateOpenAIFileClient(key, endpoint);

            if (store.FileCounts.Total == 0)
            {
                Console.WriteLine("\n  Files: (no files)");
                Console.WriteLine();
            }
            else if (store.FileCounts.Total == 1)
            {
                var association = storeClient.GetFileAssociations(store).GetAllValues().First();
                var file = fileClient.GetFile(association.FileId);

                Console.Write("\n  File:");
                Console.WriteLine($" {file.Value.Filename} ({file.Value.SizeInBytes} byte(s))");
                Console.WriteLine();
            }
            else // if (store.FileCounts.Total > 1)
            {
                Console.WriteLine("\n  Files:\n");

                var count = 0;
                var associations = storeClient.GetFileAssociations(store);
                foreach (var association in associations.GetAllValues())
                {
                    var file = fileClient.GetFile(association.FileId);
                    Console.WriteLine($"    {file.Value.Filename} ({file.Value.SizeInBytes} byte(s))");

                    if (++count >= 5 && !_verbose)
                    {
                        Console.WriteLine($"    ({associations.Count() - count} more file(s) ... )");
                        break;
                    }
                }

                Console.WriteLine();
            }
        }

        private static void PrintAssistant(Assistant assistant)
        {
            var id = assistant.Id;
            var name = assistant.Name ?? "(no name)";

            Console.WriteLine();
            Console.WriteLine($"  ID: {id}");
            Console.WriteLine($"  Name: {name}");
            Console.WriteLine();
            Console.WriteLine($"  Model: {assistant.Model}");
            Console.WriteLine($"  Instructions: {assistant.Instructions}");
            Console.WriteLine();

            var toolNames = string.Join(", ", assistant.Tools.Select(x => x.GetType().Name));
            Console.WriteLine($"  Tools: {toolNames}");

            var countVectorStoreIds = assistant.ToolResources?.FileSearch?.VectorStoreIds?.Count();
            if (countVectorStoreIds > 0)
            {
                var vectorStoreIds = string.Join(", ", assistant.ToolResources?.FileSearch?.VectorStoreIds);
                Console.WriteLine(vectorStoreIds.Contains(',')
                    ? $"  Vector stores: {vectorStoreIds}"
                    : $"  Vector store: {vectorStoreIds}");
            }

            Console.WriteLine();
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

        private void SetAssistantIdConfig(Assistant assistant)
        {
            ConfigSetHelpers.ConfigSet("assistant.id", assistant.Id, print: !_quiet);

            var vectorStoreId = assistant.ToolResources.FileSearch?.VectorStoreIds?.FirstOrDefault();
            if (!string.IsNullOrEmpty(vectorStoreId))
            {
                Console.WriteLine();
                ConfigSetHelpers.ConfigSet("vector.store.id", vectorStoreId, print: !_quiet);
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

            var outputHistoryFile = OutputChatHistoryFileToken.Data().GetOrDefault(_values);
            if (!string.IsNullOrEmpty(outputHistoryFile))
            {
                _outputChatHistoryFileName = FileHelpers.GetOutputDataFileName(outputHistoryFile, _values, "chat.input.id");
            }

            var outputAnswerFile = OutputChatAnswerFileToken.Data().GetOrDefault(_values);
            if (!string.IsNullOrEmpty(outputAnswerFile))
            {
                _outputAnswerFile = FileHelpers.GetOutputDataFileName(outputAnswerFile, _values);
            }

            var outputAddAnswerFile = OutputAddChatAnswerFileToken.Data().GetOrDefault(_values);
            if (!string.IsNullOrEmpty(outputAddAnswerFile))
            {
                _outputAddAnswerFile = FileHelpers.GetOutputDataFileName(outputAddAnswerFile, _values);
            }

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

        public const string DefaultSystemPrompt = "You are a helpful AI assistant.";

        private const float _defaultTemperature = 0.7f;
        private const float _defaultFrequencyPenalty = 0.0f;
        private const float _defaultPresencePenalty = 0.0f;

        private string? _outputAnswerFile;
        private string? _outputAddAnswerFile;
        private string? _outputChatHistoryFileName;
    }
}
