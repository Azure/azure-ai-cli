using System;
using System.Diagnostics;
using Azure.AI.Details.Common.CLI;

namespace Azure.AI.Details.Common.CLI.Extensions.Otel
{
    public class Dashboard
    {
        // Execute a command in the command prompt and return the output
        private static string ExecuteCommand(string command)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (Process process = new Process())
            {
                process.StartInfo = processStartInfo;
                process.Start();
                process.WaitForExit();  
                return process.StandardOutput.ReadToEnd();  
            }
        }
        public static void StartDashboard()
        {
            
            // Commands for the terminal
            string dockerStartCommand = "docker run --rm -it -p 18888:18888 -p 4317:18889 -d --name aspire-dashboard mcr.microsoft.com/dotnet/aspire-dashboard:8.0.0";
            string dockerLogsCommand = "docker logs aspire-dashboard";

            try
            {
                // Start the Docker container
                string startResult = ExecuteCommand(dockerStartCommand);
                Console.WriteLine(startResult);

                // Optionally wait a bit before fetching logs if needed
                System.Threading.Thread.Sleep(3000); 

                // Get logs from the Docker container
                string logsResult = ExecuteCommand(dockerLogsCommand);
                Console.WriteLine(logsResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }
        public static void StopDashboard()
        {
            string dockerStopCommand = "docker stop aspire-dashboard";
            try
            {
                // Stop the Docker container
                string stopResult = ExecuteCommand(dockerStopCommand);
                Console.WriteLine("Docker Stop Command Output:");
                Console.WriteLine(stopResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred when stopping the dashboard: " + ex.Message);
            }
        }
        public static bool IsRunning()
        {
            string checkCommand = "docker ps --filter \"name=aspire-dashboard\" --format \"{{.Names}}\"";
            string result = ExecuteCommand(checkCommand);
            return result.Contains("aspire-dashboard");
        }
    }
}
