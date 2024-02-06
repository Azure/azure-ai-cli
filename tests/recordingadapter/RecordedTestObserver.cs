using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using YamlTestAdapter;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public class RecordedTestObservor : IYamlTestFrameworkReporter
    {
        private readonly IYamlTestFrameworkReporter _wrappedReporter;
        RecordedTestMode _mode;
        string _id;

        public RecordedTestObservor(IYamlTestFrameworkReporter wrappedReporter = null)
        {
            _wrappedReporter = wrappedReporter;
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
            _wrappedReporter?.RecordStart(testCase);

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
            _wrappedReporter?.RecordResult(testResult);
        }

        public void RecordEnd(TestCase testCase, TestOutcome outcome)
        {
            _wrappedReporter?.RecordEnd(testCase, outcome);
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
