#nullable enable

using Azure.AI.CLI.Common.Clients.Models.Utils;

namespace Azure.AI.Details.Common.CLI.AzCli
{
    /// <summary>
    /// Information about a Cognitive services resource
    /// </summary>
    public class CognitiveServicesResourceInfo : ResourceInfoBase
    {
        private static readonly IReadOnlyDictionary<string, string> EMPTY = new Dictionary<string, string>();

        /// <summary>
        /// (Optional) The URI of the endpoint for this resource (e.g. https://{name}.cognitiveservices.azure.com/)
        /// </summary>
        [JsonPathPropertyName("properties.endpoint")]
        public string? Endpoint { get; init; }

        /// <summary>
        /// Any additional endpoints associated with this resource. Will be empty if there are no additional
        /// endpoints
        /// </summary>
        [JsonPathPropertyName("properties.endpoints")]
        public IReadOnlyDictionary<string, string> Endpoints { get; init; } = EMPTY;
    }
}
