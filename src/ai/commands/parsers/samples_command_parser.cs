//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    class SamplesCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("samples", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("samples", true)
        };

        private static readonly string[] _partialCommands = {
            "samples"
        };

        private static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();
            foreach (var command in _commands)
            {
                if (commandName == command.name)
                {
                    return _samplesPlaceHolderParsers;
                }
            }

            return null;
        }

        #region private data

        public class CommonSamplesNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonSamplesNamedValueTokenParsers() : base(

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

        private static INamedValueTokenParser[] _samplesPlaceHolderParsers = {

            new CommonSamplesNamedValueTokenParsers()

        };

        #endregion
    }
}
