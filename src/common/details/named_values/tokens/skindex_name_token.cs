//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class SKIndexNameToken
    {
        public static bool IsSKIndexKind(ICommandValues values) => values.GetOrDefault("search.index.kind", null) == "sk";
        public static NamedValueTokenData Data() => SearchIndexNameToken.Data();

        public static INamedValueTokenParser Parser(bool requireIndexPart = true) => new NamedValueTokenParserList(
            new NamedValueTokenParser(null, "search.index.kind", requireIndexPart ? "011" : "001", "1", "ml;sk"),
            new NamedValueTokenParser("--skindex-name", "skindex.name", "10", "1", null, "search.index.name", "ml", "search.index.kind")
        );
    }
}
