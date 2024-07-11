//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using YamlDotNet.RepresentationModel;

namespace Azure.AI.Details.Common.CLI.TestFramework
{

    public class YamlTestCaseRunner
    {
        public static IEnumerable<TestResult> TestCaseGetResults(TestCase test, string cli, string? command, string? script, bool scriptIsBash, string? arguments, string? input, string? expectGpt, string? expectRegex, string? notExpectRegex, string? env, string? workingDirectory, int timeout, bool skipOnFailure, string? @foreach)
        {
            Logger.Log($"YamlTestCaseRunner.TestCaseGetResults: ENTER");

            foreach (var foreachItem in ExpandForEachGroups(@foreach))
            {
                var result = TestCaseGetResult(test, cli, command, script, scriptIsBash, foreachItem, arguments, input, expectGpt, expectRegex, notExpectRegex, env, workingDirectory!, timeout, skipOnFailure);
                if (!string.IsNullOrEmpty(foreachItem) && foreachItem != "{}")
                {
                    result.DisplayName = GetTestResultDisplayName(test.DisplayName, foreachItem);
                }
                yield return result;
            }

            Logger.Log($"YamlTestCaseRunner.TestCaseGetResults: EXIT");
        }

        #region private methods

        private static TestResult TestCaseGetResult(TestCase test, string cli, string? command, string? script, bool scriptIsBash, string? foreachItem, string? arguments, string? input, string? expectGpt, string? expectRegex, string? notExpectRegex, string? env, string workingDirectory, int timeout, bool skipOnFailure)
        {
            var start = DateTime.Now;

            var outcome = RunTestCase(skipOnFailure, cli, command, script, scriptIsBash, foreachItem, arguments, input, expectGpt, expectRegex, notExpectRegex, env, workingDirectory, timeout, out string stdOut, out string stdErr, out string errorMessage, out string stackTrace, out string additional, out string debugTrace);

            // #if DEBUG
            // additional += outcome == TestOutcome.Failed ? $"\nEXTRA: {ExtraDebugInfo()}" : "";
            // #endif

            var stop = DateTime.Now;
            return CreateTestResult(test, start, stop, stdOut, stdErr, errorMessage, stackTrace, additional, debugTrace, outcome);
        }

        private static string GetTestResultDisplayName(string testDisplayName, string json)
        {
            var testResultDisplayName = testDisplayName;

            var document = JsonDocument.Parse(json);
            if(document.RootElement.ValueKind == JsonValueKind.Object)
            {
                var root = document.RootElement;
                foreach(var property in root.EnumerateObject())
                {
                    var keys = property.Name.Split(new char[] { '\t' });
                    var values = property.Value.GetString()!.Split(new char[] { '\t' });

                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (testResultDisplayName.Contains("{" + keys[i] + "}"))
                        {
                            testResultDisplayName = testResultDisplayName.Replace("{" +keys[i] + "}", values[i]);
                        }
                    }
                }
            }

            // if the testDisplayName was not templatized, ie, it had no {}
            if (testResultDisplayName == testDisplayName)
            {
                return $"{testDisplayName}: {RedactSensitiveDataFromForeachItem(json)}";
            }

            return testResultDisplayName;
        }

        private static string RedactSensitiveDataFromForeachItem(string foreachItem)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions{ Indented = false });
            writer.WriteStartObject();

            var foreachObject = JsonDocument.Parse(foreachItem).RootElement;
            foreach (var item in foreachObject.EnumerateObject())
            {
                if (string.IsNullOrWhiteSpace(item.Value.ToString()))
                {
                    continue;
                }
                var keys = item.Name.ToLower().Split(new char[] {'\t'});
                
                //find index of "token" in foreach key and redact its value to avoid getting it displayed
                var tokenIndex = Array.IndexOf(keys, "token");
                var valueString = item.Value.GetRawText();
                
                if (tokenIndex >= 0)
                {
                    var values = item.Value.ToString().Split(new char[] {'\t'});
                    if (values.Count() == keys.Count())
                    {
                        values[tokenIndex] = "***";
                        valueString = string.Join("\t", values);
                    }
                }
                writer.WritePropertyName(item.Name);
                writer.WriteRawValue(valueString);
            }

            writer.WriteEndObject();

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static IEnumerable<string> ExpandForEachGroups(string? @foreach)
        {
            var kvs = KeyValuePairsFromJson(@foreach, false)
                .Select(kv => new KeyValuePair<string, IEnumerable<string>>(
                    kv.Key,
                    kv.Value.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)));

            var dicts = new[] { new Dictionary<string, string>() }.ToList();
            foreach (var item in kvs)
            {
                var lines = item.Value;
                dicts = lines.SelectMany(
                    line => dicts.Select(
                        d => DupAndAdd(d, item.Key, line)))
                    .ToList();
            }

            Logger.Log($"YamlTestCaseRunner.ExpandForEachGroups: expanded count = {dicts.Count()}");

            return dicts.Select(d =>
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false
                };
                return JsonSerializer.Serialize(d, options);
            });
        }

        private static Dictionary<string, string> DupAndAdd(Dictionary<string, string> d, string key, string value)
        {
            var dup = new Dictionary<string, string>(d);
            dup[key] = value;
            return dup;
        }

        private static TestOutcome RunTestCase(bool skipOnFailure, string cli, string? command, string? script, bool scriptIsBash, string? @foreach, string? arguments, string? input, string? expectGpt, string? expectRegex, string? notExpectRegex, string? env, string workingDirectory, int timeout, out string stdOut, out string stdErr, out string errorMessage, out string stackTrace, out string additional, out string debugTrace)
        {
            var outcome = TestOutcome.None;

            additional = $"START TIME: {DateTime.Now}";
            debugTrace = "";
            stackTrace = script ?? string.Empty;

            List<string>? filesToDelete = null;

            var sbOut = new StringBuilder();
            var sbErr = new StringBuilder();
            var sbMerged = new StringBuilder();

            try
            {
                var kvs = KeyValuePairsFromJson(arguments, true);
                kvs.AddRange(KeyValuePairsFromJson(@foreach, false));
                stackTrace = UpdateStackTrace(stackTrace, command, kvs);
                kvs = ConvertValuesToAtArgs(kvs, ref filesToDelete);

                var useCmd = !scriptIsBash;
                script = WriteTextToTempFile(script, useCmd ? "cmd" : null);

                expectRegex = WriteTextToTempFile(expectRegex);
                notExpectRegex = WriteTextToTempFile(notExpectRegex);

                GetStartInfoArgs(out var startProcess, out var startArgs, cli, command, script, scriptIsBash, kvs, expectRegex, notExpectRegex, ref filesToDelete);
                stackTrace = $"{startProcess} {startArgs}\n{stackTrace ?? string.Empty}";

                Logger.Log($"Process.Start('{startProcess} {startArgs}')");
                var startInfo = new ProcessStartInfo(startProcess, startArgs)
                {
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = workingDirectory
                };
                UpdateEnvironment(startInfo, env);
                UpdatePathEnvironment(startInfo);

                var process = Process.Start(startInfo);
                if (process == null) throw new Exception("Process.Start() returned null!");

                process.StandardInput.WriteLine(input ?? string.Empty);
                process.StandardInput.Close();

                var outDoneSignal = new ManualResetEvent(false);
                var errDoneSignal = new ManualResetEvent(false);
                process.OutputDataReceived += (sender, e) => AppendLineOrSignal(e.Data, sbOut, sbMerged, outDoneSignal);
                process.ErrorDataReceived += (sender, e) => AppendLineOrSignal(e.Data, sbErr, sbMerged, errDoneSignal);
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var exitedNotKilled = WaitForExit(process, timeout);
                outcome = exitedNotKilled && process.ExitCode == 0
                    ? TestOutcome.Passed
                    : skipOnFailure
                        ? TestOutcome.Skipped
                        : TestOutcome.Failed;

                if (exitedNotKilled)
                {
                    outDoneSignal.WaitOne();
                    errDoneSignal.WaitOne();
                }

                var exitCode = exitedNotKilled
                    ? process.ExitCode.ToString()
                    : $"(did not exit; timedout; killed)";
                var exitTime = exitedNotKilled
                    ? process.ExitTime.ToString()
                    : DateTime.UtcNow.ToString();

                errorMessage = $"EXIT CODE: {exitCode}";
                additional = additional
                    + $" STOP TIME: {exitTime}"
                    + $" EXIT CODE: {exitCode}";
            }
            catch (Exception ex)
            {
                outcome = TestOutcome.Failed;
                errorMessage = ex.Message;
                debugTrace = ex.ToString();
                stackTrace = $"{stackTrace}\n{ex.StackTrace}";
            }
            finally
            {
                if (script != null) File.Delete(script);
                if (expectRegex != null) File.Delete(expectRegex);
                if (notExpectRegex != null) File.Delete(notExpectRegex);
                filesToDelete?.ForEach(x => File.Delete(x));
            }

            stdOut = sbOut.ToString();
            stdErr = sbErr.ToString();

            return outcome == TestOutcome.Passed && !string.IsNullOrEmpty(expectGpt)
                ? CheckExpectGptOutcome(sbMerged.ToString(), expectGpt, workingDirectory, ref stdOut, ref stdErr)
                : outcome;
        }

        private static List<KeyValuePair<string, string>> ConvertValuesToAtArgs(List<KeyValuePair<string, string>> kvs, ref List<string>? files)
        {
            var newList = new List<KeyValuePair<string, string>>();
            foreach (var item in kvs)
            {
                if (item.Value.Count(x => x == '\t' || x == '\r' || x == '\n' || x == '\f' || x == '\"') > 0)
                {
                    string file = WriteMultilineTsvToTempFile(item.Value, ref files);
                    newList.Add(new KeyValuePair<string, string>(item.Key, $"@{file}"));
                }
                else
                {
                    newList.Add(item);
                }
            }

            return newList;
        }

        private static List<KeyValuePair<string, string>> KeyValuePairsFromJson(string? json, bool allowSimpleString)
        {
            var kvs = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrEmpty(json))
            {
                Logger.Log($"KeyValuePairsFromJson: 'json'='{json}'");
                using JsonDocument document = JsonDocument.Parse(json);
                var parsed = document.RootElement;
                if (parsed.ValueKind == JsonValueKind.String && allowSimpleString)
                {
                    // if it's a simple string, there is no "key" for the argument... pass it as value with an empty string as key
                    // this will ensure that an additional '--' isn't emitted preceding the string-only arguments
                    kvs.Add(new KeyValuePair<string, string>("", parsed.GetString() ?? string.Empty));
                }
                else if (parsed.ValueKind != JsonValueKind.Object)
                {
                    // if it's not a simple string, it must be an object... if it's not, we'll just log and continue
                    Logger.Log("KeyValuePairsFromJson: Invalid json (only supports `\"string\"`, or `{\"mapItem1\": \"value1\", \"...\": \"...\"}`!");
                }
                else
                {
                    foreach (var item in parsed.EnumerateObject())
                    {
                        kvs.Add(new KeyValuePair<string, string>(item.Name, item.Value.GetString() ?? string.Empty));
                    }
                }
            }
            return kvs;
        }

        private static string UpdateStackTrace(string stackTrace, string? command, List<KeyValuePair<string, string>> kvs)
        {
            if (command?.EndsWith("dev shell") ?? false)
            {
                var devShellRunBashScriptArguments = string.Join("\n", kvs
                    .Where(kv => kv.Key switch { "run" => true, "bash" => true, "script" => true, _ => false })
                    .Select(kv => !string.IsNullOrEmpty(kv.Key)
                        ? $"{kv.Key}:\n{kv.Value.Replace("\n", "\n  ")}"
                        : kv.Value));
                if (!string.IsNullOrEmpty(devShellRunBashScriptArguments))
                {
                    stackTrace = $"{stackTrace}\n{devShellRunBashScriptArguments}".Trim('\r', '\n', ' ');
                }
            }
            else if (command != null)
            {
                var commandArguments = string.Join("\n", kvs
                    .Where(kv => !string.IsNullOrEmpty(kv.Key))
                    .Select(kv => $"{kv.Key}:\n{kv.Value.Replace("\n", "\n  ")}"));
                stackTrace = !string.IsNullOrEmpty(commandArguments)
                    ? $"{stackTrace}\nCOMMAND: {command}\n{commandArguments}".Trim('\r', '\n', ' ')
                    : $"{stackTrace}\nCOMMAND: {command}".Trim('\r', '\n', ' ');
            }

            return stackTrace;
        }

        private static string WriteMultilineTsvToTempFile(string text, ref List<string>? files)
        {
            files ??= new List<string>();

            var lines = text.Split('\r', '\n');
            var newLines = new List<string>();
            foreach (var line in lines)
            {
                if (!line.Contains('\f'))
                {
                    newLines.Add(line);
                    continue;
                }

                var values = line.Split('\t');
                var newValues = new List<string>();
                foreach (var value in values)
                {
                    if (!value.Contains('\f'))
                    {
                        newValues.Add(value);
                        continue;
                    }

                    var newValue = WriteTextToTempFile(value.Replace('\f', '\n'))!;
                    files.Add(newValue);

                    newValues.Add($"@{newValue}");
                }

                newLines.Add(string.Join("\t", newValues));
            }

            var newText = string.Join("\n", newLines);
            var file = WriteTextToTempFile(newText);
            files.Add(file!);
            return file!;
        }

        private static string? WriteTextToTempFile(string? text, string? extension = null)
        {
            if (!string.IsNullOrEmpty(text))
            {
                var tempFile = Path.GetTempFileName();
                if (!string.IsNullOrEmpty(extension))
                {
                    tempFile = $"{tempFile}.{extension}";
                }

                File.WriteAllText(tempFile, text);

                var content = File.ReadAllText(tempFile).Replace("\n", "\\n");
                Logger.Log($"FILE: {tempFile}: '{content}'");

                return tempFile;
            }
            return null;
        }

        private static string FindCacheCli(string cli)
        {
            if (_cliCache.ContainsKey(cli))
            {
                return _cliCache[cli];
            }

            var found = FindCli(cli);
            _cliCache[cli] = found;

            return found;
        }

        private static string FindCli(string cli)
        {
            var specified = !string.IsNullOrEmpty(cli);
            if (specified)
            {
                var found = FindCliOrNull(cli);
                return found != null
                    ? CliFound(cli, found)              // use what we found
                    : CliNotFound(cli);                 // use what was specified
            }
            else
            {
                var clis = new[] { "ai", "spx", "vz" };
                var found = PickCliOrNull(clis);
                return found != null
                    ? PickCliFound(clis, found)         // use what we found
                    : PickCliNotFound(clis, clis[0]);   // use ai
            }
        }

        private static string? FindCliOrNull(string cli)
        {
            var dll = $"{cli}.dll";
            var exe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"{cli}.exe" : cli;

            var path1 = Environment.GetEnvironmentVariable("PATH");
            var path2 = Directory.GetCurrentDirectory();
            var path3 = FileHelpers.GetAssemblyFileInfo(typeof(YamlTestCaseRunner)).DirectoryName;
            var path = $"{path3}{Path.PathSeparator}{path2}{Path.PathSeparator}{path1}";

            var paths = path.Split(Path.PathSeparator);
            foreach (var part2 in new string[]{ "", "net6.0"})
            {
                foreach (var part1 in paths)
                {
                    var checkExe = Path.Combine(part1, part2, exe);
                    if (File.Exists(checkExe))
                    {
                        // Logger.TraceInfo($"FindCliOrNull: Found CLI: {checkExe}");
                        var checkDll = FindCliDllOrNull(checkExe, dll);
                        if (checkDll != null)
                        {
                            // Logger.TraceInfo($"FindCliOrNull: Found DLL: {checkDll}");
                            return checkExe;
                        }
                    }
                }
            }

            return null;
        }

        private static string? FindCliDllOrNull(string cli, string dll)
        {
            var fi = new FileInfo(cli);
            if (!fi.Exists) return null;

            var check = Path.Combine(fi.DirectoryName!, dll);
            if (File.Exists(check)) return check;

            var matches = fi.Directory!.GetFiles(dll, SearchOption.AllDirectories);
            if (matches.Length == 1) return matches.First().FullName;

            return null;
        }

        private static string CliFound(string cli, string found)
        {
            Logger.Log($"CliFound: CLI specified ({cli}); found; using {found}");
            return found;
        }

        private static string CliNotFound(string cli)
        {
            var message = $"CliNotFound: CLI specified ({cli}); tried searching PATH and working directory; not found; using {cli}";
            Logger.LogWarning(message);
            return cli;
        }

        private static string? PickCliOrNull(IEnumerable<string> clis)
        {
            var cliOrNulls = new List<string?>();
            foreach (var cli in clis)
            {
                cliOrNulls.Add(FindCliOrNull(cli));
            }

            var clisFound = cliOrNulls.Where(cli => !string.IsNullOrEmpty(cli));
            return clisFound.Count() == 1
                ? clisFound.First()
                : null;
        }

        private static void PickCliUpdateYamlDefaultsFileWarning(IEnumerable<string> clis)
        {
            var message = string.Join(" or ", clis.Select(cli => $"`cli: {cli}`"));
            message = $"PickCli: CLI not specified; please create/update {YamlTestFramework.YamlDefaultTagsFileName} with one of: {message}";
            Logger.LogWarning(message);
        }

        private static string PickCliFound(IEnumerable<string> clis, string cli)
        {
            PickCliUpdateYamlDefaultsFileWarning(clis);

            var message = $"PickCliFound: CLI not specified; found 1 CLI; using {cli}";
            Logger.LogInfo(message);
            return cli;
        }

        private static string PickCliNotFound(IEnumerable<string> clis, string cli)
        {
            PickCliUpdateYamlDefaultsFileWarning(clis);

            var message = $"PickCliNotFound: CLI not specified; tried searching PATH and working directory; found 0 or >1 CLIs; using {cli}";
            Logger.LogInfo(message);
            return cli;
        }

        private static IEnumerable<string> GetPossibleRunTimeLocations()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new string[]{ "", "runtimes/win-x64/native/", "../runtimes/win-x64/native/" };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new string[]{ "", "runtimes/linux-x64/native/", "../../runtimes/linux-x64/native/" };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new string[]{ "", "runtimes/osx-x64/native/", "../../runtimes/osx-x64/native/" };
            }
            return new string[]{ "" };
        }

        private static void UpdateEnvironment(ProcessStartInfo startInfo, string? envAsMultiLineString)
        {
            var ok = !string.IsNullOrEmpty(envAsMultiLineString);
            if (!ok) return;
            
            var env = YamlEnvHelpers.GetEnvironmentFromMultiLineString(envAsMultiLineString!);
            foreach (var item in env)
            {
                startInfo.Environment[item.Key] = item.Value;
            }
        }

        static void UpdatePathEnvironment(ProcessStartInfo startInfo)
        {
            var cli = new FileInfo(startInfo.FileName);
            if (cli.Exists)
            {
                var dll = FindCliDllOrNull(cli.FullName, cli.Name.Replace(".exe", "") + ".dll");
                if (dll != null)
                {
                    var cliPath = cli.Directory!.FullName;
                    var dllPath = new FileInfo(dll).Directory!.FullName;

                    var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                    var pathVar = isWindows ? "PATH" : "LD_LIBRARY_PATH";
                    var path = Environment.GetEnvironmentVariable(pathVar) ?? "";

                    var locations = GetPossibleRunTimeLocations();
                    path = AddToPath(path, cliPath, locations);
                    path = AddToPath(path, dllPath, locations);

                    startInfo.Environment.Add(pathVar, path);
                    Logger.LogInfo($"UpdatePathEnvironment: {pathVar}={path}");
                }
            }
        }

        private static string AddToPath(string path, string value, IEnumerable<string> locations)
        {
            foreach (var location in locations)
            {
                var check = Path.Combine(value, location);
                if (Directory.Exists(check))
                {
                    path = AddToPath(path, check);
                }
            }
            return path;
        }

        private static string AddToPath(string path, string value)
        {
            var paths = path.Split(Path.PathSeparator);
            return !paths.Contains(value)
                ? $"{value}{Path.PathSeparator}{path}".Trim(Path.PathSeparator)
                : path;
        }

        private static bool WaitForExit(Process process, int timeout)
        {
            var completed = process.WaitForExit(timeout);
            if (!completed)
            {
                var name = process.ProcessName;
                var message = $"Timedout! Stopping process ({name})...";
                Logger.LogWarning(message);

                var softKillFunc = new Func<bool>(() => {
                    var message = $"Timedout! Sending <ctrl-c> ...";
                    Logger.LogWarning(message);

                    process.StandardInput.WriteLine("\x3"); // try ctrl-c first
                    process.StandardInput.Close();

                    Logger.LogWarning($"{message} Sent!");

                    return process.WaitForExit(200);
                });
                completed = TryCatchHelpers.TryCatchNoThrow<bool>(softKillFunc, false, out var _);

                message = "Timedout! Sent <ctrl-c>" + (completed ? "; stopped" : "; trying Kill()");
                Logger.LogWarning(message);

                if (!completed)
                {
                    message = $"Timedout! Killing process ({name})...";
                    Logger.LogWarning(message);

                    process.Kill();

                    message = process.HasExited ? $"{message} Done." : $"{message} Failed!";
                    Logger.LogWarning(message);
                }
            }

            return completed;
        }

        private static void GetStartInfoArgs(out string startProcess, out string startArgs,string cli, string? command, string? script, bool scriptIsBash, List<KeyValuePair<string, string>> kvs, string? expectRegex, string? notExpectRegex, ref List<string>? files)
        {
            startProcess = FindCacheCli(cli);

            var isCommand = !string.IsNullOrEmpty(command) || string.IsNullOrEmpty(script);
            if (isCommand)
            {
                command = $"{command} {GetKeyValueArgs(kvs)}";

                var hasExpectations = !string.IsNullOrEmpty(expectRegex) || !string.IsNullOrEmpty(notExpectRegex);
                if (hasExpectations) 
                {
                    command = WriteTextToTempFile(command)!;
                    files ??= new List<string>();
                    files.Add(command);

                    startArgs = $"run --command @{command} {GetAtArgs(expectRegex, notExpectRegex)}";
                    return;
                }

                startArgs = command;
                return;
            }

            if (scriptIsBash)
            {
                var bash = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                    ? EnsureFindCacheGetBashExe()
                    : "/bin/bash";
                startArgs = $"run --process \"{bash}\" --pre.script -l --script \"{script}\" {GetKeyValueArgs(kvs)} {GetAtArgs(expectRegex, notExpectRegex)}";
                return;
            }

            startArgs = $"run --cmd --script \"{script}\" {GetKeyValueArgs(kvs)} {GetAtArgs(expectRegex, notExpectRegex)}";
            return;
        }

        private static string EnsureFindCacheGetBashExe()
        {
            var gitBash = FindCacheGitBashExe();
            if (gitBash == null || gitBash == "bash.exe")
            {
                throw new Exception("Could not find Git for Windows bash.exe in PATH!");
            }
            return gitBash;
        }

        private static string FindCacheGitBashExe()
        {
            var bashExe = "bash.exe";
            if (_cliCache.ContainsKey(bashExe))
            {
                return _cliCache[bashExe];
            }

            var found = FindGitBashExe();
            _cliCache[bashExe] = found;

            return found;
        }

        private static string FindGitBashExe()
        {
            var found = FileHelpers.FindFilesInOsPath("bash.exe");
            return found.Where(x => x.ToLower().Contains("git")).FirstOrDefault() ?? "bash.exe";
        }

        private static string GetAtArgs(string? expectRegex, string? notExpectRegex)
        {
            var atArgs = $"";
            if (!string.IsNullOrEmpty(expectRegex)) atArgs += $" --expect @{expectRegex}";
            if (!string.IsNullOrEmpty(notExpectRegex)) atArgs += $" --not-expect @{notExpectRegex}";
            return atArgs.TrimStart(' ');
        }

        private static string GetKeyValueArgs(List<KeyValuePair<string, string>> kvs)
        {
            var args = new StringBuilder();
            foreach (var item in kvs)
            {
                if (!string.IsNullOrEmpty(item.Key))
                {
                    if (item.Key.Contains('\t'))
                    {
                        var key = item.Key.Replace('\t', ';');
                        args.Append($"--foreach {key} in ");
                    }
                    else
                    {
                        args.Append($"--{item.Key} ");
                    }
                    
                    if (!string.IsNullOrEmpty(item.Value))
                    {
                        args.Append($"\"{item.Value}\" ");
                    }
                }
                else if (!string.IsNullOrEmpty(item.Value))
                {
                    args.Append(item.Value);
                }
            }
            return args.ToString().TrimEnd();
        }

        private static TestResult CreateTestResult(TestCase test, DateTime start, DateTime stop, string stdOut, string stdErr, string errorMessage, string stackTrace, string additional, string debugTrace, TestOutcome outcome)
        {
            Logger.Log($"YamlTestCaseRunner.CreateTestResult({test.DisplayName})");

            var result = new TestResult(test) { Outcome = outcome };
            result.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, stdOut));
            result.Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory, stdErr));
            result.Messages.Add(new TestResultMessage(TestResultMessage.AdditionalInfoCategory, additional));
            result.Messages.Add(new TestResultMessage(TestResultMessage.DebugTraceCategory, debugTrace));
            result.ErrorMessage = errorMessage;
            result.ErrorStackTrace = stackTrace;
            result.StartTime = start;
            result.EndTime = stop;
            result.Duration = stop - start;

            Logger.Log("----------------------------\n\n");
            Logger.Log($"    STDOUT: {stdOut}");
            Logger.Log($"    STDERR: {stdErr}");
            Logger.Log($"     STACK: {stackTrace}");
            Logger.Log($"     ERROR: {errorMessage}");
            Logger.Log($"   OUTCOME: {outcome}");
            Logger.Log($"ADDITIONAL: {additional}");
            Logger.Log($"DEBUGTRACE: {debugTrace}");
            Logger.Log("----------------------------\n\n");

            return result;
        }

        private static string ExtraDebugInfo()
        {
            var sb = new StringBuilder();

            var cwd = new DirectoryInfo(Directory.GetCurrentDirectory());
            sb.AppendLine($"CURRENT DIRECTORY: {cwd.FullName}");

            var files = cwd.GetFiles("*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                sb.AppendLine($"{file.Length,10}   {file.CreationTime.Date:MM/dd/yyyy}   {file.CreationTime:hh:mm:ss tt}   {file.FullName}");
            }

            var variables = Environment.GetEnvironmentVariables();
            var keys = new List<string>(variables.Count);
            foreach (var key in variables.Keys) keys.Add((key as string)!);

            keys.Sort();
            foreach (var key in keys)
            {
                var value = variables[key] as string;
                sb.AppendLine($"{key,-20}  {value}");
            }

            return sb.ToString();
        }

        private static TestOutcome CheckExpectGptOutcome(string output, string expectGpt, string workingDirectory, ref string stdOut, ref string stdErr)
        {
            var outcome = ExpectGptOutcome(output, expectGpt, workingDirectory, out var gptStdOut, out var gptStdErr, out var gptMerged);
            if (outcome == TestOutcome.Failed)
            {
                if (!string.IsNullOrEmpty(gptStdOut)) stdOut = $"{stdOut}\n--expect--\n{gptStdOut}\n".Trim('\n');
                if (!string.IsNullOrEmpty(gptStdErr)) stdErr = $"{stdErr}\n--expect--\n{gptStdErr}\n".Trim('\n');
            }
            return outcome;
        }

        private static TestOutcome ExpectGptOutcome(string output, string expectGpt, string workingDirectory, out string gptStdOut, out string gptStdErr, out string gptMerged)
        {
            Logger.Log($"ExpectGptOutcome: Checking for {expectGpt} in '{output}'");

            var outcome = TestOutcome.None;

            var sbOut = new StringBuilder();
            var sbErr = new StringBuilder();
            var sbMerged = new StringBuilder();

            var question = new StringBuilder();
            question.AppendLine($"Here's the console output:\n\n{output}\n");
            question.AppendLine($"Here's the expectation:\n\n{expectGpt}\n");
            question.AppendLine("You **must always** answer \"PASS\" if the expectation is met.");
            question.AppendLine("You **must always** answer \"FAIL\" if the expectation is not met.");
            question.AppendLine("You **must only** answer \"PASS\" or \"FAIL\".");
            var questionTempFile = WriteTextToTempFile(question.ToString())!;

            try
            {
                var startProcess = FindCacheCli("ai");
                var startArgs = $"chat --quiet true --index-name @none --question @{questionTempFile}";
                var startInfo = new ProcessStartInfo(startProcess, startArgs)
                {
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = workingDirectory
                };

                Logger.Log($"ExpectGptOutcome: Process.Start('{startProcess} {startArgs}')");
                var process = Process.Start(startInfo);
                if (process == null) throw new Exception("Process.Start() returned null!");
                process.StandardInput.Close();

                var outDoneSignal = new ManualResetEvent(false);
                var errDoneSignal = new ManualResetEvent(false);
                process.OutputDataReceived += (sender, e) => AppendLineOrSignal(e.Data, sbOut, sbMerged, outDoneSignal);
                process.ErrorDataReceived += (sender, e) => AppendLineOrSignal(e.Data, sbErr, sbMerged, errDoneSignal);
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var exitedNotKilled = WaitForExit(process, 60000);
                if (exitedNotKilled)
                {
                    outDoneSignal.WaitOne();
                    errDoneSignal.WaitOne();
                }

                var passed = exitedNotKilled && process.ExitCode == 0;
                outcome = passed ? TestOutcome.Passed : TestOutcome.Failed;

                var timedoutOrKilled = !exitedNotKilled;
                if (timedoutOrKilled)
                {
                    var message = "ExpectGptOutcome: WARNING: Timedout or killed!";
                    sbErr.AppendLine(message);
                    sbMerged.AppendLine(message);
                    Logger.LogWarning(message);
                }
            }
            catch (Exception ex)
            {
                outcome = TestOutcome.Failed;

                var exception = $"ExpectGptOutcome: EXCEPTION: {ex.Message}";
                sbErr.AppendLine(exception);
                sbMerged.AppendLine(exception);
                Logger.Log(exception);
            }

            File.Delete(questionTempFile);
            gptStdOut = sbOut.ToString();
            gptStdErr = sbErr.ToString();
            gptMerged = sbMerged.ToString();

            if (outcome == TestOutcome.Passed)
            {
                Logger.Log($"ExpectGptOutcome: Checking for 'PASS' in '{gptMerged}'");

                var passed = gptMerged.Contains("PASS") || gptMerged.Contains("TRUE") || gptMerged.Contains("YES");
                outcome = passed ? TestOutcome.Passed : TestOutcome.Failed;

                Logger.Log($"ExpectGptOutcome: {outcome}");
            }

            return outcome;
        }

        private static void AppendLineOrSignal(string? text, StringBuilder sb1, StringBuilder sb2, ManualResetEvent signal)
        {
            if (text != null)
            {
                sb1.AppendLine(text);
                sb2.AppendLine(text);
            }
            else
            {
                signal.Set();
            }
        }

        #endregion

        private static Dictionary<string, string> _cliCache = new Dictionary<string, string>();
    }
}
