//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Collections.Generic;

namespace Azure.AI.Details.Common.CLI
{
    class TestCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("test", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("test.list", false),
            ("test.run", false),
        };

        private static readonly string[] _partialCommands = {
            "test"
        };

        private static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommandOrEmpty();

            switch (commandName)
            {
                case "test.list": return _testListParsers;
                case "test.run": return _testRunParsers;
            }

            foreach (var command in _commands)
            {
                if (commandName == command.name)
                {
                    return _testPlaceHolderParsers;
                }
            }

            return null;
        }

        public class CommonTestNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonTestNamedValueTokenParsers() : base(

                    new NamedValueTokenParser(null, "x.command", "11", "1"),

                    new ExpectOutputTokenParser(),
                    new DiagnosticLogTokenParser(),
                    new CommonNamedValueTokenParsers(),

                    new NamedValueTokenParser("--ini", "ini.file", "10", "1", "@"),

                    new NamedValueTokenParser(null, "x.command.expand.file.name", "11111", "1"),

                    FileOptionXToken.Parser(),
                    FilesOptionXToken.Parser(),

                    TestOptionXToken.Parser(),
                    TestsOptionXToken.Parser(),

                    ContainsOptionXToken.Parser(),
                    RemoveOptionXToken.Parser(),

                    SearchOptionXToken.Parser()
                )
            {
            }
        }

        private static INamedValueTokenParser[] _testPlaceHolderParsers = {
            new CommonTestNamedValueTokenParsers()
        };

        private static INamedValueTokenParser[] _testListParsers = {
            new CommonTestNamedValueTokenParsers()
        };

        private static INamedValueTokenParser[] _testRunParsers = {
            new CommonTestNamedValueTokenParsers(),
            OutputResultsFormatToken.Parser(),
            OutputResultsFileToken.Parser()
        };
    }
}
