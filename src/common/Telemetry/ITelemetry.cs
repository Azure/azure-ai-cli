using System.Diagnostics;

namespace Azure.AI.Details.Common.CLI.Telemetry
{
    /// <summary>
    /// Bases interface for telemetry events
    /// </summary>
    public interface ITelemetryEvent
    {
        /// <summary>
        /// The name of the telemetry event (e.g. init.stage)
        /// </summary>
        string Name { get; }
    }

    /// <summary>
    /// The base interface for telemetry implementations
    /// </summary>
    public interface ITelemetry : IAsyncDisposable
    {
        /// <summary>
        /// Logs a telemetry event
        /// </summary>
        /// <param name="evt">The event to log</param>
        void LogEvent(ITelemetryEvent evt);
    }

    /// <summary>
    /// Helper methods to make working with telemetry easier
    /// </summary>
    [DebuggerStepThrough]
    public static class TelemetryExtensions
    {
        #region delegates

        /// <summary>
        /// Creates a telemetry event
        /// </summary>
        /// <param name="outcome">The outcome of the function or work</param>
        /// <param name="exception">Any caught exceptions. This can be null</param>
        /// <param name="duration">How long the function or work took</param>
        /// <returns>The telemetry event to raise, or null to not raise an event</returns>
        public delegate ITelemetryEvent CreateTelemetryEvent(
            Outcome outcome, Exception exception, TimeSpan duration);

        /// <summary>
        /// Creates a telemetry event
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="outcome">The outcome of the function or work</param>
        /// <param name="result">The result of the function or work</param>
        /// <param name="exception">Any caught exceptions. This can be null</param>
        /// <param name="duration">How long the function or work took</param>
        /// <returns>The telemetry event to raise, or null to not raise an event</returns>
        public delegate ITelemetryEvent CreateTelemetryEvent<TResult>(
            Outcome outcome, TResult result, Exception exception, TimeSpan duration);

        #endregion

        #region Async wrappers

        /// <summary>
        /// Wraps an asynchronous task with telemetry
        /// </summary>
        /// <param name="telemetry">The telemetry instance to use</param>
        /// <param name="doWorkAsync">The async function to call</param>
        /// <param name="createTelemetryEvent">The method to call to create the telemetry event</param>
        /// <returns>Asynchronous task</returns>
        public static Task WrapAsync(
            this ITelemetry telemetry,
            Func<Task> doWorkAsync,
            CreateTelemetryEvent createTelemetryEvent)
        {
            return WrapResultAsync(
                telemetry,
                async () =>
                {
                    await doWorkAsync().ConfigureAwait(false);
                    return (Outcome.Success, true);
                },
                Wrap<bool>(createTelemetryEvent));
        }

        /// <summary>
        /// Wraps an asynchronous task with telemetry. The task can return outcome
        /// </summary>
        /// <param name="telemetry">The telemetry instance to use</param>
        /// <param name="doWorkAsync">The async function to call</param>
        /// <param name="createTelemetryEvent">The method to call to create the telemetry event</param>
        /// <returns>Asynchronous task</returns>
        public static async Task<Outcome> WrapAsync(
            this ITelemetry telemetry,
            Func<Task<Outcome>> doWorkAsync,
            CreateTelemetryEvent createTelemetryEvent)
        {
            var (outcome, _) = await WrapResultAsync(
                telemetry,
                async () =>
                {
                    var outcome = await doWorkAsync()
                        .ConfigureAwait(false);
                    return (outcome, true);
                },
                Wrap<bool>(createTelemetryEvent))
            .ConfigureAwait(false);

            return outcome;
        }

        /// <summary>
        /// Wraps an asynchronous task with telemetry. The task can return a result
        /// </summary>
        /// <typeparam name="TResult">The type of the result</typeparam>
        /// <param name="telemetry">The telemetry instance to use</param>
        /// <param name="doWorkAsync">The async function to call</param>
        /// <param name="createTelemetryEvent">The method to call to create the telemetry event</param>
        /// <returns>Asynchronous task</returns>
        public static async Task<TResult> WrapAsync<TResult>(
            this ITelemetry telemetry,
            Func<Task<TResult>> doWorkAsync,
            CreateTelemetryEvent<TResult> createTelemetryEvent)
        {
            var (_, result) = await WrapResultAsync(
                telemetry,
                async () =>
                {
                    var result = await doWorkAsync().ConfigureAwait(false);
                    return (Outcome.Success, result);
                },
                createTelemetryEvent)
            .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Wraps an asynchronous task with telemetry. The task can return an outcome, and a result
        /// </summary>
        /// <typeparam name="TResult">The type of the result</typeparam>
        /// <param name="telemetry">The telemetry instance to use</param>
        /// <param name="doWorkAsync">The async function to call</param>
        /// <param name="createTelemetryEvent">The method to call to create the telemetry event</param>
        /// <returns>Asynchronous task</returns>
        public static async Task<(Outcome, TResult)> WrapResultAsync<TResult>(
            this ITelemetry telemetry,
            Func<Task<(Outcome, TResult)>> doWorkAsync,
            CreateTelemetryEvent<TResult> createTelemetryEvent)
        {
            var outcome = Outcome.Unknown;
            TResult res = default;
            Exception exception = null;

            var stopwatch = Stopwatch.StartNew();
            try
            {
                (outcome, res) = await doWorkAsync().ConfigureAwait(false);
                return (outcome, res);
            }
            catch (Exception ex)
            {
                exception = FlattenException(ex);
                outcome = ToOutcome(ex);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                SendEvent(telemetry, () => createTelemetryEvent(outcome, res, exception, stopwatch.Elapsed));
            }
        }

        #endregion

        #region Non-async wrappers

        /// <summary>
        /// Wraps some work with telemetry
        /// </summary>
        /// <param name="telemetry">The telemetry instance to use</param>
        /// <param name="doWork">The work to do</param>
        /// <param name="createTelemetryEvent">The method to call to create the telemetry event</param>
        public static void Wrap(
            this ITelemetry telemetry,
            Action doWork,
            CreateTelemetryEvent createTelemetryEvent)
        {
            var (outcome, _) = WrapResult<bool>(
                telemetry,
                () =>
                {
                    doWork();
                    return (Outcome.Success, true);
                },
                Wrap<bool>(createTelemetryEvent));
        }

        /// <summary>
        /// Wraps some work with telemetry
        /// </summary>
        /// <param name="telemetry">The telemetry instance to use</param>
        /// <param name="doWork">The work to do</param>
        /// <param name="createTelemetryEvent">The method to call to create the telemetry event</param>
        /// <returns>The returned outcome from the work</returns>
        public static Outcome Wrap(
            this ITelemetry telemetry,
            Func<Outcome> doWork,
            CreateTelemetryEvent createTelemetryEvent)
        {
            var (outcome, _) = WrapResult<bool>(
                telemetry,
                () =>
                {
                    var outcome = doWork();
                    return (outcome, true);
                },
                Wrap<bool>(createTelemetryEvent));

            return outcome;
        }

        /// <summary>
        /// Wraps some work with telemetry
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="telemetry">The telemetry instance to use</param>
        /// <param name="doWork">The work to do</param>
        /// <param name="createTelemetryEvent">The method to call to create the telemetry event</param>
        /// <returns>The returned result from the work</returns>
        public static TResult Wrap<TResult>(
            this ITelemetry telemetry,
            Func<TResult> doWork,
            CreateTelemetryEvent<TResult> createTelemetryEvent)
        {
            var (_, result) = WrapResult<TResult>(
                telemetry,
                () =>
                {
                    var result = doWork();
                    return (Outcome.Success, result);
                },
                createTelemetryEvent);

            return result;
        }

        /// <summary>
        /// Wraps some work with telemetry
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="telemetry">The telemetry instance to use</param>
        /// <param name="doWork">The work to do</param>
        /// <param name="createTelemetryEvent">The method to call to create the telemetry event</param>
        /// <returns>The returned outcome, and result from the work</returns>
        public static (Outcome, TResult) WrapResult<TResult>(
            this ITelemetry telemetry,
            Func<(Outcome, TResult)> doWork,
            CreateTelemetryEvent<TResult> createTelemetryEvent)
        {
            var outcome = Outcome.Unknown;
            TResult res = default;
            Exception exception = null;

            var stopwatch = Stopwatch.StartNew();
            try
            {
                (outcome, res) = doWork();
                return (outcome, res);
            }
            catch (Exception ex)
            {
                exception = FlattenException(ex);
                outcome = ToOutcome(ex);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                SendEvent(telemetry, () => createTelemetryEvent(outcome, res, exception, stopwatch.Elapsed));
            }
        }

        #endregion

        #region helper methods

        private static CreateTelemetryEvent<TResult> Wrap<TResult>(CreateTelemetryEvent creator) =>
            (Outcome outcome, TResult _, Exception ex, TimeSpan duration) => creator(outcome, ex, duration);

        private static Exception FlattenException(Exception ex)
        {
            // special case for AggregateExceptions that wrap only a single exception. This can happen for
            // example if we call Task<>.Result. We unwrap here for clearer and simpler exception reporting
            if (ex is AggregateException aex && aex.InnerExceptions.Count == 1)
            {
                return aex.InnerExceptions[0];
            }
            else
            {
                return ex;
            }
        }

        private static Outcome ToOutcome(Exception ex) =>
            ex switch
            {
                OperationCanceledException => Outcome.Canceled,
                TimeoutException => Outcome.TimedOut,
                _ => Outcome.Failed

            };

        private static void SendEvent(ITelemetry telemetry, Func<ITelemetryEvent> creator)
        {
            if (telemetry != null)
            {
                ITelemetryEvent evt = creator();
                if (evt != null)
                {
                    telemetry.LogEvent(evt);
                }
            }
        }

        #endregion
    }
}