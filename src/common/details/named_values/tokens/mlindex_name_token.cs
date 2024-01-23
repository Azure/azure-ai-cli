//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class MLIndexNameToken
    {
        public static bool IsMLIndexKind(ICommandValues values) => values.GetOrDefault("search.index.kind", null) == "ml";
        public static NamedValueTokenData Data() => SearchIndexNameToken.Data();

        public static INamedValueTokenParser Parser(bool requireIndexPart = true) => new NamedValueTokenParserList(
            new NamedValueTokenParser(null, "search.index.kind", requireIndexPart ? "011" : "001", "1", "ml;sk"),
            new NamedValueTokenParser("--mlindex-name", "mlindex.name", "10", "1", null, "search.index.name", "ml", "search.index.kind")
        );
    }
}
