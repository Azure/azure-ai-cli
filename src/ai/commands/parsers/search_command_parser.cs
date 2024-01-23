//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    class SearchCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("search", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("search.index.create", true),
            ("search.index.status", true),
            ("search.index.list", false),
            ("search.index.update", true),
            ("search.index.delete", true),
            ("search.index", true),
            ("search.query", true),
            ("search", true)
        };

        private static readonly string[] _partialCommands = {
            "search.index.create",
            "search.index.status",
            "search.index.list",
            "search.index.update",
            "search.index.delete",
            "search.index",
            "search.query",
            "search"
        };

        private static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();

            switch (commandName)
            {
                case "search.index.create": return _searchIndexUpdateParsers;
                case "search.index.update": return _searchIndexUpdateParsers;
            }

            foreach (var command in _commands)
            {
                if (commandName == command.name)
                {
                    return _searchPlaceHolderParsers;
                }
            }

            return null;
        }

        #region private data

        public class CommonSearchNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonSearchNamedValueTokenParsers() : base(

                    new NamedValueTokenParser(null, "x.command", "11", "1"),

                    new ExpectOutputTokenParser(),
                    new DiagnosticLogTokenParser(),
                    new CommonNamedValueTokenParsers(),

                    new NamedValueTokenParser("--ini", "ini.file", "10", "1", "@"),

                    new NamedValueTokenParser(null, "x.command.expand.file.name", "11111", "1"),

                    ConfigEndpointUriToken.Parser(),
                    ConfigDeploymentToken.Parser()

                )
            {
            }
        }

        private static INamedValueTokenParser[] _searchPlaceHolderParsers = {

            new CommonSearchNamedValueTokenParsers()

        };

        private static INamedValueTokenParser[] _searchIndexUpdateParsers = {
            
            new CommonSearchNamedValueTokenParsers(),
            SubscriptionToken.Parser(),
            ResourceGroupNameToken.Parser(),
            ProjectNameToken.Parser(),

            BlobContainerToken.Parser(),
            SearchIndexerDataSourceConnectionNameToken.Parser(),
            SearchIndexerSkillsetNameToken.Parser(),
            IndexIdFieldNameToken.Parser(),
            IndexContentFieldNameToken.Parser(),
            IndexVectorFieldNameToken.Parser(),

            SearchIndexNameToken.Parser(requireIndexPart: false),
            MLIndexNameToken.Parser(requireIndexPart: false),
            SKIndexNameToken.Parser(),

            SearchEmbeddingModelDeploymentNameToken.Parser(),
            SearchEmbeddingModelNameToken.Parser(),

            AiServicesApiKeyToken.Parser(),

            ExternalSourceToken.Parser(),

            new NamedValueTokenParser(null,  "service.config.search.api.key", "00101", "1"),
            new NamedValueTokenParser(null,  "service.config.search.endpoint.uri", "00110;00101", "1"),

            new NamedValueTokenParser(null, "search.embedding.endpoint.uri", "0101;0110", "1"),
            new NamedValueTokenParser(null, "search.embedding.api.key", "0101", "1"),

            new NamedValueTokenParser(null, "search.index.update.file", "0001", "1"),
            new NamedValueTokenParser(null, "search.index.update.files", "0001", "1"),
        };


        #endregion
    }
}
