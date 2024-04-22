//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Azure.AI.Details.Common.CLI.ConsoleGui;
using Azure.AI.Details.Common.CLI.Extensions.HelperFunctions;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Assistants;
using Azure.Core.Diagnostics;
using Microsoft.CognitiveServices.Speech;
using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Scriban;
using System.ClientModel.Primitives;

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
                ChatNonInteractively();
            }
        }

        private async Task ChatInteractively()
        {
            var chatTextHandler = GetChatTextHandler();

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

        private void ChatNonInteractively()
        {
            var chatTextHandler = GetChatTextHandler();

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

        private Func<string, Task> GetChatTextHandler()
        {
            var parameterFile = InputChatParameterFileToken.Data().GetOrDefault(_values);
            if (!string.IsNullOrEmpty(parameterFile)) SetValuesFromParameterFile(parameterFile);

            var client = CreateOpenAIClient(out var deployment);
            var options = CreateChatCompletionOptions(deployment);
            var funcContext = CreateFunctionFactoryAndCallContext(options);

            return async (string text) =>
            {
                await GetChatCompletionsAsync(client, options, funcContext, text);
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

            var queryType = QueryTypeFrom(_values["service.config.search.query.type"]) ?? AzureSearchQueryType.VectorSimpleHybrid;

            var search = new AzureSearchChatExtensionConfiguration()
            {
                Authentication = new OnYourDataApiKeyAuthenticationOptions(searchKey),
                SearchEndpoint = new Uri(searchEndpoint),
                IndexName = indexName,
                QueryType = queryType,
                DocumentCount = 16,
                VectorizationSource = new OnYourDataEndpointVectorizationSource(embeddingEndpoint, new OnYourDataApiKeyAuthenticationOptions(embeddingsKey))
            };

            options.AzureExtensionsOptions = new() { Extensions = { search } };
        }

        private static AzureSearchQueryType? QueryTypeFrom(string queryType)
        {
            if (string.IsNullOrEmpty(queryType)) return null;

            return queryType.ToLower() switch
            {
                "semantic" => AzureSearchQueryType.Semantic,
                "simple" => AzureSearchQueryType.Simple,
                "vector" => AzureSearchQueryType.Vector,

                "hybrid"
                or "simplehybrid"
                or "simple-hybrid"
                or "vectorsimplehybrid"
                or "vector-simple-hybrid" => AzureSearchQueryType.VectorSimpleHybrid,

                "semantichybrid"
                or "semantic-hybrid"
                or "vectorsemantichybrid"
                or "vector-semantic-hybrid" => AzureSearchQueryType.VectorSemanticHybrid,

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

            var client = CreateOpenAIAssistantsClient();

            var fileIds = FileIdOptionXToken.GetOptions(_values).ToList();
            fileIds.AddRange(FileIdsOptionXToken.GetOptions(_values));

            var createOptions = new AssistantCreationOptions(deployment) { Name = name, Instructions = instructions };
            if (fileIds.Count() > 0)
            {
                createOptions.Tools.Add(new RetrievalToolDefinition());
                fileIds.ForEach(id => createOptions.FileIds.Add(id));
            }

            var response = await client.CreateAssistantAsync(createOptions);
            var assistant = response.Value;

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var fi = new FileInfo(ConfigSetHelpers.ConfigSet("assistant.id", assistant.Id));
            if (!_quiet) Console.WriteLine($"{fi.Name} (saved at {fi.DirectoryName})\n\n  {assistant.Id}");

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

            var client = CreateOpenAIAssistantsClient();
            var response = await client.DeleteAssistantAsync(id);

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

            var client = CreateOpenAIAssistantsClient();
            var response = await client.GetAssistantAsync(id);

            var assistant = response.Value;
            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var jsonModel = assistant as IJsonModel<Assistant>;
            if (jsonModel != null)
            {
                var writer = new Utf8JsonWriter(Console.OpenStandardOutput());
                jsonModel.Write(writer, ModelReaderWriterOptions.Json);
                writer.Flush();
                Console.WriteLine();
            }

            return true;
        }

        private async Task<bool> DoChatAssistantList()
        {
            var message = $"Listing assistants ...";
            if (!_quiet) Console.WriteLine(message);

            var client = CreateOpenAIAssistantsClient();

            var order = ListSortOrder.Ascending;
            var response = await client.GetAssistantsAsync(order: order);

            var pageable = response.Value;
            var assistants = pageable.ToList();

            while (pageable.HasMore)
            {
                response = await client.GetAssistantsAsync(after: pageable.LastId, order: order);
                pageable = response.Value;
                assistants.AddRange(pageable);
            }

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            if (assistants.Count == 0)
            {
                Console.WriteLine("No assistants found.");
            }
            else
            {
                Console.WriteLine("Assistants:\n");
                foreach (var assistant in assistants)
                {
                    Console.WriteLine($"  {assistant.Name} ({assistant.Id})");
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

            var client = CreateOpenAIAssistantsClient();
            var response = await client.UploadFileAsync(existing, OpenAIFilePurpose.Assistants);

            if (!_quiet) Console.WriteLine($"{message} Done!");
            return true;
        }

        private async Task<bool> DoChatAssistantFileList()
        {
            var message = $"Listing assistant files ...";
            if (!_quiet) Console.WriteLine(message);

            var client = CreateOpenAIAssistantsClient();
            var response = await client.GetFilesAsync(OpenAIFilePurpose.Assistants);
            var list = response.Value;

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var files = list.ToList();
            if (files.Count == 0)
            {
                Console.WriteLine("No files found.");
            }
            else
            {
                Console.WriteLine("Assistant files:\n");
                foreach (var file in files)
                {
                    Console.WriteLine($"  {file.Filename} ({file.Id})");
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

            var client = CreateOpenAIAssistantsClient();
            var response = await client.DeleteFileAsync(id);

            if (!_quiet) Console.WriteLine($"{message} Done!");
            return true;
        }

        private AssistantsClient CreateOpenAIAssistantsClient()
        {
            var key = _values["service.config.key"];
            var endpoint = ConfigEndpointUriToken.Data().GetOrDefault(_values);

            if (string.IsNullOrEmpty(endpoint))
            {
                _values.AddThrowError("ERROR:", $"Creating AssistantsClient; requires endpoint.");
            }

            _azureEventSourceListener = new AzureEventSourceListener((e, message) => EventSourceAiLoggerLog(e, message), System.Diagnostics.Tracing.EventLevel.Verbose);

            var options = new AssistantsClientOptions();
            options.Diagnostics.IsLoggingContentEnabled = true;
            options.Diagnostics.IsLoggingEnabled = true;

            return new AssistantsClient(new Uri(endpoint!), new AzureKeyCredential(key), options);
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

        private AzureEventSourceListener _azureEventSourceListener;

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
