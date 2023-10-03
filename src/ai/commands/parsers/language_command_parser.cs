//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    class LanguageCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("language", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("language.extract", false),
            ("language.classify", false),
            ("language.understand", false),
            ("language.summarize", false),
            ("language.translate", false)
        };

        private static readonly string[] _partialCommands = {
            "language"
        };

        private static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();
            foreach (var command in _commands)
            {
                if (commandName == command.name)
                {
                    return _languagePlaceHolderParsers;
                }
            }

            return null;
        }

        #region private data

        public class CommonLanguageNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonLanguageNamedValueTokenParsers() : base(

                    new NamedValueTokenParser(null, "x.command", "11", "1"),

                    new CommonNamedValueTokenParsers(),
                    new NamedValueTokenParser("--ini", "ini.file", "10", "1", "@"),

                    new NamedValueTokenParser(null, "x.command.expand.file.name", "11111", "1"),

                    new ExpectConsoleOutputTokenParser(),
                    new DiagnosticLogTokenParser()

                )
            {
            }
        }

        private static INamedValueTokenParser[] _languagePlaceHolderParsers = {

            new CommonLanguageNamedValueTokenParsers()

        };

        #endregion
    }
}
