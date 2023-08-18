//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    class InitCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("init", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("init", false)
        };

        private static readonly string[] _partialCommands = {
        };

        private static INamedValueTokenParser[] GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();
            switch (commandName)
            {
                case "init":
                    return initCommandParsers;
            }

            return null;
        }

        #region private data

        public class CommonInitNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonInitNamedValueTokenParsers() : base(

                new NamedValueTokenParser(null, "x.command", "11", "1"),

                new CommonNamedValueTokenParsers(false),
                new ExpectConsoleOutputTokenParser(),
                new ParallelCommandsTokenParser(),

                new NamedValueTokenParser("--ini",  "ini.file", "10", "1", "@"),

                new NamedValueTokenParser("--subscription", "init.service.subscription", "001", "1"),
                new NamedValueTokenParser("--region", "init.service.resource.region.name", "00010", "1"),
                new NamedValueTokenParser("--group", "init.service.resource.group.name", "00010", "1"),
                new NamedValueTokenParser("--name", "init.service.cognitiveservices.resource.name", "00001", "1"),
                new NamedValueTokenParser("--kind", "init.service.cognitiveservices.resource.kind", "00001", "1"),
                new NamedValueTokenParser("--sku", "init.service.cognitiveservices.resource.sku", "00001", "1"),
                new NamedValueTokenParser("--yes", "init.service.cognitiveservices.terms.agree", "00001", "1;0", "true;false", null, "true"),

                new NamedValueTokenParser("--interactive", "init.service.interactive", "001", "1;0", "true;false", null, "true")

                // new NamedValueTokenParser(null, "init.output.azcli.command.file", "01100", "1", "@@"),
                // new NamedValueTokenParser(null, "init.output.azcli.json.file", "01110", "1", "@@")
            )
            {
            }
        }

        private static readonly INamedValueTokenParser[] initCommandParsers = {
            new CommonInitNamedValueTokenParsers()
        };

        #endregion
    }
}
