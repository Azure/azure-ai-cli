//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Collections.Generic;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    public class ProfileCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("profile", GetCommandParsers(values), tokens, values) ||
                   ParseCommandValues("speaker", GetCommandParsers(values), tokens, values);;
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("profile.list", false),
            ("profile.create", true),
            ("profile.status", true),
            ("profile.enroll", true),
            ("profile.delete", true),
            ("speaker.identify", true),
            ("speaker.verify", true)
        };

        private static readonly string[] _partialCommands = {
            "profile",
            "speaker"
        };

        public static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();
            switch (commandName.Replace("speech.", ""))
            {
                case "profile.list":
                case "profile.create":
                case "profile.status":
                case "profile.enroll":
                case "profile.delete":
                case "speaker.identify":
                case "speaker.verify":
                    return profileCommandParsers;
            }
            
            return null;
        }

        #region private data

        private static INamedValueTokenParser[] profileCommandParsers = {

            new RequiredValidValueNamedValueTokenParser(null, "x.command", "11", "speaker"),

            new ExpectOutputTokenParser(),
            new DiagnosticLogTokenParser(),
            new CommonNamedValueTokenParsers(),

            new IniFileNamedValueTokenParser(),

            new NamedValueTokenParser(null,    "profile.list", "01", "0"),
            new NamedValueTokenParser(null,    "profile.create", "01", "0"),
            new Any1ValueNamedValueTokenParser(null, "profile.delete", "01"),
            new Any1ValueNamedValueTokenParser(null, "profile.status", "01"),
            new NamedValueTokenParser(null,    "profile.enroll", "01", "0"),
            new NamedValueTokenParser(null,    "speaker.identify", "01", "0"),
            new NamedValueTokenParser(null,    "speaker.verify", "01", "0"),
            new Any1ValueNamedValueTokenParser(null, "profile.id", "01"),
            new Any1ValueNamedValueTokenParser(null, "profile.kind", "01"),
            new OptionalWithDefaultNamedValueTokenParser(null, "profile.input.microphone", "001", "microphone", "profile.input.type"),
            new RequiredValidValueNamedValueTokenParser(null, "profile.input.type", "011", "file;microphone"),
            new NamedValueTokenParser(null,    "profile.input.file", "001", "1", null, "profile.input.file", "file", "profile.input.type"),
            new Any1ValueNamedValueTokenParser(null, "profile.output.file", "011"),
            new Any1ValueNamedValueTokenParser(null, "profile.output.json.file", "0110"),
            new Any1ValueNamedValueTokenParser(null, "profile.output.request.file", "0110"),
            new Any1ValueNamedValueTokenParser("--language", "speaker.source.language", "101"),
            new Any1ValueNamedValueTokenParser("--language", "profile.source.language", "101"),
            new OutputFileNameNamedValueTokenParser(null, "profile.output.id", "011"),
            new OutputFileNameNamedValueTokenParser(null, "profile.output.ids", "011"),
            new OutputFileNameNamedValueTokenParser(null, "profile.output.add.id", "0111;0111"),
        };

        #endregion
    }
}
