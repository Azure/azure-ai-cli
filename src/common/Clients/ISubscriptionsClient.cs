#nullable enable

using Azure.AI.CLI.Common.Clients.Models;
using Azure.AI.Details.Common.CLI.AzCli;

namespace Azure.AI.CLI.Common.Clients
{
    /// <summary>
    /// A client to retrieve (and optionally create) Azure subscriptions, regions, or resource groups
    /// </summary>
    public interface ISubscriptionsClient : IDisposable
    {
        /// <summary>
        /// Gets all subscriptions for the current tenant
        /// </summary>
        /// <param name="token">The cancellation token to use</param>
        /// <returns>Asynchronous task that gets all subscriptions</returns>
        Task<ClientResult<SubscriptionInfo[]>> GetAllSubscriptionsAsync(CancellationToken token);

        /// <summary>
        /// Gets details about a particular subscription
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription to retrieve</param>
        /// <param name="token">The cancellation token to use</param>
        /// <returns>Asynchronous task that retrieves the subscription information. Will return a null <see cref="ClientResult{}.Value"/>
        /// if the subscription does not exist</returns>
        Task<ClientResult<SubscriptionInfo?>> GetSubscriptionAsync(string subscriptionId, CancellationToken token);

        /// <summary>
        /// Gets all available Azure regions for the specified subscription
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription</param>
        /// <param name="token">The cancellation token to use</param>
        /// <returns>Asynchronous task that returns all regions. Will throw exceptions on failure</returns>
        Task<ClientResult<AccountRegionLocationInfo[]>> GetAllRegionsAsync(string subscriptionId, CancellationToken token);

        /// <summary>
        /// Gets all resource groups for the specified subscription
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription</param>
        /// <param name="token">The cancellation token to use</param>
        /// <returns>Asynchronous task that returns all resource groups. Will throw exceptions on errors</returns>
        Task<ClientResult<ResourceGroupInfo[]>> GetAllResourceGroupsAsync(string subscriptionId, CancellationToken token);

        /// <summary>
        /// Creates a new resource group
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription</param>
        /// <param name="regionName">The short region name to create the resource group in (e.g. westcentralus)</param>
        /// <param name="name">The name of the resource group</param>
        /// <param name="token">The cancellation token to use</param>
        /// <returns>Asynchronous task that returns the details of the created resource group. Will throw exceptions on errors (e.g. failed
        /// to create resource group)</returns>
        Task<ClientResult<ResourceGroupInfo>> CreateResourceGroupAsync(string subscriptionId, string regionName, string name, CancellationToken token);

        /// <summary>
        /// Deletes a resource group
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription</param>
        /// <param name="resourceGroup">The name of the resource group to delete</param>
        /// <param name="token">The cancellation token to use</param>
        /// <returns>Asynchronous task that deletes the resource group. If this resource group doesn't exist, it will return
        /// an error outcome</returns>
        Task<ClientResult> DeleteResourceGroupAsync(string subscriptionId, string resourceGroup, CancellationToken token);
    }
}
