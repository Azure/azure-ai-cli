//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    // This class is used to parse a named value token that can be a file or a list of values
    // In addition, it can have a pinned value w/ value key
    //
    // Usage from code:
    //
    //      new AtFileOrListWithPinnedValueNamedValueTokenParser(null, "check.sr.transcript.text.in", "10011", "check.sr.transcript.text.in", "true", "output.all.recognizer.recognized.result.text")
    //
    // Usage from CLI:
    //
    //    --check-text-in Zac;Nic;Jac;Bec
    //    --check-text-in @FILE
    //
    // Usage from @FILE:
    //
    //    check.sr.transcript.text.in=Zac;Nic;Jac;Bec
    //
    // Resultant dictionary (INamedValues):
    //
    //    check.sr.transcript.text.in: Zac;Nic;Jac;Bec
    //    output.all.recognizer.recognized.result.text: true
    //
    public class AtFileOrListWithPinnedValueNamedValueTokenParser : NamedValueTokenParser
    {
        public AtFileOrListWithPinnedValueNamedValueTokenParser(string? name, string fullName, string requiredParts, string valueKey, string pinnedValue, string pinnedValueKey) :
            base(name, fullName, requiredParts, "1", "@;", valueKey, pinnedValue, pinnedValueKey)
        {
        }
    }
}
