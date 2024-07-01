//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    // This class is used to parse a named value token that can be either "true" or "false".
    //
    // Usage from code:
    //
    //    new TrueFalseNamedValueTokenParser("chat.speech.input", "010")
    //
    // Usage from CLI:
    //
    //    --speech true
    //
    // Usage from @FILE:
    //
    //    chat.speech.input=true
    //
    // Resultant dictionary (INamedValues):
    //
    //    chat.speech.input: true
    //
    public class TrueFalseNamedValueTokenParser : NamedValueTokenParser
    {
        public TrueFalseNamedValueTokenParser(string? name, string fullName, string requiredParts, bool defaultValue = true) :
            base(name, fullName, requiredParts, "1;0", "true;false", null, defaultValue ? "true" : "false")
        {
        }

        public TrueFalseNamedValueTokenParser(string fullName, string requiredParts, bool defaultValue = true) :
            base(null, fullName, requiredParts, "1;0", "true;false", null, defaultValue ? "true" : "false")
        {
        }
    }
}
