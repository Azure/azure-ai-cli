//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    // This class is used to parse a named value token that requires exactly one value and has a pinned value.
    //
    // Usage from code:
    //
    //      new Any1PinnedNamedValueTokenParser(null, "audio.input.file", "001", "file", "audio.input.type"),
    //
    // Usage from CLI:
    //
    //    --file FILENAME
    //
    // Usage from @FILE:
    //
    //    audio.input.file=FILENAME
    //
    // Resultant dictionary (INamedValues):
    //
    //    audio.input.file: FILENAME
    //    audio.input.type: file
    //
    public class Any1PinnedNamedValueTokenParser : NamedValueTokenParser
    {
        public Any1PinnedNamedValueTokenParser(string? name, string fullName, string requiredParts, string pinnedValue, string pinnedValueKey) :
            base(name, fullName, requiredParts, "1", null, null, pinnedValue, pinnedValueKey)
        {
        }
    }
}
