//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;

namespace Azure.AI.Details.Common.CLI
{
    public abstract class INamedValueTokenParser
    {
        public abstract bool Parse(INamedValueTokens tokens, INamedValues values);
    }

    public class NamedValueTokenParser : INamedValueTokenParser
    {
        public NamedValueTokenParser(
                    string name, string fullName, string requiredParts,
                    string valueCount, string validValues = null, string valueKey = null,
                    string pinnedValue = null, string pinnedValueKey = null)
        {
            ShortName = name;
            FullName = fullName;
            RequiredParts = requiredParts;

            ValueCount = valueCount;
            ValidValues = validValues;
            ValueKey = valueKey != null ? valueKey : FullName;

            PinnedValue = pinnedValue;
            PinnedValueKey = pinnedValueKey;
        }

        public override bool Parse(INamedValueTokens tokens, INamedValues values)
        {
            bool parsed = false;
            foreach (var partsRequired in RequiredParts.Split(';'))
            {
                parsed = ParseNameAndValues(tokens, values, partsRequired);
                if (parsed) break;
            }
            return parsed;
        }

        public static string NotRequired(string dottedTokenParts)
        {
            var count = dottedTokenParts.Count(x => x == '.');
            return new string('0', count + 1);
        }

        public static string Required(string dottedTokenParts)
        {
            var count = dottedTokenParts.Count(x => x == '.');
            return new string('1', count + 1);
        }

        #region private methods

        private bool ParseNameAndValues(INamedValueTokens tokens, INamedValues values, string partsRequired)
        {
            var nameTokensMatched = 0;
            if (MatchShortName(tokens, values, out nameTokensMatched) ||
                MatchFullName(tokens, values, partsRequired, out nameTokensMatched))
            {
                if (ParseValues(tokens, values, nameTokensMatched))
                {
                    ResetWild(values);
                    ResetError(values);
                    return true;
                }

                ResetWild(values);
                return ParseValueError(tokens, values, nameTokensMatched);
            }

            return false;
        }

        private bool MatchShortName(INamedValueTokens tokens, INamedValues values, out int nameTokenCount)
        {
            nameTokenCount = 0;

            var nameToken = tokens.PeekNextToken(0);
            if (nameToken != null && !nameToken.StartsWith(tokens.NamePrefixRequired())) return false;

            var tokenOk = nameToken != null && nameToken.Equals(ShortName, StringComparison.InvariantCultureIgnoreCase);
            if (tokenOk)
            {
                nameTokenCount = 1;
            }

            return nameTokenCount > 0;
        }

        private bool MatchFullName(INamedValueTokens tokens, INamedValues values, string partsRequired, out int nameTokenCount)
        {
            nameTokenCount = 0;

            // Break the "FullName" into parts, some of which will be required, some of which are optional
            var prefix = tokens.NamePrefixRequired();
            var fullNameParts = FullName.Split('.');
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

            if (fullNamePartsOk)
            {
                nameTokenCount = iPeekToken;
                return true;
            }

            return false;
        }

         private bool ParseValues(INamedValueTokens tokens, INamedValues values, int skipNameTokens)
        {
            foreach (var valueCount in ValueCount.Split(';'))
            {
                bool parsed = ParseValues(tokens, values, int.Parse(valueCount), skipNameTokens);
                if (parsed) return true;
            }
            return false;
        }

        private bool ParseValues(INamedValueTokens tokens, INamedValues values, int count, int skipNameTokens)
        {
            bool parsed = false;

            var peekToken1 = tokens.PeekNextToken(skipNameTokens + 0);
            var peekToken2 = tokens.PeekNextToken(skipNameTokens + 1);

            var peekToken1Value = tokens.ValueFromToken(peekToken1, values);
            var peekToken2Value = tokens.ValueFromToken(peekToken2, values);

            if (PinnedValueKey == null && PinnedValue == null)
            {
                if (count == 0)
                {
                    tokens.SkipTokens(skipNameTokens);
                    parsed = true;
                }
                else if (count == 1 && ValueMatchesValidValue(peekToken1, peekToken1Value, true))
                {
                    tokens.SkipTokens(skipNameTokens);
                    parsed = AddValue(values, ValueKey, tokens.PopNextTokenValue(values));
                }
                else if (count == 1 && ValueMatchesValidValue(peekToken1, peekToken1Value, false))
                {
                    tokens.SkipTokens(skipNameTokens);
                    parsed = AddValue(values, ValueKey, tokens.PopNextToken().TrimStart('='));
                }
                else if (count == 2 && ValueMatchesValidValue(peekToken1, peekToken1Value) && peekToken2Value != null)
                {
                    tokens.SkipTokens(skipNameTokens);

                    var value = tokens.PopNextTokenValue(values);
                    value += "=" + tokens.PopNextTokenValue(values);

                    parsed = AddValue(values, ValueKey, value);
                }
            }
            else if (PinnedValueKey == null && PinnedValue != null)
            {
                if (count == 0)
                {
                    tokens.SkipTokens(skipNameTokens);
                    parsed = AddValue(values, ValueKey, PinnedValue);
                }
                else if (count == 1 && ValueMatchesValidValue(peekToken1, peekToken1Value, true))
                {
                    tokens.SkipTokens(skipNameTokens);
                    parsed = AddValue(values, ValueKey, tokens.PopNextTokenValue(values));
                }
                else if (count == 1 && ValueMatchesValidValue(peekToken1, peekToken1Value, false))
                {
                    tokens.SkipTokens(skipNameTokens);
                    parsed = AddValue(values, ValueKey, tokens.PopNextToken().TrimStart('='));
                }
            }
            else // if (PinnedValueKey != null && PinnedValue != null)
            {
                if (count == 0)
                {
                    tokens.SkipTokens(skipNameTokens);
                    parsed = AddValue(values, PinnedValueKey, PinnedValue);
                }
                else if (count == 1 && ValueMatchesValidValue(peekToken1, peekToken1Value, true))
                {
                    tokens.SkipTokens(skipNameTokens);

                    parsed = AddValue(values, ValueKey, tokens.PopNextTokenValue(values));
                    parsed = parsed && AddValue(values, PinnedValueKey, PinnedValue);
                }
                else if (count == 1 && ValueMatchesValidValue(peekToken1, peekToken1Value, false))
                {
                    tokens.SkipTokens(skipNameTokens);

                    parsed = AddValue(values, ValueKey, tokens.PopNextToken().TrimStart('='));
                    parsed = parsed && AddValue(values, PinnedValueKey, PinnedValue);
                }
                else if (count == 2 && ValueMatchesValidValue(peekToken1, peekToken1Value) && peekToken2Value != null)
                {
                    tokens.SkipTokens(skipNameTokens);

                    var value = tokens.PopNextTokenValue(values);
                    value += "=" + tokens.PopNextTokenValue(values);

                    parsed = AddValue(values, ValueKey, value.TrimStart('='));
                    parsed = parsed && AddValue(values, PinnedValueKey, PinnedValue);
                }
            }

            return parsed;
        }

        private bool ValueMatchesValidValue(string peekToken, string peekTokenValue, bool skipAtAt = false)
        {
            if (peekToken == null) return false;
            if (peekTokenValue == null) return false;
            if (ValidValues == null) return true;

            bool checkAtAt = ValidValues == "@@";
            if (checkAtAt && skipAtAt) return false;

            bool atAtOk = !string.IsNullOrEmpty(peekToken);
            if (checkAtAt) return atAtOk;

            bool checkAt = ValidValues.Length <= 3 && ValidValues.Contains("@");
            bool atOk = peekToken.StartsWith("@") || (peekToken.StartsWith("=@") && !peekTokenValue.StartsWith("=@"));
            if (checkAt && atOk) return true;

            bool checkSemi = ValidValues.Length <= 3 && ValidValues.Contains(";");
            bool semiOk = peekToken.Contains(";");
            if (checkSemi && semiOk) return true;

            bool checkTab = ValidValues.Length <= 3 && ValidValues.Contains("\t");
            bool semiTab = peekToken.Contains("\t");
            if (checkTab && semiTab) return true;

            foreach (var validValue in ValidValues.Split(';'))
            {
                if (peekTokenValue == validValue) return true;
            }

            return false;
        }

        private void SetWild(INamedValues values, string fullNamePart, string tokenPart)
        {
            values.Reset(fullNamePart);
            values.Add(fullNamePart, tokenPart);
        }

        private void ResetWild(INamedValues values)
        {
            values.Reset("*");
        }

        private void ResetError(INamedValues values)
        {
            values.Reset("error");
        }

        private bool ParseValueError(INamedValueTokens tokens, INamedValues values, int nameTokenCount)
        {
            var nameTokens = tokens.PeekAllTokens(nameTokenCount);
            var allTokens = tokens.PeekAllTokens();

            var expected = "0 values";
            if (ValueCount != null)
            {
                switch (ValueCount)
                {
                    case "2;1": expected = "1-2 values"; break;
                    case "1;0": expected = "0-1 values"; break;
                    case "1": expected = "1 value"; break;
                    default: expected = $"{ValueCount} values"; break;
                }
            }

            if (ValidValues == "@") expected = "@FILE value";
            if (ValidValues == "@@") expected = "@FILE output value";
            if (ValidValues == ";") expected = "semi-colon delimited list of values";
            if (ValidValues == "@;" || ValidValues == ";@") expected = "@FILE or semi-colon delimited list of values";

            var error = $"Expected {expected} after \"{nameTokens}\" (in \"{allTokens}\")";
            AddValue(values, "error", error);

            return false;
        }

        private bool AddValue(INamedValues values, string key, string value)
        {
            if (key.Contains(".*") && values.Contains("*"))
            {
                key = key.Replace(".*", "." + values["*"]);
            }

            bool added = false;
            bool allowOverrides = true;
            if (!values.Contains(key, false))
            {
                values.Add(key, value);
                added = true;
            }
            else if (values[key] == value)
            {
                added = true;
            }
            else if (allowOverrides)
            {
                values.Reset(key, value);
                added = true;
            }
            else if (key != "error")
            {
                var error = $"ERROR: Cannot set \"{key}\" to \"{value}\"; already set to \"{values[key]}\"";
                AddValue(values, "error", error);
                throw new Exception(values["error"]);
            }
            return added;
        }

        #endregion

        #region protected / private data

        protected string ShortName; // e.g. "--file"
        protected string FullName; // e.g. "audio.input.push.stream.file"
        protected string RequiredParts; // e.g. "00100"

        protected string ValueCount; // e.g. "0" or "1" or "2" or "2;1" or ...
        protected string ValidValues; // e.g. "file;blob;microphone"
        protected string ValueKey; // e.g. "audio.input.type"

        protected string PinnedValue;
        protected string PinnedValueKey;

        #endregion
    }
}
