//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class InputDataFileToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser() => new NamedValueTokenParserList(
            new NamedValueTokenParser(_optionName, _fullName, "110;001", "1"),
            new NamedValueTokenParser("--files", $"{_fullName}s", "110;001", "1", null, null, _fullName, "x.command.expand.file.name")
        );

        private const string _requiredDisplayName = "input data file";
        private const string _optionName = "--input-data";
        private const string _optionExample = "NAME";
        private const string _fullName = "input.data.file";
    }
}
