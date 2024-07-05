//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class FileSearchTrueFalseToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser() => new TrueFalseNamedValueTokenParser(_optionName, _fullName, "11");

        private const string _requiredDisplayName = "file search";
        private const string _optionName = "--file-search";
        private const string _optionExample = "true|false";
        private const string _fullName = "file.search";
    }
}
