#nullable enable

using System.Text.Json.Serialization;

namespace Azure.AI.CLI.Common.Clients.Models.Utils
{
    /// <summary>
    /// Attribute that can be used to create a <see cref="JsonConverter"> instance with a constructor that takes some arguments
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface, AllowMultiple = false)]
    public class JsonConverterArgsAttribute : JsonConverterAttribute
    {
        private readonly object?[]? _args;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="converterType">The type of the converter to create</param>
        /// <param name="args">The arguments to pass to the converter constructor</param>
        public JsonConverterArgsAttribute(Type converterType, params object?[]? args) : base(converterType)
        {
            _args = args;
        }

        /// <inheritdoc />
        public override JsonConverter? CreateConverter(Type typeToConvert)
        {
            if (ConverterType != null)
            {
                var instance = (JsonConverter?)Activator.CreateInstance(ConverterType, _args);
                return instance;
            }

            return null;
        }
    }
}
