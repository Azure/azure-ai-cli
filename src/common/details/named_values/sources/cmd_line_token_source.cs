//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Azure.AI.Details.Common.CLI
{
    public class CmdLineTokenSource : INamedValueTokens
    {
        private List<string> _tokens;

        private static void WaitForDebugger()
        {
            int waitFor = 30000;

            if (!Debugger.IsAttached)
            {
                Console.Write("Waiting for debugger ...");
                while (!Debugger.IsAttached && waitFor > 0)
                {
                    Console.Write(".");
                    Thread.Sleep(100);
                    waitFor -= 100;
                }
                Console.WriteLine();

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
            }
        }


        public CmdLineTokenSource(string[] tokens, INamedValues values)
        {
            _tokens = new List<string>(GetTokens(tokens));

            var token = PeekNextToken();
            while (!string.IsNullOrEmpty(token))
            {
                if (token == Program.Name)
                {
                }
                else if (token == "debug")
                {
                    Program.Debug = true;
                }
                else if (Program.Debug && token == "wait")
                {
                    WaitForDebugger();
                }
                else if (token == "dump")
                {
                    Program.DebugDumpResources();
                }
                else if (token == "cls")
                {
                    values.Add("x.cls", "true");
                }
                else if (token == "pause")
                {
                    values.Add("x.pause", "true");
                }
                else if (token == "quiet")
                {
                    values.Add("x.quiet", "true");
                }
                else if (token == "verbose")
                {
                    values.Add("x.verbose", "true");
                }
                else
                {
                    break;
                }

                PopNextToken();
                token = PeekNextToken();
            }
        }

        public override string PopNextToken()
        {
            if (_tokens.Count == 0) return null;

            var token = _tokens[0];
            _tokens.RemoveAt(0);

            return token;
        }

        public override string PopNextTokenValue(INamedValues? values = null)
        {
            var token = PopNextToken();
            return ValueFromToken(token, values);
        }

        public override string PeekNextToken(int skip = 0)
        {
            return _tokens.Count > skip ? _tokens[skip] : null;
        }

        public override string PeekNextTokenValue(int skip = 0, INamedValues? values = null)
        {
            var token = PeekNextToken(skip);
            return ValueFromToken(token, values);
        }

        public override string PeekAllTokens(int max = int.MaxValue)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < max; i++)
            {
                var token = PeekNextToken(i);
                if (token == null) break;
                sb.Append(i == 0 ? $"{token}" : $" {token}");
            }
            return sb.ToString().TrimEnd();
        }

        public override void SkipTokens(int count)
        {
            _tokens.RemoveRange(0, Math.Min(count, _tokens.Count));
        }

        public override string? ValueFromToken(string token, INamedValues? values = null)
        {
            if (token == null) return null;

            // Values are defined as being NOT CLI items
            // CLI items are defined as starting with "--" and not containing ";"
            var isCLI = token.StartsWith(NamePrefixRequired()) && !token.Contains(";");
            if (isCLI) return null;

            return FileHelpers.ExpandAtFileValue(token, values);
        }

        public override string NamePrefixRequired()
        {
            return "--";
        }

        private string[] GetTokens(string[] tokens)
        {
            var prefix = NamePrefixRequired();
            var list = new List<string>();
            foreach (var token in tokens)
            {
                var isCLI = token.StartsWith(NamePrefixRequired()) && !token.Contains(";");
                if (isCLI && token.Substring(prefix.Length).Contains('-'))
                {
                    var updated = prefix + token.Substring(prefix.Length).Replace('-', '.');
                    list.Add(updated);
                }
                else
                {
                    list.Add(token);
                }   
            }
            return list.ToArray();
        }
    }
}
