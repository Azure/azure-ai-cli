#nullable enable

using System.Reflection;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Azure.AI.CLI.Common.Clients.Models.Utils
{
    /// <summary>
    /// Setting this in a <see cref="JsonConverterAttribute"/> on a class or struct allows you to use JPath expressions for
    /// your <see cref="JsonPropertyAttribute.PropertyName"/> values. This is useful for extracting values from nested values
    /// </summary>
    public class JPathJsonConverter : JsonConverter
    {
        private delegate void Setter(object? instance, object? value);
        private static readonly BindingFlags ALL_INSTANCE_MEMBERS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var json = JToken.Load(reader);
            serializer ??= JsonSerializer.CreateDefault();

            var nonNullableType = IsNullable(objectType)
                ? objectType.GenericTypeArguments[0]
                : objectType;

            object created = existingValue
                ?? serializer.ContractResolver?.ResolveContract(objectType)?.DefaultCreator?.Invoke()
                ?? Activator.CreateInstance(nonNullableType)!;

            using (var subReader = json.CreateReader())
            {
                serializer.Populate(subReader, created);
            }

            var jsonThings = nonNullableType.GetProperties(ALL_INSTANCE_MEMBERS)
                .Where(p => p.CanRead
                    && p.GetCustomAttribute<JsonPropertyAttribute>() != null
                    && IsProbableJPath(p))
                .Select(p => new
                {
                    Name = p.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName,
                    Type = p.PropertyType,
                    Setter = (Setter)((object? i, object? v) => p.SetValue(i, v)),
                })
                .Concat(nonNullableType.GetFields(ALL_INSTANCE_MEMBERS)
                    .Where(f => f.GetCustomAttribute<JsonPropertyAttribute>() != null
                        && IsProbableJPath(f))
                    .Select(f => new
                    {
                        Name = f.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName,
                        Type = f.FieldType,
                        Setter = (Setter)((object? i, object? v) => f.SetValue(i, v)),
                    }))
                .ToArray();

            foreach (var thing in jsonThings)
            {
                string? jpathLike = thing.Name;
                if (string.IsNullOrWhiteSpace(jpathLike))
                {
                    continue;
                }

                JToken? token = json.SelectToken(jpathLike);
                if (token == null || token.Type == JTokenType.Null)
                {
                    continue;
                }

                using var tokenReader = token.CreateReader();
                object? val = token.ToObject(thing.Type);
                if (val != null)
                {
                    thing.Setter(created, val);
                }
            }

            return created;
        }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            // NOTE: this method is not called when JsonConverter attribute is used
            return true;
        }

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }


        private static bool IsNullable(Type t)
            => t.IsGenericType && t.IsConstructedGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);

        private static bool IsProbableJPath<T>(T member) where T : MemberInfo
        {
            string name = member.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? member.Name;
            return name.AsSpan().ContainsAny("$.,[]()*:@?=<>!&|");
        }
    }
}
