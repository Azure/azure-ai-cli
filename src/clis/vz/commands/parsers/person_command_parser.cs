//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    class PersonCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("person", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("person.group.create", true),
            ("person.group.list", false),
            ("person.group.update", true),
            ("person.group.delete", true),
            ("person.group.status", true),
            ("person.group.train", true),
            ("person.create", true),
            ("person.list", true),
            ("person.update", true),
            ("person.delete", true),
            ("person.face.add", true),
            ("person.face.list", true),
            ("person.face.update", true),
            ("person.face.delete", true)
        };

        private static readonly string[] _partialCommands = {
            "person.group",
            "person.face",
            "person"
        };

        private static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();
            switch (commandName)
            {
                case "person.group.create": return createPersonGroupCommandParsers;
                case "person.group.list": return groupListCommandParsers;
                case "person.group.update": return updatePersonGroupCommandParsers;
                case "person.group.delete": return deletePersonGroupCommandParsers;
                case "person.group.train": return trainPersonGroupCommandParsers;
                case "person.group.status": return trainStatusPersonGroupCommandParsers;

                case "person.list": return personListCommandParsers;
                case "person.create": return createPersonCommandParsers;
                case "person.update": return updatePersonCommandParsers;
                case "person.delete": return deletePersonCommandParsers;

                case "person.face.add": return addPersonFaceCommandParsers;
                case "person.face.list": return personFaceListCommandParsers;
                case "person.face.update": return updatePersonFaceCommandParsers;
                case "person.face.delete": return deletePersonFaceCommandParsers;
            }

            return null;
        }

        #region private data

        [Flags]
        public enum Allow
        {
            None = 0,
            VisionInput = 1,
            PersonGroupId = 2,
            PersonId = 4, 
            PersonFaceId = 8
        }

        public class CommonPersonNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonPersonNamedValueTokenParsers(Allow allow = Allow.None) : base(

                    new Any1ValueNamedValueTokenParser(null, "x.command", "11"),

                    new CommonNamedValueTokenParsers(),
                    new ExpandFileNameNamedValueTokenParser(),

                    new ExpectConsoleOutputTokenParser(),
                    new DiagnosticLogTokenParser(),

                    new VisionServiceOptionsTokenParser(),

                    new Any1ValueNamedValueTokenParser(null, "face.api.version", "011"),
                    new Any1ValueNamedValueTokenParser(null, "face.api.endpoint", "011"),

                    new RequiredValidValueNamedValueTokenParser(null, "person.group.kind", "011", "large;dynamic")
                )
            {
                if ((allow & Allow.VisionInput) != 0)
                {
                    Add(new Any1PinnedNamedValueTokenParser("--url", "vision.input.file", "001", "file", "vision.input.type"));
                    Add(new ExpandFileNameNamedValueTokenParser("--urls", "vision.input.files", "001", "vision.input.file"));
                    Add(new OptionalWithDefaultNamedValueTokenParser(null, "vision.input.camera.device", "0010", "camera", "vision.input.type"));
                    Add(new RequiredValidValueNamedValueTokenParser(null, "vision.input.type", "011", "file;files;camera"));
                }
                if ((allow & Allow.PersonGroupId) != 0)
                {
                    var noPerson = (allow & Allow.PersonId) == 0;
                    Add(new Any1ValueNamedValueTokenParser(null, "person.group.id", noPerson ? "011;001" : "011"));
                }
                if ((allow & Allow.PersonId) != 0)
                {
                    var noFace = (allow & Allow.PersonFaceId) == 0;
                    Add(new Any1ValueNamedValueTokenParser(null, "person.id", noFace ? "11;01" : "11"));
                }
                if ((allow & Allow.PersonFaceId) != 0)
                {
                    Add(new Any1ValueNamedValueTokenParser(null, "person.face.id", "001"));
                }
            }
        }

        public class CommonPersonOutputRequestResponseNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonPersonOutputRequestResponseNamedValueTokenParsers() : base(
                    new OutputFileNameNamedValueTokenParser(null, "person.output.json.file", "0110"),
                    new OutputFileNameNamedValueTokenParser(null, "person.output.request.file", "0110")
                )
            {
            }
        }


        public class CommonPersonListNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonPersonListNamedValueTokenParsers() : base(

                    new Any1ValueNamedValueTokenParser(null, "person.top", "01"),
                    new Any1ValueNamedValueTokenParser(null, "person.skip", "01"),

                    new CommonPersonOutputRequestResponseNamedValueTokenParsers(),

                    new OutputFileNameNamedValueTokenParser(null, "person.output.last.id", "0111;0101", "person.output.id", "true", "person.output.list.last"),
                    new OutputFileNameNamedValueTokenParser(null, "person.output.last.url", "0110;0101", "person.output.url", "true", "person.output.list.last"),
                    new Any1ValueNamedValueTokenParser(null, "person.output.list.last", "1111"),

                    new OutputFileNameNamedValueTokenParser(null, "person.output.group.ids", "0101", "person.output.ids"),
                    new OutputFileNameNamedValueTokenParser(null, "person.output.group.urls", "0101", "person.output.urls"),
                    new OutputFileNameNamedValueTokenParser(null, "person.output.person.ids", "0101", "person.output.ids"),
                    new OutputFileNameNamedValueTokenParser(null, "person.output.person.urls", "0101", "person.output.urls"),
                    new OutputFileNameNamedValueTokenParser(null, "person.output.personface.ids", "0101", "person.output.ids"),
                    new OutputFileNameNamedValueTokenParser(null, "person.output.personface.urls", "0101", "person.output.urls"),

                    new OutputFileNameNamedValueTokenParser(null, "person.output.last.group.id", "0101", "person.output.last.id"),
                    new OutputFileNameNamedValueTokenParser(null, "person.output.last.group.url", "0101", "person.output.last.url"),
                    new OutputFileNameNamedValueTokenParser(null, "person.output.last.person.id", "0101", "person.output.last.id"),
                    new OutputFileNameNamedValueTokenParser(null, "person.output.last.person.url", "0101", "person.output.last.url"),
                    new OutputFileNameNamedValueTokenParser(null, "person.output.last.personface.id", "0101", "person.output.last.id"),
                    new OutputFileNameNamedValueTokenParser(null, "person.output.last.personface.url", "0101", "person.output.last.url")
                )
            {
            }
        }

        private static INamedValueTokenParser[] commonCommandParsers = {
            new CommonPersonNamedValueTokenParsers()
        };

        private static INamedValueTokenParser[] groupListCommandParsers = {

            new CommonPersonNamedValueTokenParsers(),
            new CommonPersonListNamedValueTokenParsers()
        };

        private static INamedValueTokenParser[] personListCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId),
            new CommonPersonListNamedValueTokenParsers()
        };

        private static INamedValueTokenParser[] personFaceListCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId | Allow.PersonId),
            new CommonPersonListNamedValueTokenParsers()
        };

        private static INamedValueTokenParser[] createPersonGroupCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId),
            new CommonPersonOutputRequestResponseNamedValueTokenParsers(),

            new Any1ValueNamedValueTokenParser("--name", "person.group.name", "001"),
            new Any1ValueNamedValueTokenParser("--data", "person.group.user.data", "0011;0001"),

            new AtFileOrListNamedValueTokenParser(null, "person.group.add.person.ids", "00111;00101;00011"),
            new Any1ValueNamedValueTokenParser(null, "person.group.add.person.id", "00111;00101;00110"),
            new Any1ValueNamedValueTokenParser(null, "person.group.recognition.model", "0011"),
                          
            new OutputFileNameNamedValueTokenParser(null, "person.output.person.group.id", "01001;01010", "person.output.id"),
            new OutputFileNameNamedValueTokenParser(null, "person.output.add.person.group.id", "011001;011010", "person.output.add.id"),

        };

        private static INamedValueTokenParser[] updatePersonGroupCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId),
            new CommonPersonOutputRequestResponseNamedValueTokenParsers(),

            new Any1ValueNamedValueTokenParser("--name", "person.group.name", "001"),
            new Any1ValueNamedValueTokenParser("--data", "person.group.user.data", "0011;0001"),

            new AtFileOrListNamedValueTokenParser(null, "person.group.add.person.ids", "00111;00101;00011"),
            new Any1ValueNamedValueTokenParser(null, "person.group.add.person.id", "00111;00101;00110"),
            new AtFileOrListNamedValueTokenParser(null, "person.group.remove.person.ids", "00111;00101;00011"),
            new Any1ValueNamedValueTokenParser(null, "person.group.remove.person.id", "00111;00101;00110"),

        };

        private static INamedValueTokenParser[] deletePersonGroupCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId),
            new CommonPersonOutputRequestResponseNamedValueTokenParsers()
        };

        private static INamedValueTokenParser[] trainPersonGroupCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId),
            new CommonPersonOutputRequestResponseNamedValueTokenParsers(),
            new OptionalWithDefaultNamedValueTokenParser(null, "person.wait.timeout", "010", "864000000")
        };

        private static INamedValueTokenParser[] trainStatusPersonGroupCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId),
            new CommonPersonOutputRequestResponseNamedValueTokenParsers(),
            new OptionalWithDefaultNamedValueTokenParser(null, "person.wait.timeout", "010", "864000000")
        };

        private static INamedValueTokenParser[] createPersonCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId | Allow.PersonId),
            new CommonPersonOutputRequestResponseNamedValueTokenParsers(),

            new Any1ValueNamedValueTokenParser("--name", "person.name", "001"),
            new Any1ValueNamedValueTokenParser("--data", "person.user.data", "011;001"),

            new OutputFileNameNamedValueTokenParser(null, "person.output.person.id", "0101;0110", "person.output.id"),
            new OutputFileNameNamedValueTokenParser(null, "person.output.add.person.id", "01101;01110", "person.output.add.id"),

        };

        private static INamedValueTokenParser[] updatePersonCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId | Allow.PersonId),
            new CommonPersonOutputRequestResponseNamedValueTokenParsers(),

            new Any1ValueNamedValueTokenParser("--name", "person.name", "001"),
            new Any1ValueNamedValueTokenParser("--data", "person.user.data", "0011;0001"),

        };

        private static INamedValueTokenParser[] deletePersonCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId | Allow.PersonId),
            new CommonPersonOutputRequestResponseNamedValueTokenParsers()
        };

        private static INamedValueTokenParser[] addPersonFaceCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId | Allow.PersonId),
            new CommonPersonOutputRequestResponseNamedValueTokenParsers(),

            new Any1ValueNamedValueTokenParser(null, "face.detection.model", "001"),
            new Any1ValueNamedValueTokenParser(null, "person.face.user.data", "0001"),
            new Any1ValueNamedValueTokenParser(null, "person.face.target.rect", "0010;0001"),

            new Any1PinnedNamedValueTokenParser("--url", "vision.input.file", "001", "file", "vision.input.type"),
            new ExpandFileNameNamedValueTokenParser("--urls", "vision.input.files", "001", "vision.input.file"),
            // new OptionalWithDefaultNamedValueTokenParser(null, "vision.input.camera.device", "0010", "camera", "vision.input.type"),
            new RequiredValidValueNamedValueTokenParser(null, "vision.input.type", "011", "file;files"),
            new NamedValueTokenParser(null,           "vision.input.file", "010", "1", null, "vision.input.file", "file", "vision.input.type"),

            new OutputFileNameNamedValueTokenParser(null, "person.output.face.id", "0101;0110", "person.output.id"),
            new OutputFileNameNamedValueTokenParser(null, "person.output.add.face.id", "01101;01110", "person.output.add.id"),

        };

        private static INamedValueTokenParser[] updatePersonFaceCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId | Allow.PersonId | Allow.PersonFaceId),
            new CommonPersonOutputRequestResponseNamedValueTokenParsers(),

            new Any1ValueNamedValueTokenParser("--data", "person.face.user.data", "00011;00001"),

        };

        private static INamedValueTokenParser[] deletePersonFaceCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId | Allow.PersonId | Allow.PersonFaceId),
            new CommonPersonOutputRequestResponseNamedValueTokenParsers()
        };

        #endregion
    }
}
