//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    class ToolCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("tool", GetCommandParsers(values), tokens, values);
        }
        
        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("tool.dashboard.start", false),
            ("tool.dashboard.stop", false),
            ("tool", true),
            
        };
        
        private static readonly string[] _partialCommands = {
            "tool"
        };

        private static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();
            foreach (var command in _commands)
            {
                if (commandName == command.name)
                {
                    return _toolPlaceHolderParsers;
                }
            }

            return null;
        }

        #region private data

        public class CommonToolNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonToolNamedValueTokenParsers() : base(

                new Any1ValueNamedValueTokenParser(null, "x.command", "11"),

                new ExpectOutputTokenParser(),
                new DiagnosticLogTokenParser(),
                new CommonNamedValueTokenParsers(),
                new IniFileNamedValueTokenParser(),

                new ExpandFileNameNamedValueTokenParser(),

                ConfigEndpointUriToken.Parser(),
                ConfigDeploymentToken.Parser()

                )
            {
            }
        }

        private static INamedValueTokenParser[] _toolPlaceHolderParsers = {

            new CommonToolNamedValueTokenParsers()

        };

        #endregion
    }
}
