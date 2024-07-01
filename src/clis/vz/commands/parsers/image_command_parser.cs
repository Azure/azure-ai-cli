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

                    new Any1ValueNamedValueTokenParser(null, "x.command", "11"),

                    new CommonNamedValueTokenParsers(),
                    new ExpandFileNameNamedValueTokenParser(),

                    new ExpectOutputTokenParser(),
                    new DiagnosticLogTokenParser(),

                    new VisionServiceOptionsTokenParser(),

                    new Any1ValueNamedValueTokenParser("--id", "vision.input.id", "001"),
                    new Any1PinnedNamedValueTokenParser("--url", "vision.input.file", "001", "file", "vision.input.type"),
                    new ExpandFileNameNamedValueTokenParser("--urls", "vision.input.files", "001", "vision.input.file"),
                    new OptionalWithDefaultNamedValueTokenParser(null, "vision.input.camera.device", "0010", "camera", "vision.input.type"),
                    new RequiredValidValueNamedValueTokenParser(null, "vision.input.type", "011", "file;files;camera"),

                    new Any1ValueNamedValueTokenParser("--languages", "source.language.config", "100;010")

                )
            {
            }
        }

        private static INamedValueTokenParser[] imageAnalyzeParsers = {

            new CommonImageNamedValueTokenParsers(),
            new Any1ValueNamedValueTokenParser(null, "vision.image.visual.features", "0001")
        };

        private static INamedValueTokenParser[] imageReadCommandParsers = {

            new CommonImageNamedValueTokenParsers()

        };

        #endregion
    }
}
