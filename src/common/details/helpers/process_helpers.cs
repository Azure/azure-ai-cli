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

        public static Process StartProcess(string fileName, string arguments, Dictionary<string, string> addToEnvironment = null, bool redirect = true)
        {
            var start = new ProcessStartInfo(fileName, arguments);
            start.UseShellExecute = false;
            start.RedirectStandardOutput = redirect;
            start.RedirectStandardError = redirect;

            if (addToEnvironment != null)
            {
                foreach (var kvp in addToEnvironment)
                {
                    start.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
            }

            return Process.Start(start);
        }

        public static async Task<ProcessOutput> RunShellCommandAsync(string command, string arguments, Dictionary<string, string> addToEnvironment = null, Action<string> stdOutHandler = null, Action<string> stdErrHandler = null, Action<string> mergedOutputHandler = null)
        {
            SHELL_DEBUG_TRACE($"COMMAND: {command} {arguments} {DictionaryToString(addToEnvironment)}");

            var stdOut = new StringBuilder();
            var stdErr = new StringBuilder();
            var mergedOutput = new StringBuilder();
            var stdOutReceived = (string data) => {
                if (stdOutHandler != null) stdOutHandler(data);
                if (mergedOutputHandler != null) mergedOutputHandler(data);
                stdOut.AppendLine(data);
                mergedOutput.AppendLine(data);
            };
            var stdErrReceived = (string data) => {
                if (stdErrHandler != null) stdErrHandler(data);
                if (mergedOutputHandler != null) mergedOutputHandler(data);
                stdErr.AppendLine(data);
                mergedOutput.AppendLine(data);
            };

            var process = TryCatchHelpers.TryCatchNoThrow<Process>(() => StartShellCommandProcess(command, arguments, addToEnvironment), null, out Exception processException);
            process.OutputDataReceived += (sender, e) => stdOutReceived(e.Data ?? "");
            process.ErrorDataReceived += (sender, e) => stdErrReceived(e.Data ?? "");
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            var output = new ProcessOutput();
            output.StdOutput = process != null ? stdOut.ToString().Trim(' ', '\r', '\n') : "";
            output.StdError = process != null ? stdErr.ToString().Trim(' ', '\r', '\n') : processException.ToString();
            output.MergedOutput = process != null ? mergedOutput.ToString().Trim(' ', '\r', '\n') : "";
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

        private static Process StartShellCommandProcess(string command, string arguments, Dictionary<string, string> addToEnvironment = null)
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            return  isWindows
                ? StartProcess("cmd", $"/c {command} {arguments}", addToEnvironment)
                : StartProcess(command,  arguments, addToEnvironment);
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
    }
}
