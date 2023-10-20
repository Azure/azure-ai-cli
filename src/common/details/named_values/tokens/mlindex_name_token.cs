//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class MLIndexNameToken
    {
        public static bool IsMLIndexKind(ICommandValues values) => values.GetOrDefault("index.kind", null) == "mlindex";
        public static NamedValueTokenData Data() => SearchIndexNameToken.Data();

        public static INamedValueTokenParser Parser(bool requireIndexPart = true) => new NamedValueTokenParserList(
            new NamedValueTokenParser(null, "index.kind", requireIndexPart ? "11" : "01", "1", "mlindex;sk"),
            new NamedValueTokenParser(_optionName, _fullName, "10", "1", null, "search.index.name", "mlindex", "index.kind")
        );

        private const string _requiredDisplayName = "MLIndex name";
        private const string _optionName = "--mlindex-name";
        private const string _optionExample = "NAME";
        private const string _fullName = "mlindex.name";
    }
}
