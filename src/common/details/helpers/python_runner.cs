//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Azure.AI.Details.Common.CLI
{
    public class PythonRunner
    {
        public static async Task<(int, string)> RunPythonScriptAsync(string script, string args = null, Dictionary<string, string> addToEnvironment = null, Action<string> stdOutHandler = null, Action<string> stdErrHandler = null, Action<string> mergedOutputHandler = null)
        {
            EnsureFindPython();
            if (_pythonBinary == null)
            {
                ConsoleHelpers.WriteLineError("*** Please install Python 3.10 or above ***");
                Console.Write("\nNOTE: If it's already installed ensure it's in the system PATH and working (try: `python --version`)\n");
                return (-1, null);
            }

            var tempFile = Path.GetTempFileName() + ".py";
            File.WriteAllText(tempFile, script);

            try
            {
                args = args != null
                    ? $"\"{tempFile}\" {args}"
                    : $"\"{tempFile}\"";
                var process = await ProcessHelpers.RunShellCommandAsync(_pythonBinary, args, addToEnvironment, stdOutHandler, stdErrHandler, mergedOutputHandler);
                return (process.ExitCode, process.MergedOutput);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        public static string RunEmbeddedPythonScript(INamedValues values, string scriptName, string scriptArgs = null, Dictionary<string, string> addToEnvironment = null, Action<string> stdOutHandler = null, Action<string> stdErrHandler = null, Action<string> mergedOutputHandler = null)
        {
            var path = FileHelpers.FindFileInHelpPath($"help/include.python.script.{scriptName}.py");
            var script = FileHelpers.ReadAllHelpText(path, Encoding.UTF8);

            if (Program.Debug) Console.WriteLine($"DEBUG: {scriptName}.py:\n{script}");
            if (Program.Debug) Console.WriteLine($"DEBUG: PythonRunner.RunPythonScriptAsync: '{scriptName}' {scriptArgs}");

            var dbgOut = script.Replace("\n", "\\n").Replace("\r", "");
            AI.DBG_TRACE_VERBOSE($"RunEmbeddedPythonScript: {scriptName}.py: {dbgOut}");
            AI.DBG_TRACE_VERBOSE($"RunEmbeddedPythonScript: '{scriptName}' {scriptArgs}");

            (var exit, var output)= PythonRunner.RunPythonScriptAsync(script, scriptArgs, addToEnvironment, stdOutHandler, stdErrHandler, mergedOutputHandler).Result;
            if (exit != 0) AI.DBG_TRACE_WARNING($"RunEmbeddedPythonScript: exit={exit}");

            dbgOut = output.Replace("\n", "\\n").Replace("\r", "");
            AI.DBG_TRACE_INFO($"RunEmbeddedPythonScript: exit={exit}; output:{dbgOut}");
            if (Program.Debug) Console.WriteLine($"DEBUG: RunEmbeddedPythonScript: exit={exit}; output=\n<---start--->{output}\n<---stop--->");

            if (exit != 0)
            {
                output = output.Trim('\r', '\n', ' ');
                output = "\n\n    " + output.Replace("\n", "\n    ");

                var info = new List<string>();

                if (output.Contains("azure.identity"))
                {
                    info.Add("WARNING:");
                    info.Add("azure-identity Python wheel not found!");
                    info.Add("");
                    info.Add("TRY:");
                    info.Add("pip install azure-identity");
                    info.Add("SEE:");
                    info.Add("https://pypi.org/project/azure-identity/");
                    info.Add("");
                }
                else if (output.Contains("azure.mgmt.resource"))
                {
                    info.Add("WARNING:");
                    info.Add("azure-mgmt-resource Python wheel not found!");
                    info.Add("");
                    info.Add("TRY:");
                    info.Add("pip install azure-mgmt-resource");
                    info.Add("SEE:");
                    info.Add("https://pypi.org/project/azure-mgmt-resource/");
                    info.Add("");
                }
                else if (output.Contains("azure.ai.ml"))
                {
                    info.Add("WARNING:");
                    info.Add("azure-ai-ml Python wheel not found!");
                    info.Add("");
                    info.Add("TRY:");
                    info.Add("pip install azure-ai-ml");
                    info.Add("SEE:");
                    info.Add("https://pypi.org/project/azure-ai-ml/");
                    info.Add("");
                }
                else if (output.Contains("ModuleNotFoundError"))
                {
                    info.Add("WARNING:");
                    info.Add("Python wheel not found!");
                    info.Add("");
                }

                info.Add("ERROR:");
                info.Add($"Python script failed! (exit code={exit})");
                info.Add("");
                info.Add("OUTPUT:");
                info.Add(output);

                values.AddThrowError(info[0], info[1], info.Skip(2).ToArray());
            }

            return ParseOutputAndSkipLinesUntilStartsWith(output, "---").Trim('\r', '\n', ' ');
        }

        private static string ParseOutputAndSkipLinesUntilStartsWith(string output, string startsWith)
        {
            var lines = output.Split('\n');
            var sb = new StringBuilder();
            var skip = true;
            foreach (var line in lines)
            {
                if (skip && line.StartsWith(startsWith))
                {
                    skip = false;
                }
                else if (!skip)
                {
                    sb.AppendLine(line);
                }
            }
            return sb.ToString();
        }

        private static string EnsureFindPython()
        {
            if (_pythonBinary == null)
            {
                _pythonBinary = FindPython();
            }
            return _pythonBinary;
        }

        private static string FindPython()
        {
            var process = ProcessHelpers.RunShellCommandAsync("python3", "--version").Result;
            if (process.ExitCode == 0 && process.MergedOutput.Contains("Python 3.")) return "python3";

            process = ProcessHelpers.RunShellCommandAsync("python", "--version").Result;
            if (process.ExitCode == 0 && process.MergedOutput.Contains("Python 3.")) return "python";

            var lastTry = FindPythonBinaryInOsPath();
            process = ProcessHelpers.RunShellCommandAsync("python", "--version").Result;
            if (process.ExitCode == 0 && process.MergedOutput.Contains("Python 3.")) return lastTry;

            return null;
        }

        private static string? FindPythonBinaryInOsPath()
        {
            var search = OperatingSystem.IsWindows()
                ? "python.exe"
                : "python3";

            return FileHelpers.FindFileInOsPath(search);
        }

        private static string? _pythonBinary;
    }
}
