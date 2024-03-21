//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Azure.AI.Details.Common.CLI
{
    public class IniLineTokenSource : INamedValueTokens
    {
        private List<string> _tokens;

        public IniLineTokenSource(string line)
        {
            line = line.Trim();

            var eqPos = line.IndexOf('=');
            if (eqPos > 0)
            {
                var lhs = line.Substring(0, eqPos);
                var rhs = line.Substring(eqPos);

                _tokens = new List<string>(lhs.Split('.'));
                _tokens.Add(rhs);
            }
            else if (line.Length > 0)
            {
                _tokens = new List<string>();
                _tokens.Add(line);
                _tokens.Add("=");
            }
            else
            {
                _tokens = new List<string>();
            }
        }

        public override string? PopNextToken()
        {
            if (_tokens.Count == 0) return null;

            var token = _tokens[0];
            _tokens.RemoveAt(0);

            return token;
        }

        public override string? PopNextTokenValue(INamedValues? values = null)
        {
            var token = PopNextToken();
            return ValueFromToken(token, values);
        }

        public override string? PeekNextToken(int skip = 0)
        {
            return _tokens.Count > skip ? _tokens[skip] : null;
        }

        public override string? PeekNextTokenValue(int skip = 0, INamedValues? values = null)
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
                sb.Append(i == 0 || i == _tokens.Count - 1 ? $"{token}" : $".{token}");
            }
            return sb.ToString();
        }

        public override void SkipTokens(int count)
        {
            _tokens.RemoveRange(0, Math.Min(count, _tokens.Count));
        }

        public override string? ValueFromToken(string? token, INamedValues? values = null)
        {
            if (token == null || !token.StartsWith("=")) return null;
            return FileHelpers.ExpandAtFileValue(token.Substring(1), values);
        }

        public override string NamePrefixRequired()
        {
            return "";
        }
    }
}
