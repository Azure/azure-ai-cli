#nullable enable

using Azure.AI.CLI.Common.Clients;
using Azure.AI.CLI.Common.Clients.Models;
using Azure.AI.Details.Common.CLI.ConsoleGui;

namespace Azure.AI.Details.Common.CLI
{
    public static class LoginHelpers
    {
        public static async Task<TResult> GetResponseOnLogin<TResult>(ILoginManager loginManager, Func<Task<TResult>> getResult, CancellationToken token, string prompt = "Name", string loadingMessage = "*** Loading choices ***")
            where TResult : IClientResult
        {
            Console.Write($"{prompt}: ");

            using var writer = new ConsoleTempWriter();
            writer.WriteTemp(loadingMessage);

            var result = await getResult();
            if (result.Outcome == ClientOutcome.LoginNeeded)
            {
                var loginResponse = await AttemptLogin(loginManager, writer, token);
                if (loginResponse.IsSuccess)
                {
                    writer.WriteTemp(loadingMessage);
                    result = await getResult();
                }
                else
                {
                    writer.Clear();
                    ConsoleHelpers.WriteLineError("*** Please run `az login` and try again ***");
                }
            }

            return result;
        }

        private static async Task<ClientResult> AttemptLogin(ILoginManager loginManager, ConsoleTempWriter writer, CancellationToken token)
        {
            bool cancelLogin = !loginManager.CanAttemptLogin;
            bool useDeviceCode = false;
            if (loginManager.CanAttemptLogin)
            {
                writer.WriteErrorTemp("*** WARNING: `az login` required ***");
                writer.AppendTemp(" ");

                var selection = 0;
                var choices = new List<string>() {
                    "LAUNCH: `az login` (interactive device code)",
                    "CANCEL",
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

            writer.WriteTemp($"*** Launching `az login` ***");
            var response = await loginManager.LoginAsync(
                new() { Mode = useDeviceCode ? LoginMode.UseDeviceCode : LoginMode.UseWebPage },
                token);
            return response;
        }
    }
}
