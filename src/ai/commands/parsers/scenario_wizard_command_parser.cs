//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    class ScenarioWizardCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("wizard", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("wizard", false)
        };

        private static readonly string[] _partialCommands = {
        };

        private static INamedValueTokenParser[] GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();
            switch (commandName)
            {
                case "wizard":
                    return ScenarioWizardCommandParsers;
            }

            values.AddThrowError("ERROR:", $"Unknown command: {commandName}");
            return Array.Empty<INamedValueTokenParser>();
        }

        #region private data

        public class CommonScenarioNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonScenarioNamedValueTokenParsers() : base(

                new NamedValueTokenParser(null, "x.command", "11", "1", "wizard"),

                new ExpectOutputTokenParser(),
                new DiagnosticLogTokenParser(),
                new CommonNamedValueTokenParsers(),

                new NamedValueTokenParser("--ini", "ini.file", "10", "1", "@"),

                new NamedValueTokenParser(null, "x.command.expand.file.name", "11111", "1"),

                ConfigEndpointUriToken.Parser(),
                ConfigDeploymentToken.Parser(),

                new NamedValueTokenParser("--interactive", "scenario.wizard.interactive", "001", "1;0", "true;false", null, "true")

            )
            {
            }
        }

        private static readonly INamedValueTokenParser[] ScenarioWizardCommandParsers = {
            new CommonScenarioNamedValueTokenParsers()
        };

        #endregion
    }
}
