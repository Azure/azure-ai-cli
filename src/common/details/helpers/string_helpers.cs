//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Azure.AI.Details.Common.CLI
{
    public class StringHelpers
    {
        public static string PrependOrEmpty(string prepend, string value)
        {
            return !string.IsNullOrEmpty(value)
                ? $"{prepend}{value}"
                : "";
        }

        public static bool SplitNameValue(string nameEqValue, out string name, out string value)
        {
            var eqPos = Math.Max(0, nameEqValue.IndexOf('='));
            name = nameEqValue.Substring(0, eqPos);
            value = eqPos + 1 < nameEqValue.Length ? nameEqValue.Substring(eqPos + 1) : "";
            return !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value);
        }

        public static string EscapeControlChars(string value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char ch in value)
            {
                sb.Append(ch < ' ' ? $"\\{(int)ch}" : $"{ch}");
            }
            return sb.ToString();
        }

        public static IEnumerable<string> WhereLeftRight(IEnumerable<string> strings, string left, string right)
        {
            return strings.Where(x => x.StartsWith(left) && x.EndsWith(right));
        }

        public static IEnumerable<string> WhereLeftRightTrim(IEnumerable<string> strings, string left, string right)
        {
            strings = WhereLeftRight(strings, left, right);
            return strings.Select(x => x.Remove(x.LastIndexOf(right)).Remove(0, left.Length));
        }

        public static bool ContainsAllCharsInOrder(string s, string chars)
        {
            return ContainsAllCharsInOrder(s, chars, out int _);
        }

        public static bool ContainsAllCharsInOrder(string s, string chars, out int index, out int width)
        {
            if (!ContainsAllCharsInOrder(s, chars, out int iSLastChar))
            {
                index = width = -1;
                return false;
            }

            var iS = iSLastChar;
            var iChars = chars.Length - 1;
            while (iChars >= 0 && iS >= 0)
            {
                while (s[iS] != chars[iChars])
                {
                    iS--;
                }
                iS--;
                iChars--;
            }
            iS++;

            index = iS;
            width = iSLastChar - iS + 1;

            return true;
        }

        private static bool ContainsAllCharsInOrder(string s, string chars, out int lastCharAt)
        {
            lastCharAt = 0;
            foreach (var ch in chars)
            {
                lastCharAt = s.IndexOf(ch, lastCharAt);
                if (lastCharAt < 0) return false;
                lastCharAt++;
            }
            lastCharAt--;
            return lastCharAt >= 0;
        }

        public static bool UpdateNeeded(string[] current, string[] latest)
        {
            return Int32.Parse(current[0]) < Int32.Parse(latest[0])
                || Int32.Parse(current[1]) < Int32.Parse(latest[1])
                || Int32.Parse(current[2]) < Int32.Parse(latest[2]);
        }
    }

    public static class StringExtensions
    {
        public static void ExtendSplitItems(this List<string> ids, char c)
        {
            var containsSemicolon = ids.FirstOrDefault(x => x.Contains(c));
            while (containsSemicolon != null)
            {
                ids.Remove(containsSemicolon);

                var items = containsSemicolon.Split(c, StringSplitOptions.RemoveEmptyEntries);
                ids.AddRange(items);

                containsSemicolon = ids.FirstOrDefault(x => x.Contains(c));
            }
        }

        public static string IfTrimStartsWith(this string s, string value, Action<string> action)
        {
            if (s.StartsWith(value))
            {
                s = s.Substring(value.Length).TrimStart();
                action(s);
            }
            return s;
        }

        public static string IfTrimEndsWith(this string s, string value, Action<string> action)
        {
            if (s.EndsWith(value))
            {
                s = s.Substring(0, s.Length - value.Length).TrimEnd();
                action(s);
            }
            return s;
        }

        public static string IfTrim(this string s, string value, Action<string> action)
        {
            return s.IfTrimStartsWith(value, action).IfTrimEndsWith(value, action);
        }
    }
}
