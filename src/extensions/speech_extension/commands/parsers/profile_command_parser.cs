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

            new NamedValueTokenParser(null,           "x.command", "11", "1", "speaker"),

            new ExpectOutputTokenParser(),
            new DiagnosticLogTokenParser(),
            new CommonNamedValueTokenParsers(),

            new NamedValueTokenParser("--ini", "ini.file", "10", "1", "@"),

            new NamedValueTokenParser(null,    "profile.list", "01", "0"),
            new NamedValueTokenParser(null,    "profile.create", "01", "0"),
            new NamedValueTokenParser(null,    "profile.delete", "01", "1"),
            new NamedValueTokenParser(null,    "profile.status", "01", "1"),
            new NamedValueTokenParser(null,    "profile.enroll", "01", "0"),
            new NamedValueTokenParser(null,    "speaker.identify", "01", "0"),
            new NamedValueTokenParser(null,    "speaker.verify", "01", "0"),
            new NamedValueTokenParser(null,    "profile.id", "01", "1"),
            new NamedValueTokenParser(null,    "profile.kind", "01", "1"),
            new NamedValueTokenParser(null,    "profile.input.microphone", "001", "1;0", null, null, "microphone", "profile.input.type"),
            new NamedValueTokenParser(null,    "profile.input.type", "011", "1", "file;microphone"),
            new NamedValueTokenParser(null,    "profile.input.file", "001", "1", null, "profile.input.file", "file", "profile.input.type"),
            new NamedValueTokenParser(null,    "profile.output.file", "011", "1"),
            new NamedValueTokenParser(null,    "profile.output.json.file", "0110", "1"),
            new NamedValueTokenParser(null,    "profile.output.request.file", "0110", "1"),
            new NamedValueTokenParser("--language", "speaker.source.language", "101", "1"),
            new NamedValueTokenParser("--language", "profile.source.language", "101", "1"),
            new NamedValueTokenParser(null, "profile.output.id", "011", "1", "@@"),
            new NamedValueTokenParser(null, "profile.output.ids", "011", "1", "@@"),
            new NamedValueTokenParser(null, "profile.output.add.id", "0111;0111", "1", "@@"),
        };

        #endregion
    }
}
