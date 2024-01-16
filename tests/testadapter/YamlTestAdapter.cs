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

namespace TestAdapterTest
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

            return file.Extension.Trim('.') == FileExtensionYaml.Trim('.')
                ? GetTestsFromYaml(source, file)
                : GetTestsFromSource(source, file);
        }

        public static void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var parallelWorkers = Environment.ProcessorCount;
            Logger.Log($"YamlTestAdapter.RunTests(): {parallelWorkers} parallel Workers");
            // Must run before, middle, and after testSets in certain order so cannot parallelize those
            // Can parallelize tests within each testSet
            foreach (var testSet in FilterTestCases(tests, runContext, frameworkHandle))
            {
                if (!testSet.Any()) continue;
                var parallelTestSet = testSet.Where(test => YamlTestProperties.Get(test, "parallelize") == "true");
                var nonParallelTestSet = testSet.Where(test => YamlTestProperties.Get(test, "parallelize") != "true");

                var workerBlock = new ActionBlock<TestCase>(
                    test => RunAndRecordTestCase(test, frameworkHandle),
                    new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = parallelWorkers });
                foreach (var test in parallelTestSet)
                {
                    workerBlock.Post(test);
                }
                workerBlock.Complete();
                workerBlock.Completion.Wait();

                foreach (var test in nonParallelTestSet)
                {
                    RunAndRecordTestCase(test, frameworkHandle);
                }
            }
        }

        #region private methods

        private static IEnumerable<TestCase> GetTestsFromSource(string source, FileInfo file)
        {
            var sourceOk =
                source.Contains("Azure.AI.CLI.TestAdapter") ||
                Assembly.LoadFile(source).GetReferencedAssemblies().Count(x => x.Name.Contains("Azure.AI.CLI.TestAdapter")) > 0;

            // foreach (var a in Assembly.LoadFile(source).GetReferencedAssemblies())
            // {
            //     Logger.Log($"a.Name={a.Name}");
            //     Logger.Log($"a.FullName={a.FullName}");
            // }

            Logger.Log($"YamlTestAdapter.GetTestsFromSource('{source}'): sourceOk = {sourceOk}");

            return !sourceOk
                ? Enumerable.Empty<TestCase>()
                : GetTestsFromDirectory(source, file.Directory);
        }

        private static IEnumerable<TestCase> GetTestsFromDirectory(string source, DirectoryInfo directory)
        {
            Logger.Log($"YamlTestAdapter.GetTestsFromDirectory('{source}', '{directory.FullName}'): ENTER");
            foreach (var file in FindFiles(directory))
            {
                foreach (var test in GetTestsFromYaml(source, file))
                {
                    yield return test;
                }
            }
           Logger.Log($"YamlTestAdapter.GetTestsFromDirectory('{source}', '{directory.FullName}'): EXIT");
        }

        private static IEnumerable<FileInfo> FindFiles(DirectoryInfo directory)
        {
            return directory.GetFiles($"*{FileExtensionYaml}", SearchOption.AllDirectories)
                .Where(file => file.Name != YamlDefaultsFileName);
        }

        private static IEnumerable<TestCase> GetTestsFromYaml(string source, FileInfo file)
        {
            Logger.Log($"YamlTestAdapter.GetTestsFromYaml('{source}', '{file.FullName}'): ENTER");
            foreach (var test in YamlTestCaseParser.TestCasesFromYaml(source, file))
            {
                yield return test;
            }
           Logger.Log($"YamlTestAdapter.GetTestsFromYaml('{source}', '{file.FullName}'): EXIT");
        }

        private static bool IsTrait(Trait trait, string check)
        {
            return trait.Name == check || trait.Value == check;
        }

        private static IEnumerable<IEnumerable<TestCase>> FilterTestCases(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            Logger.Log($"YamlTestAdapter.FilterTestCases()");

            tests = YamlTestCaseFilter.FilterTestCases(tests, runContext, frameworkHandle);
            
            var before = tests.Where(test => test.Traits.Count(x => IsTrait(x, "before")) > 0);
            var after = tests.Where(test => test.Traits.Count(x => IsTrait(x, "after")) > 0);
            var middle = tests.Where(test => !before.Contains(test) && !after.Contains(test));

            var testsList = new List<IEnumerable<TestCase>> { before, middle, after };
            Logger.Log("YamlTestAdapter.FilterTestCases() ==> {string.Join('\n', tests.Select(x => x.Name))}");

            return testsList;
        }

        private static TestOutcome RunAndRecordTestCase(TestCase test, IFrameworkHandle frameworkHandle)
        {
            Logger.Log($"YamlTestAdapter.TestRunAndRecord({test.DisplayName})");
            return YamlTestCaseRunner.RunAndRecordTestCase(test, frameworkHandle);
        }

        #endregion

        #region test adapter registration data
        public const string FileExtensionDll = ".dll";
        public const string FileExtensionYaml = ".yaml";
        public const string Executor = "executor://spx/yaml/VsTestRunner1";
        #endregion

        #region other constants
        public const string YamlDefaultsFileName = "Azure-AI-CLI-TestRunner-Defaults.yaml";
        public const string DefaultTimeout = "600000";
        #endregion
    }
}
