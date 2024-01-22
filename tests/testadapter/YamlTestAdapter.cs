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
            var filteredBeforeMiddleAndAfterTestSets = FilterTestCases(tests, runContext, frameworkHandle);
            foreach (var testSet in filteredBeforeMiddleAndAfterTestSets)
            {
                if (!testSet.Any()) continue;
                RunAndRecordTests(frameworkHandle, testSet);
            }
        }

        #region private methods

        private static void RunAndRecordTests(IFrameworkHandle frameworkHandle, IEnumerable<TestCase> tests)
        {
            InitRunAndRecordTestCaseMaps(tests, out var testFromIdMap, out var completionFromIdMap);
            RunAndRecordParallelizedTestCases(frameworkHandle, testFromIdMap, completionFromIdMap, tests);
            RunAndRecordRemainingTestCases(frameworkHandle, testFromIdMap, completionFromIdMap);
        }

        private static void InitRunAndRecordTestCaseMaps(IEnumerable<TestCase> tests, out Dictionary<string, TestCase> testFromIdMap, out Dictionary<string, TaskCompletionSource<TestOutcome>> completionFromIdMap)
        {
            testFromIdMap = new Dictionary<string, TestCase>();
            completionFromIdMap = new Dictionary<string, TaskCompletionSource<TestOutcome>>();
            foreach (var test in tests)
            {
                var id = test.Id.ToString();
                testFromIdMap[id] = test;
                completionFromIdMap[id] = new TaskCompletionSource<TestOutcome>();
            }
        }

        private static void RunAndRecordParallelizedTestCases(IFrameworkHandle frameworkHandle, Dictionary<string, TestCase> testFromIdMap, Dictionary<string, TaskCompletionSource<TestOutcome>> completionFromIdMap, IEnumerable<TestCase> tests)
        {
            var parallelTestSet = tests.Where(test => YamlTestProperties.Get(test, "parallelize") == "true");
            foreach (var test in parallelTestSet)
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    var parallelTestId = test.Id.ToString();
                    var parallelTest = testFromIdMap[parallelTestId];
                    var parallelTestOutcome = RunAndRecordTestCase(parallelTest, frameworkHandle);
                    // defer setting completion outcome until all steps are complete

                    var checkTest = parallelTest;
                    while (true)
                    {
                        var nextStepId = YamlTestProperties.Get(checkTest, "nextStepId");
                        if (string.IsNullOrEmpty(nextStepId))
                        {
                            Logger.LogInfo($"YamlTestAdapter.RunTests() ==> No nextStepId for test '{checkTest.DisplayName}'");
                            break;
                        }

                        var stepTest = testFromIdMap.ContainsKey(nextStepId) ? testFromIdMap[nextStepId] : null;
                        if (stepTest == null)
                        {
                            Logger.LogError($"YamlTestAdapter.RunTests() ==> ERROR: nextStepId '{nextStepId}' not found for test '{checkTest.DisplayName}'");
                            break;
                        }

                        var stepCompletion = completionFromIdMap.ContainsKey(nextStepId) ? completionFromIdMap[nextStepId] : null;
                        if (stepCompletion == null)
                        {
                            Logger.LogError($"YamlTestAdapter.RunTests() ==> ERROR: nextStepId '{nextStepId}' completion not found for test '{checkTest.DisplayName}'");
                            break;
                        }

                        var stepOutcome = RunAndRecordTestCase(stepTest, frameworkHandle);
                        Logger.Log($"YamlTestAdapter.RunTests() ==> Setting completion outcome for {stepTest.DisplayName} to {stepOutcome}");
                        completionFromIdMap[nextStepId].SetResult(stepOutcome);

                        checkTest = stepTest;
                    }

                    // now that all steps are complete, set the completion outcome
                    completionFromIdMap[parallelTestId].SetResult(parallelTestOutcome);
                    Logger.Log($"YamlTestAdapter.RunTests() ==> Setting completion outcome for {parallelTest.DisplayName} to {parallelTestOutcome}");

                }, test.Id);
            }

            Logger.Log($"YamlTestAdapter.RunTests() ==> Waiting for parallel tests to complete");
            var parallelCompletions = completionFromIdMap
                .Where(x => parallelTestSet.Any(y => y.Id.ToString() == x.Key))
                .Select(x => x.Value.Task);
            Task.WaitAll(parallelCompletions.ToArray());
            Logger.Log($"YamlTestAdapter.RunTests() ==> All parallel tests complete");
        }

        private static void RunAndRecordRemainingTestCases(IFrameworkHandle frameworkHandle, Dictionary<string, TestCase> testFromIdMap, Dictionary<string, TaskCompletionSource<TestOutcome>> completionFromIdMap)
        {
            var remainingTests = completionFromIdMap
                .Where(x => x.Value.Task.Status != TaskStatus.RanToCompletion)
                .Select(x => testFromIdMap[x.Key]);
            foreach (var test in remainingTests)
            {
                var outcome = RunAndRecordTestCase(test, frameworkHandle);
                completionFromIdMap[test.Id.ToString()].SetResult(outcome);
            }
        }

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

            directory = YamlTagHelpers.GetYamlDefaultTagsFullFileName(directory)?.Directory ?? directory;
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
                .Where(file => file.Name != YamlDefaultTagsFileName);
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
        public const string Executor = "executor://ai/yaml/VsTestRunner1";
        #endregion

        #region other constants
        public const string YamlDefaultTagsFileName = "Azure-AI-CLI-TestRunner-Default-Tags.yaml";
        public const string DefaultTimeout = "600000";
        #endregion
    }
}
