#nullable enable

using System.Text.Json.Serialization;
using Azure.AI.CLI.Common.Clients.Models.Utils;

namespace Azure.AI.Details.Common.CLI.AzCli
{
    /// <summary>
    /// Represents a Cognitive Services model
    /// </summary>
    public readonly struct CognitiveServicesModelInfo
    {
        /// <summary>
        /// The kind of the model (e.g. OpenAI, or AIServices)
        /// </summary>
        public string Kind { get; init; }

        /// <summary>
        /// The name of the model (e.g. gpt-4)
        /// </summary>
        [JsonPathPropertyName("model.name")]
        public string Name { get; init; }

        /// <summary>
        /// The model version (e.g. 1106-Preview)
        /// </summary>
        [JsonPathPropertyName("model.version")]
        public string Version { get; init; }

        /// <summary>
        /// The model format (e.g. OpenAI)
        /// </summary>
        [JsonPathPropertyName("model.format")]
        public string Format { get; init; }

        /// <summary>
        /// The name of the SKU (e.g. S0)
        /// </summary>
        public string SkuName { get; init; }

        /// <summary>
        /// Whether the model is capable of chat completions
        /// </summary>
        [JsonPathPropertyName("model.capabilities.chatCompletion")]
        public bool IsChatCapable { get; init; }

        /// <summary>
        /// Whether the model is capable of embeddings
        /// </summary>
        [JsonPathPropertyName("model.capabilities.embeddings")] 
        public bool IsEmbeddingsCapable { get; init; }

        /// <summary>
        /// Whether the model is capable of image generation
        /// </summary>
        [JsonPathPropertyName("model.capabilities.imageGenerations")] 
        public bool IsImageCapable { get; init; }

        /// <summary>
        /// The default capacity for this model (e.g. 10)
        /// </summary>
        [JsonIgnore]
        public int DefaultCapacity => ModelSkus
            ?.Select(sku => sku.DefaultCapacity)
            .FirstOrDefault(v => v > 0)
            ?? 0;

        /// <summary>
        /// Whether the model is deprecated
        /// </summary>
        [JsonIgnore]
        public bool IsDeprecated
        {
            get
            {
                var now = DateTimeOffset.Now;
                return now < InferenceDeprecation
                    && (ModelSkus?.Any(sku => now < sku.DeprecationDate) == true);
            }
        }

        #region internal properties and models

        [JsonInclude]
        [JsonPathPropertyName("model.deprecation.inference")]
        internal DateTimeOffset? InferenceDeprecation { get; init; }

        [JsonInclude]
        [JsonPathPropertyName("model.skus")]
        internal IReadOnlyList<ModelSku>? ModelSkus { get; init; }

        internal readonly struct ModelSku
        {
            [JsonPathPropertyName("capacity.default")]
            public int? DefaultCapacity { get; init; }
            public DateTimeOffset? DeprecationDate { get; init; }
        }

        #endregion
    }
}
