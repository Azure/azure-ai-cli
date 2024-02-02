using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Linq;
using System.Text.Json;
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
            if (string.IsNullOrEmpty(modeAsString) || !Enum.TryParse<RecordedTestMode>(modeAsString, true, out _mode))
            {
                _mode = RecordedTestMode.Live;
            }
        }

        public void RecordStart(TestCase testCase)
        {
            _frameworkHandle?.RecordStart(testCase);

            if (_mode != RecordedTestMode.Live)
            {
                Environment.SetEnvironmentVariable("HTTPS_PROXY", "http://localhost:5004");
                foreach (var trait in testCase.Traits.Where((Trait t) => t.Name.Equals("_sanitize", StringComparison.OrdinalIgnoreCase)))
                {
                    var sanitizeJson = trait.Value;
                    var sanitize = System.Text.Json.JsonDocument.Parse(sanitizeJson);
                    foreach (var sanitizeLine in sanitize.RootElement.EnumerateArray())
                    {
                        JsonElement currentElement;

                        if (sanitizeLine.TryGetProperty("headers", out currentElement))
                        {
                            foreach (var header in currentElement.EnumerateArray())
                            {
                                var value = header.GetProperty("value").GetString();
                                var name = header.GetProperty("name").GetString();

                                JsonElement element;
                                string regex = null;
                                if (header.TryGetProperty("regex", out element))
                                {
                                    regex = element.GetString();
                                }
                                TestProxyClient.AddHeaderSanitizer(name, regex, value).Wait();
                            }
                        }
                        else if (sanitizeLine.TryGetProperty("uri", out currentElement))
                        {
                            foreach (var uri in currentElement.EnumerateArray())
                            {
                                TestProxyClient.AddUriSinatizer(uri.GetProperty("regex").GetString(), uri.GetProperty("value").GetString()).Wait();
                            }
                        }
                        else if (sanitizeLine.TryGetProperty("body", out currentElement))
                        {
                            foreach (var body in currentElement.EnumerateArray())
                            {
                                TestProxyClient.AddBodySanitizer(body.GetProperty("regex").GetString(), body.GetProperty("value").GetString()).Wait();
                            }
                        }
                    }
                }
            }

            _id = _mode switch
            {
                RecordedTestMode.Record => TestProxyClient.StartRecording(testCase.FullyQualifiedName).Result,
                RecordedTestMode.Playback => TestProxyClient.StartPlayback(testCase.FullyQualifiedName).Result,
                RecordedTestMode.Live => null,
                _ => throw new InvalidOperationException("Invalid mode")
            };
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
                    Environment.SetEnvironmentVariable("HTTPS_PROXY", null);
                    TestProxyClient.ClearSanatizers().Wait();
                    break;
                case RecordedTestMode.Playback:
                    TestProxyClient.StopPlayback(_id).Wait();
                    Environment.SetEnvironmentVariable("HTTPS_PROXY", null);
                    TestProxyClient.ClearSanatizers().Wait();
                    break;
                case RecordedTestMode.Live:
                    // Live test
                    break;
            }
        }
    }
}
