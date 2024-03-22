#nullable enable

using Azure.AI.Details.Common.CLI.AzCli;

namespace Azure.AI.CLI.Common.Clients.Models.Utils
{
    /// <summary>
    /// Helper methods to work with <see cref="ResourceKind"/>
    /// </summary>
    public static class ResourceKindHelpers
    {
        private static readonly IReadOnlyDictionary<string, ResourceKind> STRING_TO_KIND =
            new Dictionary<string, ResourceKind>(StringComparer.OrdinalIgnoreCase)
            {
                { "SpeechServices", ResourceKind.Speech },
                { "ComputerVision", ResourceKind.Vision }
            };

        private static readonly IReadOnlyDictionary<ResourceKind, string> KIND_TO_STRING =
            STRING_TO_KIND.ToDictionary(e => e.Value, e => e.Key);

        /// <summary>
        /// Parses a string value of a resource kind from an Azure response
        /// </summary>
        /// <param name="strValue">The string value to parse</param>
        /// <returns>The equivalent <see cref="ResourceKind"/>, or <see cref="ResourceKind.Unknown"/> if there is no mapping currently</returns>
        public static ResourceKind? ParseString(string? strValue)
        {
            if (strValue == null)
            {
                return null;
            }

            ResourceKind kind;
            if (STRING_TO_KIND.TryGetValue(strValue, out kind))
            {
                return kind;
            }

            if (Enum.TryParse(strValue, true, out kind))
            {
                return kind;
            }

            return null;
        }

        /// <summary>
        /// Parses the first value from a string separated by the specified chars
        /// </summary>
        /// <param name="strValue">The string value (e.g. "OpenAI; AIServices")</param>
        /// <param name="separator">The separators (e.g. ';')</param>
        /// <returns>The value of the first parsed <see cref="ResourceKind"/></returns>
        public static ResourceKind? ParseString(string? strValue, params char[] separator)
        {
            return strValue
                ?.Split(separator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(ParseString)
                .FirstOrDefault(k => k != null);
        }

        /// <summary>
        /// Gets the equivalent JSON string value for the resource kind for sending to Azure
        /// </summary>
        /// <param name="kind">The <see cref="ResourceKind"/> to convert to a string</param>
        /// <returns>The equivalent string. If unknown, an empty string will be returned</returns>
        public static string AsJsonString(this ResourceKind kind)
        {
            if (KIND_TO_STRING.TryGetValue(kind, out string? strValue))
            {
                return strValue;
            }
            else
            {
                return kind switch
                {
                    ResourceKind.Unknown => string.Empty,
                    _ => kind.ToString()
                };
            }
        }
    }
}
