//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Collections.Generic;

namespace Azure.AI.Details.Common.CLI.Extensions.Templates
{
    public static class TemplateHelpers
    {
        public static string ProcessTemplate(string template, INamedValues values)
        {
            Func<string, string> interpolate = (line) => line.ReplaceValues(values);
            Func<string, bool> evaluateCondition = (condition) =>
            {
                var value = condition.ReplaceValues(values);
                return value switch
                {
                    "true" => true,
                    _ => false
                };
            };

            return ProcessTemplate(template, evaluateCondition, interpolate);
        }

        public static string ProcessTemplate(string template, Func<string, bool> evaluateCondition, Func<string, string> interpolate)
        {
            var lines = template.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var output = new StringBuilder();

            var inTrueBranchNow = new Stack<bool>();
            inTrueBranchNow.Push(true);

            var skipElseBranches = new Stack<bool>();
            skipElseBranches.Push(true);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("{{if ") && trimmedLine.EndsWith("}}"))
                {
                    var condition = trimmedLine[5..^2].Trim();
                    var evaluated = evaluateCondition(condition);
                    inTrueBranchNow.Push(evaluated);
                    skipElseBranches.Push(evaluated);
                    continue;
                }
                else if (trimmedLine.StartsWith("{{else if ") && trimmedLine.EndsWith("}}"))
                {
                    if (inTrueBranchNow.Peek())
                    {
                        inTrueBranchNow.Pop();
                        inTrueBranchNow.Push(false);
                        // skipElseBranches.Peek() should already be true
                        continue;
                    }
                    else if (skipElseBranches.Peek())
                    {
                        continue;
                    }

                    var condition = trimmedLine[10..^2].Trim();
                    var evaluated = evaluateCondition(condition);
                    inTrueBranchNow.Pop();
                    inTrueBranchNow.Push(evaluated);
                    skipElseBranches.Pop();
                    skipElseBranches.Push(evaluated);
                    continue;
                }
                else if (trimmedLine.StartsWith("{{else}}"))
                {
                    if (inTrueBranchNow.Peek())
                    {
                        inTrueBranchNow.Pop();
                        inTrueBranchNow.Push(false);
                        // skipElseBranches.Peek() should already be true
                        continue;
                    }
                    else if (skipElseBranches.Peek())
                    {
                        continue;
                    }

                    inTrueBranchNow.Pop();
                    inTrueBranchNow.Push(true);
                    skipElseBranches.Pop();
                    skipElseBranches.Push(true);
                }
                else if (trimmedLine.StartsWith("{{endif}}"))
                {
                    inTrueBranchNow.Pop();
                    skipElseBranches.Pop();
                    continue;
                }

                if (inTrueBranchNow.All(b => b))
                {
                    var firstLine = output.Length == 0;
                    if (!firstLine) output.AppendLine();
                    output.Append(interpolate(line));
                }
            }

            return output.ToString();
        }
    }
}