namespace Azure.AI.Details.Common.CLI.Telemetry
{
    public class NoOpTelemetry : ITelemetry
    {
        private NoOpTelemetry() { }

        public static ITelemetry Instance { get; } = new NoOpTelemetry();

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public void LogEvent(ITelemetryEvent evt)
        {
            // Nothing to see here, move along...
        }
    }
}
