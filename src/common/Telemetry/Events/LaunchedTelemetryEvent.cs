namespace Azure.AI.Details.Common.CLI.Telemetry.Events
{
    public readonly struct LaunchedTelemetryEvent : ITelemetryEvent
    {
        public string Name => "launched";
    }
}
