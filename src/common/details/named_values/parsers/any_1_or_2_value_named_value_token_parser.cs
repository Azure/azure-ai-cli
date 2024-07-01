//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    // This class is used to parse a named value token that can have 1 or 2 values.
    //
    // Usage from code:
    //
    //      new Any1or2ValueNamedValueTokenParser("--pattern", "intent.pattern", "01;10")
    //
    // Usage from CLI:
    //
    //    --pattern NAME=PATTERN
    //    --pattern NAME PATTERN
    //
    // Usage from @FILE:
    //
    //    intent.pattern=NAME=PATTERN
    //
    // Resultant dictionary (INamedValues):
    //
    //    intent.pattern: NAME=PATTERN
    //
    public class Any1or2ValueNamedValueTokenParser : NamedValueTokenParser
    {
        public Any1or2ValueNamedValueTokenParser(string? name, string fullName, string requiredParts) :
            base(name, fullName, requiredParts, "2;1")
        {
        }

        public Any1or2ValueNamedValueTokenParser(string? name, string fullName, string requiredParts, string? valueKey = null, string? pinnedValue = null, string? pinnedValueKey = null) :
            base(null, fullName, requiredParts, "2;1", null, valueKey, pinnedValue, pinnedValueKey)
        {
        }
    }
}
