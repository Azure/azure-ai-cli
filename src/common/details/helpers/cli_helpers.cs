//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Text;

namespace Azure.AI.Details.Common.CLI
{
    public class CliHelpers
    {
        public static string BuildCliArgs(params string[] args)
        {
            var sb = new StringBuilder();
            for (int i = 0; i + 1 < args.Length; i += 2)
            {
                var argName = args[i];
                var argValue = args[i + 1];

                if (string.IsNullOrWhiteSpace(argValue)) continue;

                // if the argValue contains quotes or anything that needs to be "escaped" or enclosed in 
                // double quotes so we can successfully execute on the command shell, do that here.

                if (!argValue.Contains('\"') && !argValue.Contains('\'') && !argValue.Contains(' ') && !argValue.Contains('\t'))
                {
                    sb.Append($"{argName} {argValue}");
                    sb.Append(' ');
                    continue;
                }

                argValue = argValue.Replace("\"", "\\\"");

                sb.Append($"{argName} \"{argValue}\"");
                sb.Append(' ');
            }
            return sb.ToString().Trim();
        }
    }
}
