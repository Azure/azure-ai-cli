#nullable enable

using Azure.AI.CLI.Common.Clients.Models.Utils;

namespace Azure.AI.Details.Common.CLI.AzCli
{
    /// <summary>
    /// Information about an Azure subscription
    /// </summary>
    public readonly struct SubscriptionInfo
    {
        /// <summary>
        /// The unique identifier for this subscription (usually a GUID like string)
        /// </summary>
        public string Id { get; init; }

        /// <summary>
        /// The name of this subscription (e.g. Contoso subscription)
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// The user name of the currently logged in user (e.g. john.doe@contoso.com)
        /// </summary>
        [JsonPathPropertyName("user.name")]
        public string UserName { get; init; }

        /// <summary>
        /// True if this is the default subscription
        /// </summary>
        public bool IsDefault { get; init; }
    }
}
