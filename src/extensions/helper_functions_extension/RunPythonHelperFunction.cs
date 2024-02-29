//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

// using System.Diagnostics;

// namespace Azure.AI.Details.Common.CLI.Extensions.HelperFunctions
// {
//     public static class RunPythonHelperFunctions
//     {
//         [HelperFunctionDescription("Runs python code by executing 'python ...', returning the combined stdout and stderr as a string")]
//         public static string RunPython(string fileName, string arguments)
//         {
//             var processStartInfo = new ProcessStartInfo
//             {
//                 FileName = "python",
//                 Arguments = $"{fileName} {arguments}",
//                 RedirectStandardOutput = true,
//                 RedirectStandardError = true,
//                 UseShellExecute = false,
//                 CreateNoWindow = true,
//             };

//             var process = new Process
//             {
//                 StartInfo = processStartInfo,
//             };

//             process.Start();
//             string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
//             process.WaitForExit();

//             return output;
//         }
//    }
// }