//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class ArgXTokenParser : INamedValueTokenParser
    {
        public override bool Parse(INamedValueTokens tokens, INamedValues values)
        {
            var nameToken = tokens.PeekNextToken(0);
            if (nameToken == null) return false;

            var prefix = tokens.NamePrefixRequired();
            return prefix.Length > 0 ? ParseWithPrefix(tokens, values) : ParseWithoutPrefix(tokens, values);
        }

        private bool ParseWithPrefix(INamedValueTokens tokens, INamedValues values)
        {
            var peekToken1 = tokens.PeekNextToken(0);
            if (peekToken1 == null) return false;

            var prefix = tokens.NamePrefixRequired();
            if (peekToken1.StartsWith(prefix)) return false; // if we're looking for prefix ("--") and this has it, then it's not an ARG (it's somebody else's named value)

            for (int i = 0; true; i++)
            {
                var argName = "arg" + i;
                if (!values.Contains(argName))
                {
                    values.Add(argName, tokens.ValueFromToken(peekToken1, values));
                    tokens.SkipTokens(1);
                    return true;
                }
            }
        }

        private bool ParseWithoutPrefix(INamedValueTokens tokens, INamedValues values)
        {
            var peekToken1 = tokens.PeekNextToken(0);
            if (peekToken1 == null) return false;

            if (!peekToken1.StartsWith("arg")) return false;

            var digits = peekToken1.Remove(0, 3).Count(ch => char.IsDigit(ch));
            if (digits != peekToken1.Length - 3) return false;

            var index = int.Parse(peekToken1.Remove(0, 3));
            var peekToken2Value = tokens.ValueFromToken(tokens.PeekNextToken(1), values);
            if (!string.IsNullOrEmpty(peekToken2Value))
            {
                values.Add("arg" + index, peekToken2Value);
                tokens.SkipTokens(2);
                return true;
            }

            return false;
        }
    }
}
