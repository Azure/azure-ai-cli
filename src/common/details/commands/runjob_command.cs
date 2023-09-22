//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Azure.AI.Details.Common.CLI
{
    public class RunJobCommand : Command
    {
        public RunJobCommand(ICommandValues values)
        {
            _values = values.ReplaceValues();
        }

        public bool RunCommand()
        {
            var passed = DoRunJob();
            return _values.GetOrDefault("passed", passed);
        }

        private bool DoRunJob()
        {
            CheckPath();

            var quiet = _values.GetOrDefault("x.quiet", false);

            var args = _values.GetOrDefault("run.input.post.args", "").Replace(';', ' ');
            var preArgs = _values.GetOrDefault("run.input.pre.args", "").Replace(';', ' ');

            var process = _values.GetOrDefault("run.input.process", "");

            var command = _values.GetOrDefault($"run.input.{Program.Name}.command", "");
            var commandArgs = _values.GetOrDefault($"run.input.{Program.Name}.post.command.args", "").Replace(';', ' ');
            var preCommandArgs = _values.GetOrDefault($"run.input.{Program.Name}.pre.command.args", "").Replace(';', ' ');

            var job = _values.GetOrDefault($"run.input.{Program.Name}.job", "");
            var jobArgs = _values.GetOrDefault($"run.input.{Program.Name}.post.job.args", "").Replace(';', ' ');
            var preJobArgs = _values.GetOrDefault($"run.input.{Program.Name}.pre.job.args", "").Replace(';', ' ');

            var line = _values.GetOrDefault("run.input.line", "");
            var lineArgs = _values.GetOrDefault("run.input.post.line.args", "").Replace(';', ' ');
            var preLineArgs = _values.GetOrDefault("run.input.pre.line.args", "").Replace(';', ' ');

            var script = _values.GetOrDefault("run.input.script", "");
            var scriptArgs = _values.GetOrDefault("run.input.post.script.args", "").Replace(';', ' ');
            var preScriptArgs = _values.GetOrDefault("run.input.pre.script.args", "").Replace(';', ' ');

            var file = _values.GetOrDefault("run.input.file", "");
            var fileArgs = _values.GetOrDefault("run.input.post.file.args", "").Replace(';', ' ');
            var preFileArgs = _values.GetOrDefault("run.input.pre.file.args", "").Replace(';', ' ');

            var item = _values.GetOrDefault("run.input.item", "");
            var itemArgs = _values.GetOrDefault("run.input.post.item.args", "").Replace(';', ' ');
            var preItemArgs = _values.GetOrDefault("run.input.pre.item.args", "").Replace(';', ' ');

            var processOk = !string.IsNullOrEmpty(process);
            var commandOk = !string.IsNullOrEmpty(command);
            var scriptOk = !string.IsNullOrEmpty(script);
            var fileOk = !string.IsNullOrEmpty(file);
            var jobOk = !string.IsNullOrEmpty(job);

            var app = processOk && process == Program.Name;
            if (app && jobOk && !job.StartsWith("@")) job = $"@{job}";

            var startPath = UpdateJobStartPath(ref job, _values.GetOrDefault("x.input.path", Directory.GetCurrentDirectory()));

            if (!processOk && scriptOk) processOk = UpdateProcessIfFileNotExist(script, ref process);
            if (!processOk && commandOk) processOk = UpdateProcessIfFileNotExist(command, ref process);
            if (!processOk && fileOk) processOk = UpdateProcessIfFileNotExist(file, ref process);

            var cmd = processOk && process == "cmd";
            var bash = processOk && process == "bash";

            var start = "";
            var startArgs = "";

            if (app)
            {
                start = process;
                startArgs = $"{preCommandArgs} {command} {commandArgs}".Trim();
                startArgs = $"{startArgs} {preJobArgs} {job} {jobArgs}".Trim();
                startArgs = $"{startArgs} {preFileArgs} {file} {fileArgs}".Trim();
                startArgs = $"{startArgs} {preLineArgs} {line} {lineArgs}".Trim();
                startArgs = $"{startArgs} {preItemArgs} {item} {itemArgs}".Trim();
                startArgs = $"{preArgs} {startArgs} {args}".Trim();
            }
            else if (processOk)
            {
                start = process;
                startArgs = $"{preScriptArgs} {script} {scriptArgs}".Trim();
                startArgs = $"{startArgs} {preFileArgs} {file} {fileArgs}".Trim();
                startArgs = $"{startArgs} {preLineArgs} {line} {lineArgs}".Trim();
                startArgs = $"{startArgs} {preItemArgs} {item} {itemArgs}".Trim();
                startArgs = $"{preArgs} {startArgs} {args}".Trim();

                if (cmd && !startArgs.StartsWith("/c")) startArgs = $"/c {startArgs}";
                if (bash && !startArgs.StartsWith("-lic")) startArgs = $"-lic \"{startArgs}\"";
            }
            else if (scriptOk)
            {
                start = script;
                startArgs = $"{scriptArgs}".Trim();
                startArgs = $"{startArgs} {preFileArgs} {file} {fileArgs}".Trim();
                startArgs = $"{startArgs} {preLineArgs} {line} {lineArgs}".Trim();
                startArgs = $"{startArgs} {preItemArgs} {item} {itemArgs}".Trim();
                startArgs = $"{preArgs} {startArgs} {args}".Trim();
            }
            else if (fileOk)
            {
                start = file;
                startArgs = $"{fileArgs}".Trim();
                startArgs = $"{startArgs} {preLineArgs} {line} {lineArgs}".Trim();
                startArgs = $"{startArgs} {preItemArgs} {item} {itemArgs}".Trim();
                startArgs = $"{preArgs} {startArgs} {args}".Trim();
            }
            else if (quiet)
            {
                return true;
            }
            else
            {
                _values.AddThrowError(
                    "WARNING:", $"Missing arguments; requires one of process, command, script, file, or job!",
                        "",
                        "TRY:", $"{Program.Name} run --command COMMAND --args ARGS",
                                $"{Program.Name} run --script SCRIPT --args ARGS",
                                $"{Program.Name} run --job JOB",
                        "",
                        "SEE:", $"{Program.Name} help run");
            }

            var retries = _values.GetOrDefault("run.retries", 0);
            var timeout = _values.GetOrDefault("run.timeout", 86400000);
            var expected = _values.GetOrDefault("run.output.expect", "");
            var notExpected = _values.GetOrDefault("run.output.not.expect", "");
            var autoExpect = _values.GetOrDefault("run.output.auto.expect", false);

            return DoRunJob(start.Trim(), startArgs.Trim(), startPath, expected, notExpected, autoExpect, timeout, retries);
        }

        private bool UpdateProcessIfFileNotExist(string checkFileProcess, ref string process, string defaultProcess = null)
        {
            if (!FileHelpers.FileExistsInDataPath(checkFileProcess, _values))
            {
                process = string.IsNullOrEmpty(defaultProcess)
                    ? RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd" : "bash"
                    : defaultProcess;
                return true;
            }
            return false;
        }

        private string UpdateJobStartPath(ref string job, string startPath)
        {
            if (!string.IsNullOrEmpty(job))
            {
                var existing = FileHelpers.DemandFindFileInDataPath(job.Trim('@'), _values, "job");
                var fi = new FileInfo(existing);
                startPath = fi.DirectoryName;
                job = $"@{fi.Name}";
            }
            return startPath;
        }

        private bool DoRunJob(string start, string startArgs, string startPath, string expected, string notExpected, bool autoExpect, int timeout, int retries)
        {
            do
            {
                var ok = DoRunJob(start, startArgs, startPath, expected, notExpected, autoExpect, timeout);
                if (ok) return ok;
            } while (retries-- > 0);
            return false;
        }

        private bool DoRunJob(string start, string startArgs, string startPath, string expected, string notExpected, bool autoExpect, int timeout)
        {
            var verbose = _values.GetOrDefault("x.verbose", true);
            var quiet = _values.GetOrDefault("x.quiet", false);

            var message = $"RUN JOB: {start} {startArgs} ...";
            if (verbose && !quiet) Console.WriteLine($"\n{message}\n");

            var startTime = DateTime.Now;

            var exitCode = (start == Program.Name && startArgs.StartsWith("wait "))
                ? RunJobWait(startArgs)
                : RunJobProcess(start, startArgs, startPath, expected, notExpected, autoExpect, timeout);

            var stopTime = DateTime.Now;
            var deltaTime = stopTime.Subtract(startTime).TotalMilliseconds;

            var detailed = deltaTime > 200;
            if (verbose && !quiet) Console.WriteLine(detailed
                ? $"\n{message} Done! Exit code: {exitCode} [time={deltaTime / 1000} seconds]"
                : $"\n{message} Done! Exit code: {exitCode}");

            _values.Reset("passed", exitCode == 0 ? "true" : "false");
            return exitCode == 0;
        }

        private int RunJobProcess(string start, string startArgs, string startPath, string expected, string notExpected, bool autoExpect, int timeout)
        {
            var startInfo = new ProcessStartInfo(start, startArgs);
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = startPath;

            var checkExpected = !string.IsNullOrEmpty(expected);
            var checkNotExpected = !string.IsNullOrEmpty(notExpected);
            if (checkExpected || checkNotExpected || autoExpect)
            {
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
            }

            var process = Process.Start(startInfo);

            return checkExpected || checkNotExpected || autoExpect
                ? (CheckExpectedAsync(process, expected, notExpected, autoExpect, timeout).Result ? 0 : 4)
                : (WaitForExit(process, timeout).Result ? process.ExitCode : 3);
        }

        private Task<bool> WaitForExit(Process process, int timeout)
        {
            return Task.Run(() => {

                var completed = process.WaitForExit(timeout);
                if (!completed)
                {
                    var name = process.ProcessName;
                    Console.WriteLine($"Timedout! Killing process ({name})...");
                    process.Kill();

                    var killed = process.HasExited ? "Done." : "Failed!";
                    Console.WriteLine($"Timedout! Killing process ({name})... {killed}");

                    return false;
                }
                return true;

            });
        }

        private async Task<bool> CheckExpectedAsync(Process process, string expected, string notExpected, bool autoExpect, int timeout)
        {
            System.Diagnostics.Debug.Assert(process != null);
            
            var expectedItems = expected.Split(new char[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var notExpectedItems = notExpected.Split(new char[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var checkExpected = ExpectHelper.CheckExpectedConsoleOutputAsync(process, expectedItems, notExpectedItems, autoExpect, !autoExpect);

            await WaitForExit(process, timeout);
            checkExpected.Wait();

            return checkExpected.Result;
        }

        private int RunJobWait(string startArgs)
        {
            if (!_values.Contains("x.quiet")) _values.Add("x.quiet", "true");
            if (!_values.Contains("x.verbose")) _values.Reset("x.verbose", "false");
            Thread.Sleep(int.Parse(startArgs.Substring(4)));
            return 0;
        }
    }
}
