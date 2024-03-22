#nullable enable

using Newtonsoft.Json;

namespace Azure.AI.Details.Common.CLI.AzCli
{
    /// <summary>
    /// Information about an Azure resource group
    /// </summary>
    public readonly struct ResourceGroupInfo
    {
        /// <summary>
        /// The identifier for this resource group (e.g. /subscriptions/{subscriptionId}/resourceGroups/{name})
        /// </summary>
        public string Id { get; init; }

        /// <summary>
        /// The name of this resource group
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// The short name of the region this resource group is in (e.g. westcentralus)
        /// </summary>
        [JsonProperty("location")]
        public string Region { get; init; }
    }
}
