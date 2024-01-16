using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace TestAdapterTest
{
    public class YamlHelpers
    {
        public static string ToYamlOrJsonString(YamlNode node, bool yaml)
        {
            var serializer = yaml
                ? new SerializerBuilder().Build()
                : new SerializerBuilder().JsonCompatible().Build();

            using var writer = new StringWriter();
            var stream = new YamlStream { new YamlDocument(node) };
            stream.Save(writer);

            using var reader = new StringReader(writer.ToString());
            var deserializer = new Deserializer();
            var yamlObject = deserializer.Deserialize(reader);

            var trimmed = serializer.Serialize(yamlObject).Trim('\r', '\n');
            return yaml ? trimmed : trimmed.Replace("\t", "\\t").Replace("\f", "\\f");
        }
    }

    public static class YamlNodeExtensions
    {
        public static string ToYamlString(this YamlNode node)
        {
            return YamlHelpers.ToYamlOrJsonString(node, true);
        }

        public static string ToJsonString(this YamlNode node)
        {
            return YamlHelpers.ToYamlOrJsonString(node, false);
        }

        public static YamlScalarNode ConvertScalarSequenceToMultiLineTsvScalarNode(this YamlNode yaml, TestCase test, string[] keys)
        {
            var text = yaml.ConvertScalarSequenceToMultilineTsvString(keys);
            if (text == null)
            {
                text = $"Invalid sequence or sequence value at {test.CodeFilePath}({yaml.Start.Line},{yaml.Start.Column})";
                Logger.Log(text);
            }

            return new YamlScalarNode(text);
        }

        public static string ConvertScalarSequenceToMultilineTsvString(this YamlNode node, string[] keys = null)
        {
            // ensure it's a sequence
            var ok = node is YamlSequenceNode;
            if (!ok) return null;

            var lines = new List<string>();
            foreach (var item in (node as YamlSequenceNode).Children)
            {
                var line = item is YamlScalarNode
                    ? (item as YamlScalarNode).Value
                    : item is YamlSequenceNode
                        ? item.ConvertScalarSequenceToTsvString(keys)
                        : item.ConvertScalarMapToTsvString(keys);

                // ensure each item is either scalar, or sequence of scalar                
                var invalidItem = (line == null);
                Logger.LogIf(invalidItem, $"Invalid item at ({item.Start.Line},{item.Start.Column})");
                if (invalidItem) return null; 

                lines.Add(line);
            }
            return string.Join("\n", lines);
        }

        public static string ConvertScalarSequenceToTsvString(this YamlNode node, string[] keys = null)
        {
            // ensure it's a sequence (list/array)
            var sequence = node as YamlSequenceNode;
            if (sequence == null) return null;

            // ensure there are no non-scalar children
            var count = sequence.Count(x => !(x is YamlScalarNode));
            Logger.LogIf(count > 0, $"Invalid: (non-scalar) count({count}) > 0");
            if (count > 0) return null;

            // join the scalar children separated by tabs
            var tsv = string.Join("\t", sequence.Children
                .Select(x => (x as YamlScalarNode).Value));

            // if we don't have enough items, append empty string columns (count of items == count of tabs + 1)
            while (tsv.Count(x => x == '\t') + 1 < keys?.Length)
            {
                tsv += "\t";
            }

            tsv = tsv.Replace('\n', '\f');
            Logger.Log($"YamlNodeExtensions.ConvertScalarSequenceToTsvString: tsv='{tsv}'");
            return tsv;
        }

        public static string ConvertScalarMapToTsvString(this YamlNode node, string[] keys)
        {
            // ensure it's a mapping node and we have keys
            var mapping = node as YamlMappingNode;
            if (mapping == null || keys == null) return null;

            // ensure there are no non-scalar kvp children
            var count = mapping.Count(x => !(x.Key is YamlScalarNode) || !(x.Value is YamlScalarNode));
            Logger.LogIf(count > 0, $"Invalid: (non-scalar key or value) count({count}) > 0");
            if (count > 0) return null;

            // ensure the key specified is in the list of keys
            count = mapping.Count(x => !keys.Contains((x.Key as YamlScalarNode).Value));
            Logger.LogIf(count > 0, $"Invalid: key not found count({count}) > 0");
            if (count > 0) return null;

            // join the scalar children ordered by keys, separated by tabs
            var tsv = string.Join("\t", keys
                .Select(key => mapping.Children.ContainsKey(key)
                    ? (mapping.Children[key] as YamlScalarNode).Value
                    : ""));

            tsv = tsv.Replace('\n', '\f');
            Logger.Log($"YamlNodeExtensions.ConvertScalarMapToTsvString: tsv='{tsv}'");
            return tsv;
        }  
    }
}
