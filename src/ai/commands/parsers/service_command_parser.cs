//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    class ServiceCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("service", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("service.resource.create", true),
            ("service.resource.status", true),
            ("service.resource.list", false),
            ("service.resource.update", true),
            ("service.resource.delete", true),
            ("service.resource", true),
            ("service.project.create", true),
            ("service.project.status", true),
            ("service.project.list", false),
            ("service.project.update", true),
            ("service.project.delete", true),
            ("service.project", true),
            ("service.connection.create", true),
            ("service.connection.status", true),
            ("service.connection.list", false),
            ("service.connection.update", true),
            ("service.connection.delete", true),
            ("service.connection", true),
            ("service.deployment.create", true),
            ("service.deployment.status", true),
            ("service.deployment.list", false),
            ("service.deployment.update", true),
            ("service.deployment.delete", true),
            ("service.deployment", true),
            ("service.flow.create", true),
            ("service.flow.status", true),
            ("service.flow.list", false),
            ("service.flow.update", true),
            ("service.flow.delete", true),
            ("service.flow", true),
            ("service.evaluation.create", true),
            ("service.evaluation.status", true),
            ("service.evaluation.list", false),
            ("service.evaluation.update", true),
            ("service.evaluation.delete", true),
            ("service.evaluation", true),
            ("service", true)
        };

        private static readonly string[] _partialCommands = {
            "service.resource",
            "service.project",
            "service.connection",
            "service.deployment",
            "service.flow",
            "service.evaluation",
            "service"
        };

        private static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();

            switch (commandName)
            {
                case "service.resource.list": return _resourceListParsers;
            }

            foreach (var command in _commands)
            {
                if (commandName == command.name)
                {
                    return _servicePlaceHolderParsers;
                }
            }

            return null;
        }

        #region private data

        public class CommonServiceNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonServiceNamedValueTokenParsers() : base(

                    new NamedValueTokenParser(null, "x.command", "11", "1"),

                    new ExpectOutputTokenParser(),
                    new DiagnosticLogTokenParser(),
                    new CommonNamedValueTokenParsers(),

                    new NamedValueTokenParser(null, "x.command.expand.file.name", "11111", "1"),

                    new NamedValueTokenParser("--uri", "service.config.endpoint.uri", "0010;0001", "1"),
                    new NamedValueTokenParser("--deployment", "service.config.deployment", "001", "1")

                )
            {
            }
        }

        private static INamedValueTokenParser[] _servicePlaceHolderParsers = {

            new CommonServiceNamedValueTokenParsers()

        };

        private static INamedValueTokenParser[] _resourceListParsers = {
            
            new CommonServiceNamedValueTokenParsers(),
            new NamedValueTokenParser("--subscription", "service.subscription", "01", "1")
        };

        #endregion
    }
}
