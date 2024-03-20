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
using System.Collections.Generic;

namespace Azure.AI.Details.Common.CLI
{
    public static class ValueHelpers
    {
        public static string ReplaceValues(this string text, ICommandValues values, bool deleteUnresolved = false)
        {
            if (string.IsNullOrEmpty(text) || !text.Contains('{')) return text;

            var i = 0;
            var sb = new StringBuilder(text);
            while (i < sb.Length)
            {
                if (sb[i] == '{')
                {
                    i += Interpolate(1, sb, i, values, deleteUnresolved);
                }
                else if (sb[i] == '$' && i + 1 < sb.Length && sb[i + 1] == '{')
                {
                    i += Interpolate(2, sb, i, values, deleteUnresolved);
                }
                else
                {
                    i++;
                }
            }

            return sb.ToString();
        }

        private static int Interpolate(int cchPrefix, StringBuilder sb, int start, ICommandValues values, bool deleteUnresolved, int level = 0)
        {
            if (sb[start + cchPrefix - 1] != '{') throw new InvalidOperationException($"Interpolate() '{{' not found; pos={start}");

            var i = cchPrefix;

            while (start + i < sb.Length && sb[start + i] != '}')
            {
                if (sb[start + i] == '{')
                {
                    i += Interpolate(1, sb, start + i, values, deleteUnresolved, level + 1);
                }
                else if (sb[start + i] == '$' && start + i + 1 < sb.Length && sb[start + i + 1] == '{')
                {
                    i += Interpolate(2, sb, start + i, values, deleteUnresolved, level + 1);
                }
                else
                {
                    i++;
                }
            }

            if (sb[start + i] != '}') throw new InvalidOperationException($"Interpolate() '}}' not found; pos={start + i}");
            i += 1; // skipping '}'

            var nameStartsAt = start + cchPrefix;
            var name = sb.ToString().Substring(nameStartsAt, start + i - nameStartsAt - 1);

            // check if name is a well known escape sequence, like \n, \t, etc.
            if (name.Length == 2 && name[0] == '\\' && name[1] == 't')
            {
                sb.Remove(start, name.Length + cchPrefix + 1);
                sb.Insert(start, "\t");
                return 1;
            }
            else if (name.Length == 2 && name[0] == '\\' && name[1] == 'n')
            {
                sb.Remove(start, name.Length + cchPrefix + 1);
                sb.Insert(start, "\n");
                return 1;
            }
            else if (name.Length == 2 && name[0] == '\\' && name[1] == 'r')
            {
                sb.Remove(start, name.Length + cchPrefix + 1);
                sb.Insert(start, "\r");
                return 1;
            }

            var expandAtFile = name.StartsWith('@');
            if (expandAtFile)
            {
                name = name.Substring(1);
            }

            // next, try to get it from the values
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
                str = deleteUnresolved ? string.Empty : $"{{{name}}}";
                sb.Remove(start, name.Length + cchPrefix + 1);
                sb.Insert(start, str);
                return str.Length;
            }

            if (str == null && expandAtFile)
            {
                str = name;
            }

            str = expandAtFile ? FileHelpers.ExpandAtFileValue($"@{str}", values) : str;
            sb.Remove(start, name.Length + cchPrefix + 1 + (expandAtFile ? 1 : 0));
            sb.Insert(start, str);
            return 0;
        }
    }
}
