//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class InputWildcardToken
    {
        public static INamedValueTokenParser Parser() => new InputWildcardTokenParser();

        public static NamedValueTokenData Data(string wild) => new NamedValueTokenData(_optionName, _fullName.Replace("*", wild), _optionExample, _requiredDisplayName);

        public static IEnumerable<string> GetNames(ICommandValues values)
        {
            return values.Names
                .Where(x => x.StartsWith("input."))
                .Select(key =>
                {
                    var name = key.Substring("input.".Length);
                    var valueInName = values[key] == "!";
                    if (valueInName)
                    {
                        StringHelpers.SplitNameValue(name, out name, out string value);
                    }
                    return name;
                }).ToList();
        }

        private const string _requiredDisplayName = "template input";
        private const string _optionName = null;
        private const string _optionExample = "NAME=VALUE";
        private const string _fullName = "input.*";
    }
}
