//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    // This class represents a single output file name.
    //
    // Usage from code:
    //
    //      new OutputFileNameNamedValueTokenParser(null, "audio.output.file", "110", null, "file", "audio.output.type"),
    //
    // Usage from CLI:
    //
    //      --audio-output test.wav
    //
    // Usage from @FILE:
    //
    //      audio.output.file=test.wav
    //
    // Resultant dictionary (INamedValues):
    //
    //      audio.output.file: test.wav
    //      audio.output.type: file
    //
    public class OutputFileNameNamedValueTokenParser : NamedValueTokenParser
    {
        public OutputFileNameNamedValueTokenParser(string? name, string fullName, string requiredParts) :
            base(name, fullName, requiredParts, "1", "@@")
        {
        }

        public OutputFileNameNamedValueTokenParser(string? name, string fullName, string requiredParts, string? valueKey = null, string? pinnedValue = null, string? pinnedValueKey = null) :
            base(name, fullName, requiredParts, "1", "@@", valueKey, pinnedValue, pinnedValueKey)
        {
        }
    }
}
