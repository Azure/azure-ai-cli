//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class InputWildcardToken
    {
        public static NamedValueTokenData Data(string wild) => new NamedValueTokenData(_optionName, _fullName.Replace("*", wild), _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser() => new NamedValueTokenParser(_optionName, _fullName, "11", "1;0", null, null, "!");

        private const string _requiredDisplayName = "template input";
        private const string _optionName = null;
        private const string _optionExample = "NAME=VALUE";
        private const string _fullName = "input.*";
    }
}
