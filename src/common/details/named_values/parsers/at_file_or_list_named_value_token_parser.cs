//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    // This class is used to parse a named value token that can be either an @FILE or a list of values.
    //
    // Usage from code:
    //
    //      new AtFileOrListNamedValueTokenParser("--phrases", "grammar.phrase.list", "011")
    //
    // Usage from CLI:
    //
    //    --phrases @FILE
    //    --phrases PHRASE1;PHRASE2;...
    //
    // Usage from @FILE:
    //
    //    grammar.phrase.list=PHRASE1;PHRASE2;...
    //
    // Resultant dictionary (INamedValues):
    //
    //    grammar.phrase.list: PHRASE1;PHRASE2;...
    //
    public class AtFileOrListNamedValueTokenParser : NamedValueTokenParser
    {
        public AtFileOrListNamedValueTokenParser(string? name, string fullName, string requiredParts) :
            base(name, fullName, requiredParts, "1", "@;")
        {
        }
    }
}
