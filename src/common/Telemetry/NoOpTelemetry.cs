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

        public ValueTask InitAsync(CancellationToken token)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask LogEventAsync(ITelemetryEvent evt, CancellationToken token)
        {
            return ValueTask.CompletedTask;
        }
    }
}
