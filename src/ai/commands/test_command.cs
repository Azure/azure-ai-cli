//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Azure.AI.Details.Common.CLI.Extensions.Templates;
using Azure.AI.Details.Common.CLI.TestFramework;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Azure.AI.Details.Common.CLI
{
    public class TestCommand : Command
    {
        internal TestCommand(ICommandValues values) : base(values)
        {
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", false);
        }

        internal bool RunCommand()
        {
            try
            {
                RunTestCommand();
            }
            catch (WebException ex)
            {
                ConsoleHelpers.WriteLineError($"\n  ERROR: {ex.Message}");
                JsonHelpers.PrintJson(HttpHelpers.ReadWriteJson(ex.Response, _values, "test"));
            }

            return _values.GetOrDefault("passed", true);
        }

        private bool RunTestCommand()
        {
            DoCommand(_values.GetCommand());
            return _values.GetOrDefault("passed", true);
        }

        private void DoCommand(string command)
        {
            StartCommand();

            switch (command)
            {
                case "test.list": DoTestList(); break;
                case "test.run": DoTestRun(); break;

                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
            }

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private void DoTestList()
        {
            var tests = FindAndFilterTests();

            Console.ForegroundColor = ColorHelpers.MapColor(ConsoleColor.DarkGray);
            foreach (var test in tests)
            {
                Console.WriteLine(test.FullyQualifiedName);
            }
            Console.ResetColor();

            if (!_quiet)
            {
                Console.WriteLine(tests.Count() == 1
                    ? $"\nFound {tests.Count()} test..."
                    : $"\nFound {tests.Count()} tests...");
            }
        }

        private void DoTestRun()
        {
            var tests = FindAndFilterTests();

            if (!_quiet)
            {
                Console.WriteLine(tests.Count() == 1
                    ? $"Found {tests.Count()} test...\n"
                    : $"Found {tests.Count()} tests...\n");
            }

            var consoleHost = new YamlTestFrameworkConsoleHost();
            var resultsByTestCaseId = YamlTestFramework.RunTests(tests, consoleHost);

            GetOutputFileAndFormat(out var file, out var format);
            consoleHost.Finish(resultsByTestCaseId, format, file);
        }

        private IList<TestCase> FindAndFilterTests()
        {
            var files = FindTestFiles();
            var filters = GetTestFilters();

            var atLeastOneFileSpecified = files.Any();
            var tests = atLeastOneFileSpecified
                ? files.SelectMany(file => YamlTestFramework.GetTestsFromYaml(file.FullName, file))
                : YamlTestFramework.GetTestsFromDirectory("ai test", new DirectoryInfo("."));

            var filtered = YamlTestCaseFilter.FilterTestCases(tests, filters).ToList();

            if (tests.Count() == 0)
            {
                _values.AddThrowError("WARNING:", !atLeastOneFileSpecified
                    ? "No tests found"
                    : files.Count() == 1
                        ? $"No tests found in {files.Count()} file"
                        : $"No tests found in {files.Count()} files");
            }
            
            if (filtered.Count() == 0)
            {
                Console.WriteLine(atLeastOneFileSpecified
                    ? $"Found {tests.Count()} tests in {files.Count()} files\n"
                    : $"Found {tests.Count()} tests\n");

                _values.AddThrowError("WARNING:", "No tests matching criteria.");
            }

            return filtered;
        }

        private List<string> GetTestFilters()
        {
            var filters = new List<string>();

            var options = SearchOptionXToken.GetOptions(_values).ToList();
            options.AddRange(TestOptionXToken.GetOptions(_values).ToList());
            options.AddRange(TestsOptionXToken.GetOptions(_values));
            foreach (var item in options)
            {
                filters.Add(item);
            }

            options = ContainsOptionXToken.GetOptions(_values).ToList();
            foreach (var item in options)
            {
                filters.Add($"+{item}");
            }

            options = RemoveOptionXToken.GetOptions(_values).ToList();
            foreach (var item in options)
            {
                filters.Add($"-{item}");
            }

            return filters;
        }

        private List<FileInfo> FindTestFiles()
        {
            var files = new List<FileInfo>();

            var options = FileOptionXToken.GetOptions(_values).ToList();
            options.AddRange(FilesOptionXToken.GetOptions(_values).ToList());
            foreach (var item in options)
            {
                var patterns = item.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var pattern in patterns)
                {
                    AddFindFiles(files, pattern);
                }
            }

            return files;
        }

        private void AddFindFiles(List<FileInfo> filesAsList, string pattern)
        {
            var found = FindFiles(pattern);
            if (found.Count() == 0)
            {
                _values.AddThrowError("WARNING:", $"No files found: {pattern}");
            }
            filesAsList.AddRange(found);
        }

        private static IList<FileInfo> FindFiles(string pattern)
        {
            var files = FileHelpers.FindFiles(Directory.GetCurrentDirectory(), pattern, null, false, false);
            return files.Select(x => new FileInfo(x)).ToList();
        }

        private void GetOutputFileAndFormat(out string file, out string format)
        {
            format = OutputResultsFormatToken.Data().GetOrDefault(_values, "trx");
            var ext = format switch
            {
                "trx" => "trx",
                "junit" => "xml",
                _ => throw new Exception($"Unknown format: {format}")
            };

            file = OutputResultsFileToken.Data().GetOrDefault(_values, null);
            file ??= $"test-results.{ext}";
            if (!file.EndsWith($".{ext}"))
            {
                file += $".{ext}";
            }
        }

        private void StartCommand()
        {
            CheckPath();
            LogHelpers.EnsureStartLogFile(_values);
            Logger.Log(new AiCliTestFrameworkLogger());

            // _display = new DisplayHelper(_values);

            // _output = new OutputHelper(_values);
            // _output.StartOutput();

            _lock = new SpinLock();
            _lock.StartLock();
        }

        private void StopCommand()
        {
            _lock.StopLock(5000);

            // LogHelpers.EnsureStopLogFile(_values);
            // _output.CheckOutput();
            // _output.StopOutput();

            _stopEvent.Set();
        }

        private SpinLock _lock = null;
        private readonly bool _quiet;
        private readonly bool _verbose;
    }
}
