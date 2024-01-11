namespace Azure.AI.Details.Common.CLI.Telemetry
{
    public interface ITelemetryEvent
    {
        string Name { get; }
    }

    public interface ITelemetry : IAsyncDisposable
    {
        ValueTask LogEventAsync(ITelemetryEvent evt, CancellationToken token = default);
    }
}
