//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    class ReplaceForEachTokenParser : INamedValueTokenParser
    {
        public override bool Parse(INamedValueTokens tokens, INamedValues values)
        {
            var prefix = tokens.NamePrefixRequired();
            if (string.IsNullOrEmpty(prefix)) return false;

            var skip = 0;
            var token0 = tokens.PeekNextToken(skip++);
            if (token0 == prefix + "replace")
            {
                var token1 = tokens.PeekNextToken(skip++);
                if (token1 != "foreach") return false;
            }
            else if (token0 != prefix + "replace.foreach")
            {
                return false;
            }

            // token   0       1    2  3     4         5 
            // --replace-foreach NAME in files (PATTERN)

            // token   0       1    2  3                   4
            // --replace-foreach NAME in (@FILE | semi-list)

            var name = tokens.PeekNextToken(skip++);
            var token3 = tokens.PeekNextToken(skip++);
            if (token3 != "in") return false;

            var token4 = tokens.PeekNextToken(skip++);
            var token5 = tokens.PeekNextToken(skip++);

            var delegateTokens = token4 == "files" && !string.IsNullOrEmpty(token5)
                ? ParseReplaceForEachInFiles(tokens, values, name, skip -= 1)
                : ParseReplaceForEachInSemiListOrAtFile(tokens, values, name, skip -= 2);
            var forEachTokenSource = new CmdLineTokenSource(delegateTokens.ToArray(), values);

            var parsed = _forEachTokenParser.Parse(forEachTokenSource, values);
            if (parsed) tokens.SkipTokens(skip + 1);
            return parsed;
        }

        private IEnumerable<string> ParseReplaceForEachInFiles(INamedValueTokens tokens, INamedValues values, string name, int skip)
        {
            yield return "--foreach";
            yield return $"replace.{name}";
            yield return "in";

            var pattern = tokens.PeekNextToken(skip);
            var found = FileHelpers.FindFiles(pattern, values).ToList();

            yield return string.Join(";", found);
        }

        private IEnumerable<string> ParseReplaceForEachInSemiListOrAtFile(INamedValueTokens tokens, INamedValues values, string name, int v)
        {
            yield return "--foreach";
            yield return $"replace.{name}";
            yield return "in";
            yield return tokens.PeekNextToken(v);
        }

        private ForEachTokenParser _forEachTokenParser = new();
    }
}
