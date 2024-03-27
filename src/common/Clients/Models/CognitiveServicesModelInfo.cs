#nullable enable

using Newtonsoft.Json;

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
        /// The name of the SKU (e.g. S0)
        /// </summary>
        public string SkuName { get; init; }

        /// <summary>
        /// Whether the model is deprecated
        /// </summary>
        [JsonIgnore]
        public bool IsDeprecated
        {
            get
            {
                var now = DateTimeOffset.Now;
                return now < Model.Deprecation.Inference
                    && (Model.Skus?.Any(sku => now < sku.DeprecationDate) ?? false);
            }
        }

        // TODO FIXME: Simplify the following using the JsonPathConverter?

        /// <summary>
        /// Whether the model is capable of chat completions
        /// </summary>
        [JsonIgnore] public bool IsChatCapable => Model.Capabilities.ChatCompletion;

        /// <summary>
        /// Whether the model is capable of embeddings
        /// </summary>
        [JsonIgnore] public bool IsEmbeddingsCapable => Model.Capabilities.Embeddings;

        /// <summary>
        /// Whether the model is capable of image generation
        /// </summary>
        [JsonIgnore] public bool IsImageCapable => Model.Capabilities.ImageGenerations;

        /// <summary>
        /// The name of the model (e.g. gpt-4)
        /// </summary>
        [JsonIgnore] public string Name => Model.Name;

        /// <summary>
        /// The model version (e.g. 1106-Preview)
        /// </summary>
        [JsonIgnore] public string Version => Model.Version;

        /// <summary>
        /// The default capacity for this model (e.g. 10)
        /// </summary>
        [JsonIgnore] public int DefaultCapacity => Model.Skus
            ?.Select(sku => sku.Capacity.Default)
            .FirstOrDefault(v => v > 0)
            ?? 0;

        /// <summary>
        /// The model format (e.g. OpenAI)
        /// </summary>
        [JsonIgnore] public string Format => Model.Format;

        /// <summary>
        /// For internal use only
        /// </summary>
        [JsonProperty]
        internal ModelInfo Model { get; init; }

        #region Inner models

        internal readonly struct ModelCapacity
        {
            public int Default { get; init; }
            public int Maximum { get; init; }
            public int Minimum { get; init; }
            public int Step { get; init; }
        }

        internal readonly struct ModelRatelimit
        {
            public float Count { get; init; }
            public string Key { get; init; }
            public float RenewalPeriod { get; init; }
        }

        internal readonly struct ModelCapabilities
        {
            public bool ImageGenerations { get; init; }
            public bool ImageVariations { get; init; }
            public bool Inference { get; init; }
            public bool ChatCompletion { get; init; }
            public bool Completion { get; init; }
            public bool Search { get; init; }
            public bool Embeddings { get; init; }
        }

        internal readonly struct ModelDeprecation
        {
            public DateTimeOffset Inference { get; init; }
        }

        internal readonly struct ModelSku
        {
            public ModelCapacity Capacity { get; init; }
            public DateTimeOffset DeprecationDate { get; init; }
            public string Name { get; init; }
            public IReadOnlyList<ModelRatelimit> RateLimits { get; init; }
            public string UsageName { get; init; }
        }

        internal readonly struct ModelInfo
        {
            public ModelCapabilities Capabilities { get; init; }
            public ModelDeprecation Deprecation { get; init; }
            public string Format { get; init; }
            public bool IsDefaultVersion { get; init; }
            public string LifecycleStatus { get; init; }
            public int MaxCapacity { get; init; }
            public string Name { get; init; }
            public IReadOnlyList<ModelSku> Skus { get; init; }
            public string Version { get; init; }
        }

        #endregion
    }
}
