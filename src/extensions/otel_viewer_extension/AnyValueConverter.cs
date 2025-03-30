using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTelemetry.Proto.Common.V1;
using Azure.AI.Details.Common.CLI;

namespace Azure.AI.Details.Common.CLI.Extensions.Otel
{
    public class AnyValueConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(AnyValue);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject item = JObject.Load(reader);
            var anyValue = new AnyValue();

            // Check and set the string value
            if (item["hasStringValue"]?.Value<bool>() == true)
            {
                anyValue.StringValue = item["stringValue"]?.Value<string>();
            }
            if (item["hasBoolValue"]?.Value<bool>() == true)
            {
                anyValue.BoolValue = (bool)item["boolValue"]?.Value<bool>();
            }
            if (item["hasIntValue"]?.Value<bool>() == true)
            {
                anyValue.IntValue = (int)item["intValue"]?.Value<int>();
            }
            if (item["hasDoubleValue"]?.Value<bool>() == true)
            {
                anyValue.DoubleValue = (double)item["doubleValue"]?.Value<double>();
            }
            return anyValue;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }


}
