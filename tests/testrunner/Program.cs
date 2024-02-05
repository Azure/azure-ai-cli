//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Azure.AI.Details.Common.CLI.TestFramework;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Azure.AI.Details.Common.CLI.TestRunner
{
    public class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                return DisplayUsage();
            }

            var command = args[0];
            return command switch
            {
                "list" => DoCommand(args.Skip(1).ToArray(), true, false),
                "run" => DoCommand(args.Skip(1).ToArray(), false, true),
                _ => DisplayUsage()
            };
        }

        private static int DisplayUsage()
        {
            Console.WriteLine("USAGE: TestRunner COMMAND [test-directory]\n");
            Console.WriteLine("COMMANDS\n");
            Console.WriteLine("  list  - List all tests in the specified directory.");
            Console.WriteLine("  run   - Run all tests in the specified directory.");
            return 1;
        }

        private static int DoCommand(string[] args, bool list, bool run)
        {
            var argOrCwd = args.Length > 0 ? args[0] : ".";
            var testDirectory = new DirectoryInfo(argOrCwd);
            if (!testDirectory.Exists)
            {
                Console.WriteLine($"Directory '{testDirectory.FullName}' does not exist.");
                return 1;
            }

            var tests = YamlTestFramework.GetTestsFromDirectory("TestRunner", testDirectory);

            if (list) return DoListTests(tests) ? 0 : 1;
            if (run) return DoRunTests(tests) ? 0 : 1;

            return 1;
        }

        private static bool DoListTests(IEnumerable<TestCase> tests)
        {
            foreach (var test in tests)
            {
                Console.WriteLine(test.FullyQualifiedName);
            }

            return true;
        }

        private static bool DoRunTests(IEnumerable<TestCase> tests)
        {
            var consoleHost = new YamlTestFrameworkConsoleHost();
            var resultsByTestCaseId = YamlTestFramework.RunTests(tests, consoleHost);
            return consoleHost.Finish(resultsByTestCaseId);
        }
    }
}
