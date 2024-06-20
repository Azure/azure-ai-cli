// using System.Diagnostics;
// using Azure.AI.Details.Common.CLI.Extensions.HelperFunctions;

// public static class ProcessExecutionHelperFunctions
// {
//     [HelperFunctionDescription("Executes a command line process and returns the combined standard output and error.")]
//     public static string ExecuteCommand(string command, string arguments)
//     {
//         var processStartInfo = new ProcessStartInfo
//         {
//             FileName = command,
//             Arguments = arguments,
//             RedirectStandardOutput = true,
//             RedirectStandardError = true,
//             UseShellExecute = false,
//             CreateNoWindow = true
//         };

//         using (var process = new Process { StartInfo = processStartInfo })
//         {
//             process.Start();
//             string standardOutput = process.StandardOutput.ReadToEnd();
//             string standardError = process.StandardError.ReadToEnd();
//             process.WaitForExit();
//             return standardOutput + standardError;
//         }
//     }
// }