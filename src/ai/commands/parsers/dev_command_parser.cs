//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Collections.Generic;

namespace Azure.AI.Details.Common.CLI
{
    class DevCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("dev", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("dev.new.list", false),
            ("dev.new", true),
            ("dev.shell", false),
        };

        private static readonly string[] _partialCommands = {
            "dev.new",
            "dev"
        };

        private static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommandOrEmpty();

            switch (commandName)
            {
                case "dev.new.list": return _devNewParsers;
                case "dev.new": return _devNewParsers;
                case "dev.shell": return _devShellParsers;
            }

            foreach (var command in _commands)
            {
                if (commandName == command.name)
                {
                    return _devPlaceHolderParsers;
                }
            }

            return null;
        }

        public class CommonDevNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonDevNamedValueTokenParsers() : base(

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

        private static INamedValueTokenParser[] _devPlaceHolderParsers = {
            new CommonDevNamedValueTokenParsers()
        };

        private static INamedValueTokenParser[] _devNewParsers = {
            new CommonDevNamedValueTokenParsers(),
            ArgXToken.Parser(),
            InstructionsToken.Parser(),
            ProgrammingLanguageToken.Parser(),
        };

        private static INamedValueTokenParser[] _devShellParsers = {
            new CommonDevNamedValueTokenParsers(),
            RunCommandScriptToken.Parser(),
            RunBashScriptToken.Parser()
        };
    }
}
