//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Collections.Generic;

namespace Azure.AI.Details.Common.CLI
{
    class FlowCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("flow", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("flow.new", true),
            ("flow.invoke", true),
            ("flow.deploy", true),
            ("flow.serve", true),
        };

        private static readonly string[] _partialCommands = {
            "flow"
        };

        private static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();

            switch (commandName)
            {
                case "flow.new": return _flowNewParsers;
                case "flow.invoke": return _flowInvokeParsers;
                case "flow.deploy": return _flowDeployParsers;
                case "flow.serve": return _flowServeParsers;
            }

            foreach (var command in _commands)
            {
                if (commandName == command.name)
                {
                    return _flowPlaceHolderParsers;
                }
            }

            return null;
        }

        public class CommonFlowNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonFlowNamedValueTokenParsers() : base(

                    new NamedValueTokenParser(null, "x.command", "11", "1"),

                    new ExpectOutputTokenParser(),
                    new DiagnosticLogTokenParser(),
                    new CommonNamedValueTokenParsers(),

                    new NamedValueTokenParser("--ini", "ini.file", "10", "1", "@"),

                    new NamedValueTokenParser(null, "x.command.expand.file.name", "11111", "1")

                )
            {
            }
        }

        private static INamedValueTokenParser[] _flowPlaceHolderParsers = {
            new CommonFlowNamedValueTokenParsers()
        };

        private static INamedValueTokenParser[] _flowNewParsers = {
            new CommonFlowNamedValueTokenParsers()
        };

        private static INamedValueTokenParser[] _flowInvokeParsers = {
            new CommonFlowNamedValueTokenParsers()
        };

        private static INamedValueTokenParser[] _flowDeployParsers = {
            new CommonFlowNamedValueTokenParsers()
        };

        private static INamedValueTokenParser[] _flowServeParsers = {
            new CommonFlowNamedValueTokenParsers()
        };
    }
}
