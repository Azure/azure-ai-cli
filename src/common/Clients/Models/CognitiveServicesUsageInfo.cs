#nullable enable

namespace Azure.AI.Details.Common.CLI.AzCli
{
    public readonly struct CognitiveServicesUsageInfo
    {
        public float CurrentValue { get; init; }
        public float Limit { get; init; }
        public UsageName Name { get; init; }
        public string Unit { get; init; }

        public readonly struct UsageName
        {
            public string LocalizedValue { get; init; }
            public string Value { get; init; }
        }
    }
}
