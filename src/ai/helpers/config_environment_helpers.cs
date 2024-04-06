//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Azure.AI.Details.Common.CLI
{
    public class ConfigEnvironmentHelpers
    {
        public static Dictionary<string, string> GetEnvironment(INamedValues values)
        {
            var env = new Dictionary<string, string>();
            env.Add("AZURE_SUBSCRIPTION_ID", ReadConfig(values, "subscription"));
            env.Add("AZURE_RESOURCE_GROUP", ReadConfig(values, "group"));
            env.Add("AZURE_AI_PROJECT_NAME", ReadConfig(values, "project"));
            env.Add("AZURE_AI_RESOURCE_NAME", ReadConfig(values, "resource"));

            env.Add("AZURE_OPENAI_ENDPOINT", ReadConfig(values, "chat.endpoint"));
            env.Add("AZURE_OPENAI_KEY", ReadConfig(values, "chat.key"));
            env.Add("AZURE_OPENAI_API_KEY", ReadConfig(values, "chat.key"));
            env.Add("AZURE_OPENAI_API_VERSION", ChatCommand.GetOpenAIClientVersionNumber());

            env.Add("AZURE_OPENAI_CHAT_DEPLOYMENT", ReadConfig(values, "chat.deployment"));
            env.Add("AZURE_OPENAI_EVALUATION_DEPLOYMENT", ReadConfig(values, "chat.evaluation.model.deployment.name") ?? ReadConfig(values, "chat.deployment"));
            env.Add("AZURE_OPENAI_EMBEDDING_DEPLOYMENT", ReadConfig(values, "search.embedding.model.deployment.name"));

            env.Add("AZURE_OPENAI_CHAT_MODEL", ReadConfig(values, "chat.model"));
            env.Add("AZURE_OPENAI_EVALUATION_MODEL", ReadConfig(values, "chat.evaluation.model.name") ?? ReadConfig(values, "chat.model"));
            env.Add("AZURE_OPENAI_EMBEDDING_MODEL", ReadConfig(values, "search.embedding.model.name"));

            env.Add("AZURE_AI_SEARCH_ENDPOINT", ReadConfig(values, "search.endpoint"));
            env.Add("AZURE_AI_SEARCH_INDEX_NAME", ReadConfig(values, "search.index.name"));
            env.Add("AZURE_AI_SEARCH_KEY", ReadConfig(values, "search.key"));

            env.Add("AZURE_AI_SPEECH_ENDPOINT", ReadConfig(values, "speech.endpoint"));
            env.Add("AZURE_AI_SPEECH_KEY", ReadConfig(values, "speech.key"));
            env.Add("AZURE_AI_SPEECH_REGION", ReadConfig(values, "speech.region"));

            // Add "non-standard" AZURE_AI_" prefixed env variables to interop with various SDKs

            // Cognitive Search SDK
            env.Add("AZURE_COGNITIVE_SEARCH_TARGET", env["AZURE_AI_SEARCH_ENDPOINT"]);
            env.Add("AZURE_COGNITIVE_SEARCH_KEY", env["AZURE_AI_SEARCH_KEY"]);

            return env.Where(x => !string.IsNullOrEmpty(x.Value)).ToDictionary(x => x.Key, x => x.Value);
        }

        public static void SetEnvironment(Dictionary<string, string> env)
        {
            foreach (var item in env)
            {
                Environment.SetEnvironmentVariable(item.Key, item.Value);
            }
        }

        public static string SaveEnvironment(Dictionary<string, string> env, string fileName)
        {
            var items = env.ToList();
            items.Sort((x, y) => x.Key.CompareTo(y.Key));

            var sb = new StringBuilder();
            foreach (var item in items)
            {
                sb.AppendLine($"{item.Key}={item.Value}");
            }

            FileHelpers.WriteAllText(fileName, sb.ToString(), Encoding.Default);
            return new FileInfo(FileHelpers.DemandFindFileInDataPath(fileName, null, fileName)).DirectoryName;
        }

        public static void PrintEnvironment(Dictionary<string, string> env)
        {
            var items = env.ToList();
            items.Sort((x, y) => x.Key.CompareTo(y.Key));

            foreach (var item in items)
            {
                var value = item.Key.EndsWith("_KEY")
                    ? item.Value.Substring(0, 4) + "****************************"
                    : item.Value;
                Console.WriteLine($"  {item.Key} = {value}");
            }
        }

        private static string ReadConfig(INamedValues values, string name)
        {
            return FileHelpers.FileExistsInConfigPath(name, values)
                ? FileHelpers.ReadAllText(FileHelpers.DemandFindFileInConfigPath(name, values, "configuration"), Encoding.UTF8)
                : null;
        }

    }
}
