//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using YamlDotNet.RepresentationModel;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public partial class YamlTestCaseParser
    {
        public static IEnumerable<TestCase> TestCasesFromYaml(string source, FileInfo file)
        {
            var defaultTags = YamlTagHelpers.FindAndGetDefaultTags(file.Directory);

            var workingDirectory = GetScalarString(null, defaultTags, "workingDirectory");
            workingDirectory = UpdateWorkingDirectory(file.Directory.FullName, workingDirectory);

            var context = new YamlTestCaseParseContext() {
                Source = source,
                File = file,
                Area = GetRootArea(file),
                Class = GetScalarString(null, defaultTags, "class", defaultClassName),
                Tags = defaultTags,
                Environment = YamlEnvHelpers.GetDefaultEnvironment(true, workingDirectory),
                WorkingDirectory = workingDirectory
            };

            var parsed = YamlHelpers.ParseYamlStream(file.FullName);
            return TestCasesFromYamlStream(context, parsed).ToList();
        }

        #region private methods

        private static IEnumerable<TestCase> TestCasesFromYamlStream(YamlTestCaseParseContext context, YamlStream parsed)
        {
            var tests = new List<TestCase>();
            foreach (var document in parsed?.Documents)
            {
                var fromDocument = TestCasesFromYamlDocumentRootNode(context, document.RootNode);
                if (fromDocument != null)
                {
                    tests.AddRange(fromDocument);
                }
            }
            return tests;
        }

        private static IEnumerable<TestCase> TestCasesFromYamlDocumentRootNode(YamlTestCaseParseContext context, YamlNode node)
        {
            return node is YamlMappingNode
                ? TestCasesFromYamlMapping(context, node as YamlMappingNode)
                : TestCasesFromYamlSequence(context, node as YamlSequenceNode);
        }

        private static IEnumerable<TestCase> TestCasesFromYamlMapping(YamlTestCaseParseContext context, YamlMappingNode mapping)
        {
            var children = CheckForChildren(context, mapping);
            if (children != null)
            {
                return children;
            }

            var test = GetTestFromNode(context, mapping);
            if (test != null)
            {
                return new[] { test };
            }

            return null;
        }

        private static IEnumerable<TestCase> TestCasesFromYamlSequence(YamlTestCaseParseContext context, YamlSequenceNode sequence)
        {
            var tests = new List<TestCase>();
            if (sequence == null) return tests;

            foreach (var node in sequence.Children)
            {
                var mapping = node as YamlMappingNode;
                if (mapping == null)
                {
                    var message = $"Error parsing YAML: expected mapping at {context.File.FullName}({node.Start.Line})";
                    Logger.LogError(message);
                    return null;
                }

                var fromMapping = TestCasesFromYamlMapping(context, mapping);
                if (fromMapping != null)
                {
                    tests.AddRange(fromMapping);
                }
            }

            return tests;
        }

        private static TestCase GetTestFromNode(YamlTestCaseParseContext context, YamlMappingNode mapping, int stepNumber = 0)
        {
            string cli = GetScalarString(mapping, context.Tags, "cli");
            string parallelize = GetScalarString(mapping, context.Tags, "parallelize");
            string skipOnFailure = GetScalarString(mapping, context.Tags, "skipOnFailure");
            string workingDirectory = UpdateWorkingDirectory(mapping, context.WorkingDirectory);

            string simulate = GetScalarString(mapping, "simulate");
            string command = GetScalarString(mapping, "command");
            string script = GetScalarString(mapping, "script");
            string bash = GetScalarString(mapping, "bash");

            string fullyQualifiedName = command == null && script == null && bash == null
                ? GetFullyQualifiedNameAndCommandFromShortForm(mapping, context.Area, context.Class, ref command, stepNumber)
                : GetFullyQualifiedName(mapping, context.Area, context.Class, stepNumber);
            fullyQualifiedName ??= GetFullyQualifiedName(context.Area, context.Class, $"Expected YAML node ('name') at {context.File.FullName}({mapping.Start.Line})", 0);

            var simulating = !string.IsNullOrEmpty(simulate);
            var neitherOrBoth = (command == null) == (script == null && bash == null);
            if (neitherOrBoth && !simulating)
            {
                var message = $"Error parsing YAML: expected/unexpected key ('name', 'command', 'script', 'bash', 'arguments') at {context.File.FullName}({mapping.Start.Line})";
                Logger.LogError(message);
                return null;
            }

            Logger.Log($"YamlTestCaseParser.GetTests(): new TestCase('{fullyQualifiedName}')");
            var test = new TestCase(fullyQualifiedName, new Uri(YamlTestFramework.FakeExecutor), context.Source)
            {
                CodeFilePath = context.File.FullName,
                LineNumber = mapping.Start.Line
            };

            SetTestCaseProperty(test, "cli", cli);
            SetTestCaseProperty(test, "command", command);
            SetTestCaseProperty(test, "script", script);
            SetTestCaseProperty(test, "bash", bash);
            SetTestCaseProperty(test, "simulate", simulate);
            SetTestCaseProperty(test, "parallelize", parallelize);
            SetTestCaseProperty(test, "skipOnFailure", skipOnFailure);

            var timeout = GetScalarString(mapping, context.Tags, "timeout", YamlTestFramework.DefaultTimeout);
            SetTestCaseProperty(test, "timeout", timeout);

            SetTestCaseProperty(test, "working-directory", workingDirectory);

            var processEnv = YamlEnvHelpers.GetCurrentProcessEnvironment();
            var testEnv = YamlEnvHelpers.UpdateCopyEnvironment(context.Environment, mapping);
            testEnv = YamlEnvHelpers.GetNewAndUpdatedEnvironmentVariables(processEnv, testEnv);
            SetTestCasePropertyMap(test, "env", testEnv);

            SetTestCasePropertyMap(test, "foreach", mapping, "foreach", workingDirectory);
            SetTestCasePropertyMap(test, "arguments", mapping, "arguments", workingDirectory);
            SetTestCasePropertyMap(test, "input", mapping, "input", workingDirectory);

            SetTestCaseProperty(test, "expect", mapping, "expect");
            SetTestCaseProperty(test, "expect-gpt", mapping, "expect-gpt");
            SetTestCaseProperty(test, "not-expect", mapping, "not-expect");

            SetTestCaseTagsAsTraits(test, YamlTagHelpers.UpdateCopyTags(context.Tags, mapping));

            CheckInvalidTestCaseNodes(context, mapping, test);
            return test;
        }

        private static IEnumerable<TestCase> CheckForChildren(YamlTestCaseParseContext context, YamlMappingNode mapping)
        {
            if (mapping.Children.ContainsKey("steps") && mapping.Children["steps"] is YamlSequenceNode stepsSequence)
            {
                context.Class = GetScalarString(mapping, "class", context.Class);
                context.Area = UpdateArea(mapping, context.Area);
                context.Tags = YamlTagHelpers.UpdateCopyTags(context.Tags, mapping);
                context.Environment = YamlEnvHelpers.UpdateCopyEnvironment(context.Environment, mapping);
                context.WorkingDirectory = UpdateWorkingDirectory(mapping, context.WorkingDirectory);

                return TestCasesFromYamlSequenceOfSteps(context, stepsSequence);
            }

            if (mapping.Children.ContainsKey("tests") && mapping.Children["tests"] is YamlSequenceNode testsSequence)
            {
                context.Class = GetScalarString(mapping, "class", context.Class);
                context.Area = UpdateArea(mapping, context.Area);
                context.Tags = YamlTagHelpers.UpdateCopyTags(context.Tags, mapping);
                context.Environment = YamlEnvHelpers.UpdateCopyEnvironment(context.Environment, mapping);
                context.WorkingDirectory = UpdateWorkingDirectory(mapping, context.WorkingDirectory);

                return TestCasesFromYamlSequence(context, testsSequence).ToList();
            }

            return null;
        }

        private static IEnumerable<TestCase> TestCasesFromYamlSequenceOfSteps(YamlTestCaseParseContext context, YamlSequenceNode sequence)
        {
            var tests = new List<TestCase>();
            for (int i = 0; i < sequence.Children.Count; i++)
            {
                var mapping = sequence.Children[i] as YamlMappingNode;
                var test = GetTestFromNode(context, mapping, i + 1);
                if (test != null)
                {
                    tests.Add(test);
                }
            }

            if (tests.Count > 0)
            {
                SetTestCaseProperty(tests[0], "parallelize", "true");
            }

            for (int i = 1; i < tests.Count; i++)
            {
                SetTestCaseProperty(tests[i - 1], "nextStepId", tests[i].Id.ToString());
                SetTestCaseProperty(tests[i], "parallelize", "false");
            }

            return tests;
        }

        private static void CheckInvalidTestCaseNodes(YamlTestCaseParseContext context, YamlMappingNode mapping, TestCase test)
        {
            foreach (YamlScalarNode key in mapping.Children.Keys)
            {
                if (!IsValidTestCaseNode(key.Value) && !test.DisplayName.EndsWith(key.Value))
                {
                    var error = $"Error parsing YAML: Unexpected YAML key/value ('{key.Value}', '{test.DisplayName}') in {context.File.FullName}({mapping[key].Start.Line})";
                    test.DisplayName = error;
                    Logger.LogError(error);
                }
            }
        }

        private static bool IsValidTestCaseNode(string value)
        {
            return ";area;class;name;cli;command;script;bash;timeout;foreach;arguments;input;expect;expect-gpt;not-expect;parallelize;simulate;skipOnFailure;tag;tags;workingDirectory;env;sanitize;".IndexOf($";{value};") >= 0;
        }

        private static void SetTestCaseProperty(TestCase test, string propertyName, YamlMappingNode mapping, string mappingName)
        {
            string value = GetScalarString(mapping, mappingName);
            SetTestCaseProperty(test, propertyName, value);
        }

        private static void SetTestCaseProperty(TestCase test, string propertyName, string value)
        {
            if (value != null)
            {
                YamlTestProperties.Set(test, propertyName, value);
            }
        }

        private static void SetTestCasePropertyMap(TestCase test, string propertyName, IDictionary<string, string> map)
        {
            var sb = new StringBuilder();
            foreach (var key in map.Keys)
            {
                sb.Append($"{key}={map[key]}\n");
            }

            SetTestCaseProperty(test, propertyName, sb.ToString());
        }

        private static void SetTestCasePropertyMap(TestCase test, string propertyName, YamlMappingNode testNode, string mappingName, string workingDirectory)
        {
            var ok = testNode.Children.ContainsKey(mappingName);
            if (!ok) return;

            var argumentsNode = testNode.Children[mappingName];
            if (argumentsNode == null) return;

            if (argumentsNode is YamlScalarNode)
            {
                var value = (argumentsNode as YamlScalarNode).Value;
                SetTestCaseProperty(test, propertyName, $"\"{value}\"");
            }
            else if (argumentsNode is YamlMappingNode)
            {
                var asMapping = argumentsNode as YamlMappingNode;
                SetTestCasePropertyMap(test, propertyName, asMapping
                    .Select(x => NormalizeToScalarKeyValuePair(test, x, workingDirectory)));
            }
            else if (argumentsNode is YamlSequenceNode)
            {
                var asSequence = argumentsNode as YamlSequenceNode;

                SetTestCasePropertyMap(test, propertyName, asSequence
                    .Select(mapping => (mapping as YamlMappingNode)?
                        .Select(x => NormalizeToScalarKeyValuePair(test, x, workingDirectory))));
            }
        }

        private static void SetTestCasePropertyMap(TestCase test, string propertyName, IEnumerable<IEnumerable<KeyValuePair<YamlNode, YamlNode>>> kvss)
        {
            // flatten the kvs
            var kvs = kvss.SelectMany(x => x);

            // ensure all keys are unique, if not, transform appropriately
            var keys = kvs.GroupBy(kv => (kv.Key as YamlScalarNode)?.Value).Select(g => g.Key).ToArray();
            if (keys.Length < kvs.Count())
            {
                Logger.Log($"keys.Length={keys.Length}, kvs.Count={kvs.Count()}");
                Logger.Log($"keys='{string.Join(",", keys)}'");

                var values = new List<string>();
                foreach (var items in kvss)
                {
                    var map = new YamlMappingNode(items);
                    values.Add(map.ConvertScalarMapToTsvString(keys));
                }

                var combinedKey = new YamlScalarNode(string.Join("\t", keys));
                var combinedValue = new YamlScalarNode(string.Join("\n", values));
                var combinedKv = new KeyValuePair<YamlNode, YamlNode>(combinedKey, combinedValue);
                kvs = new List<KeyValuePair<YamlNode, YamlNode>>(new[] { combinedKv });
            }

            SetTestCasePropertyMap(test, propertyName, kvs);
        }

        private static void SetTestCasePropertyMap(TestCase test, string propertyName, IEnumerable<KeyValuePair<YamlNode, YamlNode>> kvs)
        {
            var newMap = new YamlMappingNode(kvs);
            SetTestCaseProperty(test, propertyName, newMap.ToJsonString());
        }

        private static KeyValuePair<YamlNode, YamlNode> NormalizeToScalarKeyValuePair(TestCase test, KeyValuePair<YamlNode, YamlNode> item, string workingDirectory = null)
        {
            var key = item.Key;
            var keyOk = key is YamlScalarNode;
            var value = item.Value;
            var valueOk = value is YamlScalarNode;
            if (keyOk && valueOk) return item;

            string[] keys = null;
            if (!keyOk)
            {
                var text = key.ConvertScalarSequenceToTsvString();
                if (text == null)
                {
                    text = $"Invalid key at {test.CodeFilePath}({key.Start.Line},{key.Start.Column})";
                    Logger.Log(text);
                }
                else if (text.Contains('\t'))
                {
                    keys = text.Split('\t');
                }
                key = new YamlScalarNode(text);
            }

            if (!valueOk)
            {
                value = value.ConvertScalarSequenceToMultiLineTsvScalarNode(test, keys);
            }
            else
            {
                var scalarValue = value.ToJsonString().Trim('\"');
                if (TryGetFileContentFromScalar(scalarValue, workingDirectory, out string fileContent))
                {
                    value = fileContent;
                    if (!(value is YamlScalarNode))
                    {
                        value = value.ConvertScalarSequenceToMultiLineTsvScalarNode(test, keys);
                    }
                }
            }

            Logger.Log($"YamlTestCaseParser.NormalizeToScalarKeyValuePair: key='{(key as YamlScalarNode).Value}', value='{(value as YamlScalarNode).Value}'");
            return new KeyValuePair<YamlNode, YamlNode>(key, value);
        }

        private static bool TryGetFileContentFromScalar(string scalar, string workingDirectory, out string fileContent)
        {
            // Treat this scalar value as file if it starts with '@' and does not have InvalidFileNameChars
            if (scalar.StartsWith("@") && Path.GetFileName(scalar).IndexOfAny(Path.GetInvalidFileNameChars()) == -1)
            {
                var fileName = scalar.Substring(1);

                // check if the file already exists
                var filePath = fileName;
                if (!File.Exists(filePath))
                {
                    filePath = Path.Combine(workingDirectory, fileName);
                }

                Logger.Log($"YamlTestCaseParser.TryGetFileContentFromScalar: Read file contents from {filePath}");
                if (File.Exists(filePath))
                {
                    fileContent = File.ReadAllText(filePath);
                    return true;
                }
            }

            fileContent = "";
            return false;
        }

        private static string GetScalarString(YamlMappingNode mapping, Dictionary<string, List<string>> tags, string mappingName, string defaultValue = null)
        {
            var value = GetScalarString(mapping, mappingName, null);
            if (value != null) return value;

            if (tags.ContainsKey(mappingName))
            {
                value = tags[mappingName].Last();
            }

            return value ?? defaultValue;
        }

        private static string GetScalarString(YamlMappingNode mapping, string mappingName, string defaultValue = null)
        {
            var ok = mapping != null && mapping.Children.ContainsKey(mappingName);
            if (!ok) return defaultValue;

            var node = mapping.Children[mappingName] as YamlScalarNode;
            var value = node?.Value;

            return value ?? defaultValue;
        }

        private static string GetYamlNodeAsString(YamlMappingNode mapping, string nodeName, string defaultValue = null)
        {
            var ok = mapping.Children.ContainsKey(nodeName);
            if (!ok) return defaultValue;

            var node = mapping.Children[nodeName];
            var value = node?.ToYamlString();

            return value ?? defaultValue;
        }

        private static string GetRootArea(FileInfo file)
        {
            return $"{file.Extension.TrimStart('.')}.{file.Name.Remove(file.Name.LastIndexOf(file.Extension))}";
        }

        private static string UpdateArea(YamlMappingNode mapping, string area)
        {
            var subArea = GetScalarString(mapping, "area");
            return string.IsNullOrEmpty(subArea)
                ? area
                : $"{area}.{subArea}";
        }

        private static string GetFullyQualifiedName(YamlMappingNode mapping, string area, string @class, int stepNumber)
        {
            var name = GetScalarString(mapping, "name");
            if (name == null) return null;

            area = UpdateArea(mapping, area);
            @class = GetScalarString(mapping, "class", @class);

            return GetFullyQualifiedName(area, @class, name, stepNumber);
        }

        private static string GetFullyQualifiedNameAndCommandFromShortForm(YamlMappingNode mapping, string area, string @class, ref string command, int stepNumber)
        {
            // if there's only one invalid mapping node, we'll treat it's key as "name" and value as "command"
            var invalid = mapping.Children.Keys.Where(key => !IsValidTestCaseNode((key as YamlScalarNode).Value));
            if (invalid.Count() == 1 && command == null)
            {
                var name = (invalid.FirstOrDefault() as YamlScalarNode).Value;
                if (name == null) return null;

                command = GetScalarString(mapping, name);
                area = UpdateArea(mapping, area);
                @class = GetScalarString(mapping, "class", @class);

                return GetFullyQualifiedName(area, @class, name, stepNumber);
            }

            return null;
        }

        private static string GetFullyQualifiedName(string area, string @class, string name, int stepNumber)
        {
            return stepNumber > 0
                ? $"{area}.{@class}.{stepNumber:D2}.{name}"
                : $"{area}.{@class}.{name}";
        }

        private static void SetTestCaseTagsAsTraits(TestCase test, Dictionary<string, List<string>> tags)
        {
            foreach (var tag in tags)
            {
                foreach (var value in tag.Value)
                {
                    test.Traits.Add(tag.Key, value);
                }
            }
        }

        private static string UpdateWorkingDirectory(YamlMappingNode mapping, string currentWorkingDirectory)
        {
            var workingDirectory = GetScalarString(mapping, "workingDirectory");
            return UpdateWorkingDirectory(currentWorkingDirectory, workingDirectory);
        }

        private static string UpdateWorkingDirectory(string currentWorkingDirectory, string workingDirectory)
        {
            return string.IsNullOrEmpty(workingDirectory)
                ? currentWorkingDirectory
                : PathHelpers.Combine(currentWorkingDirectory, workingDirectory);
        }

        private const string defaultClassName = "TestCases";

        #endregion
    }
}
