//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    class SubscriptionToken
    {
        public class Parser : NamedValueTokenParser
        {

            public Parser() : base(_optionName, _fullName, _fullNameRequiredParts, _valueCount)
            {
            }
        }

        public static string Demand(INamedValues values, string action, string command)
        {
            var subscription = values.Get(_fullName, true);
            if (string.IsNullOrEmpty(subscription) || subscription.Contains("rror"))
            {
                values.AddThrowError(
                    "ERROR:", $"{action}; requires {_requiredDisplayName}.",
                            "",
                      "TRY:", $"{Program.Name} init",
                              $"{Program.Name} config --set subscription {_optionExample}",
                              $"{Program.Name} {command} {_optionName} {_optionExample}",
                            "",
                      "SEE:", $"{Program.Name} help {command}");
            }
            return subscription;
        }

        public static string GetOrDefault(INamedValues values, string defaultValue = null)
        {
            return values.GetOrDefault(_fullName, defaultValue);
        }

        public static void Set(INamedValues values, string value = null)
        {
            values.Reset(_fullName, value);
        }

        private const string _requiredDisplayName = "subscription";
        private const string _optionName = "--subscription";
        private const string _optionExample = "SUBSCRIPTION";
        private const string _fullName = "service.subscription";
        private const string _fullNameRequiredParts = "01";
        private const string _valueCount = "1";
    }
}
