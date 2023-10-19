//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class ChatModelNameToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _fullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser(bool requireChatPart = false) => new NamedValueTokenParser(_optionName, _fullName, requireChatPart ? "110" : "010", "1");

        private const string _requiredDisplayName = "chat model name";
        private const string _optionName = "--model";
        private const string _optionExample = "NAME";
        private const string _fullName = "chat.model.name";
    }
}
