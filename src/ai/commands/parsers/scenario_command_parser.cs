//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    class ScenarioCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("scenario", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("scenario", false)
        };

        private static readonly string[] _partialCommands = {
        };

        private static INamedValueTokenParser[] GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();
            switch (commandName)
            {
                case "scenario":
                    return scenarioCommandParsers;
            }

            return null;
        }

        #region private data

        public class CommonScenarioNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonScenarioNamedValueTokenParsers() : base(

                new NamedValueTokenParser(null, "x.command", "11", "1", "scenario"),

                new ExpectOutputTokenParser(),
                new DiagnosticLogTokenParser(),
                new CommonNamedValueTokenParsers(),

                new NamedValueTokenParser(null, "x.command.expand.file.name", "11111", "1"),

                new NamedValueTokenParser("--uri", "service.config.endpoint.uri", "0010;0001", "1"),
                new NamedValueTokenParser("--deployment", "service.config.deployment", "001", "1"),

                new NamedValueTokenParser("--interactive", "scenario.interactive", "01", "1;0", "true;false", null, "true")

            )
            {
            }
        }

        private static readonly INamedValueTokenParser[] scenarioCommandParsers = {
            new CommonScenarioNamedValueTokenParsers()
        };

        #endregion
    }
}
