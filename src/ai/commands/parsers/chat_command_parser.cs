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
            ("chat.assistant.create", true),
            ("chat.assistant.delete", false),
            ("chat.assistant.get", true ),
            ("chat.assistant.list", false),

            ("chat.assistant.vector-store.create", true),
            ("chat.assistant.vector-store.delete", true),
            ("chat.assistant.vector-store.get", true),
            ("chat.assistant.vector-store.list", false),
            ("chat.assistant.vector-store", true),

            ("chat.assistant.file.upload", true),
            ("chat.assistant.file.delete", true),
            ("chat.assistant.file.list", false),
            ("chat.assistant.file", true),

            ("chat.assistant", true),
            ("chat", true),
        };

        private static readonly string[] _partialCommands = {
            "chat",
        };

        private static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();

            switch (commandName)
            {
                case "chat": return _chatCommandParsers;

                case "chat.assistant.create": return _chatAssistantCreateCommandParsers;
                case "chat.assistant.get": return _chatAssistantGetCommandParsers;
                case "chat.assistant.list": return _chatPlaceHolderParsers;
                case "chat.assistant.delete": return _chatAssistantDeleteCommandParsers;

                case "chat.assistant.vector-store.create": return _chatAssistantVectorStoreCreateCommandParsers;
                case "chat.assistant.vector-store.get": return _chatAssistantVectorStoreGetCommandParsers;
                case "chat.assistant.vector-store.list": return _chatPlaceHolderParsers;
                case "chat.assistant.vector-store.delete": return _chatAssistantVectorStoreDeleteCommandParsers;

                case "chat.assistant.file.upload": return _chatAssistantFileUploadCommandParsers;
                case "chat.assistant.file.list": return _chatPlaceHolderParsers;
                case "chat.assistant.file.delete": return _chatAssistantFileDeleteCommandParsers;
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

                    new Any1ValueNamedValueTokenParser(null, "x.command", "11"),

                    new ExpectOutputTokenParser(),
                    new DiagnosticLogTokenParser(),
                    new CommonNamedValueTokenParsers(),

                    new IniFileNamedValueTokenParser(),
                    new ExpandFileNameNamedValueTokenParser(),

                    ConfigEndpointUriToken.Parser(),
                    ConfigDeploymentToken.Parser(),

                    SearchIndexNameToken.Parser(),
                    SearchEmbeddingModelDeploymentNameToken.Parser(),
                    SearchEmbeddingModelNameToken.Parser(),

                    new Any1ValueNamedValueTokenParser(null, "chat.assistant.id", "011"),
                    new Any1ValueNamedValueTokenParser(null, "chat.assistant.file.id", "0011"),

                    new Any1ValueNamedValueTokenParser(null, "service.config.search.api.key", "00101"),
                    new Any1ValueNamedValueTokenParser(null, "service.config.search.endpoint.uri", "00110;00101"),
                    new Any1ValueNamedValueTokenParser(null, "service.config.search.query.type", "00011"),

                    new Any1ValueNamedValueTokenParser(null, "chat.options.max.tokens", "0011"),
                    new Any1ValueNamedValueTokenParser(null, "chat.options.temperature", "001"),
                    new Any1ValueNamedValueTokenParser(null, "chat.options.top.p", "0001"),
                    new Any1ValueNamedValueTokenParser(null, "chat.options.frequency.penalty", "0011"),
                    new Any1ValueNamedValueTokenParser(null, "chat.options.presence.penalty", "0011"),
                    new Any1ValueNamedValueTokenParser(null, "chat.options.stop.sequence", "0010"),

                    new TrueFalseNamedValueTokenParser("chat.built.in.helper.functions", "01101"),
                    new Any1ValueNamedValueTokenParser(null, "chat.custom.helper.functions", "0101;0011")
                )
            {
            }
        }

        private static INamedValueTokenParser[] _chatPlaceHolderParsers = {
            new CommonChatNamedValueTokenParsers(),
        };

        private static INamedValueTokenParser[] _chatCommandParsers = {

            new CommonChatNamedValueTokenParsers(),

            new Any1PinnedNamedValueTokenParser(null, "chat.message.history.json.file", "10110", "json.file", "chat.history.type"),
            new Any1PinnedNamedValueTokenParser(null, "chat.message.history.jsonl.file", "10110", "jsonl.file", "chat.history.type"),
            new Any1PinnedNamedValueTokenParser(null, "chat.message.history.text.file", "10110", "text.file", "chat.history.type"),
            new RequiredValidValueNamedValueTokenParser(null, "chat.message.history.type", "1111", "interactive;interactive+;json;jsonl;text;json.file;jsonl.file;text.file"),

            new Any1ValueNamedValueTokenParser(null, "chat.message.system.prompt", "0010;0001"),
            new Any1ValueNamedValueTokenParser(null, "chat.message.user.prompt", "0010"),
            new NamedValueTokenParser(null, "chat.message.user.question", "0001", "1", null, "chat.message.user.prompt"),

            new TrueFalseNamedValueTokenParser("chat.input.interactive", "001"),
            new TrueFalseNamedValueTokenParser("chat.speech.input", "010"),

            OutputChatAnswerFileToken.Parser(),
            OutputChatHistoryFileToken.Parser(),
            InputChatHistoryJsonFileToken.Parser(),
            InputChatParameterFileToken.Parser(),
        };

        private static INamedValueTokenParser[] _chatAssistantCreateCommandParsers = {
            CodeInterpreterToken.Parser(),

            FileIdOptionXToken.Parser(),
            FileIdsOptionXToken.Parser(),
            
            FileOptionXToken.Parser(),
            FilesOptionXToken.Parser(),

            new CommonChatNamedValueTokenParsers(),

            new Any1ValueNamedValueTokenParser(null, "chat.assistant.create.name", "0001"),
            InstructionsToken.Parser(),
        };

        private static INamedValueTokenParser[] _chatAssistantGetCommandParsers = {
            new CommonChatNamedValueTokenParsers(),
            new Any1ValueNamedValueTokenParser(null, "chat.assistant.id", "001"),
        };

        private static INamedValueTokenParser[] _chatAssistantDeleteCommandParsers = {
            new CommonChatNamedValueTokenParsers(),
            new Any1ValueNamedValueTokenParser(null, "chat.assistant.id", "001"),
        };

        private static INamedValueTokenParser[] _chatAssistantVectorStoreDeleteCommandParsers = {
            new CommonChatNamedValueTokenParsers(),
            new Any1ValueNamedValueTokenParser(null, "chat.assistant.vector.store.id", "00001"),
        };

        private static INamedValueTokenParser[] _chatAssistantVectorStoreCreateCommandParsers = {
            FileIdOptionXToken.Parser(),
            FileIdsOptionXToken.Parser(),

            FileOptionXToken.Parser(),
            FilesOptionXToken.Parser(),

            new CommonChatNamedValueTokenParsers(),
            new Any1ValueNamedValueTokenParser(null, "chat.assistant.vector.store.name", "00001"),
        };

        private static INamedValueTokenParser[] _chatAssistantVectorStoreGetCommandParsers = {
            new CommonChatNamedValueTokenParsers(),
            new Any1ValueNamedValueTokenParser(null, "chat.assistant.vector.store.id", "00001"),
        };
        
        private static INamedValueTokenParser[] _chatAssistantFileUploadCommandParsers = {
            new CommonChatNamedValueTokenParsers(),
            new Any1ValueNamedValueTokenParser("--file", "assistant.upload.file", "001"),
            new ExpandFileNameNamedValueTokenParser("--files", "assistant.upload.files", "001", "assistant.upload.file"),
        };

        private static INamedValueTokenParser[] _chatAssistantFileDeleteCommandParsers = {
            new CommonChatNamedValueTokenParsers(),
            new Any1ValueNamedValueTokenParser(null, "chat.assistant.file.id", "0001"),
        };

        #endregion
    }
}
