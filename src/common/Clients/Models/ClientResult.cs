#nullable enable

using Azure.AI.Details.Common.CLI;

namespace Azure.AI.CLI.Common.Clients.Models
{
    public enum ClientOutcome
    {
        Failed = int.MinValue,
        TimedOut = -3,
        Canceled = -2,
        LoginNeeded = -1,
        Unknown = 0,
        Success,
    }

    public interface IClientResult
    {
        ClientOutcome Outcome { get; }

        string? ErrorDetails { get; }

        Exception? Exception { get; }

        bool IsSuccess => !IsError;

        bool IsError { get; }

        void ThrowOnFail(string? message = null);
    }

    public interface IClientResult<out TValue> : IClientResult
    {
        public TValue Value { get; }
    }

    public readonly partial struct ClientResult : IClientResult
    {
        public ClientOutcome Outcome { get; init; }

        public string? ErrorDetails { get; init; }

        public Exception? Exception { get; init; }

        public bool IsSuccess => !IsError;

        public bool IsError => CheckIsError(Outcome, ErrorDetails, Exception);

        public void ThrowOnFail(string? message = null) => ThrowOnFail(Outcome, message, ErrorDetails, Exception);

        public override string ToString() => ToString(this);
    }

    public readonly struct ClientResult<TValue> : IClientResult<TValue>
    {
        public ClientOutcome Outcome { get; init; }

        public TValue Value { get; init; }

        public string? ErrorDetails { get; init; }

        public Exception? Exception { get; init; }

        public bool IsSuccess => !IsError;

        public bool IsError => ClientResult.CheckIsError(Outcome, ErrorDetails, Exception);

        public void ThrowOnFail(string? message = null) => ClientResult.ThrowOnFail(Outcome, message, ErrorDetails, Exception);

        public override string ToString() => ClientResult.ToString(this);
    }

    #region helper internal methods

    public readonly partial struct ClientResult
    {
        internal static bool CheckIsError(ClientOutcome Outcome, string? ErrorDetails, Exception? Exception)
            => Outcome != ClientOutcome.Success
            || Exception != null
            || !string.IsNullOrWhiteSpace(ErrorDetails);

        internal static void ThrowOnFail(ClientOutcome outcome, string? message, string? errorDetails, Exception? ex)
        {
            switch (outcome)
            {
                case ClientOutcome.Canceled:
                    throw new OperationCanceledException(CreateExceptionMessage("CANCELED", message, errorDetails), ex);

                case ClientOutcome.Failed:
                case ClientOutcome.Unknown:
                    throw new ApplicationException(CreateExceptionMessage("FAILED", message, errorDetails), ex);

                case ClientOutcome.LoginNeeded:
                    throw new ApplicationException(CreateExceptionMessage("LOGIN REQUIRED", message, errorDetails), ex);

                case ClientOutcome.Success:
                    // nothing to do
                    return;

                case ClientOutcome.TimedOut:
                    throw new TimeoutException(CreateExceptionMessage("TIMED OUT", message, errorDetails), ex);
            }
        }

        private static string CreateExceptionMessage(string typeStr, string? messageStr, string? detailsStr)
        {
            var message = messageStr.AsSpan().Trim();
            var details = detailsStr.AsSpan().Trim();

            int bufferLen = Math.Min(2048, (typeStr.Length + messageStr?.Length + detailsStr?.Length + 20) ?? 0);
            Span<char> buffer = stackalloc char[bufferLen];
            var current = buffer;

            int len = 0;

            len += StringHelpers.AppendOrTruncate(ref current, typeStr);
            if (!message.IsEmpty || !details.IsEmpty)
            {
                len += StringHelpers.AppendOrTruncate(ref current, " - ");
            }

            if (!message.IsEmpty)
            {
                len += StringHelpers.AppendOrTruncate(ref current, message);
                if (!details.IsEmpty)
                {
                    len += StringHelpers.AppendOrTruncate(ref current, ". ");
                }
            }

            if (details != null)
            {
                len += StringHelpers.AppendOrTruncate(ref current, "Details: ");
                len += StringHelpers.AppendOrTruncate(ref current, details);
            }

            return new string(buffer.Slice(0, len));
        }

        internal static string ToString<T>(T result) where T : IClientResult
        {
            string str = result.Outcome.ToString();
            if (result.ErrorDetails != null)
            {
                str += $". ErrorDetails: {result.ErrorDetails}";
            }

            if (result.Exception != null)
            {
                str += $". Exception: {result.Exception.Message}";
            }

            return str;
        }

        internal static ClientResult From(ProcessOutput output)
        {
            bool hasError = output.HasError;
            return new ClientResult
            {
                ErrorDetails = string.IsNullOrWhiteSpace(output.StdError) ? null : output.StdError,
                Exception = hasError
                    ? new ApplicationException($"Process failed with exit code {output.ExitCode}. Details: {output.StdError}")
                    : null,
                Outcome = hasError ? ClientOutcome.Failed : ClientOutcome.Success,
            };
        }

        internal static ClientResult<TValue> From<TValue>(ProcessOutput output, TValue value)
        {
            bool hasError = output.HasError;
            return new ClientResult<TValue>
            {
                ErrorDetails = string.IsNullOrWhiteSpace(output.StdError) ? null : output.StdError,
                Exception = hasError
                    ? new ApplicationException($"Process failed with exit code {output.ExitCode}. Details: {output.StdError}")
                    : null,
                Outcome = hasError ? ClientOutcome.Failed : ClientOutcome.Success,
                Value = value
            };
        }

        internal static ClientResult<TValue> From<TValue>(TValue result)
            => new ClientResult<TValue>()
            {
                Outcome = ClientOutcome.Success,
                Value = result
            };

        internal static ClientResult FromException(Exception ex)
            => new ClientResult()
            {
                Outcome = ClientOutcome.Failed,
                Exception = ex,
                ErrorDetails = ex.Message
            };

        internal static ClientResult<TValue> FromException<TValue>(Exception ex)
            => FromException(ex, default(TValue)!);

        internal static ClientResult<TValue> FromException<TValue>(Exception ex, TValue result)
            => new ClientResult<TValue>()
            {
                Outcome = ClientOutcome.Failed,
                Exception = ex,
                ErrorDetails = ex.Message,
                Value = result
            };
    }

    #endregion
}
