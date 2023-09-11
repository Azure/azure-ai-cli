//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.Memory.AzureCognitiveSearch;
using Azure.AI.Details.Common.CLI.ConsoleGui;

namespace Azure.AI.Details.Common.CLI
{
    public class ScenarioWizardCommand : Command
    {
        internal ScenarioWizardCommand(ICommandValues values)
        {
            _values = values.ReplaceValues();
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", true);
        }

        internal bool RunCommand()
        {
            RunScenarioWizardCommand().Wait();
            return _values.GetOrDefault("passed", true);
        }

        private async Task<bool> RunScenarioWizardCommand()
        {
            try
            {
                await DoCommand(_values.GetCommand());
                return _values.GetOrDefault("passed", true);
            }
            catch (ApplicationException)
            {
                Console.WriteLine();
                _values.Reset("passed", "false");
                return false;
            }
        }

        private async Task DoCommand(string command)
        {
            CheckPath();

            switch (command)
            {
                case "wizard": await DoScenario(); break;
            }
        }

        private async Task DoScenario()
        {
            var interactive = _values.GetOrDefault("scenario.wizard.interactive", true);
            await DoScenario(interactive);
        }

        private async Task DoScenario(bool interactive)
        {
            DisplayScenarioBanner();

            Console.Write("\rName: *** Loading choices ***");

            var scenarios = GetScenarios();
            Console.Write("\rName: ");

            var scenario = ListBoxPicker.PickString(scenarios);
            if (scenario == null) return;
            Console.WriteLine($"\rName: {scenario} ");

            var actions = GetActions(scenario);
            var action = (ScenarioAction)null;
            if (actions.Count(x => !string.IsNullOrEmpty(x.Action)) > 0)
            {
                Console.Write($"\rTask: ");

                var index = ListBoxPicker.PickIndexOf(actions.Select(x => x.ActionWithPrefix).ToArray());
                if (index < 0) return;
                action = actions[index];

                Console.WriteLine($"\rTask: {action.Action.Trim()}\n");
            }

            if (scenario.ToLower().Contains("your data") && action.Action.ToLower().Contains("quickstart"))
            {
                await ChatWithYourDataScenarioAsync(scenario);
            }
            else if (scenario.ToLower().StartsWith("chat") && action.Action.ToLower().Contains("quickstart"))
            {
                SimpleChatScenario(scenario);
            }
            else if (action.InvokeAction != null)
            {
                action.InvokeAction(action);
            }
            else
            {
                ConsoleHelpers.WriteLineError("NOT YET IMPLEMENTED");
            }
        }

        private async Task ChatWithYourDataScenarioAsync(string scenario)
        {
            StartCommand();

            var subscription = await AzCliConsoleGui.PickSubscriptionAsync(true);
            var openAiResource = await AzCliConsoleGui.InitAndConfigOpenAiResource(true, subscription.Id);
            var cogSearchResource = await AzCliConsoleGui.InitAndConfigCogSearchResource(subscription.Id, openAiResource.RegionLocation, openAiResource.Group);
            // var aiHubResource = await AiSdkConsoleGui.PickOrCreateAiHubResource(_values, subscription.Id);
            // var aiHubProject = AiSdkConsoleGui.InitAndConfigAiHubProject(_values, subscription.Id, aiHubResource.Id, openAiResource.Group, openAiResource.Endpoint, openAiResource.Key, cogSearchResource.Endpoint, cogSearchResource.Key);

            ConsoleHelpers.WriteLineWithHighlight("\n`CONFIG AI SCENARIO DATA`");

            Console.Write("\rWhere: ");
            var source = ListBoxPicker.PickString(GetDataLocationChoices());
            Console.WriteLine($"\rWhere: {source}");

            if (!source.Contains("files"))
            {
                Console.WriteLine();
                ConsoleHelpers.WriteLineError($"SCENARIO: {scenario} w/ source={source} is not yet implemented.");
                return;
            }

            var path = AskPrompt("Files: ");
            var fi = new FileInfo(path);
            var files = Directory.Exists(path)
                ? Directory.GetFiles(path)
                : Directory.GetFiles(fi.DirectoryName, fi.Name);
            Console.WriteLine($"Found: {files.Count()}");

            ConsoleHelpers.WriteLineWithHighlight("\n`UPDATE COGNITIVE SEARCH INDEX`");

            var indexName = AskPrompt("Search Index Name: ");
            Console.WriteLine();

            Console.WriteLine("*** UPDATING ***");

            var kernel = CreateSemanticKernel(cogSearchResource.Endpoint, cogSearchResource.Key, openAiResource.Endpoint, openAiResource.EmbeddingsDeployment, openAiResource.Key);
            await StoreMemoryAsync(kernel, indexName, files.Select(x => new KeyValuePair<string, string>(x, FileHelpers.ReadAllHelpText(x, Encoding.UTF8))));

            Console.Write("\r*** UPDATED ***  ");
            Console.WriteLine();

            var systemPrompt = AskChatSystemPrompt(_values);
            if (string.IsNullOrEmpty(systemPrompt)) return;

            StartShellAiChatScenario(scenario, $"--index-name {indexName} --system-prompt @prompt.txt").WaitForExit();

            ConsoleHelpers.WriteLineWithHighlight($"\n`NEXT STEPS: {scenario}`");
            Console.WriteLine("");
            Console.WriteLine("  To chat w/ your data as configured here, try:");
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"      ai chat --interactive --index-name \"{indexName}\" --system-prompt @prompt.txt");
            Console.ResetColor();
            Console.WriteLine("");
            Console.WriteLine("  To share with others, try:");
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"      ai chat --interactive --index-name \"{indexName}\" --system-prompt @prompt.txt --zip \"{indexName}.zip\"");
            Console.ResetColor();
            Console.WriteLine("");
            Console.WriteLine("  To generate code, try:");
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"      ai wizard --scenario \"{scenario}\" --code --language C#");
            Console.ResetColor();
            Console.WriteLine("");
            Console.WriteLine("  To chat using public data, try:");
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("      ai wizard --scenario \"Chat (OpenAI)\" --explore");
            Console.ResetColor();
            Console.WriteLine("");

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private void SimpleChatScenario(string scenario)
        {
            Process process = StartShellAiInitProcess("openai");
            if (process.ExitCode == 0)
            {
                var systemPrompt = AskChatSystemPrompt(_values);
                if (string.IsNullOrEmpty(systemPrompt)) return;

                StartShellAiChatScenario(scenario, $"--system-prompt @prompt.txt").WaitForExit();

                ConsoleHelpers.WriteLineWithHighlight($"\n`NEXT STEPS: {scenario}`");
                Console.WriteLine("");
                Console.WriteLine("  To chat as configured here, try:");
                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("      ai chat --interactive --system @prompt.txt");
                Console.ResetColor();
                Console.WriteLine("");
                Console.WriteLine("  To share with others, try:");
                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("      ai chat --interactive --system @prompt.txt --zip chat.zip");
                Console.ResetColor();
                Console.WriteLine("");
                Console.WriteLine("  To generate code, try:");
                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"      ai wizard --scenario \"{scenario}\" --code --language C#");
                Console.ResetColor();
                Console.WriteLine("");
                Console.WriteLine("  To chat w/ your own data (files, etc.), try:");
                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("      ai wizard --scenario \"Chat w/ your data (OpenAI)\"");
                Console.ResetColor();
                Console.WriteLine("");
            }
        }

        private static Process StartShellAiInitProcess(string extraArguments = "")
        {
            var command = "ai";
            var arguments = $"init {extraArguments} --quiet";

            var process = TryCatchHelpers.TryCatchNoThrow<Process>(() => StartShellCommandProcess(command, arguments), null, out Exception processException);
            process?.WaitForExit();

            return process;
        }

        private static Process StartShellAiChatScenario(string scenario, string extraArguments = "")
        {
            ConsoleHelpers.WriteLineWithHighlight($"\r\n`SCENARIO: {scenario}`\n");
            return StartShellAiChatProcess(extraArguments);
        }

        private static Process StartShellAiChatProcess(string extraArguments = "")
        {
            var command = "ai";
            var arguments = $"chat --interactive --quiet {extraArguments}";

            var process = TryCatchHelpers.TryCatchNoThrow<Process>(() => StartShellCommandProcess(command, arguments), null, out var processException);
            process?.WaitForExit();

            return process;
        }

        private static string[] GetDataLocationChoices()
        {
            return new string[] {
                "Upload files",
                "Azure Blob Storage",
                "Azure Cognitive Search"
            };
        }

        private static string[] GetProjects()
        {
            Thread.Sleep(400);
            return new string[]
            {
                "(Create new)",
                "Copilot-project-1",
                "My-2nd-attempt",
            };
        }

        private static void DisplayScenarioBanner()
        {
            var logo = FileHelpers.FindFileInHelpPath($"help/include.{CLIContext.Name}.wizard.ascii.logo")
                    ?? FileHelpers.FindFileInHelpPath($"help/include.{CLIContext.Name}.ascii.logo");
            var text = !string.IsNullOrEmpty(logo)
                     ? FileHelpers.ReadAllHelpText(logo, Encoding.UTF8) + "\n"
                     : $"`{CLIContext.Name.ToUpper()} SCENARIO`";
            ConsoleHelpers.WriteLineWithHighlight(text);

            ConsoleHelpers.WriteLineWithHighlight("`SCENARIO`");
        }

        private static string[] GetScenarios()
        {
            Thread.Sleep(400);
            return ScenarioActions.Actions
                .Select(x => x.Scenario)
                .Distinct()
                .ToArray();
        }

        private static ScenarioAction[] GetActions(string scenario)
        {
            return ScenarioActions.Actions
                .Where(x => x.Scenario == scenario)
                .ToArray();
        }

        private static Process StartShellCommandProcess(string command, string arguments)
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            return  isWindows
                ? StartProcess("cmd", $"/c {command} {arguments}")
                : StartProcess(command,  arguments);
        }

        private static Process StartProcess(string fileName, string arguments)
        {
            var start = new ProcessStartInfo(fileName, arguments);
            start.UseShellExecute = false;
            start.RedirectStandardOutput = false;
            start.RedirectStandardError = false;

            return Process.Start(start);
        }

        private static string AskPrompt(string prompt, string value = null, bool useEditBox = false)
        {
            Console.Write(prompt);

            if (useEditBox)
            {
                var normal = new Colors(ConsoleColor.White, ConsoleColor.Blue);
                var text = EditBoxQuickEdit.Edit(40, 1, normal, value, 128);
                ColorHelpers.ResetColor();
                Console.WriteLine(text);
                return text;
            }

            if (!string.IsNullOrEmpty(value))
            {
                Console.WriteLine(value);
                return value;
            }

            return Console.ReadLine();
        }

        private static string AskChatSystemPrompt(INamedValues values)
        {
            var existing = FileHelpers.FindFileInDataPath("chat.prompt.txt", values);
            var defaultSystemPrompt = existing != null
                ? FileHelpers.ReadAllText(existing, Encoding.Default)
                : ChatCommand.DefaultSystemPrompt;

            ConsoleHelpers.WriteLineWithHighlight("\n`CONFIG CHAT SYSTEM PROMPT`");
            Console.WriteLine($"Default: {defaultSystemPrompt}");

            Console.Write("Use default? ");
            var useDefault = ListBoxPickYesNo();
            if (useDefault) return defaultSystemPrompt;

            Console.WriteLine("\nSystem prompt:");
            var sb = new StringBuilder();
            while (true)
            {
                var line = AskPrompt("> ");
                if (string.IsNullOrEmpty(line)) break;
                sb.AppendLine(line);
            }

            var newSystemPrompt = !string.IsNullOrEmpty(sb.ToString())
                ? sb.ToString()
                : ChatCommand.DefaultSystemPrompt;

            ConfigSetHelpers.ConfigSet("chat.prompt.txt", newSystemPrompt);
            return newSystemPrompt;
        }

        private static bool ListBoxPickYesNo()
        {
            var choices = "Yes;No".Split(';').ToArray();
            var picked = ListBoxPicker.PickIndexOf(choices);
            Console.WriteLine(picked switch {
                0 => "Yes",
                1 => "No",
                _ => "Canceled"
            });
            return (picked == 0);
        }

        private IKernel? CreateSemanticKernel(string searchEndpoint, string searchApiKey, string embeddingsEndpoint, string embeddingsDeployment, string embeddingsApiKey)
        {
            var store = new AzureCognitiveSearchMemoryStore(searchEndpoint, searchApiKey);
            var kernelWithACS = Kernel.Builder
                .WithAzureTextEmbeddingGenerationService(embeddingsDeployment, embeddingsEndpoint, embeddingsApiKey)
                .WithMemoryStorage(store)
                .Build();

            return kernelWithACS;
        }

        private static async Task StoreMemoryAsync(IKernel kernel, string index, IEnumerable<KeyValuePair<string, string>> kvps)
        {
            var list = kvps.ToList();
            if (list.Count() == 0) return;

            foreach (var entry in list)
            {
                await kernel.Memory.SaveInformationAsync(
                    collection: index,
                    text: entry.Value,
                    id: entry.Key);

                 Console.WriteLine($"{entry.Key}: {entry.Value.Length} bytes");
            }
            Console.WriteLine();
        }

        private void StartCommand()
        {
            CheckPath();
            LogHelpers.EnsureStartLogFile(_values);

            // _display = new DisplayHelper(_values);

            // _output = new OutputHelper(_values);
            // _output.StartOutput();

            _lock = new SpinLock();
            _lock.StartLock();
        }

        private void StopCommand()
        {
            _lock.StopLock(5000);

            // LogHelpers.EnsureStopLogFile(_values);
            // _output.CheckOutput();
            // _output.StopOutput();

            _stopEvent.Set();
        }

        private SpinLock _lock = null;
        private readonly bool _quiet = false;
        private readonly bool _verbose = false;
    }
}
