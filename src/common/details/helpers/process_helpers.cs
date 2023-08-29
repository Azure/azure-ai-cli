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
    public struct ProcessResponse<T>
    {
        public string StdOutput;
        public string StdError;
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

        public static Process StartProcess(string fileName, string arguments, Dictionary<string, string> addToEnvironment = null)
        {
            var start = new ProcessStartInfo(fileName, arguments);
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;

            if (addToEnvironment != null)
            {
                foreach (var kvp in addToEnvironment)
                {
                    start.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
            }

            return Process.Start(start);
        }

        public static async Task<(int, string)> RunShellCommandAsync(string command, string arguments, Dictionary<string, string> addToEnvironment = null)
        {
            var output = new StringBuilder();
            var outputReceived = (string data) => {
                output.AppendLine(data);
            };

            var process = TryCatchHelpers.TryCatchNoThrow<Process>(() => StartShellCommandProcess(command, arguments, addToEnvironment), null, out Exception processException);
            process.OutputDataReceived += (sender, e) => outputReceived(e.Data ?? "");
            process.ErrorDataReceived += (sender, e) => outputReceived(e.Data ?? "");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            return (process.ExitCode, output.ToString());
        }

        public static async Task<ProcessResponse<T>> ParseShellCommandJson<T>(string command, string arguments, Dictionary<string, string> addToEnvironment = null, Action<string> stdOutHandler = null, Action<string> stdErrHandler = null) where T : JToken, new()
        {
            SHELL_DEBUG_TRACE($"COMMAND: {command} {arguments} {DictionaryToString(addToEnvironment)}");

            var stdOut = new StringBuilder();
            var stdErr = new StringBuilder();
            var stdOutReceived = (string data) => {
                if (stdOutHandler != null) stdOutHandler(data);
                stdOut.AppendLine(data);
            };
            var stdErrReceived = (string data) => {
                if (stdErrHandler != null) stdErrHandler(data);
                stdErr.AppendLine(data);
            };

            var process = TryCatchHelpers.TryCatchNoThrow<Process>(() => StartShellCommandProcess(command, arguments, addToEnvironment), null, out Exception processException);
            process.OutputDataReceived += (sender, e) => stdOutReceived(e.Data ?? "");
            process.ErrorDataReceived += (sender, e) => stdErrReceived(e.Data ?? "");
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            var response = new ProcessResponse<T>();
            response.StdOutput = process != null ? stdOut.ToString() : "";
            response.StdError = process != null ? stdErr.ToString() : processException.ToString();

            if (!string.IsNullOrEmpty(response.StdOutput)) SHELL_DEBUG_TRACE($"---\nSTDOUT\n---\n{response.StdOutput}");
            if (!string.IsNullOrEmpty(response.StdError)) SHELL_DEBUG_TRACE($"---\nSTDERR\n---\n{response.StdError}");

            var parsed = !string.IsNullOrEmpty(response.StdOutput) ? JToken.Parse(response.StdOutput) : null;
            response.Payload = parsed is T ? parsed as T : new T();

            return response;
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
