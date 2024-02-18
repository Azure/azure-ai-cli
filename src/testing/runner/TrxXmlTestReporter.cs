using System;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public static class TrxXmlTestReporter
    {
        public static string WriteResultFile(TestRun testRun)
        {
            var testCases = testRun.TestCases;
            var testResults = testRun.TestResults;
            var startTime = testRun.StartTime;
            var endTime = testRun.EndTime;

            var assembly = typeof(YamlTestFrameworkConsoleHost).Assembly;
            var assemblyPath = assembly.Location;

            var resultFile = "test-results.trx";
            var testRunId = Guid.NewGuid().ToString();
            var testListId = "8c84fa94-04c1-424b-9868-57a2d4851a1d";
            var testType = "13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b";
            var userName = Environment.UserName;
            var machineName = Environment.MachineName;
            var userAtMachine = userName.Split('\\', '/').Last() + "@" + machineName;
            var testRunName = userAtMachine + " " + endTime.ToString("yyyy-MM-dd HH:mm:ss");

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
            writer.WriteAttributeString("creation", endTime.ToString("o"));
            writer.WriteAttributeString("queuing", endTime.ToString("o"));
            writer.WriteAttributeString("start", startTime.ToString("o"));
            writer.WriteAttributeString("finish", endTime.ToString("o"));
            writer.WriteEndElement();

            writer.WriteStartElement("Results");
            foreach (var testResult in testResults)
            {
                var executionId = testRun.GetExecutionId(testResult.TestCase).ToString();
                var stdout = testResult.Messages.FirstOrDefault(x => x.Category == TestResultMessage.StandardOutCategory)?.Text;
                var stderr = testResult.Messages.FirstOrDefault(x => x.Category == TestResultMessage.StandardErrorCategory)?.Text;
                var debugTrace = testResult.Messages.FirstOrDefault(x => x.Category == TestResultMessage.DebugTraceCategory)?.Text;
                var message = testResult.Messages.FirstOrDefault(x => x.Category == TestResultMessage.AdditionalInfoCategory)?.Text;

                writer.WriteStartElement("UnitTestResult");
                writer.WriteAttributeString("executionId", executionId);
                writer.WriteAttributeString("testId", testResult.TestCase.Id.ToString());
                writer.WriteAttributeString("testName", testResult.TestCase.FullyQualifiedName);
                writer.WriteAttributeString("computerName", machineName);
                writer.WriteAttributeString("duration", testResult.Duration.ToString());
                writer.WriteAttributeString("startTime", testResult.StartTime.DateTime.ToString("o"));
                writer.WriteAttributeString("endTime", testResult.EndTime.DateTime.ToString("o"));
                writer.WriteAttributeString("testType", testType);
                writer.WriteAttributeString("outcome", OutcomeToString(testResult.Outcome));
                writer.WriteAttributeString("testListId", testListId);
                writer.WriteAttributeString("relativeResultsDirectory", Guid.NewGuid().ToString());
                writer.WriteStartElement("Output");

                if (!string.IsNullOrEmpty(stdout))
                {
                    writer.WriteStartElement("StdOut");
                    writer.WriteRaw(System.Security.SecurityElement
                        .Escape(stdout.Replace("\u001b", string.Empty))
                        .Replace("\r\n", "&#xD;\n"));
                    writer.WriteEndElement();
                }

                if (!string.IsNullOrEmpty(stderr))
                {
                    writer.WriteStartElement("StdErr");
                    writer.WriteRaw(System.Security.SecurityElement
                        .Escape(stderr.Replace("\u001b", string.Empty))
                        .Replace("\r\n", "&#xD;\n"));
                    writer.WriteEndElement();
                }

                if (!string.IsNullOrEmpty(debugTrace))
                {
                    writer.WriteElementString("DebugTrace", debugTrace);
                }

                writer.WriteStartElement("ErrorInfo");
                writer.WriteElementString("Message", testResult.ErrorMessage);
                writer.WriteElementString("StackTrace", testResult.ErrorStackTrace);
                writer.WriteEndElement();
                writer.WriteStartElement("TextMessages");

                if (!string.IsNullOrEmpty(message))
                {
                    writer.WriteElementString("Message", message);
                }
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement("TestDefinitions");
            foreach (var testCase in testCases)
            {
                var executionId = testRun.GetExecutionId(testCase).ToString();
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
            foreach (var testCase in testCases)
            {
                var executionId = testRun.GetExecutionId(testCase).ToString();
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
            writer.WriteAttributeString("total", testResults.Count.ToString());
            writer.WriteAttributeString("executed", testResults.Count(r => IsExecuted(r)).ToString());
            writer.WriteAttributeString("passed", testResults.Count(r => IsPassed(r)).ToString());
            writer.WriteAttributeString("failed", testResults.Count(r => IsFailed(r)).ToString());
            writer.WriteAttributeString("error", testResults.Count(r => IsError(r)).ToString());
            writer.WriteAttributeString("timeout", testResults.Count(r => IsTimeout(r)).ToString());
            writer.WriteAttributeString("aborted", testResults.Count(r => IsAborted(r)).ToString());
            writer.WriteAttributeString("inconclusive", testResults.Count(r => IsInConclusive(r)).ToString());
            writer.WriteAttributeString("passedButRunAborted", testResults.Count(r => IsPassedButRunaborted(r)).ToString());
            writer.WriteAttributeString("notRunnable", testResults.Count(r => IsNotRunnable(r)).ToString());
            writer.WriteAttributeString("notExecuted", testResults.Count(r => IsNotExecuted(r)).ToString());
            writer.WriteAttributeString("disconnected", testResults.Count(r => IsDisconnected(r)).ToString());
            writer.WriteAttributeString("warning", testResults.Count(r => IsWarning(r)).ToString());
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

            return resultFile;
        }

        private static string OutcomeToString(TestOutcome outcome)
        {
            return outcome switch {
                TestOutcome.None => "None",
                TestOutcome.Passed => "Passed",
                TestOutcome.Failed => "Failed",
                TestOutcome.Skipped => "NotExecuted",
                TestOutcome.NotFound => "NotFound",
                _ => "None",
            };
        }

        private static bool IsExecuted(TestResult r)
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
    }
}

