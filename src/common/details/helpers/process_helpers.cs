//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Azure.AI.Details.Common.CLI;
using System.Text;

namespace Azure.AI.Details.Common.CLI
{
    public struct ProcessOutput
    {
        public string StdOutput;
        public string StdError;
        public string MergedOutput;
        public int ExitCode;
    }

    public struct ParsedJsonProcessOutput<T>
    {
        public ParsedJsonProcessOutput(ProcessOutput output)
        {
            Output = output;
        }

        public ProcessOutput Output;
        public T Payload;
    }

    public class ProcessHelpers
    {
        public static Process StartBrowser(string url)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Process.Start(new ProcessStartInfo("cmd", $"/c start {url.Replace("&", "^&")}") { CreateNoWindow = true })
                : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    ? Process.Start("xdg-open", url)
                    : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                        ? Process.Start("open", url)
                        : null;
        }

        public static Process StartProcess(string fileName, string arguments, Dictionary<string, string> addToEnvironment = null, bool redirectOutput = true, bool redirectInput = false)
        {
            var start = new ProcessStartInfo(fileName, arguments);
            start.UseShellExecute = false;
            start.RedirectStandardOutput = redirectOutput;
            start.RedirectStandardError = redirectOutput;
            start.RedirectStandardInput = redirectInput;

            if (addToEnvironment != null)
            {
                foreach (var kvp in addToEnvironment)
                {
                    start.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
            }

            return Process.Start(start);
        }

        public static async Task<ProcessOutput> RunShellCommandAsync(string scriptOrFileName, bool scriptIsBash = false, Dictionary<string, string> addToEnvironment = null, Action<string> stdOutHandler = null, Action<string> stdErrHandler = null, Action<string> mergedOutputHandler = null, bool captureOutput = true)
        {
            ProcessOutput processOutput;
            var useBinBash = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (useBinBash)
            {
                var binBashArguments = $"-lic \"{scriptOrFileName}\"";
                processOutput = await RunShellCommandAsync("/bin/bash", binBashArguments, addToEnvironment, stdOutHandler, stdErrHandler, mergedOutputHandler, captureOutput);
            }
            else if (scriptIsBash)
            {
                var cmdFile = Path.GetTempFileName() + ".cmd";
                File.WriteAllText(cmdFile, scriptOrFileName);

                var git = FindCacheGitBashExe();
                var gitBashCommand = $"@\"{git}\" -li \"{cmdFile}\"";
                var gitBashCmdFile = Path.GetTempFileName() + ".cmd";
                File.WriteAllText(gitBashCmdFile, gitBashCommand);

                processOutput = await RunShellCommandAsync(gitBashCmdFile, "", addToEnvironment, stdOutHandler, stdErrHandler, mergedOutputHandler, captureOutput);
                File.Delete(gitBashCmdFile);
                File.Delete(cmdFile);
            }
            else
            {
                var cmdFile = Path.GetTempFileName() + ".cmd";
                File.WriteAllText(cmdFile, scriptOrFileName);

                processOutput = await RunShellCommandAsync(cmdFile, "", addToEnvironment, stdOutHandler, stdErrHandler, mergedOutputHandler, captureOutput);
                File.Delete(cmdFile);
            }

            return processOutput;
        }

        public static async Task<ProcessOutput> RunShellCommandAsync(string command, string arguments, Dictionary<string, string> addToEnvironment = null, Action<string> stdOutHandler = null, Action<string> stdErrHandler = null, Action<string> mergedOutputHandler = null, bool captureOutput = true)
        {
            SHELL_DEBUG_TRACE($"COMMAND: {command} {arguments} {DictionaryToString(addToEnvironment)}");

            var redirectOutput = captureOutput || stdOutHandler != null || stdErrHandler != null || mergedOutputHandler != null;

            var outDoneSignal = new ManualResetEvent(false);
            var errDoneSignal = new ManualResetEvent(false);
            var sbOut = new StringBuilder();
            var sbErr = new StringBuilder();
            var sbMerged = new StringBuilder();

            var stdOutReceived = (string data) => {
                if (data != null)
                {
                    sbOut.AppendLine(data);
                    sbMerged.AppendLine(data);
                    if (stdOutHandler != null) stdOutHandler(data);
                    if (mergedOutputHandler != null) mergedOutputHandler(data);
                }
                else
                {
                    outDoneSignal.Set();
                }
            };
            var stdErrReceived = (string data) => {
                if (data != null)
                {
                    sbErr.AppendLine(data);
                    sbMerged.AppendLine(data);
                    if (stdErrHandler != null) stdErrHandler(data);
                    if (mergedOutputHandler != null) mergedOutputHandler(data);
                }
                else
                {
                    errDoneSignal.Set();
                }
            };

            var process = TryCatchHelpers.TryCatchNoThrow<Process>(() => StartShellCommandProcess(command, arguments, addToEnvironment, redirectOutput), null, out Exception processException);
            if (process == null)
            {
                SHELL_DEBUG_TRACE($"ERROR: {processException}");
                return new ProcessOutput() { StdError = processException.ToString() };
            }

            if (redirectOutput)
            {
                process.OutputDataReceived += (sender, e) => stdOutReceived(e.Data);
                process.ErrorDataReceived += (sender, e) => stdErrReceived(e.Data);
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            await process.WaitForExitAsync();

            if (redirectOutput)
            {
                outDoneSignal.WaitOne();
                errDoneSignal.WaitOne();
            }

            var output = new ProcessOutput();
            output.StdOutput = process != null ? sbOut.ToString().Trim(' ', '\r', '\n') : "";
            output.StdError = process != null ? sbErr.ToString().Trim(' ', '\r', '\n') : processException.ToString();
            output.MergedOutput = process != null ? sbMerged.ToString().Trim(' ', '\r', '\n') : "";
            output.ExitCode = process != null ? process.ExitCode : -1;

            if (!string.IsNullOrEmpty(output.StdOutput)) SHELL_DEBUG_TRACE($"---\nSTDOUT\n---\n{output.StdOutput}");
            if (!string.IsNullOrEmpty(output.StdError)) SHELL_DEBUG_TRACE($"---\nSTDERR\n---\n{output.StdError}");

            return output;
        }

        public static async Task<ParsedJsonProcessOutput<T>> ParseShellCommandJson<T>(string command, string arguments, Dictionary<string, string> addToEnvironment = null, Action<string> stdOutHandler = null, Action<string> stdErrHandler = null) where T : JToken, new()
        {
            var processOutput = await RunShellCommandAsync(command, arguments, addToEnvironment, stdOutHandler, stdErrHandler);
            var stdOutput = processOutput.StdOutput;

            var parsed = !string.IsNullOrWhiteSpace(stdOutput) ? JToken.Parse(stdOutput) : null;

            var x = new ParsedJsonProcessOutput<T>(processOutput);
            x.Payload = parsed is T ? parsed as T : new T();

            return x;
        }

        private static Process StartShellCommandProcess(string command, string arguments, Dictionary<string, string> addToEnvironment = null, bool captureOutput = true)
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            return  isWindows
                ? StartProcess("cmd", $"/c \"{command}\" {arguments}", addToEnvironment, captureOutput)
                : StartProcess(command,  arguments, addToEnvironment, captureOutput);
        }

        private static void SHELL_DEBUG_TRACE(string message,[CallerLineNumber] int line = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
        {
            if (Program.Debug) Console.WriteLine(message);

            message = message.Replace("\n", "\\n").Replace("\r", "\\r");
            AI.DBG_TRACE_INFO(message, line, caller, file);
        }

        private static string DictionaryToString(Dictionary<string, string> dictionary)
        {
            var kvps = new List<string>();
            if (dictionary != null)
            {
                foreach (var kvp in dictionary)
                {
                    kvps.Add($"{kvp.Key}={kvp.Value}");
                }
            }
            return string.Join(' ', kvps);
        }

        private static string EnsureFindCacheGetBashExe()
        {
            var gitBash = FindCacheGitBashExe();
            if (gitBash == null || gitBash == "bash.exe")
            {
                throw new Exception("Could not Git for Windows bash.exe in PATH!");
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

        private static Dictionary<string, string> _cliCache = new Dictionary<string, string>();
    }
}
