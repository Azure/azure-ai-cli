using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using YamlDotNet.RepresentationModel;

namespace TestAdapterTest
{
    public class YamlTestProperties
    {
        public static void Set(TestCase test, string name, string value)
        {
            Logger.Log($"YamlTestProperties.Set('{name}'='{value.Replace("\n", "\\n")}')");
            if (!string.IsNullOrEmpty(value))
            {
                var property = properties[name];
                test.SetPropertyValue(property, value);
            }
        }

        public static string Get(TestCase test, string name, string defaultValue = null)
        {
            var value = test.GetPropertyValue(properties[name], defaultValue);
            Logger.LogIf(!string.IsNullOrEmpty(value), $"TestCaseProperties.Get('{name}') = '{value?.Replace("\n", "\\n")}'");
            return value;
        }

        #region private methods and data
        private static TestProperty RegisterTestCaseProperty(string name)
        {
            return TestProperty.Register($"YamlTestCase.{name}", name, typeof(string), TestPropertyAttributes.Hidden, typeof(TestCase));
        }

        private static readonly Dictionary<string, TestProperty> properties = new Dictionary<string, TestProperty>() {
            { "cli", RegisterTestCaseProperty("CLI") },
            { "command", RegisterTestCaseProperty("Command") },
            { "script", RegisterTestCaseProperty("Script") },
            { "parallelize", RegisterTestCaseProperty("Parallelize") },
            { "nextStepId", RegisterTestCaseProperty("nextStepId") },
            { "foreach", RegisterTestCaseProperty("ForEach") },
            { "arguments", RegisterTestCaseProperty("Arguments") },
            { "input", RegisterTestCaseProperty("Input")},
            { "expect", RegisterTestCaseProperty("Expect") },
            { "not-expect", RegisterTestCaseProperty("NotExpect") },
            { "simulate", RegisterTestCaseProperty("Simulate") },
            { "skipOnFailure", RegisterTestCaseProperty("SkipOnFailure") },
            { "timeout", RegisterTestCaseProperty("Timeout") },
            { "working-directory", RegisterTestCaseProperty("WorkingDirectory") },
            { "recorded", RegisterTestCaseProperty("Recorded") }
        };

        #endregion
    }
}
