//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.IO;

namespace Azure.AI.Details.Common.CLI
{
    public class ConfigCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            var parsed = false;

            var token = tokens.PeekNextToken();
            if (token != null && token == "config")
            {
                values.Add("x.command", tokens.PopNextToken());
                token = tokens.PeekNextToken();

                if (token == "." || token == "local" || token == "user" || token == "global" || token == "system")
                {
                    values.Add("x.config.scope.hive", tokens.PopNextToken());
                    token = tokens.PeekNextToken();
                    parsed = true;
                }

                if (token != null && !token.StartsWith("@") && !token.StartsWith("-"))
                {
                    var validCommand = IsValidConfigScope(token);
                    if (!validCommand)
                    {
                        ParseInvalidArgumentError(tokens, values, "command line argument(s)");
                        return false;
                    }

                    values.Add("x.config.scope.command", tokens.PopNextToken());
                    token = tokens.PeekNextToken();
                    parsed = true;
                }

                if (token != null && token.StartsWith("@"))
                {
                    values.Add("x.config.command.at.file", tokens.PopNextToken());
                    token = tokens.PeekNextToken();
                    parsed = true;
                }

                if (!parsed && token == null)
                {
                    values.AddDisplayHelpRequest();
                    return false;
                }

                parsed = ParseCommandValues(tokens, values);
            }

            var forceNotVerbose = parsed && values.GetOrDefault("x.verbose", null) == null;
            if (forceNotVerbose) values.Add("x.verbose", "false");

            return parsed;
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("config", configCommandParsers, tokens, values);
        }

        public static bool IsValidConfigScope(string check)
        {
            return $";{Program.ConfigScopeTokens};".Contains($";{check};");
        }

        #region private data

        private static INamedValueTokenParser[] configCommandParsers = {

            new NamedValueTokenParser("--help",       "--?", "1", "0", null, null, "true", "display.help"),
            // new NamedValueTokenParser("--cls",        "x.cls", "01", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser("--pause",      "x.pause", "01", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser("--quiet",      "x.quiet", "01", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser("--verbose",    "x.verbose", "01", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null,           "x.input.path", "001", "1"),
            new NamedValueTokenParser(null,           "x.output.path", "011", "1"),
            new NamedValueTokenParser(null,           "x.run.time", "111", "1"),

            new NamedValueTokenParser("--hive",       "x.config.scope.hive", "0001", "1"),
            new NamedValueTokenParser("--region",     "x.config.scope.region", "0001", "1"),
            new NamedValueTokenParser("--scope",      "x.config.scope.command", "0001", "1", Program.ConfigScopeTokens),

            new NamedValueTokenParser("-s",           "x.config.command.set", "0001", "2;1"),
            new NamedValueTokenParser("-a",           "x.config.command.add", "0001", "2;1"),
            new NamedValueTokenParser("-f",           "x.config.command.find", "0001", "1;0", null, null, "*"),
            new NamedValueTokenParser("-c",           "x.config.command.clear", "0001", "1;0", null, null, "*"),

        };

        #endregion

    }
}
