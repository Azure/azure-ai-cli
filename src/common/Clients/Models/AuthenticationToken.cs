#nullable enable

namespace Azure.AI.Details.Common.CLI.AzCli
{
    public readonly struct AuthenticationToken
    {
        public string Token { get; init; }
        public DateTimeOffset Expires { get; init; }
    }
}
