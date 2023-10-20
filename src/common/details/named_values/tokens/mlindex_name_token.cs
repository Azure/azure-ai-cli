//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class MLIndexNameToken
    {
        public static bool IsMLIndexCreateKind(ICommandValues values) => values.GetOrDefault("index.create.kind", null) == "MLIndex";
        public static NamedValueTokenData Data() => SearchIndexNameToken.Data();

        public static INamedValueTokenParser Parser() => new NamedValueTokenParserList(
            new NamedValueTokenParser(null, "index.create.kind", "111", "1", "MLIndex;SK"),
            new NamedValueTokenParser(_optionName, _fullName, "10", "1", null, "search.index.name", "MLIndex", "index.create.kind")
        );

        private const string _requiredDisplayName = "MLIndex name";
        private const string _optionName = "--mlindex-name";
        private const string _optionExample = "NAME";
        private const string _fullName = "mlindex.name";
    }
}
