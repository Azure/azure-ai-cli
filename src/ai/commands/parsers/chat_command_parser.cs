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
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("chat", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("chat.evaluate", true),
            ("chat.run", true),
            ("chat", true),
        };

        private static readonly string[] _partialCommands = {
            "chat",
        };

        private static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommandOrEmpty();

            switch (commandName)
            {
                case "chat.evaluate": return _chatEvaluateCommandParsers;
                case "chat.run": return _chatRunCommandParsers;
                case "chat": return _chatCommandParsers;
            }

            foreach (var command in _commands)
            {
                if (commandName == command.name)
                {
                    return _chatPlaceHolderParsers;
                }
            }

            return null;
        }


        #region private data

        public class CommonChatNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonChatNamedValueTokenParsers() : base(

                    new NamedValueTokenParser(null, "x.command", "11", "1"),

                    new ExpectOutputTokenParser(),
                    new DiagnosticLogTokenParser(),
                    new CommonNamedValueTokenParsers(),

                    new NamedValueTokenParser("--ini", "ini.file", "10", "1", "@"),
                    new NamedValueTokenParser(null, "x.command.expand.file.name", "11111", "1"),

                    ConfigEndpointUriToken.Parser(),
                    ConfigDeploymentToken.Parser(),

                    SearchIndexNameToken.Parser(),
                    MLIndexNameToken.Parser(),
                    SKIndexNameToken.Parser(),

                    SearchEmbeddingModelDeploymentNameToken.Parser(),
                    SearchEmbeddingModelNameToken.Parser(),

                    FunctionToken.Parser(),

                    new NamedValueTokenParser(null,             "service.config.search.api.key", "00101", "1"),
                    new NamedValueTokenParser(null,             "service.config.search.endpoint.uri", "00110;00101", "1"),
                    new NamedValueTokenParser(null,             "service.config.search.query.type", "00011", "1"),

                    new NamedValueTokenParser(null,             "chat.message.history.json.file", "10110", "1", null, null, "json.file", "chat.history.type"),
                    new NamedValueTokenParser(null,             "chat.message.history.jsonl.file", "10110", "1", null, null, "jsonl.file", "chat.history.type"),
                    new NamedValueTokenParser(null,             "chat.message.history.text.file", "10110", "1", null, null, "text.file", "chat.history.type"),
                    new NamedValueTokenParser(null,             "chat.message.history.type", "1111", "1", "interactive;interactive+;json;jsonl;text;json.file;jsonl.file;text.file"),

                    new NamedValueTokenParser(null,             "chat.message.system.prompt", "0010;0001", "1"),
                    new NamedValueTokenParser(null,             "chat.message.user.prompt", "0010", "1"),
                    new NamedValueTokenParser(null,             "chat.message.user.question", "0001", "1", null, "chat.message.user.prompt"),

                    new NamedValueTokenParser(null,             "chat.options.max.tokens", "0011", "1"),
                    new NamedValueTokenParser(null,             "chat.options.temperature", "001", "1"),
                    new NamedValueTokenParser(null,             "chat.options.top.p", "0001", "1"),
                    new NamedValueTokenParser(null,             "chat.options.frequency.penalty", "0011", "1"),
                    new NamedValueTokenParser(null,             "chat.options.presence.penalty", "0011", "1"),
                    new NamedValueTokenParser(null,             "chat.options.stop.sequence", "0010", "1"),

                    new TrueFalseNamedValueTokenParser("chat.built.in.helper.functions", "01101"),
                    new NamedValueTokenParser(null, "chat.custom.helper.functions", "0101;0011", "1")
                )
            {
            }
        }

        private static INamedValueTokenParser[] _chatPlaceHolderParsers = {
            new CommonChatNamedValueTokenParsers(),
        };

        private static INamedValueTokenParser[] _chatCommandParsers = {

            new CommonChatNamedValueTokenParsers(),

            new TrueFalseNamedValueTokenParser("chat.input.interactive", "001"),
            new TrueFalseNamedValueTokenParser("chat.speech.input", "010"),

            OutputChatAnswerFileToken.Parser(),
            OutputChatHistoryFileToken.Parser(),
            InputChatHistoryJsonFileToken.Parser(),
            // new TrueFalseRequiredPrefixNamedValueTokenParser("output", "all.answer", "01"),
            // new TrueFalseRequiredPrefixNamedValueTokenParser("output", "each.answer", "11")

        };

        private static INamedValueTokenParser[] _chatRunCommandParsers = {

            new CommonChatNamedValueTokenParsers(),
            InputDataFileToken.Parser()
        };

        private static INamedValueTokenParser[] _chatEvaluateCommandParsers = {

            new CommonChatNamedValueTokenParsers(),
            InputDataFileToken.Parser()
        };

        #endregion
    }
}
