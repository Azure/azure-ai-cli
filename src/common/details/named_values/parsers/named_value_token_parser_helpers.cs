//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class NamedValueTokenParserHelpers
    {
        public static bool MatchShortName(string? shortName, INamedValueTokens tokens, INamedValues values, out int nameTokenCount)
        {
            nameTokenCount = 0;

            var nameToken = tokens.PeekNextToken(0);
            if (nameToken != null && !nameToken.StartsWith(tokens.NamePrefixRequired())) return false;

            var tokenOk = nameToken != null && nameToken.Equals(shortName, StringComparison.InvariantCultureIgnoreCase);
            if (tokenOk)
            {
                nameTokenCount = 1;
            }

            return nameTokenCount > 0;
        }

        public static bool MatchFullName(string fullName, INamedValueTokens tokens, INamedValues values, string partsRequired, out int nameTokenCount)
        {
            nameTokenCount = 0;

            // Break the "FullName" into parts, some of which will be required, some of which are optional
            var prefix = tokens.NamePrefixRequired();
            var fullNameParts = fullName.Split('.');
            var fullNamePartsOk = fullNameParts.Length == partsRequired.Length;

            // Break the first token into parts, so we can start checking it
            var iPeekToken = 0;
            var token = tokens.PeekNextToken(iPeekToken);
            var tokenParts = token != null ? new List<string>(token.Split('.')) : new List<string>();
            var iTokenPart = 0;

            // For each FullName "part", check to see if it matches the next token "part" and check if that FullName part was required or not
            for (int iNamePart = 0; fullNamePartsOk && iNamePart < fullNameParts.Length; iNamePart++)
            {
                // Do they match?
                var tokenPart = tokenParts.Count > iTokenPart ? tokenParts[iTokenPart] : null;
                var fullNamePart = prefix + fullNameParts[iNamePart];
                var fullNamePartWild = fullNamePart == "*";
                var partsMatch = tokenPart != null && (fullNamePartWild || tokenPart.Equals(fullNamePart, StringComparison.InvariantCultureIgnoreCase));

                // If so, we'll increment the number of parts we've matched (which is also tracking the next token to use)
                if (partsMatch)
                {
                    iTokenPart += 1;
                    prefix = "";
                }

                // If we matched as a wildcard, keep track of the wildcard value
                if (partsMatch && fullNamePartWild)
                {
                    SetWild(values, fullNamePart, tokenPart);
                }

                // Check to see if they either matched, or if the name part was optional
                var namePartOptional = iNamePart < partsRequired.Length && partsRequired[iNamePart] == '0';
                var thisNamePartOk = partsMatch || namePartOptional;
                fullNamePartsOk = thisNamePartOk;

                if (!fullNamePartsOk) break; // didn't match... we're outta here...

                if (partsMatch && iTokenPart == tokenParts.Count) // fully matched all the token parts, skip to the next token
                {
                    iPeekToken += 1;
                    token = tokens.PeekNextToken(iPeekToken);
                    tokenParts = token != null ? new List<string>(token.Split('.')) : new List<string>();
                    iTokenPart = 0;
                }
            }

            if (fullNamePartsOk && iPeekToken > 0)
            {
                nameTokenCount = iPeekToken;
                return true;
            }

            return false;
        }

        public static bool ValueMatchesValidValue(string? validValues, string? peekToken, string? peekTokenValue, bool skipAtAt = false)
        {
            if (peekToken == null) return false;
            if (peekTokenValue == null) return false;
            if (validValues == null) return true;

            bool checkAtAt = validValues == "@@";
            if (checkAtAt && skipAtAt) return false;

            bool atAtOk = !string.IsNullOrEmpty(peekToken);
            if (checkAtAt) return atAtOk;

            bool checkAt = validValues.Length <= 3 && validValues.Contains("@");
            bool atOk = peekToken.StartsWith("@") || (peekToken.StartsWith("=@") && !peekTokenValue.StartsWith("=@"));
            if (checkAt && atOk) return true;

            bool checkSemi = validValues.Length <= 3 && validValues.Contains(";");
            bool semiOk = peekToken.Contains(";");
            if (checkSemi && semiOk) return true;

            bool checkTab = validValues.Length <= 3 && validValues.Contains("\t");
            bool semiTab = peekToken.Contains("\t");
            if (checkTab && semiTab) return true;

            foreach (var validValue in validValues.Split(';'))
            {
                if (peekTokenValue == validValue) return true;
            }

            return false;
        }

        private static void SetWild(INamedValues values, string fullNamePart, string? tokenPart)
        {
            values.Reset(fullNamePart);
            values.Add(fullNamePart, tokenPart);
        }
    }
}
