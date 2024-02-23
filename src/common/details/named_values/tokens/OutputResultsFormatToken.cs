//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class OutputResultsFormatToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser() => new NamedValueTokenParser(_optionName, _fullName, "011;101", "1", "trx;junit");

        private const string _requiredDisplayName = "output results format";
        private const string _optionName = "--results-format";
        private const string _optionExample = "trx|junit";
        private const string _fullName = "output.results.format";
    }
}
