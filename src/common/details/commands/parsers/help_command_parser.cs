//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    public class HelpCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            var token = tokens.PeekNextToken();
            if (token != null && (token == "help" || token == "--help"))
            {
                values.AddDisplayHelpRequest();
                values.Add("x.command", "help");

                tokens.SkipTokens(1);
                token = tokens.PeekNextToken();

                return token == "topics" || token == "list" || token == "find" || token == "expand"
                    ? ParseListFindOrExpandHelpCommand(tokens, values)
                    : ParseHelpCommandTokens(tokens, values);
            }

            return false;
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("help", Enumerable.Empty<INamedValueTokenParser>(), tokens, values);
        }

        public static bool DisplayHelp(INamedValueTokens tokens, INamedValues values)
        {
            var topicSearch = values.GetOrEmpty("display.help.topic");
            if (!string.IsNullOrEmpty(topicSearch)) return DoListHelpTopics(topicSearch, values);

            var textSearch = values.GetOrEmpty("display.help.text");
            if (!string.IsNullOrEmpty(textSearch)) return DoFindHelpTopics(textSearch, values);

            return DoDisplayHelpTopic(values);
        }

        private static bool ParseHelpCommandTokens(INamedValueTokens tokens, ICommandValues values)
        {
            var token = tokens.PeekNextToken();
            values.Add("display.help.tokens", PeekParseAllTokens(0, tokens, values));

            string command = TrimHelpTokens(ParseHelpCommandToken(tokens, values, ref token));
            string option = TrimHelpTokens(ParseHelpCommandOptionToken(tokens, ref token));

            string more = TrimHelpTokens(ParseHelpCommandMoreTokens(tokens, ref token));
            more = more.Replace(" ", ".");

            if (string.IsNullOrEmpty(command) && string.IsNullOrEmpty(option) && string.IsNullOrEmpty(more))
            {
                more = "help";
            }

            values.Add("display.help.command", command);
            values.Add("display.help.option", option);
            values.Add("display.help.more", more);

            return true;
        }

        private static string ParseHelpCommandToken(INamedValueTokens tokens, ICommandValues values, ref string? token)
        {
            string command = values.GetOrEmpty("x.command");
            if (command == "help") command = "";

            if (string.IsNullOrEmpty(command) && token != null && IsHelpCommandToken(token))
            {
                command = token;

                tokens.SkipTokens(1);
                token = tokens.PeekNextToken();

                while (token != null && !token.StartsWith("-") && !token.StartsWith("@"))
                {
                    tokens.SkipTokens(1);
                    command = command + "." + token;
                    token = tokens.PeekNextToken();
                }
            }

            return command;
        }

        private static bool IsHelpCommandToken(string token)
        {
            return $";{Program.HelpCommandTokens};".Contains($";{token};");
        }

        private static string ParseHelpCommandOptionToken(INamedValueTokens tokens, ref string? token)
        {
            string option = "";
            if (token != null && token.StartsWith("--"))
            {
                option = token.Substring(2);
                tokens.SkipTokens(1);
                token = tokens.PeekNextToken();
            }

            return option;
        }

        private static string ParseHelpCommandMoreTokens(INamedValueTokens tokens, ref string? token)
        {
            string more = "";
            while (token != null)
            {
                more += token + " ";
                tokens.SkipTokens(1);
                token = tokens.PeekNextToken();
            }

            return more.TrimEnd();
        }

        private static bool ParseListFindOrExpandHelpCommand(INamedValueTokens tokens, ICommandValues values)
        {
            var token = tokens.PeekNextToken();
            if (token == "list" || token == "topics")
            {
                values.Add($"display.help.topic", PeekParseAllTokens(1, tokens, values, "*"));
            }
            else if (token == "find")
            {
                values.Add($"display.help.text", PeekParseAllTokens(1, tokens, values));
            }
            else if (!string.IsNullOrEmpty(token))
            {
                values.Add($"display.help.text", PeekParseAllTokens(0, tokens, values));
            }
            else
            {
                values.Add($"display.help.topic", "*");
            }

            tokens.SkipTokens(int.MaxValue);
            return true;
        }

        private static string PeekParseAllTokens(int skip, INamedValueTokens tokens, ICommandValues values, string defaultValue = "")
        {
            tokens.SkipTokens(skip);

            var findTopics = false;
            var findText = false;
            var trimmedTokens = tokens.PeekAllTokens();

            int len;
            do
            {
                len = trimmedTokens.Length;
                trimmedTokens = trimmedTokens
                    .IfTrimStartsWith("expand", x => values.AddExpandHelpRequest(true))
                    .IfTrimStartsWith("dump", x => values.AddDumpHelpRequest(true))
                    .IfTrimStartsWith("--topics", x => findTopics = true)
                    .IfTrimStartsWith("--topic", x => findTopics = true)
                    .IfTrimStartsWith("topics", x => findTopics = true)
                    .IfTrimStartsWith("topic", x => findTopics = true)
                    .IfTrimStartsWith("--text", x => findText = true)
                    .IfTrimStartsWith("text", x => findText = true)
                    .IfTrim("--expand false", x => values.AddExpandHelpRequest(false))
                    .IfTrim("--expand true", x => values.AddExpandHelpRequest(true))
                    .IfTrim("--expand", x => values.AddExpandHelpRequest(true))
                    .IfTrim("--dump false", x => values.AddDumpHelpRequest(false))
                    .IfTrim("--dump true", x => values.AddDumpHelpRequest(true))
                    .IfTrim("--dump", x => values.AddDumpHelpRequest(true))
                    .IfTrim("--interactive false", x => values.Add("x.help.interactive", "false"))
                    .IfTrim("--interactive true", x => values.Add("x.help.interactive", "true"))
                    .IfTrim("--interactive", x => values.Add("x.help.interactive", "true"));
            } while (trimmedTokens.Length != len);

            if (string.IsNullOrEmpty(trimmedTokens) && !string.IsNullOrEmpty(defaultValue))
            {
                trimmedTokens = defaultValue;
            }

            if (findTopics) values.Add("display.help.topic", trimmedTokens);
            if (findText) values.Add("display.help.text", trimmedTokens);

            return trimmedTokens;
        }

        private static string TrimHelpTokens(string allTokens)
        {
            return allTokens
                .IfTrimStartsWith("expand", x => {})
                .IfTrim("--expand false", x => {})
                .IfTrim("--expand true", x => {})
                .IfTrim("--expand", x => {})
                .IfTrim("--interactive false", x => {})
                .IfTrim("--interactive true", x => {})
                .IfTrim("--interactive", x => {});
        }

        private static bool DoDisplayHelpTopic(INamedValues values)
        {
            var allTokens = values.GetOrEmpty("display.help.tokens");
            if (allTokens == "colors")
            {
                ColorHelpers.ShowColorChart();
                return true;
            }

            string path = FindHelpTopic(values, allTokens);

            var result = path != null ? DisplayHelp(path, values) : DisplayDefaultHelp();

            var checkTokensFound = allTokens?.Replace(" ", ".").Replace("--", "").Split('.');
            var foundAllTokens = path == null || checkTokensFound?.Count(x => path.Contains(x)) == checkTokensFound?.Count();

            if (!foundAllTokens)
            {
                ErrorHelpers.WriteLineMessage(
                    "WARNING:", $"Cannot find help topic for \"{allTokens}\"\n",
                        "TRY:", $"{Program.Name} help find \"{allTokens}\"",
                                $"{Program.Name} help find topics \"{allTokens}\"",
                                $"{Program.Name} help find text \"{allTokens}\" --expand",
                                $"{Program.Name} help documentation\n");
                values?.Add("display.help.exit.code", "1");
            }

            return result;
        }

        private static bool DisplayHelp(string path, INamedValues values)
        {
            var interactive = values.GetOrDefault("x.help.interactive", true) &&
                values.GetCommand() == "help" &&
                !Console.IsInputRedirected &&
                !Console.IsOutputRedirected;

            if (interactive)
            {
                string text = FileHelpers.ReadAllHelpText(path, Encoding.UTF8);

                var normal = new Colors(ConsoleColor.White, ConsoleColor.Blue);
                var selected = ColorHelpers.GetHighlightColors(ConsoleColor.White, ConsoleColor.Blue);

                var lines = text.Split('\n').Select(x => x.TrimEnd('\r')).ToArray();
                ConsoleGui.HelpViewer.DisplayHelpText(lines, Console.WindowWidth - 1, Console.WindowHeight, normal, selected, 0, 0, 0);
            }

            if (!values.GetOrDefault("x.quiet", false) || !interactive)
            {
                FileHelpers.PrintHelpFile(path);
            }

            return values?.GetOrDefault("display.help.exit.code", 0) == 0;
        }

        private static bool DisplayDefaultHelp()
        {
            Console.WriteLine(@"  ______ ___ _  __");
            Console.WriteLine(@" /  ___// _ \ \/ /");
            Console.WriteLine(@" \___ \/ ___/   <");
            Console.WriteLine(@"/____ /_/  /__/\_\");
            Console.WriteLine();
            Console.WriteLine($"USAGE: {Program.Name} <command> [...]");
            Console.WriteLine();
            return false;
        }

        private static bool DoListHelpTopics(string find, INamedValues values)
        {
            List<string> found = FileHelpers.FindHelpFiles(find, values).Distinct().ToList();
            if (found.Count() == 0)
            {
                ErrorHelpers.WriteLineMessage(
                    "WARNING:", $"Cannot find \"{find}\"; help topic not found!\n",
                        "TRY:", $"{Program.Name} help find \"{find}\"",
                                $"{Program.Name} help find text \"{find}\" --expand",
                                $"{Program.Name} help documentation\n");
                return false;
            }

            DisplayFoundHelpFiles(found, values);
            return true;
        }

        private static bool DoFindHelpTopics(string search, INamedValues values)
        {
            List<string> found = FileHelpers.FindHelpFiles("*", values);
            if (found.Count() == 0) return false;

            found = found.Where(x => HelpContainsText(x, search)).Distinct().ToList();
            if (found.Count() == 0)
            {
                ErrorHelpers.WriteLineMessage(
                    "WARNING:", $"Cannot find text \"{search}\"; no help topics not found!\n",
                        "TRY:", $"{Program.Name} help find \"{search}\"",
                                $"{Program.Name} help find topics \"{search}\"",
                                $"{Program.Name} help documentation\n");
                return false;
            }

            DisplayFoundHelpFiles(found, values);
            return true;
        }

        private static void DisplayFoundHelpFiles(List<string> found, INamedValues values)
        {
            var interactive = values.GetOrDefault("x.help.interactive", true) && 
                !Console.IsInputRedirected && 
                !Console.IsOutputRedirected;

            if (values.ExpandHelpRequested())
            {
                ExpandHelpFiles(found, values, interactive);
            }
            else if (values.DumpHelpRequested())
            {
                FileHelpers.DumpFoundHelpFiles(found);
            }
            else
            {
                DisplayHelpFiles(found, values, interactive);
            }
        }

        private static void ExpandHelpFiles(List<string> found, INamedValues values, bool interactive)
        {
            if (interactive)
            {
                var normal = new Colors(ConsoleColor.White, ConsoleColor.Blue);
                var selected = ColorHelpers.GetHighlightColors(ConsoleColor.White, ConsoleColor.Blue);

                var expanded = FileHelpers.ExpandFoundHelpFiles(found);
                var lines = expanded.Split('\n').Select(x => x.TrimEnd('\r')).ToArray();
                ConsoleGui.HelpViewer.DisplayHelpText(lines, Console.WindowWidth - 1, Console.WindowHeight, normal, selected, 0, 0, 0);
            }

            if (!values.GetOrDefault("x.quiet", false) || !interactive)
            {
                FileHelpers.PrintExpandedFoundHelpFiles(found);
            }
        }

        private static void DisplayHelpFiles(List<string> found, INamedValues values, bool interactive)
        {
            if (interactive)
            {
                var normal = new Colors(ConsoleColor.White, ConsoleColor.Blue);
                var selected = ColorHelpers.GetHighlightColors(ConsoleColor.White, ConsoleColor.Blue);

                var topics = found.Select(x => FileHelpers.HelpTopicNameFromHelpFileName(x)).ToArray();
                ConsoleGui.HelpViewer.DisplayHelpTopics(topics, Console.WindowWidth, Console.WindowHeight, normal, selected);
            }

            if (!values.GetOrDefault("x.quiet", false) || !interactive)
            {
                FileHelpers.PrintFoundHelpFiles(found, values);
            }
        }

        private static bool HelpContainsText(string help, string search)
        {
            var file = help.Replace(" ", ".");
            var text = FileHelpers.ReadAllHelpText(file, Encoding.UTF8);
            return (text.ToLower().Contains(search));
        }

        private static string FindHelpTopic(INamedValues values, string allTokens)
        {
            var command = values.GetOrEmpty("display.help.command");
            var option = values.GetOrEmpty("display.help.option");
            var more = values.GetOrEmpty("display.help.more");

            if (string.IsNullOrEmpty(command)) command = values.GetOrEmpty("x.command");

            string? path = FindHelpFile(command, option, more);

            string partial = command;
            while (path == null && partial.Contains("."))
            {
                partial = partial.Substring(partial.IndexOf('.') + 1);
                path = FindHelpFile(partial, option, more);
            }

            partial = command;
            while (path == null && partial.Contains("."))
            {
                partial = partial.Substring(0, partial.LastIndexOf("."));
                path = FindHelpFile(partial, option, more);
            }

            partial = allTokens.Replace(" ", ".").Replace("--", "");
            while (path == null && partial.Contains("."))
            {
                partial = partial.Substring(0, partial.LastIndexOf("."));
                path = FindHelpFile(partial, option, more);
            }

            if (path == null && more == "help") path = FileHelpers.FindFileInHelpPath("help/help");
            if (path == null) path = FileHelpers.FindFileInHelpPath("help/usage");
            return path;
        }

        private static string? FindHelpFile(string command, string option, string more)
        {
            var hasCommand = !string.IsNullOrEmpty(command);
            var hasOption = !string.IsNullOrEmpty(option);
            var hasMore = !string.IsNullOrEmpty(more);

            string? path = hasCommand && hasOption && hasMore
                ? FileHelpers.FindFileInHelpPath($"help/{command}.{option}.{more}")
                : null;
            if (path == null && hasCommand && hasMore) path = FileHelpers.FindFileInHelpPath($"help/{command}.{more}");
            if (path == null && hasOption && hasMore) path = FileHelpers.FindFileInHelpPath($"help/{option}.{more}");
            if (path == null && hasOption) path = FileHelpers.FindFileInHelpPath($"help/{option}");
            if (path == null && hasMore) path = FileHelpers.FindFileInHelpPath($"help/{more}");
            if (path == null && hasCommand) path = FileHelpers.FindFileInHelpPath($"help/{command}");

            return path;
        }
    }
}
