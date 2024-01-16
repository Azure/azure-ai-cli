using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using YamlDotNet.Helpers;
using YamlDotNet.RepresentationModel;

namespace TestAdapterTest
{
    public class YamlTestCaseParser
    {
        public static IEnumerable<TestCase> TestCasesFromYaml(string source, FileInfo file)
        {
            var area = GetRootArea(file);
            var parsed = ParseYamlStream(file.FullName);
            return TestCasesFromYamlStream(source, file, area, parsed);
        }

        #region private methods

        private static YamlStream ParseYamlStream(string fullName)
        {
            var stream = new YamlStream();
            var text = File.OpenText(fullName);
            var error = string.Empty;

            try
            {
                stream.Load(text);
            }
            catch (YamlDotNet.Core.YamlException ex)
            {
                var where = $"{fullName}({ex.Start.Line},{ex.Start.Column})";
                error = $"Error parsing YAML (YamlException={ex.GetType()}):\n  {where}\n  {ex.Message}";
            }
            catch (Exception ex)
            {
                var where = fullName;
                error = $"Error parsing YAML (YamlException={ex.GetType()}):\n  {where}\n  {ex.Message}";
            }

            if (!string.IsNullOrEmpty(error))
            {
                Logger.LogError(error);
                Logger.TraceError(error);
            }

            return stream;
        }

        private static IEnumerable<TestCase> TestCasesFromYamlStream(string source, FileInfo file, string area, YamlStream parsed)
        {
            var tests = new List<TestCase>();
            var defaultTags = GetDefaultTags(file.Directory);
            var parallelize = "false";
            if (defaultTags.ContainsKey("parallelize"))
            {
                parallelize = defaultTags["parallelize"].Last();
            }
            foreach (var document in parsed?.Documents)
            {
                var fromDocument = TestCasesFromYamlNode(source, file, document.RootNode, area, defaultClassName, defaultTags, parallelize);
                tests.AddRange(fromDocument);
            }
            return tests;
        }

        private static IEnumerable<TestCase> TestCasesFromYamlNode(string source, FileInfo file, YamlNode node, string area, string @class, Dictionary<string, List<string>> tags, string parallelize)
        {
            return node is YamlMappingNode
                ? TestCasesFromYamlMapping(source, file, node as YamlMappingNode, area, @class, tags, parallelize)
                : TestCasesFromYamlSequence(source, file, node as YamlSequenceNode, area, @class, tags, parallelize);
        }

        private static IEnumerable<TestCase> TestCasesFromYamlMapping(string source, FileInfo file, YamlMappingNode mapping, string area, string @class, Dictionary<string, List<string>> tags, string parallelize)
        {
            var children = CheckForChildren(source, file, mapping, area, @class, tags, parallelize);
            if (children != null)
            {
                return children;
            }

            var test = GetTestFromNode(source, file, mapping, area, @class, tags, parallelize);
            if (test != null)
            {
                return new[] { test };
            }

            return null;
        }

        private static IEnumerable<TestCase> TestCasesFromYamlSequence(string source, FileInfo file, YamlSequenceNode sequence, string area, string @class, Dictionary<string, List<string>> tags, string parallelize)
        {
            var tests = new List<TestCase>();
            if (sequence == null) return tests;

            foreach (YamlMappingNode mapping in sequence.Children)
            {
                var fromMapping = TestCasesFromYamlMapping(source, file, mapping, area, @class, tags, parallelize);
                if (fromMapping != null)
                {
                    tests.AddRange(fromMapping);
                }
            }

            return tests;
        }

        private static TestCase GetTestFromNode(string source, FileInfo file, YamlMappingNode mapping, string area, string @class, Dictionary<string, List<string>> tags, string parallelize)
        {
            string simulate = GetScalarString(mapping, "simulate");
            var simulating = !string.IsNullOrEmpty(simulate);

            string cli = GetScalarString(mapping, "cli");
            if (cli == null && tags.ContainsKey("cli"))
            {
                cli = tags["cli"].Last();
            }

            string currentParallelize = GetScalarString(mapping, "parallelize");
            parallelize = currentParallelize == null ? parallelize : currentParallelize;

            string command = GetScalarString(mapping, "command");
            string script = GetScalarString(mapping, "script");

            string fullyQualifiedName = command == null && script == null
                ? GetFullyQualifiedNameAndCommandFromShortForm(mapping, area, @class, ref command)
                : GetFullyQualifiedName(mapping, area, @class);
            fullyQualifiedName ??= GetFullyQualifiedName(area, @class, $"Expected YAML node ('name') at {file.FullName}({mapping.Start.Line})");

            var neitherOrBoth = (command == null) == (script == null);
            if (neitherOrBoth && !simulating)
            {
                var message = $"Error parsing YAML: expected/unexpected key ('name', 'command', 'script', 'arguments') at {file.FullName}({mapping.Start.Line})";
                Logger.LogError(message);
                Logger.TraceError(message);
                return null;
            }

            Logger.Log($"YamlTestCaseParser.GetTests(): new TestCase('{fullyQualifiedName}')");
            var test = new TestCase(fullyQualifiedName, new Uri(YamlTestAdapter.Executor), source)
            {
                CodeFilePath = file.FullName,
                LineNumber = mapping.Start.Line
            };

            SetTestCaseProperty(test, "cli", cli);
            SetTestCaseProperty(test, "command", command);
            SetTestCaseProperty(test, "script", script);
            SetTestCaseProperty(test, "simulate", simulate);
            SetTestCaseProperty(test, "parallelize", parallelize);

            var timeout = GetScalarString(mapping, "timeout") ?? YamlTestAdapter.DefaultTimeout;
            SetTestCaseProperty(test, "timeout", timeout);

            var workingDirectory = GetScalarString(mapping, "workingDirectory") ?? file.DirectoryName;
            SetTestCaseProperty(test, "working-directory", workingDirectory);

            SetTestCasePropertyMap(test, "foreach", mapping, "foreach", workingDirectory);
            SetTestCasePropertyMap(test, "arguments", mapping, "arguments", workingDirectory);

            SetTestCaseProperty(test, "expect", mapping, "expect");
            SetTestCaseProperty(test, "not-expect", mapping, "not-expect");

            SetTestCaseTagsAsTraits(test, UpdateCopyTags(tags, mapping));

            CheckInvalidTestCaseNodes(file, mapping, test);
            return test;
        }

        private static IEnumerable<TestCase> CheckForChildren(string source, FileInfo file, YamlMappingNode mapping, string area, string @class, Dictionary<string, List<string>> tags, string parallelize)
        {
            var sequence = mapping.Children.ContainsKey("tests")
                ? mapping.Children["tests"] as YamlSequenceNode
                : null;
            if (sequence == null) return null;

            @class = GetScalarString(mapping, "class", @class);
            area = UpdateArea(mapping, area);
            tags = UpdateCopyTags(tags, mapping);
            parallelize = GetParallelizeTag(mapping, parallelize);

            return TestCasesFromYamlSequence(source, file, sequence, area, @class, tags, parallelize);
        }

        private static void CheckInvalidTestCaseNodes(FileInfo file, YamlMappingNode mapping, TestCase test)
        {
            foreach (YamlScalarNode key in mapping.Children.Keys)
            {
                if (!IsValidTestCaseNode(key.Value) && !test.DisplayName.EndsWith(key.Value))
                {
                    var error = $"Error parsing YAML: Unexpected YAML key/value ('{key.Value}', '{test.DisplayName}') in {file.FullName}({mapping[key].Start.Line})";
                    test.DisplayName = error;
                    Logger.LogError(error);
                    Logger.TraceError(error);
                }
            }
        }

        private static bool IsValidTestCaseNode(string value)
        {
            return ";area;class;name;cli;command;script;timeout;foreach;arguments;expect;not-expect;simulate;tag;tags;parallelize".IndexOf($";{value};") >= 0;
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

        private static string GetScalarString(YamlMappingNode mapping, string mappingName, string defaultValue = null)
        {
            var ok = mapping.Children.ContainsKey(mappingName);
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

        private static string GetFullyQualifiedName(YamlMappingNode mapping, string area, string @class)
        {
            var name = GetScalarString(mapping, "name");
            if (name == null) return null;

            area = UpdateArea(mapping, area);
            @class = GetScalarString(mapping, "class", @class);

            return GetFullyQualifiedName(area, @class, name);
        }

        private static string GetFullyQualifiedNameAndCommandFromShortForm(YamlMappingNode mapping, string area, string @class, ref string command)
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

                return GetFullyQualifiedName(area, @class, name);
            }

            return null;
        }

        private static string GetFullyQualifiedName(string area, string @class, string name)
        {
            return $"{area}.{@class}.{name}";
        }

        private static string GetYamlDefaultsFullFileName(DirectoryInfo directory)
        {
            var found = directory.GetFiles(YamlTestAdapter.YamlDefaultsFileName);
            return found.Length == 1
                ? found[0].FullName
                : directory.Parent != null
                    ? GetYamlDefaultsFullFileName(directory.Parent)
                    : null;
        }

        private static Dictionary<string, List<string>> GetDefaultTags(DirectoryInfo directory)
        {
            var defaultTags = new Dictionary<string, List<string>>();

            var defaultsFile = GetYamlDefaultsFullFileName(directory);
            if (defaultsFile != null)
            {
                var parsed = ParseYamlStream(defaultsFile);
                if (parsed.Documents.Count() > 0)
                {
                    var tagsNode = parsed.Documents[0].RootNode;
                    if (tagsNode != null)
                    {
                        defaultTags = UpdateCopyTags(defaultTags, null, tagsNode);
                    }
                }
            }

            return defaultTags;
        }

        private static string GetParallelizeTag(YamlMappingNode mapping, string currentParallelize)
        {
            var parallelizeNode = mapping.Children.ContainsKey("parallelize") ? mapping.Children["parallelize"] : null;
            return parallelizeNode == null ? currentParallelize : (parallelizeNode as YamlScalarNode)?.Value;
        }

        private static Dictionary<string, List<string>> UpdateCopyTags(Dictionary<string, List<string>> tags, YamlMappingNode mapping)
        {
            var tagNode = mapping.Children.ContainsKey("tag") ? mapping.Children["tag"] : null;
            var tagsNode = mapping.Children.ContainsKey("tags") ? mapping.Children["tags"] : null;
            if (tagNode == null && tagsNode == null) return tags;

            return UpdateCopyTags(tags, tagNode, tagsNode);
        }

        private static Dictionary<string, List<string>> UpdateCopyTags(Dictionary<string, List<string>> tags, YamlNode tagNode, YamlNode tagsNode)
        {
            // make a copy that we'll update and return
            tags = new Dictionary<string, List<string>>(tags);

            var value = (tagNode as YamlScalarNode)?.Value;
            AddOptionalTag(tags, "tag", value);

            var values = (tagsNode as YamlScalarNode)?.Value;
            AddOptionalCommaSeparatedTags(tags, values);

            AddOptionalNameValueTags(tags, tagsNode as YamlMappingNode);
            AddOptionalTagsForEachChild(tags, tagsNode as YamlSequenceNode);

            return tags;
        }

        private static void AddOptionalTag(Dictionary<string, List<string>> tags, string name, string value)
        {
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
            {
                if (!tags.ContainsKey(name))
                {
                    tags.Add(name, new List<string>());
                }
                tags[name].Add(value);
            }
        }

        private static void AddOptionalCommaSeparatedTags(Dictionary<string, List<string>> tags, string values)
        {
            if (values != null)
            {
                foreach (var tag in values.Split(",".ToArray(), StringSplitOptions.RemoveEmptyEntries))
                {
                    AddOptionalTag(tags, "tag", tag);
                }
            }
        }

        private static void AddOptionalNameValueTags(Dictionary<string, List<string>> tags, YamlMappingNode mapping)
        {
            var children = mapping?.Children;
            if (children == null) return;

            foreach (var child in children)
            {
                var key = (child.Key as YamlScalarNode)?.Value;
                var value = (child.Value as YamlScalarNode)?.Value;
                AddOptionalTag(tags, key, value);
            }
        }

        private static void AddOptionalTagsForEachChild(Dictionary<string, List<string>> tags, YamlSequenceNode sequence)
        {
            var children = sequence?.Children;
            if (children == null) return;

            foreach (var child in children)
            {
                if (child is YamlScalarNode)
                {
                    AddOptionalTag(tags, "tag", (child as YamlScalarNode).Value);
                    continue;
                }

                if (child is YamlMappingNode)
                {
                    AddOptionalNameValueTags(tags, child as YamlMappingNode);
                    continue;
                }
            }
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

        private const string defaultClassName = "TestCases";

        #endregion
    }
}
