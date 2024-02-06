using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using YamlTestAdapter;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public class RecordedTestObservor : IYamlTestFrameworkHost
    {
        private readonly IFrameworkHandle _frameworkHandle;
        RecordedTestMode _mode;
        string _id;

        public RecordedTestObservor(IFrameworkHandle frameworkHandle)
        {
            _frameworkHandle = frameworkHandle;
            var modeAsString = Environment.GetEnvironmentVariable("TEST_MODE");
            if (modeAsString != null)
            {
                _mode = (RecordedTestMode)Enum.Parse(typeof(RecordedTestMode), modeAsString, true);
            }
            else
            {
                _mode = RecordedTestMode.Live;
            }

            _mode = RecordedTestMode.Playback;
        }

        public void RecordStart(TestCase testCase)
        {
            _frameworkHandle?.RecordStart(testCase);

            TestProxyClient.AddUriSinatizer("https://(?<host>[^/]+)/", "https://FakeEndpont/").Wait();
            TestProxyClient.AddHeaderSanitizer("api-key", "KEY").Wait();

            _id = _mode switch
            {
                RecordedTestMode.Record => TestProxyClient.StartRecording(testCase.FullyQualifiedName).Result,
                RecordedTestMode.Playback => TestProxyClient.StartPlayback(testCase.FullyQualifiedName).Result,
                RecordedTestMode.Live => null,
                _ => throw new InvalidOperationException("Invalid mode")
            };

            Console.WriteLine("ID: " + _id);
        }

        public void RecordResult(TestResult testResult)
        {
            _frameworkHandle?.RecordResult(testResult);
        }

        public void RecordEnd(TestCase testCase, TestOutcome outcome)
        {
            _frameworkHandle?.RecordEnd(testCase, outcome);
            switch (_mode)
            {
                case RecordedTestMode.Record:
                    TestProxyClient.StopRecording(_id).Wait();
                    break;
                case RecordedTestMode.Playback:
                    TestProxyClient.StopPlayback(_id).Wait();
                    break;
                case RecordedTestMode.Live:
                    // Live test
                    break;
            }

            TestProxyClient.ClearSanatizers().Wait();
        }
    }
}
