#nullable enable

using Azure.AI.Details.Common.CLI.AzCli;
using Newtonsoft.Json;

namespace Azure.AI.CLI.Common.Clients.Models.Utils
{
    /// <summary>
    /// Converter used when serializing or deserializing JSON to normalize the resource kind into our defined list. Anything
    /// not in the enumeration above (or with a custom override) will be set to <see cref="ResourceKind.Unknown"/>
    /// </summary>
    public class ResourceKindJsonConverter : JsonConverter<ResourceKind>
    {
        private readonly ResourceKind _defaultKind;

        public ResourceKindJsonConverter() : this(ResourceKind.Unknown)
        { }

        public ResourceKindJsonConverter(ResourceKind fallback)
        {
            _defaultKind = fallback;
        }

        /// <inheritdoc />
        public override ResourceKind ReadJson(JsonReader reader, Type objectType, ResourceKind existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string? strValue = reader.Value as string;
            return ResourceKindHelpers.ParseString(strValue) ?? _defaultKind;
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, ResourceKind value, JsonSerializer serializer)
        {
            writer.WriteValue(ResourceKindHelpers.AsJsonString(value));
        }
    }
}
