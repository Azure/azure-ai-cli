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
    public class TsvRowTokenSource : INamedValueTokens
    {
        private List<string> _tokens;

        public TsvRowTokenSource(string columnNames, string rowValues)
        {
            var names = SplitTsvLine(columnNames);
            var nameCount = names.Length;

            var values = SplitTsvLine(rowValues);
            var valueCount = values.Length;

            _tokens = new List<string>();

            var nameValueCount = Math.Max(names.Length, values.Length);
            for (int i = 0; i < nameValueCount; i++)
            {
                _tokens.Add(i < nameCount ? names[i] : "");
                _tokens.Add("=" + (i < valueCount ? values[i] : ""));
            }
        }

        public override string PopNextToken()
        {
            if (_tokens.Count == 0) return null;

            var token = _tokens[0];
            _tokens.RemoveAt(0);

            return token;
        }

        public override string PopNextTokenValue(INamedValues values = null)
        {
            var token = PopNextToken();
            return ValueFromToken(token, values);
        }

        public override string PeekNextToken(int skip = 0)
        {
            return _tokens.Count > skip ? _tokens[skip] : null;
        }

        public override string PeekNextTokenValue(int skip = 0, INamedValues values = null)
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
                sb.Append(token);

                var value = ValueFromToken(token);
                if (value != null) break;
            }
            return sb.ToString();
        }

        public override void SkipTokens(int count)
        {
            _tokens.RemoveRange(0, Math.Min(count, _tokens.Count));
        }

        public override string ValueFromToken(string token, INamedValues values = null)
        {
            if (token == null) return null;
            return token.StartsWith("=") ? token.Substring(1) : null;
        }

        public override string NamePrefixRequired()
        {
            return "";
        }

        private string[] SplitTsvLine(string line)
        {
            return line.Contains("\t")
                ? line.Trim('\r', '\n').Split("\t".ToCharArray())
                : line.Trim('\r', '\n').Split(";\t".ToCharArray());
        }
    }
}
