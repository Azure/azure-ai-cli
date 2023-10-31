//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class AiServicesApiKeyToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser(bool requireServicesPart = true) => new NamedValueTokenParser(_optionName, _fullName, requireServicesPart ? "1101" : "0101;0011", "1");

        private const string _requiredDisplayName = "api key";
        private const string _optionName = "--ai-services-api-key";
        private const string _optionExample = "KEY";
        private const string _fullName = "ai.services.api.key";
    }
}
