#nullable enable

namespace Azure.AI.Details.Common.CLI.AzCli
{
    /// <summary>
    /// Information about a Cognitive Services model usage
    /// </summary>
    public readonly struct CognitiveServicesUsageInfo
    {
        /// <summary>
        /// The current value of the usage (please refer to <see cref="Unit"/> to see the unit for this)
        /// </summary>
        public float CurrentValue { get; init; }

        /// <summary>
        /// The maximum allowed usage (please refer to <see cref="Unit"/> to see the unit for this)
        /// </summary>
        public float Limit { get; init; }

        /// <summary>
        /// The name of this usage
        /// </summary>
        public UsageName Name { get; init; }

        /// <summary>
        /// The unit the <see cref="CurrentValue"/>, and <see cref="Limit"/> values are in (e.g. Count)
        /// </summary>
        public string Unit { get; init; }

        /// <summary>
        /// Information about a usage name
        /// </summary>
        public readonly struct UsageName
        {
            /// <summary>
            /// A human readable localized name (e.g. Tokens Per Minute (thousands) - GPT-4)
            /// </summary>
            public string LocalizedValue { get; init; }

            /// <summary>
            /// The name of this usage (e.g. OpenAI.Standard.gpt-4)
            /// </summary>
            public string Value { get; init; }
        }
    }
}
