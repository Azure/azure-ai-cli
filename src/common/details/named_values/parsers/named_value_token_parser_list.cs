//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    public class NamedValueTokenParserList : INamedValueTokenParser
    {
        public NamedValueTokenParserList()
        {
            _parsers = new List<INamedValueTokenParser>();
        }

        public NamedValueTokenParserList(IEnumerable<INamedValueTokenParser> parsers)
        {
            _parsers = new List<INamedValueTokenParser>(parsers);
            _parsers.RemoveAll(x => x == null);
        }

        public NamedValueTokenParserList(params INamedValueTokenParser[] parsers)
        {
            _parsers = new List<INamedValueTokenParser>(parsers);
            _parsers.RemoveAll(x => x == null);
        }

        public override bool Parse(INamedValueTokens tokens, INamedValues values)
        {
            foreach (var parser in GetParsers())
            {
                var parsed = parser.Parse(tokens, values);
                if (parsed) return true;
            }

            return false;
        }

        protected virtual IEnumerable<INamedValueTokenParser> GetParsers()
        {
            return _parsers;
        }

        protected void Add(INamedValueTokenParser parser)
        {
            _parsers.Add(parser);
        }

        protected void Remove(Predicate<INamedValueTokenParser> match)
        {
            _parsers.RemoveAll(match);
        }

        static protected string NotRequired(string dottedTokenParts)
        {
            var count = dottedTokenParts.Count(x => x == '.');
            return new string('0', count + 1);
        }

        static protected string Required(string dottedTokenParts)
        {
            var count = dottedTokenParts.Count(x => x == '.');
            return new string('1', count + 1);
        }

        private readonly List<INamedValueTokenParser> _parsers;
    }
}
