//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    class CompleteCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommand("complete", completeCommandParsers, tokens, values);
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("complete", completeCommandParsers, tokens, values);
        }

        #region private data

        private static INamedValueTokenParser[] completeCommandParsers = {

            new NamedValueTokenParser(null, "x.command", "11", "1", "complete"),

            new ExpectOutputTokenParser(),
            new DiagnosticLogTokenParser(),
            new CommonNamedValueTokenParsers(),

            new NamedValueTokenParser("--ini", "ini.file", "10", "1", "@"),

            new NamedValueTokenParser(null, "x.command.expand.file.name", "11111", "1"),

            ConfigEndpointUriToken.Parser(),
            ConfigDeploymentToken.Parser(),

            new NamedValueTokenParser("--interactive",  "complete.input.interactive", "001", "0", null, null, "interactive", "complete.input.type"),
            new NamedValueTokenParser("--interactive+", "complete.input.interactive+", "001", "0", null, null, "interactive+", "complete.input.type"),
            new NamedValueTokenParser(null,             "complete.input.type", "111", "1", "interactive;interactive+;text;ssml;text.file;ssml.file"),
        };

        #endregion
    }
}
