//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var outputReceived = (object sender, DataReceivedEventArgs e) => {
                output.AppendLine(e.Data ?? "");
            };

            var process = TryCatchHelpers.TryCatchNoThrow<Process>(() => StartShellCommandProcess(command, arguments, addToEnvironment), null, out Exception processException);
            process.OutputDataReceived += (sender, e) => outputReceived(sender, e);
            process.ErrorDataReceived += (sender, e) => outputReceived(sender, e);

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            return (process.ExitCode, output.ToString());
        }

        public static async Task<ProcessResponse<T>> ParseShellCommandJson<T>(string command, string arguments, Dictionary<string, string> addToEnvironment = null) where T : JToken, new()
        {
            if (Program.Debug) Console.WriteLine($"COMMAND: {command} {arguments} {addToEnvironment}");
            var process = TryCatchHelpers.TryCatchNoThrow<Process>(() => StartShellCommandProcess(command, arguments, addToEnvironment), null, out Exception processException);

            var response = new ProcessResponse<T>();
            response.StdOutput = process != null ? await process.StandardOutput.ReadToEndAsync() : "";
            response.StdError = process != null ? await process.StandardError.ReadToEndAsync() : processException.ToString();

            if (Program.Debug)
            {
                if (!string.IsNullOrEmpty(response.StdOutput)) Console.WriteLine($"---\nSTDOUT\n---\n{response.StdOutput}");
                if (!string.IsNullOrEmpty(response.StdError)) Console.WriteLine($"---\nSTDERR\n---\n{response.StdError}");
            }

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
    }
}
