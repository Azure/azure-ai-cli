#nullable enable

using Azure.AI.Details.Common.CLI;

namespace Azure.AI.CLI.Common.Clients.Models
{
    /// <summary>
    /// Enumerates the possible outcomes of a client operation
    /// </summary>
    public enum ClientOutcome
    {
        /// <summary>
        /// The operation failed
        /// </summary>
        Failed = int.MinValue,

        /// <summary>
        /// The operation timed out
        /// </summary>
        TimedOut = -3,

        /// <summary>
        /// The operation was canceled
        /// </summary>
        Canceled = -2,

        /// <summary>
        /// The operation requires login. The user should run `az login` and try again. This
        /// can happen if the login was required, and we failed to log in as well
        /// </summary>
        LoginNeeded = -1,

        /// <summary>
        /// The operation status was unknown. This is the default value
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The operation was successful
        /// </summary>
        Success,
    }

    /// <summary>
    /// Represents the result of a client operation
    /// </summary>
    public interface IClientResult
    {
        /// <summary>
        /// The outcome of the operation
        /// </summary>
        ClientOutcome Outcome { get; }

        /// <summary>
        /// (Optional) Details about the error that occurred
        /// </summary>
        string? ErrorDetails { get; }

        /// <summary>
        /// (Optional) The exception that occurred
        /// </summary>
        Exception? Exception { get; }

        /// <summary>
        /// Indicates if the operation was successful
        /// </summary>
        bool IsSuccess => !IsError;

        /// <summary>
        /// Indicates if the operation failed
        /// </summary>
        bool IsError { get; }

        /// <summary>
        /// Throws an exception if the operation failed
        /// </summary>
        /// <param name="message">(Optional) The message to include in the exception message</param>
        void ThrowOnFail(string? message = null);
    }

    /// <summary>
    /// Represents the result of a client operation with a value
    /// </summary>
    /// <typeparam name="TValue">The type of the value</typeparam>
    public interface IClientResult<out TValue> : IClientResult
    {
        /// <summary>
        /// The value of the operation
        /// </summary>
        public TValue Value { get; }
    }

    /// <summary>
    /// Represents the result of a client operation
    /// </summary>
    public readonly partial struct ClientResult : IClientResult
    {
        /// <inheritdoc />
        public ClientOutcome Outcome { get; init; }

        /// <inheritdoc />
        public string? ErrorDetails { get; init; }

        /// <inheritdoc />
        public Exception? Exception { get; init; }

        /// <inheritdoc />
        public bool IsSuccess => !IsError;

        /// <inheritdoc />
        public bool IsError => CheckIsError(Outcome, ErrorDetails, Exception);

        /// <inheritdoc />
        public void ThrowOnFail(string? message = null) => ThrowOnFail(Outcome, message, ErrorDetails, Exception);

        /// <summary>
        /// Converts the result to a string
        /// </summary>
        /// <returns>A string representation of the result</returns>
        public override string ToString() => ToString(this);
    }

    /// <summary>
    /// Represents the result of a client operation with a value
    /// </summary>
    /// <typeparam name="TValue">The type of the value</typeparam>
    public readonly struct ClientResult<TValue> : IClientResult<TValue>
    {
        /// <inheritdoc />
        public ClientOutcome Outcome { get; init; }

        /// <inheritdoc />
        public TValue Value { get; init; }

        /// <inheritdoc />
        public string? ErrorDetails { get; init; }

        /// <inheritdoc />
        public Exception? Exception { get; init; }

        /// <inheritdoc />
        public bool IsSuccess => !IsError;

        /// <inheritdoc />
        public bool IsError => ClientResult.CheckIsError(Outcome, ErrorDetails, Exception);

        /// <inheritdoc />
        public void ThrowOnFail(string? message = null) => ClientResult.ThrowOnFail(Outcome, message, ErrorDetails, Exception);

        /// <summary>
        /// Converts the result to a string
        /// </summary>
        /// <returns>A string representation of the result</returns>
        public override string ToString() => ClientResult.ToString(this);
    }

    #region helper internal methods

    public readonly partial struct ClientResult
    {
        internal static bool CheckIsError(ClientOutcome Outcome, string? ErrorDetails, Exception? Exception)
            => Outcome != ClientOutcome.Success
            || Exception != null
            || !string.IsNullOrWhiteSpace(ErrorDetails);

        internal static Exception GenerateException(ClientOutcome outcome, string? message = null, string? errorDetails = null, Exception? ex = null)
            => outcome switch
            {
                ClientOutcome.Canceled => new OperationCanceledException(CreateExceptionMessage("CANCELED", message, errorDetails), ex),
                ClientOutcome.Failed or ClientOutcome.Unknown => new ApplicationException(CreateExceptionMessage("FAILED", message, errorDetails), ex),
                ClientOutcome.LoginNeeded => new ApplicationException(CreateExceptionMessage("LOGIN REQUIRED", message, errorDetails), ex),
                ClientOutcome.TimedOut => new TimeoutException(CreateExceptionMessage("TIMED OUT", message, errorDetails), ex),
                _ => new ApplicationException(CreateExceptionMessage("FAILED", message, errorDetails), ex),
            };

        internal static void ThrowOnFail(ClientOutcome outcome, string? message, string? errorDetails, Exception? ex)
        {
            if (ClientOutcome.Success == outcome)
            {
                return;
            }

            throw GenerateException(outcome, message, errorDetails, ex);
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
            ClientOutcome outcome = ClientOutcome.Success;
            if (HasLoginError(output.StdError))
            {
                outcome = ClientOutcome.LoginNeeded;
            }
            else if (output.HasError)
            {
                outcome = ClientOutcome.Failed;
            }

            return new ClientResult
            {
                ErrorDetails = string.IsNullOrWhiteSpace(output.StdError) ? null : output.StdError,
                Exception = outcome != ClientOutcome.Success
                    ? new ApplicationException($"Process failed with exit code {output.ExitCode}. Details: {output.StdError}")
                    : null,
                Outcome = outcome,
            };
        }

        internal static ClientResult<TValue> From<TValue>(ProcessOutput output, TValue value)
        {
            ClientOutcome outcome = ClientOutcome.Success;
            if (HasLoginError(output.StdError))
            {
                outcome = ClientOutcome.LoginNeeded;
            }
            else if (output.HasError)
            {
                outcome = ClientOutcome.Failed;
            }

            return new ClientResult<TValue>
            {
                ErrorDetails = string.IsNullOrWhiteSpace(output.StdError) ? null : output.StdError,
                Exception = outcome != ClientOutcome.Success
                    ? new ApplicationException($"Process failed with exit code {output.ExitCode}. Details: {output.StdError}")
                    : null,
                Outcome = outcome,
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

        private static bool HasLoginError(string? errorMessage)
            => errorMessage != null
                && (errorMessage.Split('\'', '"').Contains("az login") || errorMessage.Contains("refresh token"));
    }

    #endregion
}
