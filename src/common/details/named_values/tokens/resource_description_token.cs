//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    class ResourceDescriptionToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser() => new NamedValueTokenParser(_optionName, _fullName, "001", "1");

        private const string _requiredDisplayName = "description";
        private const string _optionName = "--description";
        private const string _optionExample = "DESCRIPTION";
        private const string _fullName = "service.resource.description";
    }
}
