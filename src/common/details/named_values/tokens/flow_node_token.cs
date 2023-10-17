//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class FlowNodeToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser(bool requireFlowPart = false) => new NamedValueTokenParser(_optionName, _fullName, requireFlowPart ? "11" : "01;10", "1");

        private const string _requiredDisplayName = "flow node";
        private const string _optionName = "--flow-node";
        private const string _optionExample = "NODE";
        private const string _fullName = "flow.node";
    }
}
