#nullable enable

using Azure.AI.CLI.Common.Clients.Models.Utils;
using Newtonsoft.Json;

namespace Azure.AI.Details.Common.CLI.AzCli
{
    /// <summary>
    /// Base class for information about Azure resource
    /// </summary>
    [JsonConverter(typeof(JPathJsonConverter))]
    public abstract class ResourceInfoBase
    {
        private string? _key;

        /// <summary>
        /// The kind of resource
        /// </summary>
        [JsonConverter(typeof(ResourceKindJsonConverter))]
        public virtual ResourceKind Kind { get; init; }

        /// <summary>
        /// The unique identifier for this resource (e.g. /subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.CognitiveServices/accounts/{name})
        /// </summary>
        /// <remarks>The providers part of the ID will be different depending on the type of resource</remarks>
        public string Id { get; init; } = string.Empty;

        /// <summary>
        /// The name of this resource
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// The name of the resource group this subscription belongs to
        /// </summary>
        // TODO FIXME: rename to ResourceGroup
        [JsonProperty("resourceGroup")]
        public string Group { get; init; } = string.Empty;

        /// <summary>
        /// The short name of the region this resource is in
        /// </summary>
        [JsonProperty("location")]
        // TODO FIXME: rename to Region
        public string RegionLocation { get; init; } = string.Empty;

        [JsonIgnore]
        public string Key
        {
            get => _key ?? string.Empty;
            set => _key = value;
        }
    }
}
