//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class SearchIndexNameToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser(bool requireIndexPart = true) => new Any1ValueNamedValueTokenParser(_optionName, _fullName, requireIndexPart ? "010" : "010;001");

        private const string _requiredDisplayName = "index name";
        private const string _optionName = "--index-name";
        private const string _optionExample = "NAME";
        private const string _fullName = "search.index.name";
    }
}
