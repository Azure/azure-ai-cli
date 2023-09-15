//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    class ProjectConnectionKeyToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser() => new NamedValueTokenParser(_optionName, _fullName, "0011", "1");

        private const string _requiredDisplayName = "connection key";
        private const string _optionName = "--connection-key";
        private const string _optionExample = "KEY";
        private const string _fullName = "service.project.connection.key";
    }
}
