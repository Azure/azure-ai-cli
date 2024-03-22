//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

#nullable enable

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Azure.AI.CLI.Common.Clients.Models;
using Newtonsoft.Json.Linq;

namespace Azure.AI.Details.Common.CLI
{
    public readonly struct ProcessOutput
    {
        public string StdOutput { get; init; }
        public string StdError { get; init; }
        public string MergedOutput { get; init; }
        public int ExitCode { get; init; }

        public bool HasError => ExitCode != 0 || !string.IsNullOrWhiteSpace(StdError);
    }

    public class ProcessHelpers
    {
        public static Process? StartBrowser(string url)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Process.Start(new ProcessStartInfo("cmd", $"/c start {url.Replace("&", "^&")}") { CreateNoWindow = true })
                : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    ? Process.Start("xdg-open", url)
                    : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                        ? Process.Start("open", url)
                        : null;
        }

        public static Process? StartProcess(string fileName, string arguments, IDictionary<string, string> addToEnvironment = null, bool redirectOutput = true, bool redirectInput = false)
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

        public static async Task<ProcessOutput> RunShellScriptAsync(string inlineScriptOrFileName, bool scriptIsBash = false, Dictionary<string, string> addToEnvironment = null, Action<string> stdOutHandler = null, Action<string> stdErrHandler = null, Action<string> mergedOutputHandler = null, bool captureOutput = true, bool interactive = false)
        {
            ProcessOutput processOutput;
            var useBinBash = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (useBinBash)
            {
                var binBashArguments = interactive
                    ? $"-lic \"{inlineScriptOrFileName}\""
                    : $"-lc \"{inlineScriptOrFileName}\"";
                processOutput = await RunShellCommandAsync("/bin/bash", binBashArguments, addToEnvironment, stdOutHandler, stdErrHandler, mergedOutputHandler, captureOutput);
            }
            else if (scriptIsBash)
            {
                var cmdFile = Path.GetTempFileName() + ".cmd";
                File.WriteAllText(cmdFile, inlineScriptOrFileName);

                var git = FindCacheGitBashExe();
                var gitBashCommand = interactive
                    ? $"@\"{git}\" -li \"{cmdFile}\""
                    : $"@\"{git}\" -l \"{cmdFile}\"";
                var gitBashCmdFile = Path.GetTempFileName() + ".cmd";
                File.WriteAllText(gitBashCmdFile, gitBashCommand);

                processOutput = await RunShellCommandAsync(gitBashCmdFile, "", addToEnvironment, stdOutHandler, stdErrHandler, mergedOutputHandler, captureOutput);
                File.Delete(gitBashCmdFile);
                File.Delete(cmdFile);
            }
            else
            {
                var cmdFile = Path.GetTempFileName() + ".cmd";
                File.WriteAllText(cmdFile, inlineScriptOrFileName);

                processOutput = await RunShellCommandAsync(cmdFile, "", addToEnvironment, stdOutHandler, stdErrHandler, mergedOutputHandler, captureOutput);
                File.Delete(cmdFile);
            }

            return processOutput;
        }

        public static async Task<ProcessOutput> RunShellInteractiveAsync(Dictionary<string, string> addToEnvironment = null, Action<string> stdOutHandler = null, Action<string> stdErrHandler = null, Action<string> mergedOutputHandler = null, bool captureOutput = true)
        {
            var interactiveShellFileName = !OS.IsWindows() ? "bash" : "cmd.exe";
            var interactiveShellArguments = !OS.IsWindows() ? "-li" : "/k PROMPT (ai dev shell) %PROMPT%& title (ai dev shell)";

            return await RunShellCommandAsync(interactiveShellFileName, interactiveShellArguments, addToEnvironment, stdOutHandler, stdErrHandler, mergedOutputHandler, captureOutput);
        }

        public static async Task<ProcessOutput> RunShellCommandAsync(string command, string arguments, IDictionary<string, string>? addToEnvironment = null, Action<string>? stdOutHandler = null, Action<string>? stdErrHandler = null, Action<string>? mergedOutputHandler = null, bool captureOutput = true)
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

            Process? process;
            try
            {
                process = StartShellCommandProcess(command, arguments, addToEnvironment, redirectOutput);
                if (process == null)
                {
                    return new ProcessOutput()
                    {
                        ExitCode = -1,
                        StdError = "Process failed to start"
                    };
                }
            }
            catch (Exception processException)
            {
                SHELL_DEBUG_TRACE($"ERROR: {processException}");
                return new ProcessOutput()
                {
                    ExitCode = -1,
                    StdError = processException.ToString()
                };
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

            var output = new ProcessOutput()
            {
                StdOutput = sbOut.ToString().Trim(' ', '\r', '\n'),
                StdError = sbErr.ToString().Trim(' ', '\r', '\n'),
                MergedOutput = sbMerged.ToString().Trim(' ', '\r', '\n'),
                ExitCode = process.ExitCode,
            };

            if (!string.IsNullOrEmpty(output.StdOutput)) SHELL_DEBUG_TRACE($"---\nSTDOUT\n---\n{output.StdOutput}");
            if (!string.IsNullOrEmpty(output.StdError)) SHELL_DEBUG_TRACE($"---\nSTDERR\n---\n{output.StdError}");

            return output;
        }

        private static Process? StartShellCommandProcess(string command, string arguments, IDictionary<string, string> addToEnvironment = null, bool captureOutput = true)
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            return  isWindows
                ? StartProcess("cmd", $"/c {command} {arguments}", addToEnvironment, captureOutput)
                : StartProcess(command,  arguments, addToEnvironment, captureOutput);
        }

        private static void SHELL_DEBUG_TRACE(string message,[CallerLineNumber] int line = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
        {
            if (Program.Debug) Console.WriteLine(message);

            message = message.Replace("\n", "\\n").Replace("\r", "\\r");
            AI.DBG_TRACE_INFO(message, line, caller, file);
        }

        private static string DictionaryToString(IDictionary<string, string>? dictionary)
        {
            if (dictionary == null)
            {
                return string.Empty;
            }

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

        private static Dictionary<string, string> _cliCache = new Dictionary<string, string>();
    }
}
