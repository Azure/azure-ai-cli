//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Azure.AI.Details.Common.CLI
{
    public class ErrorHelpers
    {
        public static void WriteMessage(string warningOrErrorLabel, string warningOrError, params string[] extra)
        {
            var message = CreateMessage(warningOrErrorLabel, warningOrError, extra);
            ConsoleHelpers.WriteError(message);
        }

        public static void WriteLineMessage(string warningOrErrorLabel, string warningOrError, params string[] extra)
        {
            var message = CreateMessage(warningOrErrorLabel, warningOrError, extra);
            ConsoleHelpers.WriteLineError(message);
        }

        public static string CreateMessage(string warningOrErrorLabel, string warningOrError, params string[] extra)
        {
            var sb = new StringBuilder();

            EnsureEndsWithColon(ref warningOrErrorLabel);
            sb.AppendLine($"{warningOrErrorLabel} {warningOrError}");

            var label = "";
            var indent = warningOrErrorLabel.Length;
            foreach (var item in extra ?? Array.Empty<string>())
            {
                if (item.EndsWith(':'))
                {
                    label = item != ":" ? item : "";
                    indent = Math.Max(0, warningOrErrorLabel.Length - label.Length);

                    var doubleSpace = label.Length > warningOrErrorLabel.Length;
                    if (doubleSpace)
                    {
                        warningOrErrorLabel = label;
                        label = $"\n{label}";
                    } 

                    continue;
                }

                if (!string.IsNullOrEmpty(item))
                {
                    sb.Append(' ', indent);
                    sb.AppendLine($"{label} {item}");
                }
                else
                {
                    sb.AppendLine();
                }

                if (label != "OR:")
                {
                    label = "OR:";
                    indent = warningOrErrorLabel.Length - label.Length;
                }
            }

            return sb.ToString().Trim('\r', '\n');
        }

        private static void EnsureEndsWithColon(ref string warningOrErrorLabel)
        {
            if (!warningOrErrorLabel.EndsWith(':'))
            {
                warningOrErrorLabel = $"{warningOrErrorLabel}:";
            }
        }
    }
}
