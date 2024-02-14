//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class RunCommandScriptToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser() => new NamedValueTokenParser(_optionName, _fullName, "100;010;010", "1");

        private const string _requiredDisplayName = "run shell command/script";
        private const string _optionName = "--script";
        private const string _optionExample = "COMMAND/SCRIPT";
        private const string _fullName = "run.command.script";
    }
}
