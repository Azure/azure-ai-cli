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

                    new NamedValueTokenParser(null, "x.command", "11", "1"),

                    new CommonNamedValueTokenParsers(),
                    new NamedValueTokenParser(null, "x.command.expand.file.name", "11111", "1"),

                    new ExpectConsoleOutputTokenParser(),
                    new DiagnosticLogTokenParser(),

                    new VisionServiceOptionsTokenParser(),

                    new NamedValueTokenParser("--url",        "vision.input.file", "001", "1", null, null, "file", "vision.input.type"),
                    new NamedValueTokenParser("--urls",       "vision.input.files", "001", "1", null, null, "vision.input.file", "x.command.expand.file.name"),
                    new NamedValueTokenParser(null,           "vision.input.camera.device", "0010", "1;0", null, null, "camera", "vision.input.type"),
                    new NamedValueTokenParser(null,           "vision.input.type", "011", "1", "file;files;camera")

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
