//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    // This class represents the ability to "expand" a file name/pattern into a list of files
    // that are processed individually as if the CLI was invoked multiple times.
    //
    // Usage from the code:
    //
    //    new ExpandFileNameNamedValueTokenParser()
    //    new ExpandFileNameNamedValueTokenParser("--files", "assistant.upload.files", "001", "assistant.upload.file")
    //
    // Usage from the CLI:
    //
    //   --files *.md
    //
    // Resultant dictionary (INamedValues, pre-expansion):
    //
    //   assistant.upload.files: *.md
    //   x.command.expand.file.name: assistant.upload.file
    //
    public class ExpandFileNameNamedValueTokenParser : NamedValueTokenParser
    {
        public ExpandFileNameNamedValueTokenParser() :
            base(null, "x.command.expand.file.name", "11111", "1")
        {
        }

        public ExpandFileNameNamedValueTokenParser(string? name, string fullName, string requiredParts, string pinnedValue) :
            base(name, fullName, requiredParts, "1", null, null, pinnedValue, "x.command.expand.file.name")
        {
        }
    }
}
