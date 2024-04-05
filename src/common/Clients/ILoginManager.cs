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

    /// <summary>
    /// Options for logging in
    /// </summary>
    public struct LoginOptions
    {
        /// <summary>
        /// The type of login to attempt
        /// </summary>
        public LoginMode Mode { get; set; }
        
        //public string? TenantId { get; set; }
        //public string? UserName { get; set; }
    }

    /// <summary>
    /// Manages the login process for a client
    /// </summary>
    public interface ILoginManager : IDisposable
    {
        /// <summary>
        /// Whether or not we can attempt to log in
        /// </summary>
        /// <returns>True or false</returns>
        public bool CanAttemptLogin { get; }

        /// <summary>
        /// Logs the client in
        /// </summary>
        /// <param name="options">The options to use for the login</param>
        /// <param name="token">The cancellation token to use</param>
        /// <returns>Asynchronous task that completes once the login has completed (or failed)</returns>
        Task<ClientResult> LoginAsync(LoginOptions options, CancellationToken token);

        /// <summary>
        /// Gets an authentication token to use for sending HTTP REST requests
        /// </summary>
        /// <param name="token">The cancellation token to use</param>
        /// <returns>Asynchronous task that returns the authentication token</returns>
        Task<ClientResult<AuthenticationToken>> GetOrRenewAuthToken(CancellationToken token);
    }
}
