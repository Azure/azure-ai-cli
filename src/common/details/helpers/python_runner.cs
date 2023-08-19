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
    internal class PythonRunner
    {
        internal static async Task<(int, string)> RunScriptAsync(string script, string args = null)
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
                return await ProcessHelpers.RunShellCommandAsync(_pythonBinary, args);
            }
            finally
            {
                File.Delete(tempFile);
            }
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
            (var code, var version) = ProcessHelpers.RunShellCommandAsync("python3", "--version").Result;
            if (code == 0 && version.Contains("Python 3.")) return "python3";

            (code, version) = ProcessHelpers.RunShellCommandAsync("python", "--version").Result;
            if (code == 0 && version.Contains("Python 3.")) return "python";

            var lastTry = FindPythonBinaryInOsPath();
            (code, version) = ProcessHelpers.RunShellCommandAsync("python", "--version").Result;
            if (code == 0 && version.Contains("Python 3.")) return lastTry;

            return null;
        }

        private static string? FindPythonBinaryInOsPath()
        {
            var search = OperatingSystem.IsWindows()
                ? "python.exe"
                : "python3";

            var lookIn = Environment.GetEnvironmentVariable("PATH")!.Split(System.IO.Path.PathSeparator);
            var found = lookIn.SelectMany(x =>
            {
                try
                {
                    return System.IO.Directory.GetFiles(x, search);
                }
                catch (Exception)
                {
                    return Enumerable.Empty<string>();
                }
            });
            return found.FirstOrDefault();
        }

        private static string? _pythonBinary;
    }
}
