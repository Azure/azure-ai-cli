using System.Diagnostics;

namespace Azure.AI.Details.Common.CLI.Telemetry.Events
{
    [DebuggerDisplay("{Name}")]
    public readonly struct LaunchedTelemetryEvent : ITelemetryEvent
    {
        public string Name => "launched";
    }
}
