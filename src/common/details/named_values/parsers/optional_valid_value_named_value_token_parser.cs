//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class OptionalValidValueNamedValueTokenParser : NamedValueTokenParser
    {
        public OptionalValidValueNamedValueTokenParser(string? name, string fullName, string requiredParts, string validValues) :
            base(name, fullName, requiredParts, "1;0", validValues)
        {
        }
    }
}
