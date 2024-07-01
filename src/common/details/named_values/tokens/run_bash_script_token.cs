//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class RunBashScriptToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser() => new Any1ValueNamedValueTokenParser(_optionName, _fullName, "010");

        private const string _requiredDisplayName = "run bash script";
        private const string _optionName = "--bash";
        private const string _optionExample = "BASH SCRIPT";
        private const string _fullName = "run.bash.script";
    }
}
