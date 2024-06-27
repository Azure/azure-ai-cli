//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    // Represents a single option that gets remapped to a specific pinned value, possibly at pinnedValueKey
    //
    // Usage from code:
    // 
    //      new PinnedNamedValueTokenParser("--py", "programming.language.python", "001", "Python", "programming.language")
    //
    // Usage from CLI:
    //
    //      --python
    //
    // Usage from @FILE:
    //
    //      programming.language=Python
    //
    // Resultant dictionary (INamedValues):
    //
    //      programming.language: Python
    //
    public class PinnedNamedValueTokenParser : NamedValueTokenParser
    {
        public PinnedNamedValueTokenParser(string? name, string fullName, string requiredParts, string pinnedValue) :
            base(name, fullName, requiredParts, "0", null, null, pinnedValue)
        {
        }

        public PinnedNamedValueTokenParser(string? name, string fullName, string requiredParts, string pinnedValue, string pinnedValueKey) :
            base(name, fullName, requiredParts, "0", null, null, pinnedValue, pinnedValueKey)
        {
        }
    }
}
