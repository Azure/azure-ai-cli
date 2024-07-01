//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    // Represents a single value that is required, and must be from a given set of valid values.
    //
    // Usage from code:
    //
    //      new RequiredValidValueNamedValueTokenParser(null, "audio.input.type", "011", "file;files;microphone")
    //
    // Usage from CLI:
    //
    //      --input-type file
    //
    // Usage from @FILE:
    //
    //      audio.input.type=file
    //
    // Resultant dictionary (INamedValues):
    //
    //      audio.input.type: file
    //
    public class RequiredValidValueNamedValueTokenParser : NamedValueTokenParser
    {
        public RequiredValidValueNamedValueTokenParser(string? name, string fullName, string requiredParts, string validValues) :
            base(name, fullName, requiredParts, "1", validValues)
        {
        }
    }
}
