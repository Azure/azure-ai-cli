//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class InputChatParameterFileToken
    {
        public static NamedValueTokenData Data() => new NamedValueTokenData(_optionName, _parser.FullName, _optionExample, _requiredDisplayName);
        public static INamedValueTokenParser Parser() => _parser;

        private static _Parser _parser = new();
        private class _Parser : InputFileOptionalPrefixNamedValueTokenParser
        {
            public _Parser() : base("chat", "parameter", "1")
            {
            }

            public new string FullName => base.FullName;
        }

        private const string _requiredDisplayName = "chat parameter input file";
        private const string _optionName = "--input-chat-parameter-file";
        private const string _optionExample = "FILE";
    }
}