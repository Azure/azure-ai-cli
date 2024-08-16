//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Azure.AI.Inference;
using System.Text;
using System.ClientModel.Primitives;
using System.Text.Json;

namespace Azure.AI.Details.Common.CLI.Extensions.Inference;

public static class AzureInferenceHelpers
{
        public static void ReadChatHistoryFromFile(this List<ChatRequestMessage> messages, string fileName)
        {
            var historyFile = FileHelpers.ReadAllText(fileName, Encoding.UTF8);

            var historyFileLines = historyFile.Split(Environment.NewLine);
            var clearIfSystem = () =>
            {
                messages.Clear();
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
                    "system" => clearIfSystem(),
                    "tool" => typeof(ChatRequestToolMessage),
                    _ => throw new Exception($"Unknown chat role {role}")
                };

                var message = ModelReaderWriter.Read(BinaryData.FromString(line), type, ModelReaderWriterOptions.Json) as ChatRequestMessage;
                messages.Add(message!);
            }
        }

    public static void SaveChatHistoryToFile(this IList<ChatRequestMessage> messages, string fileName)
    {
        var history = new StringBuilder();

        foreach (var message in messages)
        {
            var messageText = message switch
            {
                ChatRequestUserMessage userMessage => ModelReaderWriter.Write(userMessage, ModelReaderWriterOptions.Json).ToString(),
                ChatRequestAssistantMessage assistantMessage => ModelReaderWriter.Write(assistantMessage, ModelReaderWriterOptions.Json).ToString(),
                ChatRequestSystemMessage systemMessage => ModelReaderWriter.Write(systemMessage, ModelReaderWriterOptions.Json).ToString(),
                ChatRequestToolMessage toolMessage => ModelReaderWriter.Write(toolMessage, ModelReaderWriterOptions.Json).ToString(),
                _ => null
            };

            if (!string.IsNullOrEmpty(messageText))
            {
                history.AppendLine(messageText);
            }
        }

        FileHelpers.WriteAllText(fileName, history.ToString(), Encoding.UTF8);
    }
}
