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

namespace Azure.AI.Details.Common.CLI.TestAdapter
{
    [ExtensionUri(YamlTestAdapter.Executor)]
    public class TextExecutor : ITestExecutor
    {
        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            tests = tests.ToList(); // force enumeration

            Logger.Log(new YamlTestFrameworkTestAdapterMessageLogger(frameworkHandle));
            Logger.Log($"TextExecutor.RunTests(IEnumerable<TestCase>(): ENTER");
            Logger.Log($"TextExecutor.RunTests(IEnumerable<TestCase>(): count={tests.Count()}");
            YamlTestAdapter.RunTests(tests, runContext, frameworkHandle);
            Logger.Log($"TextExecutor.RunTests(IEnumerable<TestCase>(): EXIT");
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            sources = sources.ToList(); // force enumeration
            
            Logger.Log(new YamlTestFrameworkTestAdapterMessageLogger(frameworkHandle));
            Logger.Log($"TextExecutor.RunTests(IEnumerable<string>(): ENTER");
            Logger.Log($"TextExecutor.RunTests(IEnumerable<string>(): count={sources.Count()}");
            RunTests(YamlTestAdapter.GetTestsFromFiles(sources), runContext, frameworkHandle);
            Logger.Log($"TextExecutor.RunTests(IEnumerable<string>(): EXIT");
        }

        public void Cancel()
        {
            Logger.Log($"TextExecutor.Cancel(): ENTER/EXIT");
        }
    }
}
