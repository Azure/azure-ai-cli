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

namespace TestAdapterTest
{
    [FileExtension(YamlTestAdapter.FileExtensionYaml)]
    [FileExtension(YamlTestAdapter.FileExtensionDll)]
    [DefaultExecutorUri(YamlTestAdapter.Executor)]
    public class TestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            try
            {
                Logger.Log(logger);
                Logger.Log($"TestDiscoverer.DiscoverTests(): ENTER");
                Logger.Log($"TestDiscoverer.DiscoverTests(): count={sources.Count()}");
                foreach (var test in YamlTestAdapter.GetTestsFromFiles(sources))
                {
                    discoverySink.SendTestCase(test);
                }
                Logger.Log($"TestDiscoverer.DiscoverTests(): EXIT");
            }
            catch (Exception ex)
            {
                Logger.Log($"EXCEPTION: {ex.Message}\nSTACK: {ex.StackTrace}");
                throw ex;
            }
        }
    }
}
