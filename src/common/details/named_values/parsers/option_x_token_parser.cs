//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Security.Cryptography;

namespace Azure.AI.Details.Common.CLI
{
    public class OptionXTokenParser : INamedValueTokenParser
    {
        private string _shortName;
        private string _fullName;
        private string _partsRequired;

        public OptionXTokenParser(string name, string fullName, string partsRequired)
        {
            _shortName = name;
            _fullName = fullName;
            _partsRequired = partsRequired;
        }

        public override bool Parse(INamedValueTokens tokens, INamedValues values)
        {
            var nameTokensMatched = 0;
            if (NamedValueTokenParserHelpers.MatchShortName(_shortName, tokens, values, out nameTokensMatched) ||
                NamedValueTokenParserHelpers.MatchFullName(_fullName, tokens, values, _partsRequired, out nameTokensMatched))
            {
                var prefix = tokens.NamePrefixRequired();
                return prefix.Length > 0
                    ? ParseValuesUntilNextTokenPrefixed(tokens, values, nameTokensMatched, prefix)
                    : ParseOneIndexedValue(tokens, values, nameTokensMatched); // e.g. {_fullName}.{index}=value
            }

            return false;
        }

        private bool ParseValuesUntilNextTokenPrefixed(INamedValueTokens tokens, INamedValues values, int nameTokensMatched, string prefix)
        {
            var queue = new Queue<string>();

            var iPeekToken = nameTokensMatched;
            while (true)
            {
                var peekToken = tokens.PeekNextToken(iPeekToken);
                if (peekToken == null) break;
                if (peekToken.StartsWith(prefix)) break;

                var peekTokenValue = tokens.ValueFromToken(peekToken, values);
                queue.Enqueue(peekTokenValue);

                iPeekToken++;
            }

            var parsedValueCount = queue.Count();
            if (parsedValueCount > 0)
            {
                for (int i = 0; queue.Count() > 0; i++)
                {
                    var argName = $"{_fullName}.{i}";
                    if (values.Contains(argName)) continue;

                    values.Add(argName, queue.Dequeue());
                }
            }

            if (parsedValueCount > 0)
            {
                tokens.SkipTokens(nameTokensMatched + parsedValueCount);
                return true;
            }
            
            return false;
        }

        private bool ParseOneIndexedValue(INamedValueTokens tokens, INamedValues values, int nameTokensMatched)
        {
            var peekToken = tokens.PeekNextToken(nameTokensMatched);
            if (peekToken == null) return false;

            var ok = int.TryParse(peekToken, out var index);
            if (!ok) return false;

            var argName = $"{_fullName}.{index}";
            if (values.Contains(argName)) return false;

            var value = tokens.PeekNextTokenValue(nameTokensMatched + 1, values);
            if (value == null) return false;

            values.Add(argName, value);
            tokens.SkipTokens(nameTokensMatched + 2);

            return true;
        }
    }
}
