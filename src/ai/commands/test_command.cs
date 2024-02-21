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

namespace Azure.AI.Details.Common.CLI
{
    public class TestCommand : Command
    {
        internal TestCommand(ICommandValues values)
        {
            _values = values.ReplaceValues();
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
            var tests = YamlTestFramework.GetTestsFromDirectory("ait", new DirectoryInfo(".")).ToList();
            foreach (var test in tests)
            {
                Console.WriteLine(test.FullyQualifiedName);
            }
        }

        private void DoTestRun()
        {
            var tests = YamlTestFramework.GetTestsFromDirectory("ait", new DirectoryInfo(".")).ToList();
            var consoleHost = new YamlTestFrameworkConsoleHost();
            var resultsByTestCaseId = YamlTestFramework.RunTests(tests, consoleHost);
            consoleHost.Finish(resultsByTestCaseId);
        }

        private void StartCommand()
        {
            CheckPath();
            LogHelpers.EnsureStartLogFile(_values);

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
