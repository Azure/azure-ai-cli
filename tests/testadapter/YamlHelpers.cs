using System;
using System.IO;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace TestAdapterTest
{
    public class YamlHelpers
    {
        public static YamlStream ParseYamlStream(string fullName)
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
}
