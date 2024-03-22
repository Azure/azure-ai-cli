#nullable enable

using Azure.AI.CLI.Common.Clients.Models;
using Azure.AI.Details.Common.CLI.AzCli;

namespace Azure.AI.CLI.Common.Clients
{
    /// <summary>
    /// The type of login to attempt
    /// </summary>
    public enum LoginMode
    {
        /// <summary>
        /// A window will appear with a browser page where the user can log in. Not all OSs support this
        /// </summary>
        UseWebPage,

        /// <summary>
        /// Show the user a device code and  URI. The user then needs to open a browser tab somewhere (could be a different
        /// device), navigate to that URI, enter the device code, and then sign in. Once the browser tab is closed, the login
        /// process will be complete
        /// </summary>
        UseDeviceCode,
    }

    public struct LoginOptions
    {
        public LoginMode Mode { get; set; }
        //public string? TenantId { get; set; }
        //public string? UserName { get; set; }
    }

    public interface ILoginManager : IDisposable
    {
        Task<ClientResult> LoginAsync(LoginOptions options, CancellationToken token);
        Task<ClientResult<AuthenticationToken>> GetOrRenewAuthToken(CancellationToken token);
    }
}
