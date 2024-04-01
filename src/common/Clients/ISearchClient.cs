#nullable enable

using Azure.AI.CLI.Common.Clients.Models;
using Azure.AI.Details.Common.CLI.AzCli;

namespace Azure.AI.CLI.Common.Clients
{
    /// <summary>
    /// A client for Cognitive search resources
    /// </summary>
    public interface ISearchClient : IDisposable
    {
        /// <summary>
        /// Retrieves all search resources, optionally filtering to a specific region
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription</param>
        /// <param name="regionName">(Optional) The short name of the region to filter to. Set to null if unneeded</param>
        /// <param name="token">Cancellation token to use</param>
        /// <returns>Asynchronous task that gets all search resources</returns>
        Task<ClientResult<CognitiveSearchResourceInfo[]>> GetAllAsync(string subscriptionId, string? regionName, CancellationToken token);

        /// <summary>
        /// Retrieves a specific search resource
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription</param>
        /// <param name="resourceGroup">The name of the resource group</param>
        /// <param name="name">The name of the resource to retrieve</param>
        /// <param name="token">Cancellation token to use</param>
        /// <returns>Asynchronous task that retrieves resource information. Will return a null <see cref="ClientResult{}.Value"/>
        /// if the resource could not be found</returns>
        Task<ClientResult<CognitiveSearchResourceInfo?>> GetFromNameAsync(string subscriptionId, string resourceGroup, string name, CancellationToken token);

        /// <summary>
        /// Creates a new search resource
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription</param>
        /// <param name="resourceGroup">The name of the resource group</param>
        /// <param name="regionName">The short name of the region (e.g. westus)</param>
        /// <param name="name">The name of the resource to create</param>
        /// <param name="token">Cancellation token to use</param>
        /// <returns>Asynchronous task that creates a new resource</returns>
        Task<ClientResult<CognitiveSearchResourceInfo>> CreateAsync(string subscriptionId, string resourceGroup, string regionName, string name, CancellationToken token);

        /// <summary>
        /// Deletes a search resource
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription</param>
        /// <param name="resourceGroup">The name of the resource group</param>
        /// <param name="resourceName">The name of the resource to delete</param>
        /// <param name="token">Cancellation token to use</param>
        /// <returns>Asynchronous task that deletes the resource</returns>
        Task<ClientResult> DeleteAsync(string subscriptionId, string resourceGroup, string resourceName, CancellationToken token);

        /// <summary>
        /// Retrieves the keys to use for accessing a search resource
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription</param>
        /// <param name="resourceGroup">The name of the resource group</param>
        /// <param name="name">The name of the resource to retrieve keys for</param>
        /// <param name="token">Cancellation token to use</param>
        /// <returns>Asynchronous task that retrieves the keys</returns>
        Task<ClientResult<(string, string)>> GetKeysAsync(string subscriptionId, string resourceGroup, string name, CancellationToken token);
    }
}
