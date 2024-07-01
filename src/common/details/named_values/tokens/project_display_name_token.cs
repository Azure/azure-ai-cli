//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class ProjectDisplayNameToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser() => new Any1ValueNamedValueTokenParser(_optionName, _fullName, "0011");

        private const string _requiredDisplayName = "display name";
        private const string _optionName = "--display-name";
        private const string _optionExample = "NAME";
        private const string _fullName = "service.project.display.name";
    }
}
