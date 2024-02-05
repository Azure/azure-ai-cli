using Microsoft.VisualStudio.TestPlatform.ObjectModel;
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
            var files = FindFiles(directory);
            var tests = files.SelectMany(file => GetTestsFromYaml(source, file));

           Logger.Log($"YamlTestFramework.GetTestsFromDirectory('{source}', '{directory.FullName}'): EXIT");
           return tests.ToList();
        }

        public static IEnumerable<TestCase> GetTestsFromYaml(string source, FileInfo file)
        {
            Logger.Log($"YamlTestFramework.GetTestsFromYaml('{source}', '{file.FullName}'): ENTER");
            var tests = YamlTestCaseParser.TestCasesFromYaml(source, file);

            Logger.Log($"YamlTestFramework.GetTestsFromYaml('{source}', '{file.FullName}'): EXIT");
            return tests;
        }

        public static void RunTests(IEnumerable<TestCase> tests, IYamlTestFrameworkHost host)
        {
            tests = tests.ToList(); // force enumeration

            var grouped = GroupTestCasesByPriority(tests);
            foreach (var priorityGroup in grouped)
            {
                if (priorityGroup.Count == 0) continue;
                RunAndRecordTests(host, priorityGroup);
            }
        }

        #region private methods

        private static void RunAndRecordTests(IYamlTestFrameworkHost host, IEnumerable<TestCase> tests)
        {
            var testFromIdMap = new Dictionary<string, TestCase>();
            var completionFromIdMap = new Dictionary<string, TaskCompletionSource<TestOutcome>>();
            foreach (var test in tests)
            {
                var id = test.Id.ToString();
                testFromIdMap[id] = test;
                completionFromIdMap[id] = new TaskCompletionSource<TestOutcome>();
            }

            RunAndRecordParallelizedTestCases(host, testFromIdMap, completionFromIdMap);
            RunAndRecordRemainingTestCases(host, testFromIdMap, completionFromIdMap);
        }

        private static void RunAndRecordParallelizedTestCases(IYamlTestFrameworkHost host, Dictionary<string, TestCase> testFromIdMap, Dictionary<string, TaskCompletionSource<TestOutcome>> completionFromIdMap)
        {
            var parallelTests = testFromIdMap
                .Select(x => x.Value)
                .Where(test => YamlTestProperties.Get(test, "parallelize") == "true")
                .ToList();

            foreach (var test in parallelTests)
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    var parallelTestId = test.Id.ToString();
                    RunAndRecordTestCaseSteps(host, testFromIdMap, completionFromIdMap, parallelTestId);
                });
            }

            Logger.Log($"YamlTestFramework.RunAndRecordParallelizedTestCases() ==> Waiting for parallel tests to complete");
            var parallelCompletions = completionFromIdMap
                .Where(x => parallelTests.Any(y => y.Id.ToString() == x.Key))
                .Select(x => x.Value.Task);
            Task.WaitAll(parallelCompletions.ToArray());
            Logger.Log($"YamlTestFramework.RunAndRecordParallelizedTestCases() ==> All parallel tests complete");
        }

        private static void RunAndRecordTestCaseSteps(IYamlTestFrameworkHost host, Dictionary<string, TestCase> testFromIdMap, Dictionary<string, TaskCompletionSource<TestOutcome>> completionFromIdMap, string firstTestId)
        {
            var firstTest = testFromIdMap[firstTestId];
            var firstTestOutcome = RunAndRecordTestCase(firstTest, host);
            // defer setting completion outcome until all steps are complete

            var checkTest = firstTest;
            while (true)
            {
                var nextStepId = YamlTestProperties.Get(checkTest, "nextStepId");
                if (string.IsNullOrEmpty(nextStepId))
                {
                    Logger.LogInfo($"YamlTestFramework.RunAndRecordTestCaseSteps() ==> No nextStepId for test '{checkTest.DisplayName}'");
                    break;
                }

                var stepTest = testFromIdMap.ContainsKey(nextStepId) ? testFromIdMap[nextStepId] : null;
                if (stepTest == null)
                {
                    Logger.LogError($"YamlTestFramework.RunAndRecordTestCaseSteps() ==> ERROR: nextStepId '{nextStepId}' not found for test '{checkTest.DisplayName}'");
                    break;
                }

                var stepCompletion = completionFromIdMap.ContainsKey(nextStepId) ? completionFromIdMap[nextStepId] : null;
                if (stepCompletion == null)
                {
                    Logger.LogError($"YamlTestFramework.RunAndRecordTestCaseSteps() ==> ERROR: nextStepId '{nextStepId}' completion not found for test '{checkTest.DisplayName}'");
                    break;
                }

                var stepOutcome = RunAndRecordTestCase(stepTest, host);
                Logger.Log($"YamlTestFramework.RunAndRecordTestCaseSteps() ==> Setting completion outcome for {stepTest.DisplayName} to {stepOutcome}");
                completionFromIdMap[nextStepId].SetResult(stepOutcome);

                checkTest = stepTest;
            }

            // now that all steps are complete, set the completion outcome
            completionFromIdMap[firstTestId].SetResult(firstTestOutcome);
            Logger.Log($"YamlTestFramework.RunAndRecordTestCaseSteps() ==> Setting completion outcome for {firstTest.DisplayName} to {firstTestOutcome}");
        }

        private static void RunAndRecordRemainingTestCases(IYamlTestFrameworkHost host, Dictionary<string, TestCase> testFromIdMap, Dictionary<string, TaskCompletionSource<TestOutcome>> completionFromIdMap)
        {
            var remainingTests = completionFromIdMap
                .Where(x => x.Value.Task.Status != TaskStatus.RanToCompletion)
                .Select(x => testFromIdMap[x.Key]);
            foreach (var test in remainingTests)
            {
                var outcome = RunAndRecordTestCase(test, host);
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

        private static List<List<TestCase>> GroupTestCasesByPriority(IEnumerable<TestCase> tests)
        {
            Logger.Log($"YamlTestFramework.GroupTestCasesByPriority()");

            var before = tests.Where(test => test.Traits.Count(x => IsTrait(x, "before")) > 0);
            var after = tests.Where(test => test.Traits.Count(x => IsTrait(x, "after")) > 0);
            var middle = tests.Where(test => !before.Contains(test) && !after.Contains(test));

            var testsList = new List<List<TestCase>>();
            testsList.Add(before.ToList());
            testsList.Add(middle.ToList());
            testsList.Add(after.ToList());
            Logger.Log("YamlTestFramework.GroupTestCasesByPriority() ==> {string.Join('\n', tests.Select(x => x.Name))}");

            return testsList;
        }

        private static TestOutcome RunAndRecordTestCase(TestCase test, IYamlTestFrameworkHost host)
        {
            Logger.Log($"YamlTestFramework.TestRunAndRecord({test.DisplayName})");
            return YamlTestCaseRunner.RunAndRecordTestCase(test, host);
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
