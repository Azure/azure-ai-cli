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

                    new NamedValueTokenParser(null, "x.command", "11", "1"),

                    new CommonNamedValueTokenParsers(),
                    new NamedValueTokenParser("--ini", "ini.file", "10", "1", "@"),
                    new NamedValueTokenParser(null, "x.command.expand.file.name", "11111", "1"),
                    new NamedValueTokenParser(null, "service.config.endpoint.uri", "0011", "1"),
                    new NamedValueTokenParser("--url", "vision.input.url", "001", "1", null, null, "url", "vision.input.type"),
                    new NamedValueTokenParser("--file", "vision.input.file", "001", "1", null, null, "file", "vision.input.type"),
                    new NamedValueTokenParser("--outputtype", "vision.output.type", "011", "1"),
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
            new NamedValueTokenParser("--language",  "vision.image.language", "001", "1"),
            new NamedValueTokenParser("--gender-neutral-captions",  "vision.image.gender.neutral.captions", "00111", "1"),
            new NamedValueTokenParser("--smart-crop-aspect-ratios",  "vision.image.smart.crop.aspect.ratios", "001111", "1"),
            new NamedValueTokenParser("--model-version",  "vision.image.model.version", "0011", "1"),
            new NamedValueTokenParser("--visual-features", "vision.image.visual.features", "0011", "1")
        ];

        #endregion
    }
}
