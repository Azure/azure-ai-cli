using Google.Protobuf;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.AI.Details.Common.CLI;

namespace Azure.AI.Details.Common.CLI.Extensions.Otel
{
    public class IntArrayToByteStringConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ByteString);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JArray byteValues = JArray.Load(reader);
            byte[] bytes = new byte[byteValues.Count];

            for (int i = 0; i < byteValues.Count; i++)
            {
                bytes[i] = (byte)byteValues[i].ToObject<int>();
            }

            return ByteString.CopyFrom(bytes);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            ByteString byteString = (ByteString)value;
            byte[] bytes = byteString.ToByteArray();
            writer.WriteStartArray();
            foreach (byte b in bytes)
            {
                writer.WriteValue(b);
            }
            writer.WriteEndArray();
        }
        public bool ShouldConvert(string path)
        {
            return path.EndsWith(".traceId") || path.EndsWith(".spanId") || path.EndsWith(".parentSpanId");
        }
    }
}
