//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class InputWildcardTokenParser : INamedValueTokenParser
    {
        public override bool Parse(INamedValueTokens tokens, INamedValues values)
        {
            return MatchName(tokens, values, out var nameTokensMatched) &&
                   ParseValues(tokens, values, nameTokensMatched);
        }

        private bool MatchName(INamedValueTokens tokens, INamedValues values, out int nameTokenCount)
        {
            nameTokenCount = 0;

            var nameToken = tokens.PeekNextToken(0);
            if (nameToken == null) return false;
            if (!nameToken.StartsWith(tokens.NamePrefixRequired())) return false;

            nameToken = nameToken.Remove(0, tokens.NamePrefixRequired().Length);
            if (nameToken != "input") return false;

            nameTokenCount = 1;
            return true;
        }

        private bool ParseValues(INamedValueTokens tokens, INamedValues values, int nameTokenCount)
        {
            var peekToken1 = tokens.PeekNextToken(nameTokenCount + 0);
            var peekToken2 = tokens.PeekNextToken(nameTokenCount + 1);

            var peekToken1Value = tokens.ValueFromToken(peekToken1, values);
            var peekToken2Value = tokens.ValueFromToken(peekToken2, values);

            if (peekToken1 != null && peekToken1.Contains('='))
            {
                StringHelpers.SplitNameValue(peekToken1Value!, out var name, out var value);
                values.Add("input." + name, value);
                tokens.SkipTokens(nameTokenCount + 1);
                return true;
            }
            else if (!string.IsNullOrEmpty(peekToken1) && !string.IsNullOrEmpty(peekToken2Value))
            {
                values.Add("input." + peekToken1, peekToken2Value);
                tokens.SkipTokens(nameTokenCount + 2);
                return true;
            }

            return false;
        }
    }
}
