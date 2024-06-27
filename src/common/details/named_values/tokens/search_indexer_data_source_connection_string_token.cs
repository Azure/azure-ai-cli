//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class SearchIndexerDataSourceConnectionStringToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser() => new Any1ValueNamedValueTokenParser(_optionName, _fullName, "000011");

        private const string _requiredDisplayName = "data source connection string";
        private const string _optionName = "--data-source-connection-string";
        private const string _optionExample = "CONNECTION_STRING";
        private const string _fullName = "search.indexer.data.source.connection.string";
    }
}
