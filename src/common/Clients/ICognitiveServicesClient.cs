#nullable enable

using Azure.AI.CLI.Common.Clients.Models;
using Azure.AI.Details.Common.CLI.AzCli;

namespace Azure.AI.CLI.Common.Clients
{
    /// <summary>
    /// Client for working with Cognitive Services resources (e.g. AI services resource, speech resource, chat/embedding/evaluation AI model deployments, etc..)
    /// </summary>
    public interface ICognitiveServicesClient : IDisposable
    {
        /// <summary>
        /// Retrieves all Cognitive Services resources
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription</param>
        /// <param name="token">Cancellation token to use</param>
        /// <param name="filter">(Optional) The kind of resource to filter to</param>
        /// <returns>Asynchronous task that gets all resources</returns>
        Task<ClientResult<CognitiveServicesResourceInfo[]>> GetAllResourcesAsync(string subscriptionId, CancellationToken token, ResourceKind? filter = null);

        /// <summary>
        /// Retrieves a specific Cognitive Services resource
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription</param>
        /// <param name="resourceGroup">The name of the resource group</param>
        /// <param name="name">The name of the resource to retrieve</param>
        /// <param name="token">Cancellation token to use</param>
        /// <returns>Asynchronous task that retrieves resource information. Will return a null <see cref="ClientResult{}.Value"/>
        /// if the resource could not be found</returns>
        Task<ClientResult<CognitiveServicesResourceInfo?>> GetResourceFromNameAsync(string subscriptionId, string resourceGroup, string name, CancellationToken token);

        /// <summary>
        /// Creates a new Cognitive Services resource
        /// </summary>
        /// <param name="kind">The kind of resource to create</param>
        /// <param name="subscriptionId">The ID of the subscription</param>
        /// <param name="resourceGroup">The name of the resource group</param>
        /// <param name="regionName">The short name of the region (e.g. westus)</param>
        /// <param name="name">The name of the resource to create</param>
        /// <param name="sku">The SKU of the resource (e.g. F0 for free resources)</param>
        /// <param name="token">Cancellation token to use</param>
        /// <returns>Asynchronous task that creates a new resource</returns>
        Task<ClientResult<CognitiveServicesResourceInfo>> CreateResourceAsync(ResourceKind kind, string subscriptionId, string resourceGroup, string regionName, string name, string sku, CancellationToken token);

        /// <summary>
        /// Deletes a specific Cognitive Services resource
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription</param>
        /// <param name="resourceGroup">The name of the resource group</param>
        /// <param name="resourceName">The name of the resource to delete</param>
        /// <param name="token">Cancellation token to use</param>
        /// <returns>Asynchronous task that deletes the resource</returns>
        Task<ClientResult> DeleteResourceAsync(string subscriptionId, string resourceGroup, string resourceName, CancellationToken token);

        /// <summary>
        /// Retrieves all Cognitive Services model deployments
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription</param>
        /// <param name="resourceGroup">The name of the resource group</param>
        /// <param name="resourceName">The name of the Cognitive Services resource</param>
        /// <param name="token">Cancellation token to use</param>
        /// <returns>Asynchronous task that </returns>
        Task<ClientResult<CognitiveServicesDeploymentInfo[]>> GetAllDeploymentsAsync(string subscriptionId, string resourceGroup, string resourceName, CancellationToken token);

        /// <summary>
        /// Retrieves all available Cognitive Services models
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription</param>
        /// <param name="regionName">The short name of the region (e.g. westus)</param>
        /// <param name="token">Cancellation token to use</param>
        /// <returns>Asynchronous task that gets all models</returns>
        Task<ClientResult<CognitiveServicesModelInfo[]>> GetAllModelsAsync(string subscriptionId, string regionName, CancellationToken token);

        /// <summary>
        /// Retrieves the usage of all available Cognitive Services models
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription</param>
        /// <param name="regionName">The short name of the region (e.g. westus)</param>
        /// <param name="token">Cancellation token to use</param>
        /// <returns>Asynchronous task that gets all model usage</returns>
        Task<ClientResult<CognitiveServicesUsageInfo[]>> GetAllModelUsageAsync(string subscriptionId, string regionName, CancellationToken token);

        /// <summary>
        /// Creates a new Cognitive Services model deployment
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier</param>
        /// <param name="resourceGroup">The name of the resource group this deployment belongs to</param>
        /// <param name="resourceName">The name of the resource this deployment is for</param>
        /// <param name="deploymentName">The name to use for the new deployment</param>
        /// <param name="modelName">The model name (e.g. gpt-35-turbo)</param>
        /// <param name="modelVersion">The model version (e.g. 0613)</param>
        /// <param name="modelFormat">The model format (e.g. OpenAI)</param>
        /// <param name="scaleCapacity">//FIXME TODO ????</param>
        /// <param name="token">Cancellation token to use</param>
        /// <returns>Asynchronous task that creates a new model deployment</returns>
        Task<ClientResult<CognitiveServicesDeploymentInfo>> CreateDeploymentAsync(
            string subscriptionId,
            string resourceGroup,
            string resourceName,
            string deploymentName,
            string modelName,
            string modelVersion,
            string modelFormat,
            string scaleCapacity,
            CancellationToken token);

        /// <summary>
        /// Deletes a specific Cognitive Services model deployment
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription</param>
        /// <param name="resourceGroup">The name of the resource group</param>
        /// <param name="resourceName">The name of the Cognitive Services resource</param>
        /// <param name="deploymentName">The name of the deployment to delete</param>
        /// <param name="token">Cancellation token to use</param>
        /// <returns>Asynchronous task that deletes the deployment</returns>
        Task<ClientResult> DeleteDeploymentAsync(
            string subscriptionId,
            string resourceGroup,
            string resourceName,
            string deploymentName,
            CancellationToken token);

        /// <summary>
        /// Retrieves the keys to use for a specific Cognitive Services resource
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription</param>
        /// <param name="resourceGroup">The name of the resource group</param>
        /// <param name="resourceName">The name of the Cognitive Services resource</param>
        /// <param name="token">Cancellation token to use</param>
        /// <returns>Asynchronous task that gets the resource keys</returns>
        Task<ClientResult<(string, string?)>> GetResourceKeysFromNameAsync(string subscriptionId, string resourceGroup, string resourceName, CancellationToken token);
    }
}
