using System;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public static class JunitXmlTestReporter
    {
        public static string WriteResultsFile(TestRun testRun, string resultsFile = "test-results.xml")
        {
            var testCases = testRun.TestCases;
            var testResults = testRun.TestResults;
            var startTime = testRun.StartTime;
            var endTime = testRun.EndTime;

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.NewLineChars = "\n";
            settings.NewLineHandling = NewLineHandling.Replace;
            settings.OmitXmlDeclaration = false;

            var writer = XmlWriter.Create(resultsFile, settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("testsuites");

            writer.WriteStartElement("testsuite");
            writer.WriteAttributeString("name", "TestSuite");
            writer.WriteAttributeString("tests", testResults.Count.ToString());
            writer.WriteAttributeString("failures", testResults.Count(r => r.Outcome == TestOutcome.Failed).ToString());
            writer.WriteAttributeString("errors", "0");
            writer.WriteAttributeString("time", testResults.Sum(r => r.Duration.TotalSeconds).ToString());
            writer.WriteAttributeString("timestamp", endTime.ToString("yyyy-MM-ddTHH:mm:ss"));

            foreach (var testResult in testResults)
            {
                writer.WriteStartElement("testcase");
                writer.WriteAttributeString("name", testResult.TestCase.DisplayName);
                writer.WriteAttributeString("classname", testResult.TestCase.FullyQualifiedName);
                writer.WriteAttributeString("time", testResult.Duration.TotalSeconds.ToString());

                var stdout = testResult.Messages.FirstOrDefault(x => x.Category == TestResultMessage.StandardOutCategory)?.Text;
                var stderr = testResult.Messages.FirstOrDefault(x => x.Category == TestResultMessage.StandardErrorCategory)?.Text;
                var debugTrace = testResult.Messages.FirstOrDefault(x => x.Category == TestResultMessage.DebugTraceCategory)?.Text;
                var message = testResult.Messages.FirstOrDefault(x => x.Category == TestResultMessage.AdditionalInfoCategory)?.Text;

                writer.WriteStartElement("system-out");
                writer.WriteRaw(System.Security.SecurityElement
                    .Escape(stdout.Replace("\u001b", string.Empty))
                    .Replace("\r\n", "&#xD;\n"));
                writer.WriteEndElement();

                writer.WriteStartElement("system-err");
                writer.WriteRaw(System.Security.SecurityElement
                    .Escape(stderr.Replace("\u001b", string.Empty))
                    .Replace("\r\n", "&#xD;\n"));
                writer.WriteEndElement();

                if (testResult.Outcome == TestOutcome.Failed)
                {
                    writer.WriteStartElement("failure");
                    writer.WriteAttributeString("message", testResult.ErrorMessage);
                    writer.WriteAttributeString("type", "Failure");
                    writer.WriteCData(testResult.ErrorStackTrace);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Close();
            writer.Dispose();

            return resultsFile;
        }
    }
}

