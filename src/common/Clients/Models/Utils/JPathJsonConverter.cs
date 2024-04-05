#nullable enable

using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace Azure.AI.CLI.Common.Clients.Models.Utils
{
    /// <summary>
    /// An attribute to specify the JSON path to use when deserializing a property or field
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
    public class JsonPathPropertyNameAttribute : JsonAttribute
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="jsonPath">The JSON path to use when deserializing</param>
        public JsonPathPropertyNameAttribute(string jsonPath)
        {
            JsonPath = jsonPath;
        }

        /// <summary>
        /// The JSON path to use when deserializing
        /// </summary>
        public string JsonPath { get; set; }
    }

    /// <summary>
    /// A converter to enable the use of a JPath when deserializing JSON
    /// </summary>
    /// <remarks>
    /// To use this correctly you'll need to to the following:
    /// <list type="bullet">
    /// <item>Add a <see cref="JsonPathPropertyNameAttribute"/> attribute to each property or field with the Json path expression you want</item>
    /// <item>Create a <see cref="JsonSerializerOptions"/> instance and add a new instance of this to its converter. DO **NOT** SET THIS IN A <see cref="JsonConverterAttribute"/> as doing so will cause an infinite loop</item>
    /// </list>
    /// </remarks>
    public class JsonPathConverterFactory : JsonConverterFactory
    {
        private static readonly BindingFlags ALL_INSTANCE_MEMBERS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            // TODO ralphe: Should we cache to avoid reflection on every call?
            var nonNullableType = IsNullable(typeToConvert)
            ? typeToConvert.GenericTypeArguments[0]
            : typeToConvert;

            bool hasJPathMembers = GetJPathMembers(typeToConvert).Any();
            return hasJPathMembers;
        }

        /// <inheritdoc />
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var closedType = typeof(JsonPathJsonConverter<>).MakeGenericType([typeToConvert]);
            var converter = (JsonConverter?)Activator.CreateInstance(closedType);
            return converter;
        }

        /// <summary>
        /// Implementation of a converter that uses default serialization first, and then tries to deserialize JSON path
        /// properties
        /// </summary>
        /// <typeparam name="T">The type that contains properties or fields marked with <see cref="JsonPathPropertyNameAttribute"/></typeparam>
        public class JsonPathJsonConverter<T> : JsonConverter<T>
        {
            /// <inheritdoc />
            public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                // 1. Clone the reader (Utf8JsonReader is a struct)
                Utf8JsonReader clonedReader = reader;

                // 2. Use the cloned options (removing this converter to prevent infinite loop) to deserialize the type normally
                var original = options ?? JsonSerializerOptions.Default;
                options = new JsonSerializerOptions(original);
                options.Converters.Clear();
                foreach (var conv in original.Converters
                    .Where(c => c is not JsonPathConverterFactory
                        && !IsFromGeneric(c.GetType(), typeof(JsonPathJsonConverter<>))))
                {
                    options.Converters.Add(conv);
                }

                T? instance = JsonSerializer.Deserialize<T>(ref reader, options);

                // 3. Populate any JPath expressions
                if (instance != null)
                {
                    using var doc = JsonDocument.ParseValue(ref clonedReader);

                    // TODO FIXME Force to be boxed so setters work on structs. Not the most efficient, but it is
                    // the fastest way for now.
                    object? boxed = instance;

                    foreach (var member in GetJPathMembers(typeToConvert))
                    {
                        JsonElement? match = GetJPath(doc.RootElement, member.Path);
                        if (match != null)
                        {
                            object? value = JsonSerializer.Deserialize((JsonElement)match, member.Type, original);
                            if (value != null)
                            {
                                member.Setter(boxed, value);
                            }
                        }
                    }

                    instance = (T?)boxed;
                }

                return instance;
            }

            /// <summary>
            /// This method is not supported
            /// </summary>
            /// <param name="writer">The writer to use</param>
            /// <param name="value">The value to write</param>
            /// <param name="options">The options to use</param>
            /// <exception cref="NotSupportedException"></exception>
            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                throw new NotSupportedException();
            }
        }

        private static bool IsNullable(Type t)
            => t.IsGenericType && t.IsConstructedGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);

        private static IEnumerable<Entry> GetJPathMembers(Type type) =>
            type.GetProperties(ALL_INSTANCE_MEMBERS)
            .Where(p => p.GetCustomAttribute<JsonPathPropertyNameAttribute>() != null)
            .Select(p => new Entry(
                p.Name,
                p.GetCustomAttribute<JsonPathPropertyNameAttribute>()?.JsonPath,
                p.PropertyType,
                p.SetValue))
            .Concat(
                type.GetFields(ALL_INSTANCE_MEMBERS)
                .Where(p => p.GetCustomAttribute<JsonPathPropertyNameAttribute>() != null)
                .Select(f => new Entry(
                    f.Name,
                    f.GetCustomAttribute<JsonPathPropertyNameAttribute>()?.JsonPath,
                    f.FieldType,
                    f.SetValue)));

        private static bool IsFromGeneric(Type type, Type genericType)
        {
            return type.IsGenericType
                && (
                    (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == genericType)
                    ||
                    (!type.IsConstructedGenericType && genericType.IsAssignableFrom(type))
                );
        }

        private static JsonElement? GetJPath(JsonElement node, string? jpath)
        {
            var parts = ParseJPath(jpath);
            if (!parts.Any())
            {
                return null;
            }

            // NOTE:
            // ============================================
            // Unfortunately, System.Text.Json does not yet support Json path expressions. As a workaround for now,
            // we will use some basic implementation of that

            JsonElement? match = node;
            foreach (var part in ParseJPath(jpath))
            {
                switch (part.Type)
                {
                    default: throw new NotSupportedException("Don't know to handle " + part.Type);

                    case JPathEntryType.Property:
                        if (match?.TryGetProperty(part.Name, out var inner) == true)
                        {
                            match = inner;
                        }
                        else
                        {
                            match = null;
                        }
                        break;

                    case JPathEntryType.ArrayIndex:
                        match = match?.EnumerateArray().Cast<JsonElement?>().ElementAtOrDefault(part.Index);
                        break;
                }

                if (match == null)
                {
                    break;
                }
            }

            return match;
        }

        /// <summary>
        /// Parses the JSON path expression. Right now only property and array index are supported
        /// </summary>
        /// <param name="jPath">The JSON path to parse</param>
        /// <returns>The parts of the JSON path</returns>
        /// <exception cref="ArgumentException">The JSON path is invalid</exception>
        private static IEnumerable<JPathEntry> ParseJPath(string? jPath)
        {
            if (string.IsNullOrWhiteSpace(jPath))
            {
                yield break;
            }

            // For now we just support . and []
            int start = 0;
            bool isArray = false;
            int i;

            for (i = 0; i < jPath.Length; i++)
            {
                char c = jPath[i];
                switch (c)
                {
                    case '.':
                        if (isArray)
                        {
                            throw new ArgumentException("Unexpected property name in JPath");
                        }

                        var span = jPath.AsSpan(start, i - start);
                        if (span.IsEmpty)
                        {
                            throw new ArgumentException("Cannot have an empty property name in JPath");
                        }

                        yield return new JPathEntry { Name = new string(span), Type = JPathEntryType.Property };
                        start = i + 1;
                        break;

                    case '[':
                        if (isArray)
                        {
                            throw new ArgumentException("Cannot have nested arrays in JPath");
                        }

                        // anything before this?
                        span = jPath.AsSpan(start, i - start);
                        if (!span.IsEmpty)
                        {
                            yield return new JPathEntry { Name = new string(span), Type = JPathEntryType.Property };
                        }

                        start = i + 1;
                        isArray = true;
                        break;

                    case ']':
                        if (!isArray)
                        {
                            throw new ArgumentException("Unexpected end of array in JPath");
                        }

                        span = jPath.AsSpan(start, i - start).Trim();
                        ReadOnlySpan<char> x = span;
                        if (!int.TryParse(span, NumberStyles.None, CultureInfo.InvariantCulture, out int index))
                        {
                            throw new ArgumentException("Invalid index in array in JPath");
                        }

                        yield return new JPathEntry { Type = JPathEntryType.ArrayIndex, Index = index };
                        start = i + 1;
                        isArray = false;
                        break;

                    default:
                        break;
                }
            }

            if (i > start)
            {
                if (isArray)
                {
                    throw new ArgumentException("Unterminated array index in JPath");
                }

                yield return new JPathEntry { Type = JPathEntryType.Property, Name = jPath.Substring(start, i - start) };
            }
        }

        private delegate void Setter<T>(ref T? instance, object? value);

        private record Entry(string Name, string? Path, Type Type, Action<object?, object?> Setter);

        private enum JPathEntryType
        {
            Property,
            ArrayIndex,
        }

        private struct JPathEntry
        {
            public JPathEntry() { }

            public JPathEntryType Type;
            public string Name = string.Empty;
            public int Index = -1;
        }
    }
}
