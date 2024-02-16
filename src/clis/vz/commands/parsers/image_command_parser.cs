//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    class ImageCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("image", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("image.analyze", true),
            ("image.read", true)
        };

        private static readonly string[] _partialCommands = {
            "image"
        };

        private static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();
            switch (commandName)
            {
                case "image.analyze":
                    return imageAnalyzeParsers;

                case "image.read":
                    return imageReadCommandParsers;
            }

            return null;
        }

        #region private data

        public class CommonImageNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonImageNamedValueTokenParsers() : base(

                    new NamedValueTokenParser(null, "x.command", "11", "1"),

                    new CommonNamedValueTokenParsers(),
                    new NamedValueTokenParser(null, "x.command.expand.file.name", "11111", "1"),

                    new ExpectOutputTokenParser(),
                    new DiagnosticLogTokenParser(),

                    new VisionServiceOptionsTokenParser(),

                    new NamedValueTokenParser("--id",         "vision.input.id", "001", "1"),
                    new NamedValueTokenParser("--url",        "vision.input.file", "001", "1", null, null, "file", "vision.input.type"),
                    new NamedValueTokenParser("--urls",       "vision.input.files", "001", "1", null, null, "vision.input.file", "x.command.expand.file.name"),
                    new NamedValueTokenParser(null,           "vision.input.camera.device", "0010", "1;0", null, null, "camera", "vision.input.type"),
                    new NamedValueTokenParser(null,           "vision.input.type", "011", "1", "file;files;camera"),

                    new NamedValueTokenParser("--languages",  "source.language.config", "100;010", "1")

                )
            {
            }
        }

        private static INamedValueTokenParser[] imageAnalyzeParsers = {

            new CommonImageNamedValueTokenParsers(),
            new NamedValueTokenParser(null, "vision.image.visual.features", "0001", "1")
        };

        private static INamedValueTokenParser[] imageReadCommandParsers = {

            new CommonImageNamedValueTokenParsers()

        };

        #endregion
    }
}
