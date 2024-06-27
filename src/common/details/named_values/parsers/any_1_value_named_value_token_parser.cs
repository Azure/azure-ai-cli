//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    // Represents a named value token parser that requires exactly one value.
    // 
    // Usage from code:
    //
    //     new Any1ValueNamedValueTokenParser(null, "chat.assistant.id", "011")
    //
    // Usage from CLI:
    // 
    //    --assistant-id 123
    //
    // Usage from @FILE:
    //
    //    chat.assistant.id=123
    //
    // Resultant dictionary (INamedValues):
    //
    //    chat.assistant.id: 123
    //
    public class Any1ValueNamedValueTokenParser : NamedValueTokenParser
    {
        public Any1ValueNamedValueTokenParser(string? name, string fullName, string requiredParts) :
            base(name, fullName, requiredParts, "1")
        {
        }
    }
}
