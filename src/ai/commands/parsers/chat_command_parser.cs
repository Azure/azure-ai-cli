//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    class ChatCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommand("chat", chatCommandParsers, tokens, values);
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("chat", chatCommandParsers, tokens, values);
        }

        #region private data

        private static INamedValueTokenParser[] chatCommandParsers = {

            new NamedValueTokenParser(null, "x.command", "11", "1", "chat"),

            new ExpectOutputTokenParser(),
            new DiagnosticLogTokenParser(),
            new CommonNamedValueTokenParsers(),

            new NamedValueTokenParser(null, "x.command.expand.file.name", "11111", "1"),

            new NamedValueTokenParser("--uri", "service.config.endpoint.uri", "0010;0001", "1"),
            new NamedValueTokenParser("--deployment", "service.config.deployment", "001", "1"),

            new NamedValueTokenParser(null,             "service.config.embeddings.deployment", "0010", "1"),
            new NamedValueTokenParser(null,             "service.config.embeddings.index.name", "00010", "1"),

            new NamedValueTokenParser(null,             "service.config.acs.key", "0011", "1"),
            new NamedValueTokenParser(null,             "service.config.acs.endpoint.uri", "00110;00101", "1"),

            new NamedValueTokenParser("--interactive",  "chat.input.interactive", "001", "0", null, null, "interactive", "chat.input.type"),
            new NamedValueTokenParser("--interactive+", "chat.input.interactive+", "001", "0", null, null, "interactive+", "chat.input.type"),
            new NamedValueTokenParser(null,             "chat.input.type", "111", "1", "interactive;interactive+;text;ssml;text.file;ssml.file"),

            new NamedValueTokenParser(null,             "chat.message.system.prompt", "0010;0001", "1"),
        };

        #endregion
    }
}
