namespace Azure.AI.Details.Common.CLI.Telemetry
{
    public enum Outcome
    {
        /// <summary>
        /// Outcome was unknown (should not happen)
        /// </summary>
        Unknown,

        /// <summary>
        /// Success
        /// </summary>
        Success,

        /// <summary>
        /// Failure due to one or more errors
        /// </summary>
        Failed,

        /// <summary>
        /// Canceled by the user
        /// </summary>
        Canceled,

        /// <summary>
        /// Took too long and was aborted
        /// </summary>
        TimedOut
    }
}
