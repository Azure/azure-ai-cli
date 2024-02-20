//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    class VersionCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("version", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("version", true),
        };

        private static readonly string[] _partialCommands = {
            "version",
        };

        private static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();

            switch (commandName)
            {
                case "version": return _versionCommandParsers;
            }

            foreach (var command in _commands)
            {
                if (commandName == command.name)
                {
                    return _versionPlaceHolderParsers;
                }
            }

            return null;
        }


        #region private data

        public class CommonVersionNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonVersionNamedValueTokenParsers() : base(

                    new NamedValueTokenParser(null, "x.command", "11", "1"),

                    new ExpectOutputTokenParser(),
                    new DiagnosticLogTokenParser(),
                    new CommonNamedValueTokenParsers(),

                    new NamedValueTokenParser("--ini", "ini.file", "10", "1", "@"),
                )
            {
            }
        }

        private static INamedValueTokenParser[] _versionPlaceHolderParsers = {
            new CommonVersionNamedValueTokenParsers(),
        };

        private static INamedValueTokenParser[] _versionCommandParsers = {

            new CommonVersionNamedValueTokenParsers(),

        };

        #endregion
    }
}
