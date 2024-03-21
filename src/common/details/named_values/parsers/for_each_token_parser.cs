//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Azure.AI.Details.Common.CLI
{
    class ForEachTokenParser : INamedValueTokenParser
    {
        public override bool Parse(INamedValueTokens tokens, INamedValues values)
        {
            var prefix = tokens.NamePrefixRequired();

            var token = tokens.PeekNextToken();
            if (token != prefix + "foreach") return false;

            int skip = 1;
            token = tokens.PeekNextToken(skip++);

            int index = values.GetOrDefault("foreach.count", 0);
            if (token == "count+")
            {
                return ParseTsvCountPlus1(tokens, values, index);
            }
            else if (token == "count")
            {
                return ParseTsvCount(tokens, values);
            }
            else if (token == "{count}")
            {
                return ParseIndexedTsvFile(tokens, values, index);
            }
            else if (token != null && token.Where(x => char.IsDigit(x)).Count() == token.Length)
            {
                return ParseIndexedTsvFile(tokens, values, int.Parse(token));
            }

            return ParseNonIndexedTsvFile(tokens, values, token, skip, index);
        }

        private static bool ParseTsvCount(INamedValueTokens tokens, INamedValues values)
        {
            // foreach.count=N

            int skip = 2;
            var value = tokens.PeekNextTokenValue(skip++, values);
            if (value == null) return false;

            tokens.SkipTokens(skip);
            values.Add($"foreach.count", value);

            return true;
        }

        private static bool ParseTsvCountPlus1(INamedValueTokens tokens, INamedValues values, int index)
        {
            // foreach.count+=1

            int skip = 2;
            var value = tokens.PeekNextTokenValue(skip++, values);
            if (value == null || value != "1") return false;

            tokens.SkipTokens(skip);
            values.Reset("foreach.count", (index + 1).ToString());

            return true;
        }


        private static bool ParseIndexedTsvFile(INamedValueTokens tokens, INamedValues values, int index)
        {
            // foreach.0.tsv.file.*

            int skip = 2;
            var token = tokens.PeekNextToken(skip++);
            if (token != "tsv") return false;

            token = tokens.PeekNextToken(skip++);
            if (token != "file") return false;

            token = tokens.PeekNextToken(skip++);
            if (token == "has" || token == "skip") // foreach.0.tsv.file.has.header=true|false
            {
                var hasOrSkip = token;

                token = tokens.PeekNextToken(skip++);
                if (token != "header") return false;

                var value = tokens.PeekNextTokenValue(skip++, values);
                if (value != "true" && value != "false") return false;

                tokens.SkipTokens(skip);
                values.Add($"foreach.{index}.tsv.file.{hasOrSkip}.header", value);

                return true;
            }
            else if (token == "columns") // foreach.0.tsv.file.columns=COL(\tCOL2)+
            {
                var value = tokens.PeekNextTokenValue(skip++, values);
                if (value == null) return false;

                tokens.SkipTokens(skip);
                values.Add($"foreach.{index}.tsv.file.columns", value);

                return true;
            }
            else if (token != null) // foreach.0.tsv.file=(@FILE)|((VAL(\tVAL)+)(\nVAL(\tVAL)+)+)
            {
                var value = tokens.ValueFromToken(token);

                tokens.SkipTokens(skip);
                values.Add($"foreach.{index}.tsv.file", value);

                return true;
            }

            return false;
        }

        private static bool ParseNonIndexedTsvFile(INamedValueTokens tokens, INamedValues values, string? token, int skip, int index)
        {
            // --foreach [tsv] @COL-TSV-FILE [skip header] in @TSV-FILE
            // --foreach [tsv] [columns] "COL1;COL2" [skip header] in @TSV-FILE

            if (token == "tsv") token = tokens.PeekNextToken(skip++);
            if (token == "columns") token = tokens.PeekNextToken(skip++);

            var skipHeader = false;
            if (tokens.PeekNextTokenValue(skip, values) == "skip" && tokens.PeekNextTokenValue(skip + 1, values) == "header")
            {
                skipHeader = true;
                skip += 2;
            }

            var columns = "";
            if (tokens.PeekNextTokenValue(skip, values) == "in")
            {
                skip++;
                columns = tokens.ValueFromToken(token);
                token = tokens.PeekNextToken(skip++);
            }
            else if (token == "in")
            {
                token = tokens.PeekNextToken(skip++);
            }

            if (token == null) return false;

            var hasHeader = string.IsNullOrEmpty(columns) || skipHeader;

            var atFile = token.StartsWith("@");
            var isList = !atFile && token.Contains(";");
            var isFile = !atFile && !isList && FileHelpers.FileExistsInDataPath(token, values);
            if (atFile || isList || isFile)
            {
                var value = isFile
                    ? FileHelpers.ReadAllText(token, Encoding.Default)
                    : tokens.ValueFromToken(token)!;
                if (isList)
                {
                    hasHeader = false;
                    value = value.Replace(";", "\n");
                }

                tokens.SkipTokens(skip);
                values.Add($"foreach.{index}.tsv.file", value);
                values.Add($"foreach.{index}.tsv.file.has.header", hasHeader ? "true" : "false");
                if (!string.IsNullOrEmpty(columns)) values.Add($"foreach.{index}.tsv.file.columns", columns);

                index++;
                values.Reset("foreach.count", index.ToString());

                return true;
            }

            return false;
        }
    }
}
