using Azure.AI.Details.Common.CLI.TestFramework;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Azure.AI.Details.Common.CLI.RecordedTestAdapter
{
    public class RecordedTestAdapter
    {
        public static IEnumerable<TestCase> GetTestsFromFiles(IEnumerable<string> sources)
        {
            Logger.Log($"RecordedTestAdapter.GetTestsFromFiles(source.Count={sources.Count()})");

            var tests = new List<TestCase>();
            foreach (var source in sources)
            {
                Logger.Log($"RecordedTestAdapter.GetTestsFromFiles('{source}')");
                tests.AddRange(GetTestsFromFile(source));
            }

            Logger.Log($"RecordedTestAdapter.GetTestsFromFiles() found count={tests.Count()}");
            return tests;
        }

        public static IEnumerable<TestCase> GetTestsFromFile(string source)
        {
           Logger.Log($"RecordedTestAdapter.GetTestsFromFile('{source}')");

           var file = new FileInfo(source);
           Logger.Log($"RecordedTestAdapter.GetTestsFromFile('{source}'): Extension={file.Extension}");

            var tests=  file.Extension.Trim('.') == YamlTestFramework.YamlFileExtension.Trim('.')
                ? YamlTestFramework.GetTestsFromYaml(source, file)
                : GetTestsFromRecordedTestAdapterOrReferenceDirectory(source, file);

            return tests;
        }

        public static void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var filtered = YamlTestCaseFilter.FilterTestCases(tests, runContext);
            foreach (var test in filtered)
            {
                YamlTestProperties.Set(test, "parallelize", "false");
            }
            Environment.SetEnvironmentVariable("HTTPS_PROXY", "localhost:5004");
            YamlTestFramework.RunTests(filtered, new RecordedTestObservor( frameworkHandle));
            Environment.SetEnvironmentVariable("HTTPS_PROXY", null);

        }

        #region private methods

        private static IEnumerable<TestCase> GetTestsFromRecordedTestAdapterOrReferenceDirectory(string source, FileInfo file)
        {
            var sourceOk =
                source.Contains("Azure.AI.CLI.RecordedTestAdapter") ||
                Assembly.LoadFile(source).GetReferencedAssemblies().Count(x => x.Name.Contains("Azure.AI.CLI.RecordedTestAdapter")) > 0;

            // foreach (var a in Assembly.LoadFile(source).GetReferencedAssemblies())
            // {
            //     Logger.Log($"a.Name={a.Name}");
            //     Logger.Log($"a.FullName={a.FullName}");
            // }

            Logger.Log($"RecordedTestAdapter.GetTestsFromRecordedTestAdapterOrReferenceDirectory('{source}'): sourceOk = {sourceOk}");

            return !sourceOk
                ? Enumerable.Empty<TestCase>()
                : YamlTestFramework.GetTestsFromDirectory(source, file.Directory);
        }

        #endregion

        #region recording adapter registration data
        public const string DllFileExtension = ".dll";
        public const string Executor = "executor://ai/cli/RecordedTestAdapter/v1";
        #endregion
    }
}