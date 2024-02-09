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
using Azure.AI.Details.Common.CLI.Extensions.Templates;

namespace Azure.AI.Details.Common.CLI
{
    public class DevCommand : Command
    {
        internal DevCommand(ICommandValues values)
        {
            _values = values.ReplaceValues();
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", false);
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
            StartCommand();

            switch (command)
            {
                case "dev.new": DoNew(); break;
                case "dev.new.list": DoNewList(); break;
                case "dev.shell": DoDevShell(); break;

                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
            }

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private void DoNew()
        {
            var newWhat = string.Join(" ", ArgXToken.GetArgs(_values));
            var language = ProgrammingLanguageToken.Data().GetOrDefault(_values);
            switch (newWhat)
            {
                case ".env": DoNewEnv(); break;
                default: DoNewTemplate(newWhat, language); break;
            }
        }

        private void DoNewEnv()
        {
            var fileName = ".env";

            var env = ConfigEnvironmentHelpers.GetEnvironment(_values);
            var fqn = ConfigEnvironmentHelpers.SaveEnvironment(env, fileName);

            Console.WriteLine($"{fileName} (saved at '{fqn}')\n");
            ConfigEnvironmentHelpers.PrintEnvironment(env);
        }

        private void DoNewTemplate(string templateName, string language)
        {
            var outputDirectory = templateName + ProgrammingLanguageToken.GetSuffix(language);
            var instructions = InstructionsToken.Data().GetOrDefault(_values);

            var found = TemplateFactory.GenerateTemplateFiles(templateName, language, instructions, outputDirectory, _quiet, _verbose);
            CheckGenerateTemplateFileWarnings(templateName, language, found);
        }

        private void DoNewList()
        {
            var newWhat = string.Join(" ", ArgXToken.GetArgs(_values));
            var language = ProgrammingLanguageToken.Data().GetOrDefault(_values);

            TemplateFactory.ListTemplates(newWhat, language);
        }

        private void DoDevShell()
        {
            DisplayBanner("dev.shell");

            var fileName = !OS.IsWindows() ? "bash" : "cmd.exe";
            var arguments = !OS.IsWindows() ? "-li" : "/k PROMPT (ai dev shell) %PROMPT%& title (ai dev shell)";

            Console.WriteLine("Environment populated:\n");

            var env = ConfigEnvironmentHelpers.GetEnvironment(_values);
            ConfigEnvironmentHelpers.PrintEnvironment(env);
            ConfigEnvironmentHelpers.SetEnvironment(env);
            Console.WriteLine();

            var runCommand = RunCommandScriptToken.Data().GetOrDefault(_values);

            // var processOutput = string.IsNullOrEmpty(runCommand)
            //     ? ProcessHelpers.RunShellCommandAsync(fileName, arguments, env, null, null, null, false).Result
            //     : ProcessHelpers.RunShellCommandAsync(runCommand, env, null, null, null, false).Result;

            // var exitCode = processOutput.ExitCode;

            UpdateFileNameArguments(runCommand, ref fileName, ref arguments, out var deleteWhenDone);

            var process = ProcessHelpers.StartProcess(fileName, arguments, env, false);
            process.WaitForExit();

            if (!string.IsNullOrEmpty(deleteWhenDone))
            {
                File.Delete(deleteWhenDone);
            }

            var exitCode = process.ExitCode;
            if (exitCode != 0)
            {
                Console.WriteLine("\n(ai dev shell) FAILED!\n");
                _values.AddThrowError("ERROR:", $"Shell exited with code {exitCode}");
            }
            else
            {
                Console.WriteLine("\n(ai dev shell) exited successfully");
            }
        }

        private static void UpdateFileNameArguments(string runCommand, ref string fileName, ref string arguments, out string? deleteTempFileWhenDone)
        {
            deleteTempFileWhenDone = null;

            if (!string.IsNullOrEmpty(runCommand))
            {
                var isSingleLine = !runCommand.Contains('\n') && !runCommand.Contains('\r');
                if (isSingleLine)
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
                else
                {
                    deleteTempFileWhenDone = Path.GetTempFileName() + (OS.IsWindows() ? ".cmd" : ".sh");
                    File.WriteAllText(deleteTempFileWhenDone, runCommand);

                    fileName = OS.IsLinux() ? "bash" : "cmd.exe";
                    arguments = OS.IsLinux() ? $"-lic \"{deleteTempFileWhenDone}\"" : $"/c \"{deleteTempFileWhenDone}\"";

                    Console.WriteLine($"Running script:\n\n{runCommand}\n");
                }
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

        private void CheckGenerateTemplateFileWarnings(string templateName, string language, object check)
        {
            if (check != null && check is TemplateFactory.Group)
            {
                var group = check as TemplateFactory.Group;
                var groupHasZeroLanguages = string.IsNullOrEmpty(group.Languages);
                var groupHasMultipleLanguages = group.Languages.Contains(',');
                var groupHasOneLanguage = !groupHasZeroLanguages && !groupHasMultipleLanguages;

                var languageSupplied = !string.IsNullOrEmpty(language);
                if (languageSupplied)
                {
                    if (groupHasZeroLanguages || groupHasOneLanguage)
                    {
                        _values.AddThrowError("WARNING:", $"Template '{templateName}' does not support language '{language}'.",
                                                          "",
                                                  "TRY:", $"{Program.Name} dev new {templateName}");
                    }
                    else
                    {
                        _values.AddThrowError("WARNING:", $"Template '{templateName}' doesn't support language '{language}'.",
                                                          "",
                                                  "TRY:", $"{Program.Name} dev new {templateName} --LANGUAGE",
                                                          "",
                                                  "WHERE:", $"LANGUAGE is one of {group.Languages}");
                    }
                }
                else
                {
                    _values.AddThrowError("WARNING:", $"Template '{templateName}' supports multiple languages.",
                                                      "",
                                              "TRY:", $"{Program.Name} dev new {templateName} --LANGUAGE",
                                                      "",
                                            "WHERE:", $"LANGUAGE is one of {group.Languages}");
                }
            }
            if (check == null)
            {
                _values.AddThrowError("WARNING:", $"Template '{templateName}' not found.",
                                                    "",
                                            "TRY:", $"{Program.Name} dev new list");
            }
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
        private readonly bool _quiet;
        private readonly bool _verbose;
    }
}
