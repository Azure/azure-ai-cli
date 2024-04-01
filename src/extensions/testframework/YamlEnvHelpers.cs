//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Helpers;
using YamlDotNet.RepresentationModel;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public class YamlEnvHelpers
    {
        public static Dictionary<string, string> GetDefaultEnvironment(bool fromCurrentProcess, string workingDirectory)
        {
            var env = new Dictionary<string, string>();

            if (fromCurrentProcess)
            {
                var environmentFromCurrentProcess = GetCurrentProcessEnvironment();
                foreach (var key in environmentFromCurrentProcess.Keys)
                {
                    env[key] = environmentFromCurrentProcess[key];
                }
            }

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                var environmentFromFile = GetEnvironmentFromDirectory(workingDirectory);
                foreach (var key in environmentFromFile.Keys)
                {
                    env[key] = environmentFromFile[key];
                }
            }

            return env;
        }

        public static Dictionary<string, string> GetEnvironmentFromDirectory(string workingDirectory)
        {
            var envFile = Path.Combine(workingDirectory, ".env");
            return GetEnvironmentFromFile(envFile);
        }

        public static Dictionary<string, string> GetEnvironmentFromFile(string envFile)
        {
            var fileOk = File.Exists(envFile);
            if (!fileOk) return new Dictionary<string, string>();

            var content = File.ReadAllText(envFile);
            return GetEnvironmentFromMultiLineString(content);
        }

        public static Dictionary<string, string> GetEnvironmentFromMultiLineString(string content)
        {
            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            return lines
                .Select(line => line.Split(new[] { '=' }, 2))
                .ToDictionary(parts => parts[0], parts => parts[1]);
        }

        public static Dictionary<string, string> GetCurrentProcessEnvironment()
        {
            var env = Environment.GetEnvironmentVariables();
            return env.Keys.Cast<string>().ToDictionary(key => key, key => env[key]?.ToString() ?? string.Empty);
        }

        public static Dictionary<string, string> UpdateCopyEnvironment(Dictionary<string, string> environment, YamlMappingNode mapping)
        {
            var envNode = mapping.Children.ContainsKey("env") ? mapping.Children["env"] : null;
            if (envNode == null) return environment;

            var asMapping = envNode as YamlMappingNode;
            var asSequence = envNode as YamlSequenceNode;
            if (asMapping == null && asSequence == null) return environment;

            var env = new Dictionary<string, string>(environment);
            if (asMapping != null)
            {
                UpdateEnvironment(env, asMapping.Children);
            }
            else if (asSequence != null)
            {
                foreach (var item in asSequence.Children)
                {
                    var itemAsMapping = item as YamlMappingNode;
                    if (itemAsMapping != null)
                    {
                        UpdateEnvironment(env, itemAsMapping.Children);
                    }
                }
            }

            return env;
        }

        public static Dictionary<string, string> GetNewAndUpdatedEnvironmentVariables(Dictionary<string, string> original, Dictionary<string, string> check)
        {
            var newAndUpdated = new Dictionary<string, string>();
            foreach (var item in check)
            {
                var isNew = !original.ContainsKey(item.Key);
                var isUpdated = !isNew && original[item.Key] != item.Value;
                if (isNew || isUpdated)
                {
                    newAndUpdated.Add(item.Key, item.Value);
                }
            }
            return newAndUpdated;
        }

        private static void UpdateEnvironment(Dictionary<string, string> env, IOrderedDictionary<YamlNode, YamlNode> children)
        {
            foreach (var item in children)
            {
                var key = (item.Key as YamlScalarNode)?.Value;
                var value = (item.Value as YamlScalarNode)?.Value;
                if (key != null && value != null)
                {
                    env[key] = value.Contains("$(") && value.Contains(')')
                        ? ExpandInlineEnvironmentVariables(env, value!)
                        : value!;
                }
            }
        }

        private static string ExpandInlineEnvironmentVariables(Dictionary<string, string> env, string s)
        {
            var sb = new StringBuilder();

            var i = 0;
            while (i < s.Length)
            {
                if (s[i] == '$' && i + 1 < s.Length && s[i+1] == '(')
                {
                    var closeAt = s.IndexOf(')', i + 1);
                    if (closeAt > 0 && closeAt < s.Length)
                    {
                        var nameLen = closeAt - i - 2;
                        var name = s.Substring(i + 2, nameLen);
                        if (env.ContainsKey(name))
                        {
                            sb.Append(env[name]);
                            i += nameLen + 3;
                            continue;
                        }
                    }
                }

                sb.Append(s[i]);
                i++;
            }

            return sb.ToString();

        }
    }
}
