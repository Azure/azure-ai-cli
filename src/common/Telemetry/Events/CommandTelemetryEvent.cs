#nullable enable

using System.Diagnostics;

namespace Azure.AI.Details.Common.CLI.Telemetry.Events
{
    [DebuggerDisplay("{Name}: {Outcome} {Type}")]
    public readonly struct CommandTelemetryEvent : ITelemetryEvent
    {
        public string Name => "Command";

        public string Type { get; init; }

        public Outcome Outcome { get; init; }

        public string? ErrorInfo { get; init; }
    }
}
