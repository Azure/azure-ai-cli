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
            Console.WriteLine("AIT - Azure AI CLI Test runner, Version 1.0.0");
            Console.WriteLine("Copyright (c) 2024 Microsoft Corporation. All Rights Reserved.");
            Console.WriteLine();
            Console.WriteLine("USAGE: ait list [...]");
            Console.WriteLine("   OR: ait run [...]");
            Console.WriteLine();
            Console.WriteLine("  FILES");
            Console.WriteLine("    --file FILE");
            Console.WriteLine("    --files FILE1 [FILE2 [...]]");
            Console.WriteLine("    --files PATTERN1 [PATTERN2 [...]]");
            Console.WriteLine();
            Console.WriteLine("  FILTERS");
            Console.WriteLine("    --filter FILTER");
            Console.WriteLine("    --filters FILTER1 [FILTER2 [...]]");
            Console.WriteLine();
            Console.WriteLine("EXAMPLES");
            Console.WriteLine();
            Console.WriteLine("  ait list");
            Console.WriteLine("  ait list --files test1.yaml test2.yaml --filter +chat -skip");
            Console.WriteLine();
            Console.WriteLine("  ait run");
            Console.WriteLine("  ait run --filters +nightly -skip");
            Console.WriteLine("  ait run --files ../tests/**/*.yaml --filter +\"best shoes\" -skip");
            return 1;
        }

        private static int DoCommand(string[] args, bool list, bool run)
        {
            var tests = FindAndFilterTests(args);
            if (tests == null) return 1;

            if (list) return DoListTests(tests) ? 0 : 1;
            if (run) return DoRunTests(tests) ? 0 : 1;

            return 1;
        }

        private static IEnumerable<TestCase> FindAndFilterTests(string[] args)
        {
            var parsedOk = ParseFilesAndFilterArgs(args, out var files, out var filters);
            if (!parsedOk) return null;

            var atLeastOneFileSpecified = files.Any();
            var tests = atLeastOneFileSpecified
                ? files.SelectMany(file => YamlTestFramework.GetTestsFromYaml(file.FullName, file)).ToList()
                : YamlTestFramework.GetTestsFromDirectory("ait", new DirectoryInfo(".")).ToList();

            return YamlTestCaseFilter.FilterTestCases(tests, filters);
        }

        private static bool ParseFilesAndFilterArgs(string[] args, out IList<FileInfo> files, out IList<string> filters)
        {
            var filesAsList = new List<FileInfo>();
            files = filesAsList;
            filters = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--file" || args[i] == "--files")
                {
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--"))
                    {
                        Console.WriteLine($"Expected a file or pattern after '{args[i]}'.");
                        return false;
                    }

                    do
                    {
                        i++;
                        var pattern = args[i];
                        var found = FindFiles(pattern);
                        if (found.Count() == 0)
                        {
                            Console.WriteLine($"No files found for pattern '{pattern}'.");
                            return false;
                        }

                        filesAsList.AddRange(found);
                    }
                    while (i + 1 < args.Length && !args[i + 1].StartsWith("--"));
                }
                else if (args[i] == "--filter" || args[i] == "--filters")
                {
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--"))
                    {
                        Console.WriteLine($"Expected a filter after '{args[i]}'.");
                        return false;
                    }

                    do
                    {
                        i++;
                        filters.Add(args[i]);
                    }
                    while (i + 1 < args.Length && !args[i + 1].StartsWith("--"));
                }
                else
                {
                    Console.WriteLine($"Invalid command line argument at '{args[i]}'.");
                    return false;
                }
            }

            return true;
        }

        private static IList<FileInfo> FindFiles(string pattern)
        {
            var files = FileHelpers.FindFiles(Directory.GetCurrentDirectory(), pattern, null, false, false);
            return files.Select(x => new FileInfo(x)).ToList();
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
