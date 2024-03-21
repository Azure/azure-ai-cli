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
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">The short name if you want --shortname</param>
        /// <param name="fullName">The full name of this property.</param>
        /// <param name="requiredParts">A string based bitmap for the fullName. i.e. 101 turns my.property.name into --myname</param>
        /// <param name="valueCount">The number of values accepted from 1 instance of the argument on the command line.</param>
        /// <param name="validValues">a semi-colon deliminated list of possible values</param>
        /// <param name="valueKey">The long name my.property.name of the internal property to set from this argument. If unset fullName is used.</param>
        /// <param name="pinnedValue">The default value, unless pinnedValueKey is non-null, then the value passed to a property with the name whose value is pinnedValueKey</param>
        /// <param name="pinnedValueKey">The name of the property value pinnedValue will be set to.</param>
        public NamedValueTokenParser(
                    string? name, string fullName, string requiredParts,
                    string valueCount, string? validValues = null, string? valueKey = null,
                    string? pinnedValue = null, string? pinnedValueKey = null)
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
            return NamedValueTokenParserHelpers.MatchShortName(ShortName, tokens, values, out nameTokenCount);
        }

        private bool MatchFullName(INamedValueTokens tokens, INamedValues values, string partsRequired, out int nameTokenCount)
        {
            return NamedValueTokenParserHelpers.MatchFullName(FullName, tokens, values, partsRequired, out nameTokenCount);
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
            return NamedValueTokenParserHelpers.ValueMatchesValidValue(ValidValues, peekToken, peekTokenValue, skipAtAt);
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
