//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Text;
using System.Text.Json;
using Azure.AI.Details.Common.CLI;

namespace Azure.AI.Details.Common.CLI.Extensions.ONNX;

public static class ONNXGenAIHelpers
{
    public static void ReadChatHistoryFromFile(this List<ContentMessage> messages, string fileName)
    {
        var historyFile = FileHelpers.ReadAllText(fileName, Encoding.UTF8);
        var historyFileLines = historyFile.Split(Environment.NewLine);
        foreach (var line in historyFileLines)
        {
            var jsonObject = JsonDocument.Parse(line);
            if (!jsonObject.RootElement.TryGetProperty("role", out var roleElement) ||
                !jsonObject.RootElement.TryGetProperty("content", out var contentElement))
            {
                continue;
            }

            var role = roleElement.GetString();
            var content = contentElement.GetString();
            if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(content))
            {
                continue;
            }

            if (content == "system")
            {
                messages.Clear();
            }

            var message = new ContentMessage { Role = role, Content = content };
            messages.Add(message!);
        }
    }

    public static void SaveChatHistoryToFile(this IList<ContentMessage> messages, string fileName)
    {
        var history = new StringBuilder();

        foreach (var message in messages)
        {
            var role = message.Role;
            var content = EscapeJson(message.Content);
            var messageText = ($"{{\"role\":\"{role}\",\"content\":\"{content}\"}}");
            history.AppendLine(messageText);
        }

        FileHelpers.WriteAllText(fileName, history.ToString(), Encoding.UTF8);
    }

    private static string EscapeJson(string text)
    {
        var asJsonString = System.Text.Json.JsonSerializer.Serialize(text);
        return asJsonString[1..^1];
    }
}
