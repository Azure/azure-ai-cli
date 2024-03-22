#nullable enable

using Azure.AI.CLI.Common.Clients;
using Azure.AI.CLI.Common.Clients.Models;
using Azure.AI.Details.Common.CLI;
using Azure.AI.Details.Common.CLI.ConsoleGui;
using Azure.AI.Details.Common.CLI.details.console;

namespace Azure.AI.CLI.Clients.AzPython
{
    public abstract class LoginHelpers
    {
        private const string LOADING_CHOICES = "*** Loading choices ***";
        protected readonly ILoginManager _loginManager;
        private readonly Func<bool> _getAllowInteractiveLogin;

        protected LoginHelpers(ILoginManager loginManager, Func<bool> getAllowInteractiveLogin)
        {
            _loginManager = loginManager ?? throw new ArgumentNullException(nameof(loginManager));
            _getAllowInteractiveLogin = getAllowInteractiveLogin ?? throw new ArgumentNullException(nameof(getAllowInteractiveLogin));
        }

        protected bool IsInteractive => _getAllowInteractiveLogin();

        protected async Task<ProcessOutput> GetResponseOnLogin(Func<Task<ProcessOutput>> getResponse, CancellationToken token)
        {
            using var writer = new ConsoleTempWriter();
            writer.WriteTemp(LOADING_CHOICES);

            var response = await getResponse();
            if (string.IsNullOrEmpty(response.StdOutput) && !string.IsNullOrEmpty(response.StdError))
            {
                if (HasLoginError(response.StdError))
                {
                    var loginResponse = await AttemptLogin(writer, token);
                    if (loginResponse.IsSuccess)
                    {
                        writer.WriteTemp(LOADING_CHOICES);

                        response = await getResponse();
                    }
                    else
                    {
                        writer.Clear();
                        ConsoleHelpers.WriteLineError("*** Please run `az login` and try again ***");
                    }
                }
            }

            return response;
        }

        protected async Task<ClientResult> AttemptLogin(ConsoleTempWriter writer, CancellationToken token)
        {
            bool cancelLogin = !IsInteractive;
            bool useDeviceCode = false;
            if (IsInteractive)
            {
                writer.WriteErrorTemp("*** WARNING: `az login` required ***");
                writer.AppendTemp(" ");

                var selection = 0;
                var choices = new List<string>() {
                        "LAUNCH: `az login` (interactive device code)",
                        "CANCEL: `az login ...` (non-interactive)",
                    };

                if (!OS.IsCodeSpaces())
                {
                    choices.Insert(0, "LAUNCH: `az login` (interactive browser)");
                    selection = OS.IsWindows() ? 0 : 1;
                }

                var picked = ListBoxPicker.PickIndexOf(choices.ToArray(), selection);

                cancelLogin = picked < 0 || picked == choices.Count() - 1;
                useDeviceCode = picked == choices.Count() - 2;
            }

            if (cancelLogin)
            {
                return new ClientResult()
                {
                    Outcome = ClientOutcome.Canceled,
                };
            }

            writer.WriteTemp($"*** Launching `az login` (interactive) ***");
            var response = await _loginManager.LoginAsync(
                new() { Mode = useDeviceCode ? LoginMode.UseDeviceCode : LoginMode.UseWebPage },
                token);
            return response;
        }

        private static bool HasLoginError(string errorMessage) => errorMessage.Split('\'', '"').Contains("az login") || errorMessage.Contains("refresh token");
    }
}
