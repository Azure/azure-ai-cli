using Azure.AI.OpenAI;
using System.Text;
using System.Text.Json;
using System.ClientModel.Primitives;
using System.Runtime.CompilerServices;

namespace Azure.AI.Details.Common.CLI.Extensions.HelperFunctions
{
    public static class HelperFunctionFactoryExtensions
    {
        // extension method to ChatCompletionsOptions
        public static HelperFunctionCallContext AddFunctions(this ChatCompletionsOptions options, HelperFunctionFactory functionFactory)
        {
            foreach (var function in functionFactory.GetFunctionDefinitions())
            {
                // options.Tools.Add(new ChatCompletionsFunctionToolDefinition(function));
                options.Functions.Add(function);
            }

            return new HelperFunctionCallContext(functionFactory);
        }

        public static bool TryCallFunction(this ChatCompletionsOptions options, HelperFunctionCallContext context, out string? result)
        {
            return context.TryCallFunction(options, out result);
        }

        public static void SaveChatHistoryToFile(this ChatCompletionsOptions options, string fileName)
        {
            var historyFile = new StringBuilder();

            foreach (var message in options.Messages)
            {
                var messageText = message switch
                {
                    ChatRequestUserMessage userMessage => ModelReaderWriter.Write(userMessage, ModelReaderWriterOptions.Json).ToString(),
                    ChatRequestAssistantMessage assistantMessage => ModelReaderWriter.Write(assistantMessage, ModelReaderWriterOptions.Json).ToString(),
                    ChatRequestFunctionMessage functionMessage => ModelReaderWriter.Write(functionMessage, ModelReaderWriterOptions.Json).ToString(),
                    ChatRequestSystemMessage systemMessage => ModelReaderWriter.Write(systemMessage, ModelReaderWriterOptions.Json).ToString(),
                    ChatRequestToolMessage toolMessage => ModelReaderWriter.Write(toolMessage, ModelReaderWriterOptions.Json).ToString(),
                    _ => null
                };

                if (!string.IsNullOrEmpty(messageText))
                {
                    historyFile.AppendLine(messageText);
                }
            }

            FileHelpers.WriteAllText(fileName, historyFile.ToString(), Encoding.UTF8);
        }

        public static void ReadChatHistoryFromFile(this ChatCompletionsOptions options, string fileName)
        {
            var historyFile = FileHelpers.ReadAllText(fileName, Encoding.UTF8);

            var historyFileLines = historyFile.Split(Environment.NewLine);
            var clearIfSystem = () =>
            {
                options.Messages.Clear();
                return typeof(ChatRequestSystemMessage);
            };

            foreach (var line in historyFileLines)
            {
                var jsonObject = JsonDocument.Parse(line);
                JsonElement roleObj;

                if (!jsonObject.RootElement.TryGetProperty("role", out roleObj))
                {
                    continue;
                }

                var role = roleObj.GetString();

                var type = role?.ToLowerInvariant() switch
                {
                    "user" => typeof(ChatRequestUserMessage),
                    "assistant" => typeof(ChatRequestAssistantMessage),
                    "function" => typeof(ChatRequestFunctionMessage),
                    "system" => clearIfSystem(),
                    "tool" => typeof(ChatRequestToolMessage),
                    _ => throw new Exception($"Unknown chat role {role}")
                };

                var message = ModelReaderWriter.Read(BinaryData.FromString(line), type, ModelReaderWriterOptions.Json);

                options.Messages.Add(message as ChatRequestMessage);
            }
        }
    }
}
