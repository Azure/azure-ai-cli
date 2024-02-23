//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class ArgXToken
    {
        public static INamedValueTokenParser Parser() => new ArgXTokenParser();

        public static string GetArg(ICommandValues values, int x) => values.GetOrDefault("arg" + x, null);

        public static IEnumerable<string> GetArgs(ICommandValues values)
        {
            for (int i = 0; true; i++)
            {
                var arg = GetArg(values, i);
                if (arg == null) yield break;
                yield return arg;
            }
        }

        private const string _requiredDisplayName = "template input";
        private const string _optionName = null;
        private const string _optionExample = "NAME=VALUE";
        private const string _fullName = "input.*";
    }
}
