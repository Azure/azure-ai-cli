﻿using System.Diagnostics;

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

    public static class TelemetryExtensions
    {
        public delegate ITelemetryEvent CreateTelemetryEvent(
            Outcome outcome, Exception exception, TimeSpan duration);

        [DebuggerStepThrough]
        public static Outcome WrapWithTelemetry(
            this ITelemetry telemetry,
            Action doWork,
            CreateTelemetryEvent createTelemetryEvent,
            CancellationToken token)
        {
            return WrapWithTelemetry(
                telemetry,
                () =>
                {
                    doWork();
                    return Outcome.Success;
                },
                createTelemetryEvent,
                token);
        }

        [DebuggerStepThrough]
        public static Outcome WrapWithTelemetry(
            this ITelemetry telemetry,
            Func<Outcome> doWork,
            CreateTelemetryEvent createTelemetryEvent,
            CancellationToken token)
        {
            return WrapWithTelemetryAsync(
                telemetry,
                token => Task.FromResult(doWork()),
                createTelemetryEvent,
                token)
            .GetAwaiter()
            .GetResult();
        }

        [DebuggerStepThrough]
        public static Task<Outcome> WrapWithTelemetryAsync(
            this ITelemetry telemetry,
            Func<CancellationToken, Task> doWorkAsync,
            CreateTelemetryEvent createTelemetryEvent,
            CancellationToken token)
        {
            return WrapWithTelemetryAsync(
                telemetry,
                async token =>
                {
                    await doWorkAsync(token)
                        .ConfigureAwait(false);
                    return Outcome.Success;
                },
                createTelemetryEvent,
                token);
        }

        [DebuggerStepThrough]
        public static async Task<Outcome> WrapWithTelemetryAsync(
            this ITelemetry telemetry,
            Func<CancellationToken, Task<Outcome>> doWorkAsync,
            CreateTelemetryEvent createTelemetryEvent,
            CancellationToken token)
        {
            var outcome = Outcome.Unknown;
            Exception exception = null;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                outcome = await doWorkAsync(token)
                    .ConfigureAwait(false);

                return outcome;
            }
            catch (Exception ex)
            {
                exception = ex;
                outcome = ex switch
                {
                    OperationCanceledException => Outcome.Canceled,
                    TimeoutException => Outcome.TimedOut,
                    _ => Outcome.Failed
                };
                throw;
            }
            finally
            {
                stopwatch.Stop();
                ITelemetryEvent evt = createTelemetryEvent(outcome, exception, stopwatch.Elapsed);
                var _ = telemetry?.LogEventAsync(evt, token);
            }
        }
    }
}