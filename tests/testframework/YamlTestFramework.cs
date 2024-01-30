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

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public class YamlTestFramework
    {
        public static IEnumerable<TestCase> GetTestsFromDirectory(string source, DirectoryInfo directory)
        {
            Logger.Log($"YamlTestFramework.GetTestsFromDirectory('{source}', '{directory.FullName}'): ENTER");

            directory = YamlTagHelpers.GetYamlDefaultTagsFullFileName(directory)?.Directory ?? directory;
            foreach (var file in FindFiles(directory))
            {
                foreach (var test in GetTestsFromYaml(source, file))
                {
                    yield return test;
                }
            }
           Logger.Log($"YamlTestFramework.GetTestsFromDirectory('{source}', '{directory.FullName}'): EXIT");
        }

        public static IEnumerable<TestCase> GetTestsFromYaml(string source, FileInfo file)
        {
            Logger.Log($"YamlTestFramework.GetTestsFromYaml('{source}', '{file.FullName}'): ENTER");
            foreach (var test in YamlTestCaseParser.TestCasesFromYaml(source, file))
            {
                yield return test;
            }
            Logger.Log($"YamlTestFramework.GetTestsFromYaml('{source}', '{file.FullName}'): EXIT");
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
                            Logger.LogInfo($"YamlTestFramework.RunTests() ==> No nextStepId for test '{checkTest.DisplayName}'");
                            break;
                        }

                        var stepTest = testFromIdMap.ContainsKey(nextStepId) ? testFromIdMap[nextStepId] : null;
                        if (stepTest == null)
                        {
                            Logger.LogError($"YamlTestFramework.RunTests() ==> ERROR: nextStepId '{nextStepId}' not found for test '{checkTest.DisplayName}'");
                            break;
                        }

                        var stepCompletion = completionFromIdMap.ContainsKey(nextStepId) ? completionFromIdMap[nextStepId] : null;
                        if (stepCompletion == null)
                        {
                            Logger.LogError($"YamlTestFramework.RunTests() ==> ERROR: nextStepId '{nextStepId}' completion not found for test '{checkTest.DisplayName}'");
                            break;
                        }

                        var stepOutcome = RunAndRecordTestCase(stepTest, frameworkHandle);
                        Logger.Log($"YamlTestFramework.RunTests() ==> Setting completion outcome for {stepTest.DisplayName} to {stepOutcome}");
                        completionFromIdMap[nextStepId].SetResult(stepOutcome);

                        checkTest = stepTest;
                    }

                    // now that all steps are complete, set the completion outcome
                    completionFromIdMap[parallelTestId].SetResult(parallelTestOutcome);
                    Logger.Log($"YamlTestFramework.RunTests() ==> Setting completion outcome for {parallelTest.DisplayName} to {parallelTestOutcome}");

                }, test.Id);
            }

            Logger.Log($"YamlTestFramework.RunTests() ==> Waiting for parallel tests to complete");
            var parallelCompletions = completionFromIdMap
                .Where(x => parallelTestSet.Any(y => y.Id.ToString() == x.Key))
                .Select(x => x.Value.Task);
            Task.WaitAll(parallelCompletions.ToArray());
            Logger.Log($"YamlTestFramework.RunTests() ==> All parallel tests complete");
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

        private static IEnumerable<FileInfo> FindFiles(DirectoryInfo directory)
        {
            return directory.GetFiles($"*{YamlFileExtension}", SearchOption.AllDirectories)
                .Where(file => file.Name != YamlDefaultTagsFileName);
        }

        private static bool IsTrait(Trait trait, string check)
        {
            return trait.Name == check || trait.Value == check;
        }

        private static IEnumerable<IEnumerable<TestCase>> FilterTestCases(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            Logger.Log($"YamlTestFramework.FilterTestCases()");

            tests = YamlTestCaseFilter.FilterTestCases(tests, runContext, frameworkHandle);
            
            var before = tests.Where(test => test.Traits.Count(x => IsTrait(x, "before")) > 0);
            var after = tests.Where(test => test.Traits.Count(x => IsTrait(x, "after")) > 0);
            var middle = tests.Where(test => !before.Contains(test) && !after.Contains(test));

            var testsList = new List<IEnumerable<TestCase>> { before, middle, after };
            Logger.Log("YamlTestFramework.FilterTestCases() ==> {string.Join('\n', tests.Select(x => x.Name))}");

            return testsList;
        }

        private static TestOutcome RunAndRecordTestCase(TestCase test, IFrameworkHandle frameworkHandle)
        {
            Logger.Log($"YamlTestFramework.TestRunAndRecord({test.DisplayName})");
            return YamlTestCaseRunner.RunAndRecordTestCase(test, frameworkHandle);
        }

        #endregion

        #region constants
        public const string YamlFileExtension = ".yaml";
        public const string FakeExecutor = "executor://ai/cli/TestFramework/v1";
        public const string YamlDefaultTagsFileName = "Azure-AI-CLI-TestFramework-Default-Tags.yaml";
        public const string DefaultTimeout = "600000";
        #endregion
    }
}
