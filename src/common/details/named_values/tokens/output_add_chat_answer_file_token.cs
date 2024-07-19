//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class OutputAddChatAnswerFileToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _parser.FullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser() => _parser;

        private static _Parser _parser = new();
        private class _Parser : OutputFileOptionalPrefixNamedValueTokenParser
        {
            public _Parser() : base("chat", "add.answer", "11")
            {
            }

            public new string FullName => base.FullName;
        }

        private const string _requiredDisplayName = "chat add answer output file";
        private const string _optionName = "--output-add-answer-file";
        private const string _optionExample = "FILE";
    }
}
