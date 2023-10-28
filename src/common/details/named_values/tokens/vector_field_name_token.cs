//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class VectorFieldNameToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser() => new NamedValueTokenParser(_optionName, _fullName, "110", "1");

        private const string _requiredDisplayName = "vector field name";
        private const string _optionName = "--vector-field";
        private const string _optionExample = "NAME";
        private const string _fullName = "vector.field.name";
    }
}
