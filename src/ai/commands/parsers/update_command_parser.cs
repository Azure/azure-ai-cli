//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;

namespace Azure.AI.Details.Common.CLI
{
    class UpdateCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            values.Add("display.update", "true");
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("update", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("update", false),
        };

        private static readonly string[] _partialCommands = {
        };

        private static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();

            switch (commandName)
            {
                case "update": return _updateCommandParsers;
            }

            return null;
        }


        #region private data

        public class CommonUpdateNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonUpdateNamedValueTokenParsers() : base(

                    new NamedValueTokenParser(null, "x.command", "11", "1", "update"),

                    new ExpectOutputTokenParser(),
                    new DiagnosticLogTokenParser(),
                    new CommonNamedValueTokenParsers(),

                    new NamedValueTokenParser("--ini", "ini.file", "10", "1", "@"),
                    new NamedValueTokenParser(null, "x.command.expand.file.name", "11111", "1")
                )
            {
            }
        }

        private static INamedValueTokenParser[] _updateCommandParsers = {

            new CommonUpdateNamedValueTokenParsers(),

        };

        #endregion
    }
}
