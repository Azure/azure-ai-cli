//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Azure.AI.Details.Common.CLI
{
    public interface INamedValues
    {
        void Add(string name, string value);
        bool Contains(string name, bool checkDefault = true);
        string Get(string name, bool checkDefault = true);

        void Reset(string name, string value = null);

        string this[string name] { get; }
        IEnumerable<string> Names { get; }
    }

    public static class NamedValueExtensions
    {
        public static string GetOrDefault(this INamedValues values, string name, string defaultValue)
        {
            var value = values[name];
            return !string.IsNullOrEmpty(value) ? value : defaultValue;
        }

        public static int GetOrDefault(this INamedValues values, string name, int defaultValue)
        {
            var value = values[name];
            var returnValue = defaultValue;
            return !string.IsNullOrEmpty(value) && int.TryParse(value, out returnValue) ? returnValue : defaultValue;
        }

        public static bool GetOrDefault(this INamedValues values, string name, bool defaultValue)
        {
            var value = values[name];
            var returnValue = defaultValue;
            return !string.IsNullOrEmpty(value) && bool.TryParse(value, out returnValue) ? returnValue : defaultValue;
        }

        public static string DemandGetOrDefault(this INamedValues values, string name, string defaultValue, string error)
        {
            var value = values.GetOrDefault(name, defaultValue);
            if (string.IsNullOrEmpty(value))
            {
                values.Add("error", error);
                throw new Exception(values["error"]);
            }
            return value;
        }

        public static string ReplaceValues(this string s, INamedValues values)
        {
            if (s == null || !s.Contains("{") || !s.Contains("}")) return s;
            if (values is ICommandValues) return s.ReplaceValues(values as ICommandValues);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                var ch = s[i];
                int j = ch == '{' ? s.IndexOf('}', i + 1) : 0;
                if (j <= i)
                {
                    sb.Append(ch);
                    continue;
                }

                var lookup = s.Substring(i + 1, j - i - 1);
                sb.Append(values.GetOrDefault(lookup, s.Substring(i, j - i + 1)));
                i = j;
            }

            return sb.ToString();
        }

        public static T ReplaceValues<T>(this T values) where T : INamedValues
        {
            foreach (var name in values.Names)
            {
                var value0 = values[name];
                var value1 = value0.ReplaceValues(values);
                if (value0 != value1)
                {
                    values.Reset(name, value1);
                }
            }

            return values;
        }

        public static string SaveAs(this INamedValues values, string fileName = null)
        {
            return values.SaveAs(values.Names, fileName);
        }

        public static string SaveAs(this INamedValues values, IEnumerable<string> names, string fileName = null)
        {
            fileName = fileName == null
                ? Path.GetTempFileName()
                : FileHelpers.GetOutputDataFileName(fileName, values);

            string allFileNames = fileName;

            List<string> lines = new List<string>();
            foreach (var name in names)
            {
                var value = values[name];
                if (value.Contains('\n') || value.Contains('\t'))
                {
                    var additionalFile = fileName + "." + name;
                    FileHelpers.WriteAllText(additionalFile, value, Encoding.UTF8);
                    lines.Add($"{name}=@{additionalFile}");
                    allFileNames = allFileNames + ";" + additionalFile;
                }
                else
                {
                    lines.Add($"{name}={values[name]}");
                }
            }

            FileHelpers.WriteAllLines(fileName, lines, new UTF8Encoding(false));
            return allFileNames;
        }

        public static string AddError(this INamedValues values, string warningOrErrorLabel, string warningOrError, params string[] extra)
        {
            var error = ErrorHelpers.CreateMessage(warningOrErrorLabel, warningOrError, extra);
            values.Add("error", error);
            return error;
        }

        public static void AddThrowError(this INamedValues values, string warningOrErrorLabel, string warningOrError, params string[] extra)
        {
            var error = values.AddError(warningOrErrorLabel, warningOrError, extra);
            throw new Exception(error);
        }

        public static void AddDisplayHelpRequest(this INamedValues values)
        {
            values.Add("display.help", "true");
        }

        public static void AddDisplayVersionRequest(this INamedValues values)
        {
            values.Add("display.version", "true");
        }

        public static void AddExpandHelpRequest(this INamedValues values, bool value = true)
        {
            values.Add("display.help.expand", value ? "true" : "false");
        }

        public static void AddDumpHelpRequest(this INamedValues values, bool value = true)
        {
            values.Add("display.help.dump", value ? "true" : "false");
        }

        public static bool DisplayVersionRequested(this INamedValues values)
        {
            return values.GetOrDefault("display.version", false);
        }

        public static bool DisplayUpdateRequested(this INamedValues values)
        {
            return values.GetOrDefault("display.update", false);
        }

        public static bool DisplayHelpRequested(this INamedValues values)
        {
            return values.GetOrDefault("display.help", false);
        }

        public static bool ExpandHelpRequested(this INamedValues values)
        {
            return values.GetOrDefault("display.help.expand", false);
        }

        public static bool DumpHelpRequested(this INamedValues values)
        {
            return values.GetOrDefault("display.help.dump", false);
        }

        public static string GetCommand(this INamedValues values, string defaultValue = "")
        {
            return values.GetOrDefault("x.command", defaultValue);
        }

        public static string GetCommandRoot(this INamedValues values, string defaultValue = "")
        {
            return values.GetCommand(defaultValue).Split('.').FirstOrDefault();
        }

        public static string GetCommandForDisplay(this INamedValues values)
        {
            return values.GetCommand().Replace('.', ' ');
        }
    }

    public class NamedValues : INamedValues
    {
        public void Add(string name, string value)
        {
            var exists = _values.ContainsKey(name);
            var current = exists ? _values[name] : null;

            var matches = exists && current == value;
            if (matches) return;

            var remove = exists && string.IsNullOrWhiteSpace(current);
            if (remove) _values.Remove(name);

            _values.Add(name, value);
            _names.Add(name);
        }

        public bool Contains(string name, bool checkDefault = true)
        {
            return _values.ContainsKey(name);
        }

        public string Get(string name, bool checkDefault = true)
        {
            return Contains(name, checkDefault) ? _values[name] : null;
        }

        public void Reset(string name, string value = null)
        {
            _values.Remove(name);
            _names.Remove(name);
            if (value != null)
            {
                Add(name, value);
            }
        }

        public string this[string name]
        {
            get
            {
                return Get(name, true);
            }
        }

        public IEnumerable<string> Names
        {
            get
            {
                return _names;
            }
        }

        private Dictionary<string, string> _values = new Dictionary<string, string>();
        private List<string> _names = new List<string>();
    }
}
