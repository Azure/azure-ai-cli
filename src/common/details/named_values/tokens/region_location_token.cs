//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class RegionLocationToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser() => new NamedValueTokenParser(_optionName, _fullName, "010;001", "1");

        private const string _requiredDisplayName = "location";
        private const string _optionName = "--location";
        private const string _optionExample = "LOCATION";
        private const string _fullName = "service.region.location";
    }
}
