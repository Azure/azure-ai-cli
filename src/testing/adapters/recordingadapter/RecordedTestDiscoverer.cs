//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

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
    [FileExtension(YamlTestFramework.YamlFileExtension)]
    [FileExtension(RecordedTestAdapter.DllFileExtension)]
    [DefaultExecutorUri(RecordedTestAdapter.Executor)]
    public class RecordingDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            try
            {
                Logger.Log(new YamlTestFrameworkTestAdapterMessageLogger(logger));
                Logger.Log($"RecordingDiscoverer.DiscoverTests(): ENTER");
                Logger.Log($"RecordingDiscoverer.DiscoverTests(): count={sources.Count()}");
                foreach (var test in RecordedTestAdapter.GetTestsFromFiles(sources))
                {
                    test.ExecutorUri = new Uri(RecordedTestAdapter.Executor);
                    discoverySink.SendTestCase(test);
                }
                Logger.Log($"RecordingDiscoverer.DiscoverTests(): EXIT");
            }
            catch (Exception ex)
            {
                Logger.Log($"EXCEPTION: {ex.Message}\nSTACK: {ex.StackTrace}");
                throw;
            }
        }
    }
}