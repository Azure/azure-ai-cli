#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.Details.Common.CLI.AzCli;

namespace Azure.AI.CLI.Common.Clients.Models.Utils
{
    /// <summary>
    /// Converter used when serializing or deserializing JSON to normalize the resource kind into our defined list. Anything
    /// not in the enumeration above (or with a custom override) will be set to <see cref="ResourceKind.Unknown"/>
    /// </summary>
    public class ResourceKindJsonConverter : JsonConverter<ResourceKind>
    {
        private readonly ResourceKind _defaultKind;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public ResourceKindJsonConverter() : this(ResourceKind.Unknown)
        { }

        /// <summary>
        /// Creates a new instance with the kind to use should parsing the resource kind fail
        /// </summary>
        /// <param name="fallback">The fallback <see cref="ResourceKind"/> to use</param>
        public ResourceKindJsonConverter(ResourceKind fallback)
        {
            _defaultKind = fallback;
        }

        /// <inheritdoc />
        public override ResourceKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? strValue = reader.GetString();
            return ResourceKindHelpers.ParseString(strValue) ?? _defaultKind;
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, ResourceKind value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(ResourceKindHelpers.AsJsonString(value));
        }
    }
}
