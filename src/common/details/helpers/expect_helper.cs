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
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace Azure.AI.Details.Common.CLI
{
    public class ExpectHelper
    {
        public static async Task<bool> CheckExpectedLinesAsync(IAsyncEnumerable<string> lines, IEnumerable<string> expected, IEnumerable<string> unexpected, bool autoExpect, bool echoStdOut)
        {
            // use the existing (unhooked) console output, for auto expect
            Action<string> writeLine = Console.WriteLine;
            Action<string>? autoExpectOutput = autoExpect ? writeLine : null;
            TextWriter? echoStdOutWriter = echoStdOut ? Console.Out : null;

            // instance the helper, and await it's completion
            var helper = new ExpectHelper(lines, expected, unexpected, autoExpectOutput, echoStdOutWriter, Console.Out);
            return await helper.ExpectAsync();
        }

        public static async Task<bool> CheckExpectedConsoleOutputAsync(Func<bool> consoleOutputProducingFunction, IEnumerable<string> expected, IEnumerable<string> unexpected, bool autoExpect, bool echoStdOut, bool ignoreCheckFailures)
        {
            // hook the existing console output (both stdout and stderr), using TextWriterReadLineHelpers
            var oldStdOut = Console.Out;
            var oldStdErr = Console.Error;
            var newStdOut = new TextWriterReadLineHelper();
            var newStdErr = new TextWriterReadLineHelper();
            Console.SetOut(newStdOut);
            Console.SetError(newStdErr);

            var stdOutLines = newStdOut.ReadAllLinesAsync();
            var stdErrLines = newStdErr.ReadAllLinesAsync();
            var allLines = AsyncEnumerableEx.Merge<string>(stdOutLines, stdErrLines);

            // use the original console output, for auto expect and echoing output
            Action<string> writeLine = oldStdOut.WriteLine;
            Action<string>? autoExpectOutput = autoExpect ? writeLine : null;
            TextWriter? echoStdOutWriter = echoStdOut ? oldStdOut : null;

            // instance the helper, and start the ExpectAsync task
            var helper = new ExpectHelper(allLines, expected, unexpected, autoExpectOutput, echoStdOutWriter, oldStdOut);
            var expectTask = helper.ExpectAsync();

            // run the function, safely
            var funcResult = TryCatchHelpers.TryCatchNoThrow<bool>(consoleOutputProducingFunction, false, out var functionThrewException);

            // now ... close the console output, restoring the original
            // this will trigger the end of expectation input
            Console.Out.Close();
            Console.Error.Close();

            // unhook the capture, restoring the original stdout and stderr
            Console.SetOut(oldStdOut);
            Console.SetError(oldStdErr);

            // after closing, wait for ExpectAsync to complete
            var expectResult = await expectTask;

            // if the function threw, rethrow
            if (functionThrewException != null) throw functionThrewException;

            // for scenarios where we want to report the "expected" results isolated from status of API interactions
            if (ignoreCheckFailures)
            {
                return expected == null && unexpected == null
                    ? funcResult        // return the functional result, if we were only doing auto expect
                    : expectResult;     // return the expectation result, if we were checking expectations
            }
            else
            {
                return funcResult && expectResult;
            }
        }

        public static async Task<bool> CheckExpectedConsoleOutputAsync(Process process, IEnumerable<string> expected, IEnumerable<string> unexpected, bool autoExpect, bool echoStdOut)
        {
            // hook the process console output (both stdout and stderr)
            var stdOutLines = process.StandardOutput.ReadAllLinesAsync();
            var stdErrLines = process.StandardError.ReadAllLinesAsync();
            var allLines = AsyncEnumerableEx.Merge<string>(stdOutLines, stdErrLines);

            // use the existing (unhooked) console output, for auto expect and echoing output
            Action<string> writeLine = Console.WriteLine;
            Action<string>? autoExpectOutput = autoExpect ? writeLine : null;
            TextWriter? echoStdOutWriter = echoStdOut ? Console.Out : null;

            // instance the helper, and await it's completion
            var helper = new ExpectHelper(allLines, expected, unexpected, autoExpectOutput, echoStdOutWriter, Console.Out);
            return await helper.ExpectAsync();
        }

        #region private methods

        private ExpectHelper(IAsyncEnumerable<string> lines, IEnumerable<string> expected, IEnumerable<string> unexpected, Action<string>? autoExpectOutput, TextWriter? echoStdOutWriter, TextWriter errorWriter)
        {
            this.allLines = lines;
            this.expected = expected != null ? new Queue<string>(expected) : null;
            this.unexpected = unexpected != null ? new List<string>(unexpected) : null;
            this.autoExpectOutput = autoExpectOutput;

            this.echoStdOutWriter = echoStdOutWriter;
            this.errorWriter = errorWriter;
        }

        private async Task<bool> ExpectAsync()
        {
            await foreach (string line in allLines)
            {
                echoStdOutWriter?.WriteLine(line);
                if (autoExpectOutput != null) AutoExpectOutput(line);
                if (expected != null) CheckExpected(line);
                if (unexpected != null) CheckUnexpected(line);
            }

            var allExpectedFound = expected == null || expected.Count == 0;
            if (!allExpectedFound && errorWriter != null)
            {
                ColorHelpers.SetErrorColors();
                errorWriter.WriteLine($"UNEXPECTED: Couldn't find '{expected!.Peek()}' in:\n```\n{unmatchedInput}```");
                ColorHelpers.ResetColor();
            }

            return !foundUnexpected && allExpectedFound;
        }

        private void AutoExpectOutput(string line)
        {
            var regexEscapedLine = line.Replace("\\", "\\\\")
                .Replace("*", "\\*").Replace("+", "\\+")
                .Replace(".", "\\.").Replace("?", "\\?")
                .Replace("[", "\\[").Replace("]", "\\]")
                .Replace("(", "\\(").Replace(")", "\\)");
            autoExpectOutput!($"^{regexEscapedLine}\\r?$\\n");
        }

        private void CheckExpected(string line)
        {
            unmatchedInput.AppendLine(line);
            while (expected!.Count > 0)
            {
                var pattern = expected.Peek();
                var check = unmatchedInput.ToString();

                var match = Regex.Match(check, pattern);
                if (!match.Success) break; // continue reading input...

                unmatchedInput.Remove(0, match.Index + match.Length);
                expected.Dequeue();
            }
        }

        private void CheckUnexpected(string line)
        {
            foreach (var pattern in unexpected!)
            {
                var match = Regex.Match(line, pattern);
                if (!match.Success) continue; // check more patterns

                foundUnexpected = true;

                if (errorWriter != null)
                {
                    ColorHelpers.SetErrorColors();
                    errorWriter.WriteLine($"UNEXPECTED: Found '{pattern}' in '{line}'");
                    ColorHelpers.ResetColor();
                }
            }
        }

        #endregion

        #region private data
        private StringBuilder unmatchedInput = new StringBuilder();
        private IAsyncEnumerable<string> allLines;

        private Queue<string>? expected;
        private List<string>? unexpected;
        bool foundUnexpected = false;

        private Action<string>? autoExpectOutput;
        private TextWriter? echoStdOutWriter;
        private TextWriter? errorWriter;

        #endregion

        #region future

        // public static async Task<bool> ExpectAsync(Func<Task<string>> input, IEnumerable<string>? expected, IEnumerable<string>? unexpected, Action<string>? autoExpectOutput)
        // {
        //     var expectedItems = expected != null ? new Queue<string>(expected) : null;
        //     var unexpectedItems = unexpected != null ? new List<string>(unexpected) : null;
        //     var helper = new ExpectHelper(input, expectedItems, unexpectedItems, autoExpectOutput);
        //     return await helper.ExpectAsync();
        // }

        // private ExpectHelper(Func<Task<string>> input, Queue<string>? expected, List<string>? unexpected, Action<string>? autoExpectOutput)
        // {
        //     // this.readAllLinesAsync = input;
        //     // this.expected = expected;
        //     // this.unexpected = unexpected;
        //     // this.autoExpectOutput = autoExpectOutput;
        // }

        #endregion
    }
}
