//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Text;
using System.Text.Json;

namespace Azure.AI.Details.Common.CLI.ConsoleGui
{
    public class InOutPipeServer
    {
        public static bool IsInOutPipeServer => _inOutPipeServer != null;

        public static string? GetInputFromUser(string prompt, string? value = null)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions{ Indented = false });
            writer.WriteStartObject();

            writer.WriteString("prompt", prompt);
            if (value != null)
            {
                writer.WriteString("value", value);
            }

            writer.WriteEndObject();
            writer.Flush();

            var json = Encoding.UTF8.GetString(stream.ToArray());
            Console.WriteLine($"\n{_inOutPipeServer} GetInputFromUser {json}");

            return Console.ReadLine();
        }

        public static int GetSelectionFromUser(string[] items, int selected = 0)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions{ Indented = false });
            writer.WriteStartObject();
            writer.WriteStartArray("items");
            foreach (var line in items)
            {
                writer.WriteStringValue(line);
            }
            writer.WriteEndArray();
            writer.WriteNumber("selected", selected);
            writer.WriteEndObject();
            writer.Flush();

            var json = Encoding.UTF8.GetString(stream.ToArray());
            Console.WriteLine($"\n{_inOutPipeServer} GetSelectionFromUser {json}");

            var response = Console.ReadLine();
            if (response == null) return -1;

            var responseOk = int.TryParse(response, out var index) && index >= 0 && index < items.Length;
            return responseOk ? index : -1;
        }
        
        private static string? _inOutPipeServer = Environment.GetEnvironmentVariable("AZURE_AI_CLI_IN_OUT_PIPE_SERVER");
    }
}
