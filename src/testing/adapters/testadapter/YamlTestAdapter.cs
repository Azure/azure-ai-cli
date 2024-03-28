//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Azure.AI.Details.Common.CLI.TestFramework;

namespace Azure.AI.Details.Common.CLI.TestAdapter
{
    public class YamlTestAdapter
    {
        public static IEnumerable<TestCase> GetTestsFromFiles(IEnumerable<string> sources)
        {
            Logger.Log($"YamlTestAdapter.GetTestsFromFiles(source.Count={sources.Count()})");

            var tests = new List<TestCase>();
            foreach (var source in sources)
            {
                Logger.Log($"YamlTestAdapter.GetTestsFromFiles('{source}')");
                tests.AddRange(GetTestsFromFile(source));
            }

            Logger.Log($"YamlTestAdapter.GetTestsFromFiles() found count={tests.Count()}");
            return tests;
        }

        public static IEnumerable<TestCase> GetTestsFromFile(string source)
        {
           Logger.Log($"YamlTestAdapter.GetTestsFromFile('{source}')");

           var file = new FileInfo(source);
           Logger.Log($"YamlTestAdapter.GetTestsFromFile('{source}'): Extension={file.Extension}");

            return file.Extension.Trim('.') == YamlTestFramework.YamlFileExtension.Trim('.')
                ? YamlTestFramework.GetTestsFromYaml(source, file).ToList()
                : GetTestsFromTestAdapterOrReferenceDirectory(source, file).ToList();
        }

        public static void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var filtered = YamlTestCaseFilter.FilterTestCases(tests, runContext);
            YamlTestFramework.RunTests(filtered, new YamlTestFrameworkHandleHost(frameworkHandle));
        }

        #region private methods

        private static IEnumerable<TestCase> GetTestsFromTestAdapterOrReferenceDirectory(string source, FileInfo file)
        {
            var sourceOk =
                source.Contains("Azure.AI.CLI.TestAdapter") ||
                Assembly.LoadFile(source).GetReferencedAssemblies().Count(x => x.Name.Contains("Azure.AI.CLI.TestAdapter")) > 0;

            // foreach (var a in Assembly.LoadFile(source).GetReferencedAssemblies())
            // {
            //     Logger.Log($"a.Name={a.Name}");
            //     Logger.Log($"a.FullName={a.FullName}");
            // }

            Logger.Log($"YamlTestAdapter.GetTestsFromTestAdapterOrReferenceDirectory('{source}'): sourceOk = {sourceOk}");

            return !sourceOk
                ? Enumerable.Empty<TestCase>()
                : YamlTestFramework.GetTestsFromDirectory(source, file.Directory);
        }

        #endregion

        #region test adapter registration data
        public const string DllFileExtension = ".dll";
        public const string Executor = "executor://ai/cli/TestAdapter/v1";
        #endregion
    }
}
