using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public class YamlTestFrameworkConsoleHost : IYamlTestFrameworkHost
    {
        public YamlTestFrameworkConsoleHost()
        {
        }

        public void RecordStart(TestCase testCase)
        {
            _testRun.StartTest(testCase);

            lock (this)
            {
                Console.ForegroundColor = ColorHelpers.MapColor(ConsoleColor.DarkGray);
                Console.WriteLine("Starting test: " + testCase.FullyQualifiedName);
                Console.ResetColor();
            }
        }

        public void RecordResult(TestResult testResult)
        {
            _testRun.RecordTest(testResult);
            PrintResult(testResult);
        }

        public void RecordEnd(TestCase testCase, TestOutcome outcome)
        {
            _testRun.EndTest(testCase, outcome);
        }

        public bool Finish(IDictionary<string, IList<TestResult>> resultsByTestCaseId)
        {
            _testRun.EndRun();

            var allResults = resultsByTestCaseId.Values.SelectMany(x => x);
            var failedResults = allResults.Where(x => x.Outcome == TestOutcome.Failed).ToList();
            var passedResults = allResults.Where(x => x.Outcome == TestOutcome.Passed).ToList();
            var skippedResults = allResults.Where(x => x.Outcome == TestOutcome.Skipped).ToList();
            var passed = failedResults.Count == 0;

            if (failedResults.Count > 0)
            {
                Console.ResetColor();
                Console.WriteLine();
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("FAILURE SUMMARY:");
                Console.ResetColor();
                Console.WriteLine();
                failedResults.ForEach(r => PrintResult(r));
            }
            else
            {
                Console.WriteLine();
            }

            var count = allResults.Count();
            var duration = TimeSpanFormatter.FormatMsOrSeconds(_testRun.Duration);
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("TEST RESULT SUMMARY:");

            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"\nPassed: {passedResults.Count}");
            Console.ForegroundColor = ColorHelpers.MapColor(ConsoleColor.DarkGray);
            Console.WriteLine($" ({100f * passedResults.Count / count:0.0}%)");

            if (failedResults.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"Failed: {failedResults.Count}");
                Console.ForegroundColor = ColorHelpers.MapColor(ConsoleColor.DarkGray);
                Console.WriteLine($" ({100f * failedResults.Count / count:0.0}%)");
            }

            if (skippedResults.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"Skipped: {skippedResults.Count}");
                Console.ForegroundColor = ColorHelpers.MapColor(ConsoleColor.DarkGray);
                Console.WriteLine($" ({100f * skippedResults.Count / count:0.0}%)");
            }

            Console.ResetColor();
            Console.Write("\nTests: ");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write($"{count}");
            Console.ForegroundColor = ColorHelpers.MapColor(ConsoleColor.DarkGray);
            Console.WriteLine($" ({duration})");

            PrintResultsFile(TrxXmlTestReporter.WriteResultFile(_testRun));
            // PrintResultsFile(JunitXmlTestReporter.WriteResultFile(_testRun));

            return passed;
        }

        private static void PrintResultsFile(string resultsFile)
        {
            var fi = new FileInfo(resultsFile);
            Console.ResetColor();
            Console.Write("Results: ");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(fi.FullName);
            Console.ResetColor();
            Console.WriteLine();
        }

        private void PrintResult(TestResult testResult)
        {
            lock (this)
            {
                Console.ForegroundColor = ColorHelpers.MapColor(ConsoleColor.DarkGray);
                if (testResult.Outcome == TestOutcome.Passed) Console.ForegroundColor = ConsoleColor.Green;
                if (testResult.Outcome == TestOutcome.Skipped) Console.ForegroundColor = ConsoleColor.Yellow;
                if (testResult.Outcome == TestOutcome.Failed) Console.ForegroundColor = ConsoleColor.Red;

                var duration = TimeSpanFormatter.FormatMsOrSeconds(testResult.Duration);
                Console.WriteLine($"{testResult.Outcome} ({duration}): {testResult.TestCase.FullyQualifiedName}");
                Console.ResetColor();

                if (testResult.Outcome == TestOutcome.Failed)
                {
                    var codeFilePath = testResult.TestCase.CodeFilePath;
                    var hasCodeFilePath = !string.IsNullOrEmpty(codeFilePath);
                    if (hasCodeFilePath)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"at {codeFilePath}({testResult.TestCase.LineNumber})");
                    }

                    var stack = testResult.ErrorStackTrace;
                    var hasStack = !string.IsNullOrEmpty(stack);
                    if (hasStack)
                    {
                        Console.ForegroundColor = ColorHelpers.MapColor(ConsoleColor.DarkGray);
                        Console.WriteLine(stack.TrimEnd('\r', '\n', ' '));
                    }

                    var stdErr = testResult.Messages.FirstOrDefault(x => x.Category == TestResultMessage.StandardErrorCategory)?.Text;
                    var hasStdErr = !string.IsNullOrEmpty(stdErr);
                    if (hasStdErr)
                    {
                        var lines = stdErr.Split('\n');
                        if (lines.Length > 10)
                        {
                            var first5 = lines.Take(5);
                            var last5 = lines.Skip(lines.Length - 5);
                            lines = first5.Concat(new[] { $"[ ******* ------- TRIMMED +{lines.Length - 10} LINE(s) ------- ******* ]" }).Concat(last5).ToArray();
                            stdErr = string.Join("\n", lines) + "\n...";
                        }
                        Console.ForegroundColor = ColorHelpers.MapColor(ConsoleColor.DarkGray);
                        Console.WriteLine(stdErr.TrimEnd('\r', '\n', ' '));
                    }

                    var err = testResult.ErrorMessage;
                    var hasErr = !string.IsNullOrEmpty(err);
                    if (hasErr)
                    {
                        Console.ResetColor();
                        Console.WriteLine(err.TrimEnd('\r', '\n', ' '));
                    }

                    Console.ResetColor();
                    if (hasStack || hasStdErr || hasErr || hasCodeFilePath) Console.WriteLine();
                }
            }
        }

        private TestRun _testRun = new();
    }
}

