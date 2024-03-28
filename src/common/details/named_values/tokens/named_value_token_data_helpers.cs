//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class NamedValueTokenDataHelpers
    {
        public static string Demand(INamedValues values, string valueName, string requires, string option, string action, string command)
        {
            var value = values.Get(valueName, true);
            if (string.IsNullOrEmpty(value))
            {
                values.AddThrowError(
                    "ERROR:", $"{action}; requires {requires}.",
                      "TRY:", $"{Program.Name} {command} {option}",
                      "SEE:", $"{Program.Name} help {command}");
            }
            return value!;
        }
    }
}
