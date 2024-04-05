#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Azure.AI.CLI.Common.Clients.Models.Utils
{
    /// <summary>
    /// Json converter that supports a boolean value. Unlike the default behavior of System.Text.Json, this supports deserializing
    /// a <see cref="bool"> value from a string
    /// </summary>
    public class StringToBoolJsonConverter : JsonConverter<bool>
    {
        /// <inheritdoc />
        public override bool HandleNull => true;

        /// <inheritdoc />
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => JsonStringToBoolHelpers.ReadBool(ref reader, options) ?? false;

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
            => JsonStringToBoolHelpers.WriteBool(writer, value, options);
    }

    /// <summary>
    /// Json converter that supports a nullable boolean. Unlike the default behavior of System.Text.Json, this supports
    /// deserializing a <see cref="bool?"/> value from a string
    /// </summary>
    public class StringToNullableBoolJsonConverter : JsonConverter<bool?>
    {
        /// <inheritdoc />
        public override bool HandleNull => true;

        /// <inheritdoc />
        public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => JsonStringToBoolHelpers.ReadBool(ref reader, options);

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
            => JsonStringToBoolHelpers.WriteBool(writer, value, options);
    }

    /// <summary>
    /// Helper methods to read and write a boolean to/from strings
    /// </summary>
    internal static class JsonStringToBoolHelpers
    {
        /// <summary>
        /// Helper method to read a boolean. This supports parsing string values as booleans
        /// </summary>
        /// <param name="reader">The reader to read from. The reader must be at a position where a boolean can be read (e.g. a boolean token)/// </param>
        /// <param name="options">The options to use when deserializing. This is used to determine how to parse the boolean value</param>
        /// <returns>The boolean value read from the reader, or null if the value was null</returns>
        /// <exception cref="JsonException">
        /// Thrown if the reader is not at a position where a boolean can be read, or the string value could not be parsed into boolean
        /// </exception>
        internal static bool? ReadBool(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.String:
                    string? str = reader.GetString();
                    if (bool.TryParse(str, out var parsed))
                    {
                        return parsed;
                    }

                    throw new JsonException($"Can't parse '{str}' into a boolean");
                default:
                    throw new JsonException("Cannot parse into a boolean");
            }
        }

        /// <summary>
        /// Helper method to write a boolean
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">The value to write</param>
        /// <param name="options">The options to use when serializing. This is used to determine how to write the boolean value</param>
        internal static void WriteBool(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteBooleanValue(value.Value);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
