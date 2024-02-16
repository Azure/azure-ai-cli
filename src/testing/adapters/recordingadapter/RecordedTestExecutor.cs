using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.Details.Common.CLI.TestFramework;

namespace Azure.AI.Details.Common.CLI.RecordedTestAdapter
{
    [ExtensionUri(RecordedTestAdapter.Executor)]
    public class RecordingExecutor : ITestExecutor
    {
        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            Logger.Log(frameworkHandle);
            Logger.Log($"RecordingExecutor.RunTests(IEnumerable<TestCase>(): ENTER");
            Logger.Log($"RecordingExecutor.RunTests(IEnumerable<TestCase>(): count={tests.Count()}");
            RecordedTestAdapter.RunTests(tests, runContext, frameworkHandle);
            Logger.Log($"RecordingExecutor.RunTests(IEnumerable<TestCase>(): EXIT");
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            Logger.Log(frameworkHandle);
            Logger.Log($"RecordingExecutor.RunTests(IEnumerable<string>(): ENTER");
            Logger.Log($"RecordingExecutor.RunTests(IEnumerable<string>(): count={sources.Count()}");
            RunTests(RecordedTestAdapter.GetTestsFromFiles(sources), runContext, frameworkHandle);
            Logger.Log($"RecordingExecutor.RunTests(IEnumerable<string>(): EXIT");
        }

        public void Cancel()
        {
            Logger.Log($"RecordingExecutor.Cancel(): ENTER/EXIT");
        }
    }
}