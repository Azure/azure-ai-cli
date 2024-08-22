//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Collections.Generic;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public static class PropertyInterpolationHelpers
    {
        public static string Interpolate(string text, Dictionary<string, string> properties, bool escapeJson = false)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (!text.Contains("${{") || !text.Contains("}}")) return text;

            var index = 0;
            var start = text.IndexOf("${{", index);
            while (start >= 0)
            {
                var end = text.IndexOf("}}", start);
                if (end < 0) break;

                var key = text.Substring(start + 3, end - start - 3).Trim();
                if (properties.ContainsKey(key))
                {
                    var value = properties[key];
                    if (escapeJson) value = EscapeJson(value);
                    text = text.Substring(0, start) + value + text.Substring(end + 2);
                }
                else if (key.StartsWith("matrix."))
                {
                    key = key.Substring(7);
                    if (properties.ContainsKey(key))
                    {
                        var value = properties[key];
                        if (escapeJson) value = EscapeJson(value);
                        text = text.Substring(0, start) + value + text.Substring(end + 2);
                    }
                }

                index = start + 1;
                start = text.IndexOf("${{", index);
            }

            return text;
        }

        private static string EscapeJson(string text)
        {
            var asJsonString = System.Text.Json.JsonSerializer.Serialize(text);
            return asJsonString[1..^1];
        }
    }
}
