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
            ("service.search.create", true),
            ("service.search.status", true),
            ("service.search.list", false),
            ("service.search.update", true),
            ("service.search.delete", true),
            ("service.search", true),
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
            "service.search",
            "service.evaluation",
            "service"
        };

        private static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();

            switch (commandName)
            {
                case "service.resource.create": return _resourceCreateParsers;
                case "service.resource.list": return _resourceListParsers;
                case "service.resource.delete": return _resourceDeleteParsers;

                case "service.project.create": return _projectCreateParsers;
                case "service.project.list": return _projectListParsers;
                case "service.project.delete": return _projectDeleteParsers;

                case "service.connection.create": return _connectionCreateParsers;
                case "service.connection.list": return _connectionListParsers;
                case "service.connection.delete": return _connectionDeleteParsers;

                case "service.search.create": return _searchCreateParsers;
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

                    new NamedValueTokenParser("--ini", "ini.file", "10", "1", "@"),

                    new NamedValueTokenParser(null, "x.command.expand.file.name", "11111", "1"),

                    ConfigEndpointUriToken.Parser(),
                    ConfigDeploymentToken.Parser(),
                    SubscriptionToken.Parser()
                )
            {
            }
        }

        private static INamedValueTokenParser[] _servicePlaceHolderParsers = {

            new CommonServiceNamedValueTokenParsers()

        };

        private static INamedValueTokenParser[] _resourceCreateParsers = {
            
            new CommonServiceNamedValueTokenParsers(),
          
            ResourceGroupNameToken.Parser(),
            ResourceNameToken.Parser(),
            RegionLocationToken.Parser(),
            ResourceDisplayNameToken.Parser(),
            ResourceDescriptionToken.Parser(),

            new NamedValueTokenParser("--output-resource-id", "service.output.resource.id", "0110;0101", "1"),
            new NamedValueTokenParser(null, "service.output.json", "011", "1")
        };

        private static INamedValueTokenParser[] _resourceListParsers = {
            
            new CommonServiceNamedValueTokenParsers(),

            new NamedValueTokenParser(null, "service.output.json", "011", "1")
        };

        private static INamedValueTokenParser[] _resourceDeleteParsers = {
            
            new CommonServiceNamedValueTokenParsers(),
          
            ResourceGroupNameToken.Parser(),
            ResourceNameToken.Parser(),
            DeleteDependentResourcesToken.Parser(),

            new NamedValueTokenParser(null, "service.output.json", "011", "1")
        };

        private static INamedValueTokenParser[] _projectCreateParsers = {
            
            new CommonServiceNamedValueTokenParsers(),
          
            ResourceGroupNameToken.Parser(),
            ResourceNameToken.Parser(requireResourcePart: true),
            RegionLocationToken.Parser(),
            ProjectNameToken.Parser(requireProjectPart: false),
            ProjectDisplayNameToken.Parser(),
            ProjectDescriptionToken.Parser(),

            new NamedValueTokenParser("--output-project-id", "service.output.project.id", "0110;0101", "1"),
            new NamedValueTokenParser(null, "service.output.json", "011", "1")
        };

        private static INamedValueTokenParser[] _projectListParsers = {
            
            new CommonServiceNamedValueTokenParsers(),

            new NamedValueTokenParser(null, "service.output.json", "011", "1")
        };

        private static INamedValueTokenParser[] _projectDeleteParsers = {
            
            new CommonServiceNamedValueTokenParsers(),
          
            ResourceGroupNameToken.Parser(),
            ProjectNameToken.Parser(requireProjectPart: false),
            DeleteDependentResourcesToken.Parser(),

            new NamedValueTokenParser(null, "service.output.json", "011", "1")
        };

        private static INamedValueTokenParser[] _connectionCreateParsers = {
            
            new CommonServiceNamedValueTokenParsers(),
          
            ResourceGroupNameToken.Parser(),
            ProjectNameToken.Parser(),
            ProjectConnectionNameToken.Parser(),
            ProjectConnectionTypeToken.Parser(),
            ProjectConnectionEndpointToken.Parser(),
            ProjectConnectionKeyToken.Parser(),
            CognitiveServicesResourceKindToken.Parser(),

            new NamedValueTokenParser(null, "service.output.json", "011", "1")
        };

        private static INamedValueTokenParser[] _connectionListParsers = {
            
            new CommonServiceNamedValueTokenParsers(),

            ResourceGroupNameToken.Parser(),
            ProjectNameToken.Parser(),

            new NamedValueTokenParser(null, "service.output.json", "011", "1")
        };

        private static INamedValueTokenParser[] _connectionDeleteParsers = {
            
            new CommonServiceNamedValueTokenParsers(),
          
            ResourceGroupNameToken.Parser(),
            ProjectNameToken.Parser(),
            ProjectConnectionNameToken.Parser(),

            new NamedValueTokenParser(null, "service.output.json", "011", "1")
        };

        private static INamedValueTokenParser[] _searchCreateParsers = {
            
            new CommonServiceNamedValueTokenParsers(),
          
            // ResourceGroupNameToken.Parser(),
            // ResourceNameToken.Parser(requireResourcePart: true),
            // ProjectNameToken.Parser(),
            // RegionLocationToken.Parser(),
            // ProjectDisplayNameToken.Parser(),
            // ProjectDescriptionToken.Parser(),

            // new NamedValueTokenParser("--output-project-id", "service.output.project.id", "0110;0101", "1"),
            // new NamedValueTokenParser(null, "service.output.json", "011", "1")
        };

        #endregion
    }
}
