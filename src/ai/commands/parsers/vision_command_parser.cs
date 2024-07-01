//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    class VisionCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("vision", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("vision.form", true),
            ("vision.document", true),
            ("vision.image.analyze", true),
            ("vision.image.read", true),
            ("vision.image", true),
            ("vision.person.group.create", true),
            ("vision.person.group.list", false),
            ("vision.person.group.update", true),
            ("vision.person.group.delete", true),
            ("vision.person.group", true),
            ("vision.person.create", true),
            ("vision.person.list", false),
            ("vision.person.update", true),
            ("vision.person.delete", true),
            ("vision.person.face.add", true),
            ("vision.person.face.delete", true),
            ("vision.person.face.list", false),
            ("vision.person.face.update", true),
            ("vision.person.face", true),
            ("vision.person", true),
            ("vision.face.identify", true),
            ("vision.face.verify", true),
            ("vision.face", true),
            ("vision", true)
        };

        private static readonly string[] _partialCommands = {
            "vision.form",
            "vision.document",
            "vision.image",
            "vision.person.group",
            "vision.person.face",
            "vision.person",
            "vision.face",
            "vision"
        };

        private static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();
            switch (commandName)
            {
                case "vision.image":
                case "vision.image.analyze":
                    return imageAnalyzeParsers;
            }

            return null;
        }

        #region private data

        public class CommonVisionNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonVisionNamedValueTokenParsers() : base(

                    new Any1ValueNamedValueTokenParser(null, "x.command", "11"),

                    new CommonNamedValueTokenParsers(),
                    new IniFileNamedValueTokenParser(),
                    new ExpandFileNameNamedValueTokenParser(),
                    new Any1ValueNamedValueTokenParser(null, "service.config.endpoint.uri", "0011"),
                    new Any1PinnedNamedValueTokenParser("--url", "vision.input.url", "001", "url", "vision.input.type"),
                    new Any1PinnedNamedValueTokenParser("--file", "vision.input.file", "001", "file", "vision.input.type"),
                    new Any1ValueNamedValueTokenParser("--outputtype", "vision.output.type", "011"),
                    new ExpectConsoleOutputTokenParser(),
                    new DiagnosticLogTokenParser()

                )
            {
            }
        }

        private static INamedValueTokenParser[] _visionPlaceHolderParsers = {

            new CommonVisionNamedValueTokenParsers()

        };

        private static INamedValueTokenParser[] imageAnalyzeParsers = [
            new CommonVisionNamedValueTokenParsers(),
            new Any1ValueNamedValueTokenParser("--language", "vision.image.language", "001"),
            new Any1ValueNamedValueTokenParser("--gender-neutral-captions", "vision.image.gender.neutral.captions", "00111"),
            new Any1ValueNamedValueTokenParser("--smart-crop-aspect-ratios", "vision.image.smart.crop.aspect.ratios", "001111"),
            new Any1ValueNamedValueTokenParser("--model-version", "vision.image.model.version", "0011"),
            new Any1ValueNamedValueTokenParser("--visual-features", "vision.image.visual.features", "0011")
        ];

        #endregion
    }
}
