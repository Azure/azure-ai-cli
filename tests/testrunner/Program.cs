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

namespace Azure.AI.Details.Common.CLI.TestRunner
{
    public class Program
    {
        public static bool Debug { get; internal set; }

        public static int Main(string[] mainArgs)
        {
            var tests = YamlTestFramework.GetTestsFromDirectory("TestRunner", new DirectoryInfo("d:\\src\\ai-cli\\tests"));

            foreach (var test in tests)
            {
                Console.WriteLine(test.FullyQualifiedName);
            }

            // YamlTestFramework.RunTests(tests, null, null);

            return 0;
        }
    }
}

