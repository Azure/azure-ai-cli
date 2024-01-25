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
    public class YamlTestCaseFilter
    {
        public static IEnumerable<TestCase> FilterTestCases(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var names = GetSupportedFilterableNames(tests);
            var filter = runContext.GetTestCaseFilter(names, null);
            return tests.Where(test => filter == null || filter.MatchTestCase(test, name => GetPropertyValue(test, name)));
        }

        private static HashSet<string> GetSupportedFilterableNames(IEnumerable<TestCase> tests)
        {
            var filterable = new HashSet<string>(supportedFilterProperties);
            foreach (var test in tests)
            {
                foreach (var trait in test.Traits)
                {
                    filterable.Add(trait.Name);
                }
            }

            if (filterable.Contains("tag")) filterable.Add("tags");

            return filterable;
        }

        private static object GetPropertyValue(TestCase test, string name)
        {
            switch (name.ToLower())
            {
                case "name":
                case "displayname": return test.DisplayName;

                case "fqn":
                case "fullyqualifiedname": return test.FullyQualifiedName;

                case "cli": return YamlTestProperties.Get(test, "cli");
                case "command": return YamlTestProperties.Get(test, "command");
                case "script": return YamlTestProperties.Get(test, "script");

                case "foreach": return YamlTestProperties.Get(test, "foreach");
                case "arguments": return YamlTestProperties.Get(test, "arguments");
                case "input": return YamlTestProperties.Get(test, "input");

                case "expect": return YamlTestProperties.Get(test, "expect");
                case "not-expect": return YamlTestProperties.Get(test, "not-expect");

                case "parallelize": return YamlTestProperties.Get(test, "parallelize");
                case "simulate": return YamlTestProperties.Get(test, "simulate");
                case "skipOnFailure": return YamlTestProperties.Get(test, "skipOnFailure");

                case "timeout": return YamlTestProperties.Get(test, "timeout");
                case "working-directory": return YamlTestProperties.Get(test, "working-directory");
            }

            var tags = test.Traits.Where(x => x.Name == name || name == "tags");
            if (tags.Count() == 0) return null;

            return tags.Select(x => x.Value).ToArray();
        }

        private static readonly string[] supportedFilterProperties = { "DisplayName", "FullyQualifiedName", "Category", "cli", "command", "script", "foreach", "arguments", "input", "expect", "not-expect", "parallelize", "simulate", "skipOnFailure" };
    }
}
