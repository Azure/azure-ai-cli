using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public class YamlTestFrameworkConsoleHost : IYamlTestFrameworkHost
    {
        public YamlTestFrameworkConsoleHost()
        {
        }

        public void RecordStart(TestCase testCase)
        {
            _startTime ??= DateTime.Now;
            _testCases.Add(testCase);
            SetExecutionId(testCase, Guid.NewGuid());

            Console.WriteLine("Starting test: " + testCase.FullyQualifiedName);
        }

        public void RecordResult(TestResult testResult)
        {
            _testResults.Add(testResult);
            
            Console.WriteLine("Test: " + testResult.TestCase.DisplayName);
            Console.WriteLine("Result: " + testResult.Outcome);
            Console.WriteLine("Duration: " + testResult.Duration.TotalMilliseconds + "ms");
            if (testResult.Outcome != TestOutcome.Passed)
            {
                Console.WriteLine("ErrorMessage: " + testResult.ErrorMessage);
                Console.WriteLine("ErrorStackTrace: " + testResult.ErrorStackTrace);
            }
            Console.WriteLine();
        }

        public void RecordEnd(TestCase testCase, TestOutcome outcome)
        {
            _endTime = DateTime.Now;
            if (outcome != TestOutcome.Passed)
            {
                Console.WriteLine("FAILED " + testCase.FullyQualifiedName);
            }
        }

        public void WriteResultFile()
        {
            var assembly = typeof(YamlTestFrameworkConsoleHost).Assembly;
            var assemblyPath = assembly.Location;

            _startTime ??= DateTime.Now;
            _endTime ??= DateTime.Now;

            var resultFile = "test-results.trx";
            var testRunId = Guid.NewGuid().ToString();
            var testListId = "8c84fa94-04c1-424b-9868-57a2d4851a1d";
            var testType = "13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b";
            var userName = Environment.UserName;
            var machineName = Environment.MachineName;
            var userAtMachine = userName.Split('\\', '/').Last() + "@" + machineName;
            var testRunName = userAtMachine + _endTime.Value.ToString("yyyy-MM-dd HH:mm:ss");

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.NewLineChars = "\n";
            settings.NewLineHandling = NewLineHandling.Replace;
            settings.OmitXmlDeclaration = false;

            var writer = XmlWriter.Create(resultFile, settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("", "TestRun", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010");
            writer.WriteAttributeString("id", testRunId);
            writer.WriteAttributeString("name", testRunName);
            writer.WriteAttributeString("runUser", userName);

            writer.WriteStartElement("Times");
            writer.WriteAttributeString("creation", _endTime.Value.ToString("o"));
            writer.WriteAttributeString("queuing", _endTime.Value.ToString("o"));
            writer.WriteAttributeString("start", _startTime.Value.ToString("o"));
            writer.WriteAttributeString("finish", _endTime.Value.ToString("o"));
            writer.WriteEndElement();

            writer.WriteStartElement("Results");
            foreach (var testResult in _testResults)
            {
                var executionId = GetExecutionId(testResult.TestCase).ToString();
                var stdout = testResult.Messages.First(x => x.Category == TestResultMessage.StandardOutCategory).Text
                    .Replace("\u001b", string.Empty);

                var debugTrace = testResult.Messages.First(x => x.Category == TestResultMessage.DebugTraceCategory).Text;
                var message = testResult.Messages.First(x => x.Category == TestResultMessage.AdditionalInfoCategory).Text;

                writer.WriteStartElement("UnitTestResult");
                writer.WriteAttributeString("executionId", executionId);
                writer.WriteAttributeString("testId", testResult.TestCase.Id.ToString());
                writer.WriteAttributeString("testName", testResult.TestCase.FullyQualifiedName);
                writer.WriteAttributeString("computerName", machineName);
                writer.WriteAttributeString("duration", testResult.Duration.ToString());
                writer.WriteAttributeString("startTime", testResult.StartTime.DateTime.ToString("o"));
                writer.WriteAttributeString("endTime", testResult.EndTime.DateTime.ToString("o"));
                writer.WriteAttributeString("testType", testType);
                writer.WriteAttributeString("outcome", testResult.Outcome.ToString());
                writer.WriteAttributeString("testListId", testListId);
                writer.WriteAttributeString("relativeResultsDirectory", Guid.NewGuid().ToString());
                writer.WriteStartElement("Output");
                writer.WriteElementString("StdOut", stdout);
                writer.WriteElementString("DebugTrace", debugTrace);
                writer.WriteStartElement("ErrorInfo");
                writer.WriteElementString("Message", testResult.ErrorMessage);
                writer.WriteElementString("StackTrace", testResult.ErrorStackTrace);
                writer.WriteEndElement();
                writer.WriteStartElement("TextMessages");

                writer.WriteElementString("Message", message);
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement("TestDefinitions");
            foreach (var testCase in _testCases)
            {
                var executionId = GetExecutionId(testCase).ToString();
                var qualifiedParts = testCase.FullyQualifiedName.Split('.');
                var className = string.Join(".", qualifiedParts.Take(qualifiedParts.Length - 1));
                var name = qualifiedParts.Last();
                writer.WriteStartElement("UnitTest");
                writer.WriteAttributeString("name", testCase.DisplayName);
                writer.WriteAttributeString("storage", assemblyPath);
                writer.WriteAttributeString("id", testCase.Id.ToString());
                writer.WriteStartElement("Execution");
                writer.WriteAttributeString("id", executionId);
                writer.WriteEndElement();
                writer.WriteStartElement("TestMethod");
                writer.WriteAttributeString("codeBase", assemblyPath);
                writer.WriteAttributeString("adapterTypeName", testCase.ExecutorUri.ToString());
                writer.WriteAttributeString("className", className);
                writer.WriteAttributeString("name", name);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement("TestEntries");
            foreach (var testCase in _testCases)
            {
                var executionId = GetExecutionId(testCase).ToString();
                writer.WriteStartElement("TestEntry");
                writer.WriteAttributeString("testId", testCase.Id.ToString());
                writer.WriteAttributeString("executionId", executionId);
                writer.WriteAttributeString("testListId", testListId);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement("TestLists");
            writer.WriteStartElement("TestList");
            writer.WriteAttributeString("name", "Results Not in a List");
            writer.WriteAttributeString("id", testListId);
            writer.WriteEndElement();
            writer.WriteStartElement("TestList");
            writer.WriteAttributeString("name", "All Loaded Results");
            writer.WriteAttributeString("id", "19431567-8539-422a-85d7-44ee4e166bda");
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("ResultSummary");
            writer.WriteAttributeString("outcome", "Completed");

            writer.WriteStartElement("Counters");
            writer.WriteAttributeString("total", _testResults.Count.ToString());
            writer.WriteAttributeString("executed", _testResults.Count(r => IsExecuted(r)).ToString());
            writer.WriteAttributeString("passed", _testResults.Count(r => IsPassed(r)).ToString());
            writer.WriteAttributeString("failed", _testResults.Count(r => IsFailed(r)).ToString());
            writer.WriteAttributeString("error", _testResults.Count(r => IsError(r)).ToString());
            writer.WriteAttributeString("timeout", _testResults.Count(r => IsTimeout(r)).ToString());
            writer.WriteAttributeString("aborted", _testResults.Count(r => IsAborted(r)).ToString());
            writer.WriteAttributeString("inconclusive", _testResults.Count(r => IsInConclusive(r)).ToString());
            writer.WriteAttributeString("passedButRunAborted", _testResults.Count(r => IsPassedButRunaborted(r)).ToString());
            writer.WriteAttributeString("notRunnable", _testResults.Count(r => IsNotRunnable(r)).ToString());
            writer.WriteAttributeString("notExecuted", _testResults.Count(r => IsNotExecuted(r)).ToString());
            writer.WriteAttributeString("disconnected", _testResults.Count(r => IsDisconnected(r)).ToString());
            writer.WriteAttributeString("warning", _testResults.Count(r => IsWarning(r)).ToString());
            writer.WriteAttributeString("completed", "0");
            writer.WriteAttributeString("inProgress", "0");
            writer.WriteAttributeString("pending", "0");
            writer.WriteEndElement();

            writer.WriteStartElement("Output");
            writer.WriteElementString("StdOut", "");
            writer.WriteEndElement();

            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Close();
            writer.Dispose();
        }

        private bool IsExecuted(TestResult r)
        {
            return IsPassed(r) || IsFailed(r);
        }

        private static bool IsPassed(TestResult r)
        {
            return r.Outcome == TestOutcome.Passed;
        }

        private static bool IsFailed(TestResult r)
        {
            return r.Outcome == TestOutcome.Failed;
        }

        private static bool IsError(TestResult r)
        {
            return false;
            // return r.Outcome == TestOutcome.Error;
        }

        private static bool IsTimeout(TestResult r)
        {
            return false;
            // return r.Outcome == TestOutcome.Timeout;
        }

        private static bool IsAborted(TestResult r)
        {
            return false;
            // return r.Outcome == TestOutcome.Aborted;
        }

        private static bool IsInConclusive(TestResult r)
        {
            return false;
            // return r.Outcome == TestOutcome.Inconclusive;
        }

        private static bool IsPassedButRunaborted(TestResult r)
        {
            return false;
            // return r.Outcome == TestOutcome.PassedButRunAborted;
        }

        private static bool IsNotRunnable(TestResult r)
        {
            return false;
            // return r.Outcome == TestOutcome.NotRunnable;
        }

        private static bool IsNotExecuted(TestResult r)
        {
            return false;
            // return r.Outcome == TestOutcome.NotExecuted;
        }

        private static bool IsDisconnected(TestResult r)
        {
            return false;
            // return r.Outcome == TestOutcome.Disconnected;
        }

        private static bool IsWarning(TestResult r)
        {
            return false;
            // return r.Outcome == TestOutcome.Warning;
        }

        private void SetExecutionId(TestCase testCase, Guid guid)
        {
            lock (_testToExecutionMap)
            {
                _testToExecutionMap[testCase.Id] = guid;
            }
        }

        private Guid GetExecutionId(TestCase testCase)
        {
            lock (_testToExecutionMap)
            {
                return _testToExecutionMap[testCase.Id];
            }
        }

        private DateTime? _startTime;
        private DateTime? _endTime;

        private List<TestCase> _testCases = new List<TestCase>();
        private Dictionary<Guid, Guid> _testToExecutionMap = new Dictionary<Guid, Guid>();
        private List<TestResult> _testResults = new List<TestResult>();
    }
}

