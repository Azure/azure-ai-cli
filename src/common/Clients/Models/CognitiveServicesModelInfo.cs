#nullable enable

using Newtonsoft.Json;

namespace Azure.AI.Details.Common.CLI.AzCli
{
    public readonly struct CognitiveServicesModelInfo
    {
        public string Kind { get; init; }

        public string SkuName { get; init; }

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

        // TODO FIXME: Simplify these in some way?
        [JsonIgnore] public bool IsChatCapable => Model.Capabilities.ChatCompletion;
        [JsonIgnore] public bool IsEmbeddingsCapable => Model.Capabilities.Embeddings;
        [JsonIgnore] public bool IsImageCapable => Model.Capabilities.ImageGenerations;
        [JsonIgnore] public string Name => Model.Name;
        [JsonIgnore] public string Version => Model.Version;
        [JsonIgnore] public int DefaultCapacity => Model.Skus
            ?.Select(sku => sku.Capacity.Default)
            .FirstOrDefault(v => v > 0)
            ?? 0;
        [JsonIgnore] public string Format => Model.Format;

        [JsonProperty]
        internal ModelInfo Model { get; init; }

        #region Inner models

        public readonly struct ModelCapacity
        {
            public int Default { get; init; }
            public int Maximum { get; init; }
            public int Minimum { get; init; }
            public int Step { get; init; }
        }

        public readonly struct ModelRatelimit
        {
            public float Count { get; init; }
            public string Key { get; init; }
            public float RenewalPeriod { get; init; }
        }

        public readonly struct ModelCapabilities
        {
            public bool ImageGenerations { get; init; }
            public bool ImageVariations { get; init; }
            public bool Inference { get; init; }
            public bool ChatCompletion { get; init; }
            public bool Completion { get; init; }
            public bool Search { get; init; }
            public bool Embeddings { get; init; }
        }

        public readonly struct ModelDeprecation
        {
            public DateTimeOffset Inference { get; init; }
        }

        public readonly struct ModelSku
        {
            public ModelCapacity Capacity { get; init; }
            public DateTimeOffset DeprecationDate { get; init; }
            public string Name { get; init; }
            public IReadOnlyList<ModelRatelimit> RateLimits { get; init; }
            public string UsageName { get; init; }
        }

        public readonly struct ModelInfo
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
