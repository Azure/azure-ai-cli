#nullable enable

using Azure;

namespace Azure.AI.Details.Common.CLI.AzCli
{
    /// <summary>
    /// Information about an Azure regions
    /// </summary>
    public readonly struct AccountRegionLocationInfo
    {
        /// <summary>
        /// The identifier for this region (e.g. /subscriptions/{subscriptionId}/locations/eastus
        /// </summary>
        public string Id { get; init; }

        /// <summary>
        /// The short name for the region (e.g. eastus)
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// The display name for the region (e.g. East US)
        /// </summary>
        public string DisplayName { get; init; }

        /// <summary>
        /// The display name that includes the country (e.g. (US) East US)
        /// </summary>
        public string RegionalDisplayName { get; init; }
    }
}
