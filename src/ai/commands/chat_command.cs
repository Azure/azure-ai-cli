//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Azure.AI.Details.Common.CLI.ConsoleGui;
using Azure.AI.Details.Common.CLI.Extensions.HelperFunctions;
using Azure.AI.OpenAI;
using Azure.Core.Diagnostics;
using Microsoft.CognitiveServices.Speech;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Memory.AzureCognitiveSearch;
using Microsoft.SemanticKernel.Memory;
using System;
using System.Text.Json;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Text.Json.Nodes;
using System.ComponentModel;
using Scriban;

namespace Azure.AI.Details.Common.CLI
{
    public class ChatCommand : Command
    {
        internal ChatCommand(ICommandValues values)
        {
            _values = values.ReplaceValues();
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", true);
        }

        internal bool RunCommand()
        {
            DoCommand(_values.GetCommand());
            return _values.GetOrDefault("passed", true);
        }

        private void DoCommand(string command)
        {
            StartCommand();

            switch (command)
            {
                case "chat": DoChat(); break;
                case "chat.run": DoChatRun(); break;
                case "chat.evaluate": DoChatEvaluate(); break;

                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
            }

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private void DoChatRun()
        {
            var action = "Running chats";
            var command = "chat run";

            var function = FunctionToken.Data().GetOrDefault(_values, null);
            var isFunction = function != null;

            var message = isFunction ? $"{action} w/ {function} ..." : $"{action} ...";
            if (!_quiet) Console.WriteLine(message);

            string output = isFunction
                ? ChatRunFunction(action, command, function)
                : ChatRunNonFunction();

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            if (!string.IsNullOrEmpty(output))
            {
                var parsed = JsonDocument.Parse(output).RootElement.EnumerateArray();
                foreach (var item in parsed)
                {
                    Console.WriteLine(item.GetRawText());
                }
            }
        }

        private void DoChatEvaluate()
        {
            var action = "Evaluating chats";
            var command = "chat evaluate";

            var function = FunctionToken.Data().GetOrDefault(_values, null);
            var isFunction = function != null;

            var message = isFunction ? $"{action} w/ {function} ..." : $"{action} ...";
            if (!_quiet) Console.WriteLine(message);

            string output = isFunction
                ? ChatEvalFunction(action, command, function)
                : ChatEvalNonFunction(action, command);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            if (!string.IsNullOrEmpty(output)) Console.WriteLine(output);
        }

        private string ChatRunNonFunction()
        {
            var data = InputDataFileToken.Data().Demand(_values, "Running chats", "chat run");
            var dataFile = FileHelpers.DemandFindFileInDataPath(data, _values, "chat data");
            var lines = File.ReadAllLines(dataFile);

            var parameterFile = InputChatParameterFileToken.Data().GetOrDefault(_values);
            if (!string.IsNullOrEmpty(parameterFile)) SetValuesFromParameterFile(parameterFile);

            var client = CreateOpenAIClient(out var deployment);
            var options = CreateChatCompletionOptions(deployment);
            var funcContext = CreateFunctionFactoryAndCallContext(options);

            var chatTextHandler = (string text) =>
            {
                var systemMessage = options.Messages.First();
                options.Messages.Clear();

                options.Messages.Add(systemMessage);
                options.Messages.Add(new ChatRequestUserMessage(text));

                var message = GetChatCompletion(client, options, funcContext);
                var answer = message.Content;
                var context = message.AzureExtensionsContext != null && message.AzureExtensionsContext.Messages != null
                    ? string.Join('\n', message.AzureExtensionsContext.Messages.Select(x => x.Content))
                    : null;
                return (answer, context);
            };

            var results = new JsonArray();
            foreach (var line in lines)
            {
                var jsonText = line.Trim();
                if (string.IsNullOrEmpty(jsonText)) continue;

                var json = JsonDocument.Parse(jsonText).RootElement;
                var question = json.GetPropertyStringOrNull("question");

                var (answer, context) = chatTextHandler(question);
                if (!string.IsNullOrEmpty(answer))
                {
                    var result = new JsonObject();
                    result.Add("question", question);
                    result.Add("answer", answer);
                    if (json.TryGetProperty("truth", out var truth)) result.Add("truth", truth.GetRawText());
                    if (!string.IsNullOrEmpty(context)) result.Add("context", context);
                    results.Add(result);
                }
            }

            return results.ToString();
        }

        private ChatResponseMessage GetChatCompletion(OpenAIClient client, ChatCompletionsOptions options, HelperFunctionCallContext funcContext)
        {
            while (true)
            {
                var response = client.GetChatCompletions(options);
                var message = CheckChoiceFinishReason(response.Value.Choices.Last()).Message;

                if (funcContext.CheckForFunction(message))
                {
                    if (options.TryCallFunction(funcContext, out var result))
                    {
                        funcContext.Reset();
                        continue;
                    }
                }

                return message;
            }
        }

        private string ChatRunFunction(string action, string command, string function)
        {
            var setEnv = _values.GetOrDefault("chat.set.environment", true);
            var env = setEnv ? ConfigEnvironmentHelpers.GetEnvironment(_values) : null;

            var data = InputDataFileToken.Data().Demand(_values, action, command);
            var dataFile = FileHelpers.DemandFindFileInDataPath(data, _values, "chat data");

            var output = PythonRunner.RunEmbeddedPythonScript(_values, "function_call_run",
                CliHelpers.BuildCliArgs(
                    "--function", function,
                    "--data", dataFile),
                addToEnvironment: env);
            return output;
        }

        private string ChatEvalNonFunction(string action, string command)
        {
            var data = ChatRunNonFunction();
            var parsed = JsonDocument.Parse(data).RootElement.EnumerateArray();

            var sb = new StringBuilder();
            parsed.ToList().ForEach(x => sb.AppendLine(x.GetRawText()));
            data = sb.ToString();

            var dataFile = Path.GetTempFileName();
            FileHelpers.WriteAllText(dataFile, data, new UTF8Encoding(false));

            var setEnv = _values.GetOrDefault("chat.set.environment", true);
            var env = setEnv ? ConfigEnvironmentHelpers.GetEnvironment(_values) : null;

            var subscription = SubscriptionToken.Data().Demand(_values, action, command, checkConfig: "subscription");
            var group = ResourceGroupNameToken.Data().Demand(_values, action, command, checkConfig: "group");
            var project = ProjectNameToken.Data().Demand(_values, action, command, checkConfig: "project");

            var dataFileForEvaluationNameOnly = InputDataFileToken.Data().Demand(_values, action, command);
            var evaluationName = $"{new FileInfo(dataFileForEvaluationNameOnly).Name}"
                .Replace(' ', '-')
                .Replace('.', '-')
                .Replace('_', '-')
                .Replace(':', '-')
                .Replace(Path.DirectorySeparatorChar, '-')
                .Replace(Path.AltDirectorySeparatorChar, '-')
                .Replace("--", "-")
                .Trim('-')
                .ToLower();
            evaluationName = $"{evaluationName}-{DateTime.Now.ToString("yyyyMMddHHmmss")}";

            var output = PythonRunner.RunEmbeddedPythonScript(_values, "function_call_evaluate",
                CliHelpers.BuildCliArgs(
                    "--data", dataFile,
                    "--subscription", subscription,
                    "--group", group,
                    "--project-name", project,
                    "--name", evaluationName),
                addToEnvironment: env);
            return output;
        }

        private string ChatEvalFunction(string action, string command, string function)
        {
            var setEnv = _values.GetOrDefault("chat.set.environment", true);
            var env = setEnv ? ConfigEnvironmentHelpers.GetEnvironment(_values) : null;

            var subscription = SubscriptionToken.Data().Demand(_values, action, command, checkConfig: "subscription");
            var group = ResourceGroupNameToken.Data().Demand(_values, action, command, checkConfig: "group");
            var project = ProjectNameToken.Data().Demand(_values, action, command, checkConfig: "project");

            var data = InputDataFileToken.Data().Demand(_values, action, command);
            var dataFile = FileHelpers.DemandFindFileInDataPath(data, _values, "chat data");

            var evaluationName = $"{function}-{new FileInfo(dataFile).Name}"
                .Replace(' ', '-')
                .Replace('.', '-')
                .Replace('_', '-')
                .Replace(':', '-')
                .Replace(Path.DirectorySeparatorChar, '-')
                .Replace(Path.AltDirectorySeparatorChar, '-')
                .Replace("--", "-")
                .Trim('-')
                .ToLower();
            evaluationName = $"{evaluationName}-{DateTime.Now.ToString("yyyyMMddHHmmss")}";

            Action<string> stdErrVerbose = x => Console.Error.WriteLine(x);
            var stdErr = (Program.Debug || _verbose) ? stdErrVerbose : null;

            var output = PythonRunner.RunEmbeddedPythonScript(_values, "function_call_evaluate",
                CliHelpers.BuildCliArgs(
                    "--function", function,
                    "--data", dataFile,
                    "--subscription", subscription,
                    "--group", group,
                    "--project-name", project,
                    "--name", evaluationName),
                addToEnvironment: env, null, stdErr, null);
            return output;
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
            var chatTextHandler = await GetChatTextHandler();

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

            DisplayUserChatPromptLabel();
            var text = userPrompt;
            DisplayUserChatPromptText(text);

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

        private async Task<Func<string, Task<string>>> GetChatFunctionTextHandler(string function)
        {
            var setEnv = _values.GetOrDefault("chat.set.environment", true);
            var env = setEnv ? ConfigEnvironmentHelpers.GetEnvironment(_values) : null;

            var messages = new List<ChatRequestMessage>();

            var systemPrompt = _values.GetOrDefault("chat.message.system.prompt", DefaultSystemPrompt);
            messages.Add(new ChatRequestSystemMessage(systemPrompt));

            return await Task.Run(() =>
            {
                Func<string, Task<string>> handler = (string text) =>
                {

                    messages.Add(new ChatRequestUserMessage(text));

                    DisplayAssistantPromptLabel();
                    Console.ForegroundColor = ConsoleColor.Gray;

                    var chatProtocolFunc = new Func<string>(() =>
                    {
                        return PythonRunner.RunEmbeddedPythonScript(_values, "function_call",
                            CliHelpers.BuildCliArgs(
                                "--function", function,
                                "--parameters", ConvertMessagesToJson(messages)),
                            addToEnvironment: env);
                    });
                    var questionFunc = new Func<string>(() =>
                    {
                        return PythonRunner.RunEmbeddedPythonScript(_values, "function_call",
                           CliHelpers.BuildCliArgs(
                               "--function", function,
                               "--parameters", $"{{\"question\": \"{text}\"}}"),
                           addToEnvironment: env);
                    });

                    var output = TryCatchHelpers.TryCatchNoThrow<string>(chatProtocolFunc, null, out var ex1);
                    if (output == null && ex1 != null)
                    {
                        var error1 = _values.GetOrDefault("error", null);
                        _values.Reset("error");

                        output = TryCatchHelpers.TryCatchNoThrow<string>(questionFunc, null, out var ex2);

                        if (output == null && ex2 != null)
                        {
                            var error2 = _values.GetOrDefault("error", null);
                            _values.Reset("error");

                            _values.AddThrowError("ERROR", $"{ex1.Message}\n\n{error1}\n\n{ex2.Message}\n\n{error2}");
                        }
                    }

                    DisplayAssistantPromptTextStreaming(output);
                    DisplayAssistantPromptTextStreamingDone();
                    CheckWriteChatAnswerOutputFile(output);

                    messages.Add(new ChatRequestAssistantMessage(output));

                    return Task.FromResult(output);
                };
                return handler;
            });
        }

        private async Task<Func<string, Task>> GetNormalChatTextHandler()
        {
            var doSK = SKIndexNameToken.IsSKIndexKind(_values);

            var kernel = CreateSemanticKernel(out var acsIndex);
            if (kernel != null && doSK) await StoreMemoryAsync(kernel, acsIndex);

            var parameterFile = InputChatParameterFileToken.Data().GetOrDefault(_values);
            if (!string.IsNullOrEmpty(parameterFile)) SetValuesFromParameterFile(parameterFile);

            var client = CreateOpenAIClient(out var deployment);
            var options = CreateChatCompletionOptions(deployment);
            var funcContext = CreateFunctionFactoryAndCallContext(options);

            var handler = async (string text) =>
            {
                if (doSK)
                {
                    var relevantMemories = await SearchMemoryAsync(kernel, acsIndex, text);
                    if (relevantMemories != null)
                    {
                        text = UpdateUserInputWithSearchResultInfo(text, relevantMemories);
                    }
                }

                await GetChatCompletionsAsync(client, options, funcContext, text);
            };
            return handler;
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
            return ListBoxPicker.PickString(choices, 20, choices.Length + 2, new Colors(ConsoleColor.White, ConsoleColor.Blue), new Colors(ConsoleColor.White, ConsoleColor.Red), select);
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
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        private void DisplayAssistantPromptTextStreaming(string text)
        {
            // do "tab" indentation when not quiet
            Console.Write(!_quiet
                ? text.Replace("\n", "\n           ")
                : text);
        }

        private void DisplayAssistantPromptTextStreamingDone()
        {
            Console.WriteLine("\n");
        }

        private void DisplayAssistantFunctionCall(HelperFunctionCallContext context, string result)
        {
            if (!_quiet && _verbose)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\rassistant-function");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(": ");
                Console.ForegroundColor = ColorHelpers.MapColor(ConsoleColor.DarkGray);
                Console.WriteLine($"{context.FunctionName}({context.Arguments}) = {result}");

                DisplayAssistantPromptLabel();
                Console.ForegroundColor = ColorHelpers.MapColor(ConsoleColor.DarkGray);
            }
        }

        private async Task<StreamingResponse<StreamingChatCompletionsUpdate>> GetChatCompletionsAsync(OpenAIClient client, ChatCompletionsOptions options, HelperFunctionCallContext funcContext, string text)
        {
            var requestMessage = new ChatRequestUserMessage(text);
            options.Messages.Add(requestMessage);
            
            CheckWriteChatHistoryOutputFile(options);
            DisplayAssistantPromptLabel();

            Console.ForegroundColor = ConsoleColor.Gray;

            string contentComplete = string.Empty;

            while (true)
            {
                var response = await client.GetChatCompletionsStreamingAsync(options);
                await foreach (var update in response.EnumerateValues())
                {
                    funcContext.CheckForUpdate(update);

                    CheckChoiceFinishReason(update.FinishReason);

                    var content = update.ContentUpdate;
                    if (content == null) continue;

                    contentComplete += content;
                    DisplayAssistantPromptTextStreaming(content);
                }

                if (options.TryCallFunction(funcContext, out var result))
                {
                    DisplayAssistantFunctionCall(funcContext, result);
                    funcContext.Reset();
                    CheckWriteChatHistoryOutputFile(options);
                    continue;
                }

                DisplayAssistantPromptTextStreamingDone();
                CheckWriteChatAnswerOutputFile(contentComplete);

                var currentContent = new ChatRequestAssistantMessage(contentComplete);
                options.Messages.Add(currentContent);
                
                CheckWriteChatHistoryOutputFile(options);

                return response;
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

        private void ClearMessageHistory()
        {
            var outputHistoryFile = OutputChatHistoryFileToken.Data().GetOrDefault(_values);
            if (!string.IsNullOrEmpty(outputHistoryFile))
            {
                var fileName = FileHelpers.GetOutputDataFileName(outputHistoryFile, _values);
                FileHelpers.WriteAllText(fileName, "", Encoding.UTF8);
            }
        }

        private void CheckWriteChatHistoryOutputFile(ChatCompletionsOptions options)
        {
            var outputHistoryFile = OutputChatHistoryFileToken.Data().GetOrDefault(_values);
            if (!string.IsNullOrEmpty(outputHistoryFile))
            {
                var fileName = FileHelpers.GetOutputDataFileName(outputHistoryFile, _values);
                options.SaveChatHistoryToFile(fileName);
            }
        }

        private ChatCompletionsOptions CreateChatCompletionOptions(string deployment)
        {
            var options = new ChatCompletionsOptions();
            options.DeploymentName = deployment;

            var systemPrompt = _values.GetOrDefault("chat.message.system.prompt", DefaultSystemPrompt);
            options.Messages.Add(new ChatRequestSystemMessage(systemPrompt));

            var textFile = _values["chat.message.history.text.file"];
            var jsonFile = InputChatHistoryJsonFileToken.Data().GetOrDefault(_values);

            if(!string.IsNullOrEmpty(jsonFile) && !string.IsNullOrEmpty(textFile))
            {
                _values.AddThrowError("chat.message.history.text.file", "chat.message.history.json.file", "Only one of these options can be specified");
            }

            if (!string.IsNullOrEmpty(jsonFile)) options.ReadChatHistoryFromFile(jsonFile);
            if (!string.IsNullOrEmpty(textFile)) AddChatMessagesFromTextFile(options.Messages, textFile);

            var maxTokens = _values["chat.options.max.tokens"];
            var temperature = _values["chat.options.temperature"];
            var frequencyPenalty = _values["chat.options.frequency.penalty"];
            var presencePenalty = _values["chat.options.presence.penalty"];

            options.MaxTokens = TryParse(maxTokens, null);
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

            var queryType = QueryTypeFrom(_values["service.config.search.query.type"]) ?? AzureCognitiveSearchQueryType.VectorSimpleHybrid;

            var search = new AzureCognitiveSearchChatExtensionConfiguration()
            {
                SearchEndpoint = new Uri(searchEndpoint),
                Key = searchKey,
                IndexName = indexName,
                QueryType = queryType,
                DocumentCount = 16,
                EmbeddingEndpoint = embeddingEndpoint,
                EmbeddingKey = embeddingsKey,
            };

            options.AzureExtensionsOptions = new() { Extensions = { search } };
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

        private HelperFunctionCallContext CreateFunctionFactoryAndCallContext(ChatCompletionsOptions options)
        {
            var customFunctions = _values.GetOrDefault("chat.custom.helper.functions", null);
            var useCustomFunctions = !string.IsNullOrEmpty(customFunctions);
            var useBuiltInFunctions = _values.GetOrDefault("chat.built.in.helper.functions", false);

            var factory = useCustomFunctions && useBuiltInFunctions
                ? CreateFunctionFactoryForCustomFunctions(customFunctions) + CreateFunctionFactoryWithBuiltinFunctions()
                : useCustomFunctions
                    ? CreateFunctionFactoryForCustomFunctions(customFunctions)
                    : useBuiltInFunctions
                        ? CreateFunctionFactoryWithBuiltinFunctions()
                        : CreateFunctionFactoryWithNoFunctions();

            return options.AddFunctions(factory);
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

        private ChatChoice CheckChoiceFinishReason(ChatChoice choice)
        {
            if (choice.FinishReason == CompletionsFinishReason.ContentFiltered)
            {
                if (!_quiet) ConsoleHelpers.WriteLineWithHighlight("#e_;WARNING: Content filtered!");
            }
            if (choice.FinishReason == CompletionsFinishReason.TokenLimitReached)
            {
                _values.AddThrowError("ERROR:", $"exceeded token limit!",
                                         "TRY:", $"{Program.Name} chat --max-tokens TOKENS");
            }
            return choice;
        }

        private void CheckChoiceFinishReason(CompletionsFinishReason? finishReason)
        {
            if (finishReason == CompletionsFinishReason.ContentFiltered)
            {
                if (!_quiet) ConsoleHelpers.WriteLineWithHighlight("#e_;WARNING: Content filtered!");
            }
            if (finishReason == CompletionsFinishReason.TokenLimitReached)
            {
                _values.AddThrowError("ERROR:", $"exceeded token limit!",
                                         "TRY:", $"{Program.Name} chat --max-tokens TOKENS");
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

        private void SetValuesFromParameterFile(string parameterFile)
        {
            var existing = FileHelpers.DemandFindFileInDataPath(parameterFile, _values, "chat parameter");
            var text = FileHelpers.ReadAllText(existing, Encoding.Default);
            string[] sections = text.Split("---\n");
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

        private void AddChatMessagesFromTextFile(IList<ChatRequestMessage> messages, string textFile)
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

                if (i == 0 && role == ChatRole.System && FirstMessageIsDefaultSystemPrompt(messages, role))
                {
                    messages[0] = new ChatRequestSystemMessage(line);
                    continue;
                }

                messages.Add(role == ChatRole.System
                    ? new ChatRequestSystemMessage(line)
                    : role == ChatRole.User
                        ? new ChatRequestUserMessage(line)
                        : new ChatRequestAssistantMessage(line));
            }
        }

        private static bool FirstMessageIsDefaultSystemPrompt(IList<ChatRequestMessage> messages, ChatRole role)
        {
            var message = messages.FirstOrDefault() as ChatRequestSystemMessage;
            return message != null && message.Content == DefaultSystemPrompt;
        }

        private static string ConvertMessagesToJson(IList<ChatRequestMessage> messages)
        {
            var sb = new StringBuilder();
            sb.Append("[");
            foreach (var message in messages)
            {
                var user = message as ChatRequestUserMessage;
                var system = message as ChatRequestSystemMessage;
                var assistant = message as ChatRequestAssistantMessage;
                var content = system?.Content ?? user?.Content ?? assistant?.Content;
                content = content.Replace("\\", "\u005C").Replace("\"", "");

                var ok = !string.IsNullOrEmpty(content);
                if (!ok) continue;

                if (sb.Length > 1) sb.Append(",");

                sb.Append($"{{\"role\": \"{message.Role}\", \"content\": \"{content}\"}}");
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
        private readonly bool _quiet = false;
        private readonly bool _verbose = false;

        private AzureEventSourceListener _azureEventSourceListener;

        // OutputHelper _output = null;
        // DisplayHelper _display = null;

#nullable enable
        private int? TryParse(string? s, int? defaultValue)
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
#nullable disable

        private async Task StoreMemoryAsync(IKernel kernel, string index)
        {
            if (!_quiet) Console.Write("Storing files in semantic memory...");
            var githubFiles = SampleData();
            foreach (var entry in githubFiles)
            {
                await kernel.Memory.SaveReferenceAsync(
                    collection: index,
                    externalSourceName: "GitHub",
                    externalId: entry.Key,
                    description: entry.Value,
                    text: entry.Value);

                if (!_quiet) Console.Write(".");
            }

            if (!_quiet) Console.WriteLine(". Done!\n");
        }

#nullable enable
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
           // Console.ForegroundColor = ColorHelpers.MapColor(ConsoleColor.DarkGray);
           // Console.WriteLine("Relevant?\n" + result + "\n");
           // Console.ResetColor();

            return result;
        }
#nullable disable

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

        private const int _defaultMaxTokens = 0;
        private const float _defaultTemperature = 0.7f;
        private const float _defaultFrequencyPenalty = 0.0f;
        private const float _defaultPresencePenalty = 0.0f;
        private const float _defaultTopP = 0.95f;
    }
}
