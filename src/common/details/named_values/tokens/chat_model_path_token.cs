//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class ChatModelPathToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser(bool requireChatPart = false) => new Any1ValueNamedValueTokenParser(_optionName, _fullName, requireChatPart ? "111" : "011");

        private const string _requiredDisplayName = "model path";
        private const string _optionName = "--model-path";
        private const string _optionExample = "PATH";
        private const string _fullName = "chat.model.path";
    }
}
