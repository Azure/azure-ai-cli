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

            var expandAtFile = text[i] == '@';
            if (expandAtFile) i++;

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

            // first try to get it from the values
            var str = values[name];

            // if that's not it, check the chat replacements
            if (str == null)
            {
                var check = $"replace.var.{name}";
                if (values.Contains(check))
                {
                    str = values[check];
                }
                else if (values.Names.Any(x => x.StartsWith($"{check}=")))
                {
                    str = values.Names.Where(x => x.StartsWith($"{check}=")).First();
                    str = str.Substring(check.Length + 1);
                }
            }
          
            if (str == null && !expandAtFile)
            {
                return deleteUnresolved ? string.Empty : $"{{{name}}}";
            }
            else if (str == null && expandAtFile)
            {
                str = name;
            }

            if (Program.Debug)
            {
                Console.WriteLine($"*** REPLACED: '{name}' => {str}");
            }

            return expandAtFile ? FileHelpers.ExpandAtFileValue($"@{str}", values) : str;
        }
    }
}
