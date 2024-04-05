//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;

namespace Azure.AI.Details.Common.CLI
{
    class VersionCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            values.Add("display.version", "true");
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("version", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("version", false),
        };

        private static readonly string[] _partialCommands = {
        };

        private static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommandOrEmpty();

            switch (commandName)
            {
                case "version": return _versionCommandParsers;
            }

            return null;
        }


        #region private data

        public class CommonVersionNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonVersionNamedValueTokenParsers() : base(

                    new NamedValueTokenParser(null, "x.command", "11", "1", "version"),

                    new ExpectOutputTokenParser(),
                    new DiagnosticLogTokenParser(),
                    new CommonNamedValueTokenParsers(),

                    new NamedValueTokenParser("--ini", "ini.file", "10", "1", "@"),
                    new NamedValueTokenParser(null, "x.command.expand.file.name", "11111", "1")
                )
            {
            }
        }

        private static INamedValueTokenParser[] _versionCommandParsers = {

            new CommonVersionNamedValueTokenParsers(),

        };

        #endregion
    }
}
