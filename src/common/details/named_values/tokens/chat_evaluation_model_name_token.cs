//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class ChatEvaluationModelNameToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser(bool requireSearchPart = false) => new Any1ValueNamedValueTokenParser(_optionName, _fullName, requireSearchPart ? "1110" : "0110");

        private const string _requiredDisplayName = "evaluation model name";
        private const string _optionName = "--evaluation-model";
        private const string _optionExample = "NAME";
        private const string _fullName = "chat.evaluation.model.name";
    }
}
