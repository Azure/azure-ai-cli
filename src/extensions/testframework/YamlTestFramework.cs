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

            directory = YamlTestConfigHelpers.GetTestDirectory(directory);

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

        public static IDictionary<string, IList<TestResult>> RunTests(IEnumerable<TestCase> tests, IYamlTestFrameworkHost host)
        {
            var resultsByTestCaseId = new Dictionary<string, IList<TestResult>>();

            tests = tests.ToList(); // force enumeration
            var groupedByPriority = GroupTestCasesByPriority(tests);

            foreach (var priorityGroup in groupedByPriority)
            {
                if (priorityGroup.Count == 0) continue;

                var resultsByTestCaseIdForGroup = RunAndRecordTests(host, priorityGroup);
                foreach (var resultsForTestCase in resultsByTestCaseIdForGroup)
                {
                    var testCaseId = resultsForTestCase.Key;
                    var testResults = resultsForTestCase.Value;
                    resultsByTestCaseId[testCaseId] = testResults;
                }
            }

            return resultsByTestCaseId;
        }

        #region private methods

        private static IDictionary<string, IList<TestResult>> RunAndRecordTests(IYamlTestFrameworkHost host, IEnumerable<TestCase> tests)
        {
            InitRunAndRecordTestCaseMaps(tests, out var testFromIdMap, out var completionFromIdMap);

            RunAndRecordParallelizedTestCases(host, testFromIdMap, completionFromIdMap);
            RunAndRecordRemainingTestCases(host, testFromIdMap, completionFromIdMap);

            return GetRunAndRecordTestResultsMap(completionFromIdMap);
        }

        private static void InitRunAndRecordTestCaseMaps(IEnumerable<TestCase> tests, out Dictionary<string, TestCase> testFromIdMap, out Dictionary<string, TaskCompletionSource<IList<TestResult>>> completionFromIdMap)
        {
            testFromIdMap = new Dictionary<string, TestCase>();
            completionFromIdMap = new Dictionary<string, TaskCompletionSource<IList<TestResult>>>();
            foreach (var test in tests)
            {
                var id = test.Id.ToString();
                testFromIdMap[id] = test;
                completionFromIdMap[id] = new TaskCompletionSource<IList<TestResult>>();
            }
        }

        private static IDictionary<string, IList<TestResult>> GetRunAndRecordTestResultsMap(Dictionary<string, TaskCompletionSource<IList<TestResult>>> completionFromIdMap)
        {
            var resultsPerTestCase = completionFromIdMap.Select(x => x.Value.Task.Result);

            var resultsMap = new Dictionary<string, IList<TestResult>>();
            foreach (var resultsForCase in resultsPerTestCase)
            {
                var test = resultsForCase.FirstOrDefault()?.TestCase;
                if (test == null) continue;

                var id = test.Id.ToString();
                resultsMap[id] = resultsForCase;
            }

            return resultsMap;
        }

        private static void RunAndRecordParallelizedTestCases(IYamlTestFrameworkHost host, Dictionary<string, TestCase> testFromIdMap, Dictionary<string, TaskCompletionSource<IList<TestResult>>> completionFromIdMap)
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

        private static void RunAndRecordTestCaseSteps(IYamlTestFrameworkHost host, Dictionary<string, TestCase> testFromIdMap, Dictionary<string, TaskCompletionSource<IList<TestResult>>> completionFromIdMap, string firstTestId)
        {
            var firstTest = testFromIdMap[firstTestId];
            var firstTestResults = RunAndRecordTestCase(firstTest, host);
            var firstTestOutcome = TestResultHelpers.TestOutcomeFromResults(firstTestResults);
            // defer setting completion until all steps are complete

            var checkTest = firstTest;
            while (true)
            {
                var nextStepId = YamlTestProperties.Get(checkTest, "nextStepId");
                if (string.IsNullOrEmpty(nextStepId))
                {
                    // Logger.LogInfo($"YamlTestFramework.RunAndRecordTestCaseSteps() ==> No nextStepId for test '{checkTest.DisplayName}'", trace: false);
                    break;
                }

                var stepTest = testFromIdMap.ContainsKey(nextStepId) ? testFromIdMap[nextStepId] : null;
                if (stepTest == null)
                {
                    // Logger.LogInfo($"YamlTestFramework.RunAndRecordTestCaseSteps() ==> nextStepId '{nextStepId}' not found for test '{checkTest.DisplayName}'");
                    break;
                }

                var stepCompletion = completionFromIdMap.ContainsKey(nextStepId) ? completionFromIdMap[nextStepId] : null;
                if (stepCompletion == null)
                {
                    // Logger.LogInfo($"YamlTestFramework.RunAndRecordTestCaseSteps() ==> nextStepId '{nextStepId}' completion not found for test '{checkTest.DisplayName}'");
                    break;
                }

                var stepResults = RunAndRecordTestCase(stepTest, host);
                var stepOutcome = TestResultHelpers.TestOutcomeFromResults(stepResults);
                Logger.Log($"YamlTestFramework.RunAndRecordTestCaseSteps() ==> Setting completion outcome for {stepTest.DisplayName} to {stepOutcome}");
                completionFromIdMap[nextStepId].SetResult(stepResults);

                checkTest = stepTest;
            }

            // now that all steps are complete, set the completion outcome
            completionFromIdMap[firstTestId].SetResult(firstTestResults);
            Logger.Log($"YamlTestFramework.RunAndRecordTestCaseSteps() ==> Setting completion; outcome for {firstTest.DisplayName}: {firstTestOutcome}");
        }

        private static void RunAndRecordRemainingTestCases(IYamlTestFrameworkHost host, Dictionary<string, TestCase> testFromIdMap, Dictionary<string, TaskCompletionSource<IList<TestResult>>> completionFromIdMap)
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

        private static IList<TestResult> RunAndRecordTestCase(TestCase test, IYamlTestFrameworkHost host)
        {
            Logger.Log($"YamlTestFramework.TestRunAndRecord({test.DisplayName})");
            return YamlTestCaseRunner.RunAndRecordTestCase(test, host);
        }

        #endregion

        #region constants
        public const string YamlFileExtension = ".yaml";
        public const string FakeExecutor = "executor://ai/cli/TestFramework/v1";
        public const string YamlDefaultTagsFileName = "Azure-AI-CLI-TestFramework-Default-Tags.yaml";
        public const string YamlTestsConfigDirectoryName = ".aitests";
        public const string YamlTestsConfigFileName = "config";
        public const string DefaultTimeout = "600000";
        #endregion
    }
}
