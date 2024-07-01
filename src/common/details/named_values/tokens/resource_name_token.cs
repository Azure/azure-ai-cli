//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class ResourceNameToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser(bool requireResourcePart = false) => new Any1ValueNamedValueTokenParser(_optionName, _fullName, requireResourcePart ? "010" : "010;001");

        private const string _requiredDisplayName = "resource name";
        private const string _optionName = "--resource-name";
        private const string _optionExample = "NAME";
        private const string _fullName = "service.resource.name";
    }
}
