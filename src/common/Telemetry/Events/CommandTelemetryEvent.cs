#nullable enable

namespace Azure.AI.Details.Common.CLI.Telemetry.Events
{
    public readonly struct CommandTelemetryEvent : ITelemetryEvent
    {
        public string Name => "Command";

        public string Type { get; init; }

        public Outcome Outcome { get; init; }

        public string? ErrorInfo { get; init; }
    }
}
