using Azure.AI.CLI.Common.Clients;
using Azure.AI.CLI.Common.Clients.Models;
using Azure.AI.Details.Common.CLI;
using Azure.AI.Details.Common.CLI.AzCli;
using Newtonsoft.Json.Linq;

namespace Azure.AI.CLI.Clients.AzPython
{
    /// <summary>
    /// Does login via a console using the AZ CLI
    /// </summary>
    public class AzConsoleLoginManager : ILoginManager
    {
        private readonly IDictionary<string, string> _cliEnv;
        private readonly TimeSpan _minValidity;
        private AuthenticationToken? _authToken;

        public AzConsoleLoginManager(string userAgent, TimeSpan? minValidity = null)
        {
            _cliEnv = new Dictionary<string, string>()
            {
                {  "AZURE_HTTP_USER_AGENT", userAgent ?? throw new ArgumentNullException(nameof(userAgent)) }
            };

            _minValidity = minValidity ?? TimeSpan.FromMinutes(5);
        }

        public async Task<ClientResult> LoginAsync(LoginOptions options, CancellationToken token)
        {
            try
            {
                var showDeviceCodeMessage = (string message) =>
                {
                    if (message.Contains("device") && message.Contains("code"))
                    {
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(message);
                        Console.WriteLine();
                        Console.ResetColor();
                    }
                };

                var stdErrHandler = options.Mode == LoginMode.UseDeviceCode ? showDeviceCodeMessage : null;
                var deviceCodePart = options.Mode == LoginMode.UseDeviceCode ? "--use-device-code" : "";
                var queryPart = $"--query \"[?state=='Enabled'].{{Name:name,Id:id,IsDefault:isDefault,UserName:user.name}}\"";

                var cmdOut = await ProcessHelpers.RunShellCommandAsync(
                    "az",
                    $"login --output json {queryPart} {deviceCodePart}",
                    _cliEnv,
                    stdErrHandler: stdErrHandler);
                if (cmdOut.HasError)
                {
                    return ClientResult.From(cmdOut);
                }

                return new ClientResult
                {
                    Outcome = ClientOutcome.Success
                };
            }
            catch (Exception ex)
            {
                return ClientResult.FromException(ex);
            }
        }

        public async Task<ClientResult<AuthenticationToken>> GetOrRenewAuthToken(CancellationToken token)
        {
            try
            {
                if (_authToken?.Expires > (DateTimeOffset.Now + _minValidity))
                {
                    return new ClientResult<AuthenticationToken>()
                    {
                        Outcome = ClientOutcome.Success,
                        Value = _authToken.Value
                    };
                }

                var cmdOut = await ProcessHelpers.RunShellCommandAsync(
                    "az",
                    "account get-access-token --output json",
                    _cliEnv);
                if (cmdOut.HasError)
                {
                    return ClientResult.From(cmdOut, default(AuthenticationToken));
                }

                var json = JObject.Parse(cmdOut.StdOutput);
                var authToken = new AuthenticationToken()
                {
                    Expires = DateTimeOffset.FromUnixTimeSeconds(json?["expires_on"]?.Value<long>() ?? 0),
                    Token = json?["accessToken"]?.Value<string>() ?? string.Empty
                };

                if (authToken.Expires <= (DateTimeOffset.Now + _minValidity)
                    && string.IsNullOrWhiteSpace(authToken.Token))
                {
                    return new ClientResult<AuthenticationToken>()
                    {
                        Outcome = ClientOutcome.Failed,
                        ErrorDetails = "Could not parse auth token and/or renewed auth token was invalid",
                        Value = default
                    };
                }

                _authToken = authToken;
                return ClientResult.From(authToken);
            }
            catch (Exception ex)
            {
                return ClientResult.FromException(ex, default(AuthenticationToken));
            }
        }

        public void Dispose()
        {
            // TODO implement?
        }
    }
}
