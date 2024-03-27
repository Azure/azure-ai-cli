#nullable enable

namespace Azure.AI.Details.Common.CLI.AzCli
{
    /// <summary>
    /// An Azure auth token
    /// </summary>
    public readonly struct AuthenticationToken
    {
        /// <summary>
        /// The token value
        /// </summary>
        public string Token { get; init; }

        /// <summary>
        /// When this token expires
        /// </summary>
        public DateTimeOffset Expires { get; init; }
    }
}
