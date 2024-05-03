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
            var calculator = new ExpressionCalculator();

            Func<string, string> interpolate = (line) => line.ReplaceValues(values);
            Func<string, bool> evaluateCondition = (condition) =>
            {
                condition = condition.ReplaceValues(values).Trim();
                return condition switch
                {
                    "" => false,
                    "true" => true,
                    "false" => false,
                    _ => calculator.Evaluate(condition)
                };
            };
            Action<string> setVariable = (s) => {
                var result = calculator.Evaluate(s);
                var name = s.Split('=')[0].Trim();
                values.Reset(name, result.ToString());
            };

            return ProcessTemplate(template, evaluateCondition, interpolate, setVariable);
        }

        public static string ProcessTemplate(string template, Func<string, bool> evaluateCondition, Func<string, string> interpolate, Action<string> setVariable)
        {
            var lines = template.Split('\n').ToList();
            var output = new StringBuilder();

            var inTrueBranchNow = new Stack<bool>();
            inTrueBranchNow.Push(true);

            var skipElseBranches = new Stack<bool>();
            skipElseBranches.Push(true);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim('\n', '\r', ' ', '\t');

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
                    continue;
                }
                else if (trimmedLine.StartsWith("{{endif}}"))
                {
                    inTrueBranchNow.Pop();
                    skipElseBranches.Pop();
                    continue;
                }
                else if (trimmedLine.StartsWith("{{set ") && trimmedLine.EndsWith("}}"))
                {
                    if (inTrueBranchNow.All(b => b))
                    {
                        var assignment = trimmedLine[6..^2].Trim();
                        setVariable(assignment);
                    }
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