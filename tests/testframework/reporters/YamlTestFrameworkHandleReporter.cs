using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public class YamlTestFrameworkHandleReporter : IYamlTestFrameworkReporter
    {
        private readonly IFrameworkHandle _frameworkHandle;

        public YamlTestFrameworkHandleReporter(IFrameworkHandle frameworkHandle)
        {
            _frameworkHandle = frameworkHandle;
        }

        public void RecordStart(TestCase testCase)
        {
            _frameworkHandle.RecordStart(testCase);
        }

        public void RecordResult(TestResult testResult)
        {
            _frameworkHandle.RecordResult(testResult);
        }

        public void RecordEnd(TestCase testCase, TestOutcome outcome)
        {
            _frameworkHandle.RecordEnd(testCase, outcome);
        }
    }
}
