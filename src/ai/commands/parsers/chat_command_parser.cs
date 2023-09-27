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

            ConfigEndpointUriToken.Parser(),
            ConfigDeploymentToken.Parser(),

            new NamedValueTokenParser(null,             "service.config.embeddings.deployment", "0010", "1"),

            new NamedValueTokenParser(null,             "service.config.search.api.key", "00101", "1"),
            new NamedValueTokenParser(null,             "service.config.search.endpoint.uri", "00110;00101", "1"),
            new NamedValueTokenParser(null,             "service.config.search.index.name", "00010", "1"),
            new NamedValueTokenParser(null,             "service.config.search.query.type", "00011", "1"),

            new NamedValueTokenParser("--interactive",  "chat.input.interactive", "001", "0", null, null, "true"),

            new NamedValueTokenParser(null,             "chat.message.history.json.file", "00011", "1", null, null, "json.file", "chat.history.type"),
            new NamedValueTokenParser(null,             "chat.message.history.jsonl.file", "00011", "1", null, null, "jsonl.file", "chat.history.type"),
            new NamedValueTokenParser(null,             "chat.message.history.text.file", "00011;00001", "1", null, null, "text.file", "chat.history.type"),
            new NamedValueTokenParser(null,             "chat.message.history.type", "1111", "1", "interactive;interactive+;json;jsonl;text;json.file;jsonl.file;text.file"),

            new NamedValueTokenParser(null,             "chat.message.system.prompt", "0010;0001", "1"),
            new NamedValueTokenParser(null,             "chat.message.user.prompt", "0010", "1"),

            new NamedValueTokenParser(null,             "chat.options.max.tokens", "0011", "1"),
            new NamedValueTokenParser(null,             "chat.options.temperature", "001", "1"),
            new NamedValueTokenParser(null,             "chat.options.top.p", "0001", "1"),
            new NamedValueTokenParser(null,             "chat.options.frequency.penalty", "0011", "1"),
            new NamedValueTokenParser(null,             "chat.options.presence.penalty", "0011", "1"),
            new NamedValueTokenParser(null,             "chat.options.stop.sequence", "0010", "1"),

            new NamedValueTokenParser(null,             "chat.replace.value.*", "0011;0101", "1;0", null, null, "="),

            new NamedValueTokenParser(null,             "chat.speech.input", "010", "1;0", "true;false", null, "true"),

            ChatFunctionToken.Parser(),
        };

        #endregion
    }
}
