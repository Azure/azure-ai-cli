//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class EnvironmentVariablesToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser() => new NamedValueTokenParser(_optionName, _fullName, "11;10", "1", "@;");

        private const string _requiredDisplayName = "environment variables";
        private const string _optionName = "--environment-variables";
        private const string _optionExample = "VAR1=value1;VAR2=value2;VAR3=value3";
        private const string _fullName = "environment.variables";
    }
}
