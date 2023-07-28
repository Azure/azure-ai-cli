//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace Azure.AI.Details.Common.CLI
{
    class CommandParseDispatcher
    {
        public static bool DispatchParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            var command = tokens.PeekNextToken();
            if (command == null)
            {
                values.AddDisplayHelpRequest();
                return false;
            }

            if (command.StartsWith("@") && tokens.PeekNextToken(1) == null)
            {
                var content = FileHelpers.ExpandAtFileValue(command, values);
                var line = content.Split('\r', '\n')[0];
                if (line.StartsWith("x.command="))
                {
                    command = line.Substring("x.command=".Length);
                    values.Add("x.command", command);
                    values.Add("x.command.nodefaults", "true");
                }
            }

            var parsed = Program.DispatchParseCommand(tokens, values);
            if (parsed || values.DisplayHelpRequested()) return parsed;

            switch (command.Split('.')[0])
            {
                case "-?":
                case "-h":
                case "--?":
                case "--help":
                    values.AddDisplayHelpRequest();
                    break;

                default:
                    values.AddError("ERROR:", $"Unknown command: {command}");
                    values.AddDisplayHelpRequest();
                    break;
            }

            return parsed;
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return Program.DispatchParseCommandValues(tokens, values);
        }
    }

    class CommandParser
    {
        protected static bool ParseCommandValues(string commandName, IEnumerable<INamedValueTokenParser> parsers, INamedValueTokens tokens, ICommandValues values)
        {
            return commandName != null && values.GetCommand().StartsWith(commandName) && ParseAllCommandValues(parsers, tokens, values);
        }

        protected static bool ParseCommand(string commandName, IEnumerable<INamedValueTokenParser> parsers, INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandName(commandName, tokens, values) &&
                   ParseCommandDefaults(commandName, parsers, tokens, values) &&
                   ParseAllCommandValues(parsers, tokens, values);
        }

        protected static bool ParseCommands(IEnumerable<(string commandName, bool valuesRequired)> commands, IEnumerable<string> partials, INamedValueTokens tokens, ICommandValues values, Func<ICommandValues, IEnumerable<INamedValueTokenParser>> parsers)
        {
            return ParseCommandName(commands, partials, tokens, values) &&
                   ParseCommandDefaults(values.GetCommand(), parsers(values), tokens, values) &&
                   ParseAllCommandValues(parsers(values), tokens, values);
        }

        protected static bool ParseCommandName(IEnumerable<(string name, bool requireCommandValues)> subCommands, IEnumerable<string> partials, INamedValueTokens tokens, ICommandValues values)
        {
            bool parsed = true;
            foreach (var (commandName, requireCommandValues) in subCommands)
            {
                parsed = ParseCommandName(commandName, tokens, values, requireCommandValues);
                if (parsed) break;
            }

            if (!parsed && !values.DisplayHelpRequested())
            {
                foreach (var name in partials)
                {
                    if (ParseCommandName(name, tokens, values, false))
                    {
                        values.AddDisplayHelpRequest();

                        var forceNotVerbose = values.GetOrDefault("x.verbose", null) == null;
                        if (forceNotVerbose) values.Add("x.verbose", "false");

                        var token = tokens.PeekNextToken();
                        if (token != null && !token.StartsWith("@") && !token.StartsWith("-"))
                        {
                            values.AddError("ERROR:", $"Unknown command: {token}");
                        }

                        break;
                    }
                }
            }

            return parsed;
        }

        protected static bool ParseCommandName(string commandName, INamedValueTokens tokens, ICommandValues values, bool requireCommandValues = true)
        {
            if (commandName == values.GetCommand()) return true;

            var parsedDotName = commandName.Contains(".") && ParseCommandDotName(commandName, tokens, values, requireCommandValues);
            var parsed = parsedDotName || ParseCommandName1(commandName, tokens, values, requireCommandValues);

            if (parsed)
            {
                var noCommandValues = tokens.PeekNextToken() == null;
                if (noCommandValues && requireCommandValues)
                {
                    values.AddDisplayHelpRequest();
                    return false;
                }

                return true;
            }

            return parsed;
        }

        protected static bool ParseCommandName1(string commandName, INamedValueTokens tokens, ICommandValues values, bool requireCommandValues = true)
        {
            if (tokens.PeekNextTokenValue() == commandName)
            {
                tokens.SkipTokens(1);
                values.Add("x.command", commandName);
            }

            return values.Contains("x.command") && values.GetCommand() == commandName;
        }

        protected static bool ParseCommandDotName(string commandName, INamedValueTokens tokens, ICommandValues values, bool requireCommandValues = true)
        {
            bool parsed = true;

            var skip = 0;
            foreach (var part in commandName.Split('.'))
            {
                if (tokens.PeekNextTokenValue(skip) != part)
                {
                    parsed = false;
                    break;
                }

                skip++;
            }

            if (parsed)
            {
                tokens.SkipTokens(skip);
                values.Add("x.command", commandName);
            }

            return parsed;
        }

        protected static bool ParseCommandDefaults(string commandName, IEnumerable<INamedValueTokenParser> parsers, INamedValueTokens tokens, ICommandValues values)
        {
            var noDefaults = values.GetOrDefault("x.command.nodefaults", false);
            if (commandName != "config" && commandName != "help" && !noDefaults)
            {
                var token = tokens.PeekNextToken();
                if (token == "--nodefaults")
                {
                    tokens.PopNextToken();
                    return true;
                }

                var existing = FileHelpers.FindFileInConfigPath($"{Program.Name}.defaults", values);
                if (existing != null)
                {
                    return ParseIniFile($"{Program.Name}.defaults", parsers, values);
                }
            }

            return true;
        }

        protected static bool ParseAllCommandValues(IEnumerable<INamedValueTokenParser> parsers, INamedValueTokens tokens, ICommandValues values)
        {
            if (tokens.PeekNextToken() == "help")
            {
                tokens.PopNextToken();
                values.AddDisplayHelpRequest();

                values.Add("display.help.more", tokens.PeekAllTokens());
                while (tokens.PopNextToken() != null) ;

                return true;
            }

            bool parsed = true;
            while (tokens.PeekNextToken() != null)
            {
                parsed = tokens.PeekNextToken().StartsWith("@")
                    ? ParseAtFileToken(parsers, tokens, values)
                    : ParseNextCommandValue(parsers, tokens, values);

                if (!parsed) break;
            }

            if (!parsed && tokens.PeekNextToken() != null)
            {
                ParseInvalidArgumentError(tokens, values, "command line argument(s)");
            }

            return parsed;
        }

        private static bool ParseNextCommandValue(IEnumerable<INamedValueTokenParser> parsers, INamedValueTokens tokens, INamedValues values)
        {
            bool parsed = false;
            foreach (var parser in parsers)
            {
                parsed = parser.Parse(tokens, values);
                if (parsed) break;
            }

            if (parsed && ShouldParseIniFileValue(values))
            {
                parsed = ParseIniFileValue(parsers, values);
            }

            var help = values.GetOrDefault("display.help", false);
            if (parsed && help)
            {
                values.Add("display.help.more", tokens.PeekAllTokens());
                while (tokens.PopNextToken() != null) ;
                return true;
            }

            return parsed;
        }

        private static bool ParseAtFileToken(IEnumerable<INamedValueTokenParser> parsers, INamedValueTokens tokens, ICommandValues values)
        {
            if (tokens.PeekNextToken().StartsWith("@"))
            {
                var iniFile = tokens.PeekNextTokenValue(0, values);
                if (ParseIniFileLines(iniFile, parsers, values))
                {
                    tokens.PopNextToken();
                    return true;
                }
            }
            return false;
        }

        private static bool ShouldParseIniFileValue(INamedValues values)
        {
            return values.Contains("ini.file");
        }

        private static bool ParseIniFileValue(IEnumerable<INamedValueTokenParser> parsers, INamedValues values)
        {
            var iniFile = values["ini.file"];
            values.Reset("ini.file");

            return ParseIniFileLines(iniFile, parsers, values);
        }

        private static bool ParseIniFile(string fileName, IEnumerable<INamedValueTokenParser> parsers, INamedValues values)
        {
            fileName = FileHelpers.DemandFindFileInConfigPath(fileName, values, "configuration");
            var lines = FileHelpers.ReadAllText(fileName, Encoding.UTF8);
            return ParseIniFileLines(lines, parsers, values);
        }

        private static bool ParseIniFileLines(string lines, IEnumerable<INamedValueTokenParser> parsers, INamedValues values)
        {
            bool parsed = true;
            var splitLines = lines.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in splitLines)
            {
                parsed = line.StartsWith("@") && FileHelpers.FileExistsInConfigPath(line[1..], values)
                    ? ParseIniFile(line.Substring(1), parsers, values)
                    : ParseIniFileLine(line, parsers, values);
                if (!parsed) break;
            }
            return parsed;
        }

        private static bool ParseIniFileLine(string line, IEnumerable<INamedValueTokenParser> parsers, INamedValues values)
        {
            bool parsed = true;
            var tokens = new IniLineTokenSource(line);
            while (tokens.PeekNextToken() != null)
            {
                parsed = ParseNextCommandValue(parsers, tokens, values);
                if (!parsed) break;
            }

            if (!parsed && tokens.PeekNextToken() != null)
            {
                ParseInvalidArgumentError(tokens, values, "@FILE content");
            }

            return parsed;
        }

        protected static void ParseInvalidArgumentError(INamedValueTokens tokens, INamedValues values, string kind)
        {
            if (!values.Contains("error"))
            {
                var restOfTokens = tokens.PeekAllTokens();
                var command = values.GetCommandForDisplay();
                values.AddError(
                    "ERROR:", $"Invalid {kind} at \"{restOfTokens}\".", "",
                      "SEE:", $"{Program.Name} help {command}");
            }
        }
    }
}
