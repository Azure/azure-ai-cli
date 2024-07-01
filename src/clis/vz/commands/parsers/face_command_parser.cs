//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    class FaceCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("face", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("face.identify", true),
            ("face.verify", true)
        };

        private static readonly string[] _partialCommands = {
            "face"
        };

        private static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();
            switch (commandName)
            {
                case "face.identify":
                    return faceIdentifyParsers;

                case "face.verify":
                    return faceVerifyParsers;
            }

            return null;
        }

        #region private data

        public class CommonFaceNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonFaceNamedValueTokenParsers() : base(

                    new Any1ValueNamedValueTokenParser(null, "x.command", "11"),

                    new CommonNamedValueTokenParsers(),
                    new ExpandFileNameNamedValueTokenParser(),

                    new ExpectConsoleOutputTokenParser(),
                    new DiagnosticLogTokenParser(),

                    new VisionServiceOptionsTokenParser(),

                    new Any1PinnedNamedValueTokenParser("--url", "vision.input.file", "001", "file", "vision.input.type"),
                    new ExpandFileNameNamedValueTokenParser("--urls", "vision.input.files", "001", "vision.input.file"),
                    new OptionalWithDefaultNamedValueTokenParser(null, "vision.input.camera.device", "0010", "camera", "vision.input.type"),
                    new RequiredValidValueNamedValueTokenParser(null, "vision.input.type", "011", "file;files;camera")

                )
            {
            }
        }

        private static INamedValueTokenParser[] faceIdentifyParsers = {

            new CommonFaceNamedValueTokenParsers()

        };

        private static INamedValueTokenParser[] faceVerifyParsers = {

            new CommonFaceNamedValueTokenParsers()

        };

        #endregion
    }
}
