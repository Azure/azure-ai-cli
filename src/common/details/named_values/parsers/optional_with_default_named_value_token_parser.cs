//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    // This class represents an optional single value named value token parser, when, not specified,
    // defaults to that specified in the constructor
    //
    // Usage from code:
    //
    //      new OptionalWithDefaultNamedValueTokenParser(null, "csr.wait.timeout", "010", "864000000")
    //
    // Usage from CLI:
    //
    //      --wait 30000
    //
    // Usage from @FILE:
    //
    //      csr.wait.timeout=30000
    //
    // Resultant dictionary (INamedValues):
    //
    //      csr.wait.timeout: 30000
    //
    public class OptionalWithDefaultNamedValueTokenParser : NamedValueTokenParser
    {
        public OptionalWithDefaultNamedValueTokenParser(string? name, string fullName, string requiredParts, string defaultValue) :
            base(null, fullName, requiredParts, "1;0", null, null, defaultValue)
        {
        }

        public OptionalWithDefaultNamedValueTokenParser(string? name, string fullName, string requiredParts, string defaultValue, string pinnedValueKey) :
            base(null, fullName, requiredParts, "1;0", null, null, defaultValue, pinnedValueKey)
        {
        }
    }
}
