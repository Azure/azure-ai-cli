//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class TestsOptionXToken
    {
        public static INamedValueTokenParser Parser() => new OptionXTokenParser(_optionName, _fullName, "1");

        public static string GetOption(ICommandValues values, int x) => values.GetOrDefault(_fullName + x, null);

        public static IEnumerable<string> GetOptions(ICommandValues values)
        {
            for (int i = 0; true; i++)
            {
                var arg = GetOption(values, i);
                if (arg == null) yield break;
                yield return arg;
            }
        }

        private const string _requiredDisplayName = "tests";
        private const string _optionName = null;
        private const string _optionExample = "TEST1 [...]";
        private const string _fullName = "tests";
    }
}
