//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class ProjectNameToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser(bool requireProjectPart = false) => new NamedValueTokenParser(_optionName, _fullName, requireProjectPart ? "010" : "010;001", "1");

        private const string _requiredDisplayName = "project name";
        private const string _optionName = "--project-name";
        private const string _optionExample = "NAME";
        private const string _fullName = "service.project.name";
    }
}
