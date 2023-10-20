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
    public class DevCommand : Command
    {
        internal DevCommand(ICommandValues values)
        {
            _values = values.ReplaceValues();
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", true);
        }

        internal bool RunCommand()
        {
            try
            {
                RunDevCommand();
            }
            catch (WebException ex)
            {
                ConsoleHelpers.WriteLineError($"\n  ERROR: {ex.Message}");
                JsonHelpers.PrintJson(HttpHelpers.ReadWriteJson(ex.Response, _values, "dev"));
            }

            return _values.GetOrDefault("passed", true);
        }

        private bool RunDevCommand()
        {
            DoCommand(_values.GetCommand());
            return _values.GetOrDefault("passed", true);
        }

        private void DoCommand(string command)
        {
            CheckPath();

            switch (command)
            {
                case "dev.new.env": DoNewEnv(); break;
                case "dev.new": DoNew(); break;
                case "dev.shell": DoDevShell(); break;

                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
            }
        }

        private void DoNew()
        {
            _values.AddThrowError("WARNING:", $"''ai dev new' NOT YET IMPLEMENTED!!");
        }

        private void DoNewEnv()
        {
            var fileName = ".env";

            var env = GetEnvironment();
            var fqn = SaveEnvironment(env, fileName);

            Console.WriteLine($"{fileName} (saved at '{fqn}')\n");
            PrintEnvironment(env);
        }

        private void DoDevShell()
        {
            DisplayBanner("dev.shell");

            var fileName = OS.IsLinux() ? "bash" : "cmd.exe";
            var arguments = OS.IsLinux() ? "-li" : "/k PROMPT (ai dev shell) %PROMPT%& title (ai dev shell)";

            Console.WriteLine("Environment populated:\n");

            var env = GetEnvironment();
            PrintEnvironment(env);
            SetEnvironment(env);
            Console.WriteLine();

            var runCommand = RunCommandToken.Data().GetOrDefault(_values);
            UpdateFileNameArguments(runCommand, ref fileName, ref arguments);

            var process = ProcessHelpers.StartProcess(fileName, arguments, env, false);
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Console.WriteLine("\n(ai dev shell) FAILED!\n");
                _values.AddThrowError("ERROR:", $"Shell exited with code {process.ExitCode}");
            }
            else
            {
                Console.WriteLine("\n(ai dev shell) exited successfully");
            }
        }

        private static void UpdateFileNameArguments(string runCommand, ref string fileName, ref string arguments)
        {
            if (!string.IsNullOrEmpty(runCommand))
            {
                var parts = runCommand.Split(new char[] { ' ' }, 2);
                var inPath = FileHelpers.FileExistsInOsPath(parts[0]) || (OS.IsWindows() && FileHelpers.FileExistsInOsPath(parts[0] + ".exe"));

                var filePart = parts[0];
                var argsPart = parts.Length == 2 ? parts[1] : null;

                fileName = inPath ? filePart : fileName;
                arguments = inPath ? argsPart : (OS.IsLinux()
                    ? $"-lic \"{runCommand}\""
                    : $"/c \"{runCommand}\"");

                Console.WriteLine($"Running command: {runCommand}\n");
            }
        }

        private string ReadConfig(string name)
        {
            return FileHelpers.FileExistsInConfigPath(name, _values)
                ? FileHelpers.ReadAllText(FileHelpers.DemandFindFileInConfigPath(name, _values, "configuration"), Encoding.UTF8)
                : null;
        }

        private Dictionary<string, string> GetEnvironment()
        {
            var env = new Dictionary<string, string>();
            env.Add("AZURE_SUBSCRIPTION_ID", ReadConfig("subscription"));
            env.Add("AZURE_RESOURCE_GROUP", ReadConfig("group"));
            env.Add("AZURE_AI_PROJECT_NAME", ReadConfig("project"));
            env.Add("AZURE_AI_HUB_NAME", ReadConfig("hub"));

            env.Add("AZURE_OPENAI_CHAT_DEPLOYMENT", ReadConfig("chat.deployment"));
            env.Add("AZURE_OPENAI_EVALUATION_DEPLOYMENT", ReadConfig("chat.evaluation.model.deployment.name") ?? ReadConfig("chat.deployment"));
            env.Add("AZURE_OPENAI_EMBEDDING_DEPLOYMENT", ReadConfig("search.embedding.model.deployment.name"));

            env.Add("AZURE_OPENAI_CHAT_MODEL", ReadConfig("chat.model"));
            env.Add("AZURE_OPENAI_EVALUATION_MODEL", ReadConfig("chat.evaluation.model.name") ?? ReadConfig("chat.model"));
            env.Add("AZURE_OPENAI_EMBEDDING_MODEL", ReadConfig("search.embedding.model.name"));

            env.Add("AZURE_AI_SEARCH_ENDPOINT", ReadConfig("search.endpoint"));
            env.Add("AZURE_AI_SEARCH_INDEX_NAME", ReadConfig("search.index.name"));
            env.Add("AZURE_AI_SEARCH_KEY", ReadConfig("search.key"));

            // Add "non-standard" AZURE_AI_" prefixed env variables to interop with various SDKs

            // OpenAI's SDK
            env.Add("OPENAI_ENDPOINT", ReadConfig("chat.endpoint"));
            env.Add("OPENAI_API_BASE", ReadConfig("chat.endpoint"));
            env.Add("OPENAI_API_KEY", ReadConfig("chat.key"));
            env.Add("OPENAI_API_TYPE", "azure");
            env.Add("OPENAI_API_VERSION", ChatCommand.GetOpenAIClientVersionNumber());

            // Cognitive Search SDK
            env.Add("AZURE_COGNITIVE_SEARCH_TARGET", env["AZURE_AI_SEARCH_ENDPOINT"]);
            env.Add("AZURE_COGNITIVE_SEARCH_KEY", env["AZURE_AI_SEARCH_KEY"]);

            return env.Where(x => !string.IsNullOrEmpty(x.Value)).ToDictionary(x => x.Key, x => x.Value);
        }

        private static void SetEnvironment(Dictionary<string, string> env)
        {
            foreach (var item in env)
            {
                Environment.SetEnvironmentVariable(item.Key, item.Value);
            }
        }

        private string SaveEnvironment(Dictionary<string, string> env, string fileName)
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

        private static void PrintEnvironment(Dictionary<string, string> env)
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

        private void DisplayBanner(string which)
        {
            if (_quiet) return;

            var logo = FileHelpers.FindFileInHelpPath($"help/include.{Program.Name}.{which}.ascii.logo");
            if (!string.IsNullOrEmpty(logo))
            {
                var text = FileHelpers.ReadAllHelpText(logo, Encoding.UTF8);
                ConsoleHelpers.WriteLineWithHighlight(text);
            }
        }

        private readonly bool _quiet;
        private readonly bool _verbose;
    }
}
