using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public class YamlTestFrameworkConsoleReporter : IYamlTestFrameworkReporter
    {
        public YamlTestFrameworkConsoleReporter()
        {
        }

        public void RecordStart(TestCase testCase)
        {
            Console.WriteLine("Starting test: " + testCase.FullyQualifiedName);
        }

        public void RecordResult(TestResult testResult)
        {
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
            if (outcome != TestOutcome.Passed)
            {
                Console.WriteLine("FAILED " + testCase.FullyQualifiedName);
            }
        }
    }
}
