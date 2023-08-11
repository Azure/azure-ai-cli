//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Azure.AI.Details.Common.CLI
{
    public static class ValueHelpers
    {
        public static string ReplaceValues(this string text, ICommandValues values, bool deleteUnresolved = false)
        {
            if (string.IsNullOrEmpty(text) || !text.Contains('{')) return text;

            var i = 0;
            var resolved = new StringBuilder();
            while (i < text.Length)
            {
                if (text[i] == '{')
                {
                    resolved.Append(Interpolate(text, ref i, values, deleteUnresolved));
                }
                else if (text[i] == '$' && i + 1 < text.Length && text[i + 1] == '{')
                {
                    i++;
                    resolved.Append(Interpolate(text, ref i, values, deleteUnresolved));
                }
                else
                {
                    resolved.Append(text[i]);
                    i++;
                }
            }

            return resolved.ToString();
        }

        private static string Interpolate(string text, ref int i, ICommandValues values, bool deleteUnresolved)
        {
            if (text[i] != '{') throw new InvalidOperationException($"Interpolate() '{{' not found; pos={i}");
            i += 1; // skipping '{'

            var sb = new StringBuilder();

            while (i < text.Length && text[i] != '}')
            {
                if (text[i] == '{')
                {
                    sb.Append(Interpolate(text, ref i, values, deleteUnresolved));
                }
                else if (text[i] == '$' && i + 1 < text.Length && text[i + 1] == '{')
                {
                    i++;
                    sb.Append(Interpolate(text, ref i, values, deleteUnresolved));
                }
                else
                {
                    sb.Append(text[i]);
                    i++;
                }
            }

            if (text[i] != '}') throw new InvalidOperationException($"Interpolate() '}}' not found; pos={i}");
            i += 1; // skipping '}'

            var name = sb.ToString();

            var str = values[name];
            if (str != null) return str;

            var check = $"chat.replace.value.{name}";
            if (values.Contains(check)) return values[check];

            if (values.Names.Any(x => x.StartsWith($"{check}=")))
            {
                str = values.Names.Where(x => x.StartsWith($"{check}=")).First();
                return str.Substring(check.Length + 1);
            }

            return deleteUnresolved ? string.Empty : $"{{{name}}}";
        }
    }
}
