#nullable enable

using Azure.AI.CLI.Common.Clients.Models.Utils;
using Newtonsoft.Json;

namespace Azure.AI.Details.Common.CLI.AzCli
{
    /// <summary>
    /// Information about an AI model deployment
    /// </summary>
    [JsonConverter(typeof(JPathJsonConverter))]
    public readonly struct CognitiveServicesDeploymentInfo
    {
        /// <summary>
        /// The unique identifier for this deployment (e.g. /subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.CognitiveServices/accounts/{resourceName}/deployments/{deploymentName})
        /// </summary>
        public string Id { get; init; }

        /// <summary>
        /// The name of the deployment
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// The name of the resource group this deployment is in
        /// </summary>
        public string ResourceGroup { get; init; }

        /// <summary>
        /// The name of the underlying model for this deployment (e.g. gpt-4)
        /// </summary>
        [JsonProperty("properties.model.name")]
        public string ModelName { get; init; }

        /// <summary>
        /// The format of the underlying model for this deployment (e.g. OpenAI)
        /// </summary>
        [JsonProperty("properties.model.format")]
        public string ModelFormat { get; init; }

        /// <summary>
        /// Whether this deployment is capable of chat completion
        /// </summary>
        [JsonProperty("properties.capabilities.chatCompletion")]
        public bool ChatCompletionCapable { get; init; }

        /// <summary>
        /// Whether this deployment is capable of embeddings
        /// </summary>
        [JsonProperty("properties.capabilities.embeddings")]
        public bool EmbeddingsCapable { get; init; }
    }
}
