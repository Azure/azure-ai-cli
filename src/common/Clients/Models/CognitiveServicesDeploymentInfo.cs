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
        public string Id { get; init; }

        public string Name { get; init; }

        public string ResourceGroup { get; init; }

        [JsonProperty("properties.model.name")]
        public string ModelName { get; init; }

        [JsonProperty("properties.model.format")]
        public string ModelFormat { get; init; }

        [JsonProperty("properties.capabilities.chatCompletion")]
        public bool ChatCompletionCapable { get; init; }

        [JsonProperty("properties.capabilities.embeddings")]
        public bool EmbeddingsCapable { get; init; }
    }
}
