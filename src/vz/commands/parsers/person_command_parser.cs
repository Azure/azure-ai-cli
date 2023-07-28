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

                    new NamedValueTokenParser(null, "x.command", "11", "1"),

                    new CommonNamedValueTokenParsers(),
                    new NamedValueTokenParser(null, "x.command.expand.file.name", "11111", "1"),

                    new ExpectConsoleOutputTokenParser(),
                    new DiagnosticLogTokenParser(),

                    new VisionServiceOptionsTokenParser(),

                    new NamedValueTokenParser(null, "face.api.version", "011", "1"),
                    new NamedValueTokenParser(null, "face.api.endpoint", "011", "1"),

                    new NamedValueTokenParser(null, "person.group.kind", "011", "1", "large;dynamic")
                )
            {
                if ((allow & Allow.VisionInput) != 0)
                {
                    Add(new NamedValueTokenParser("--url",  "vision.input.file", "001", "1", null, null, "file", "vision.input.type"));
                    Add(new NamedValueTokenParser("--urls", "vision.input.files", "001", "1", null, null, "vision.input.file", "x.command.expand.file.name"));
                    Add(new NamedValueTokenParser(null,     "vision.input.camera.device", "0010", "1;0", null, null, "camera", "vision.input.type"));
                    Add(new NamedValueTokenParser(null,     "vision.input.type", "011", "1", "file;files;camera"));
                }
                if ((allow & Allow.PersonGroupId) != 0)
                {
                    var noPerson = (allow & Allow.PersonId) == 0;
                    Add(new NamedValueTokenParser(null, "person.group.id", noPerson ? "011;001" : "011", "1"));
                }
                if ((allow & Allow.PersonId) != 0)
                {
                    var noFace = (allow & Allow.PersonFaceId) == 0;
                    Add(new NamedValueTokenParser(null, "person.id", noFace ? "11;01" : "11", "1"));
                }
                if ((allow & Allow.PersonFaceId) != 0)
                {
                    Add(new NamedValueTokenParser(null, "person.face.id", "001", "1"));
                }
            }
        }

        public class CommonPersonOutputRequestResponseNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonPersonOutputRequestResponseNamedValueTokenParsers() : base(
                    new NamedValueTokenParser(null, "person.output.json.file", "0110", "1", "@@"),
                    new NamedValueTokenParser(null, "person.output.request.file", "0110", "1", "@@")
                )
            {
            }
        }


        public class CommonPersonListNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonPersonListNamedValueTokenParsers() : base(

                    new NamedValueTokenParser(null, "person.top", "01", "1"),
                    new NamedValueTokenParser(null, "person.skip", "01", "1"),

                    new CommonPersonOutputRequestResponseNamedValueTokenParsers(),

                    new NamedValueTokenParser(null, "person.output.last.id", "0111;0101", "1", "@@", "person.output.id", "true", "person.output.list.last"),
                    new NamedValueTokenParser(null, "person.output.last.url", "0110;0101", "1", "@@", "person.output.url", "true", "person.output.list.last"),
                    new NamedValueTokenParser(null, "person.output.list.last", "1111", "1"),

                    new NamedValueTokenParser(null, "person.output.group.ids", "0101", "1", "@@", "person.output.ids"),
                    new NamedValueTokenParser(null, "person.output.group.urls", "0101", "1", "@@", "person.output.urls"),
                    new NamedValueTokenParser(null, "person.output.person.ids", "0101", "1", "@@", "person.output.ids"),
                    new NamedValueTokenParser(null, "person.output.person.urls", "0101", "1", "@@", "person.output.urls"),
                    new NamedValueTokenParser(null, "person.output.personface.ids", "0101", "1", "@@", "person.output.ids"),
                    new NamedValueTokenParser(null, "person.output.personface.urls", "0101", "1", "@@", "person.output.urls"),

                    new NamedValueTokenParser(null, "person.output.last.group.id", "0101", "1", "@@", "person.output.last.id"),
                    new NamedValueTokenParser(null, "person.output.last.group.url", "0101", "1", "@@", "person.output.last.url"),
                    new NamedValueTokenParser(null, "person.output.last.person.id", "0101", "1", "@@", "person.output.last.id"),
                    new NamedValueTokenParser(null, "person.output.last.person.url", "0101", "1", "@@", "person.output.last.url"),
                    new NamedValueTokenParser(null, "person.output.last.personface.id", "0101", "1", "@@", "person.output.last.id"),
                    new NamedValueTokenParser(null, "person.output.last.personface.url", "0101", "1", "@@", "person.output.last.url")
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

            new NamedValueTokenParser("--name",             "person.group.name", "001", "1"),
            new NamedValueTokenParser("--data",             "person.group.user.data", "0011;0001", "1"),

            new NamedValueTokenParser(null,                 "person.group.add.person.ids", "00111;00101;00011", "1", "@;"),
            new NamedValueTokenParser(null,                 "person.group.add.person.id", "00111;00101;00110", "1"),
            new NamedValueTokenParser(null,                 "person.group.recognition.model", "0011", "1"),
                          
            new NamedValueTokenParser(null,                 "person.output.person.group.id", "01001;01010", "1", "@@", "person.output.id"),
            new NamedValueTokenParser(null,                 "person.output.add.person.group.id", "011001;011010", "1", "@@", "person.output.add.id"),

        };

        private static INamedValueTokenParser[] updatePersonGroupCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId),
            new CommonPersonOutputRequestResponseNamedValueTokenParsers(),

            new NamedValueTokenParser("--name",             "person.group.name", "001", "1"),
            new NamedValueTokenParser("--data",             "person.group.user.data", "0011;0001", "1"),

            new NamedValueTokenParser(null,                 "person.group.add.person.ids", "00111;00101;00011", "1", "@;"),
            new NamedValueTokenParser(null,                 "person.group.add.person.id", "00111;00101;00110", "1"),
            new NamedValueTokenParser(null,                 "person.group.remove.person.ids", "00111;00101;00011", "1", "@;"),
            new NamedValueTokenParser(null,                 "person.group.remove.person.id", "00111;00101;00110", "1"),

        };

        private static INamedValueTokenParser[] deletePersonGroupCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId),
            new CommonPersonOutputRequestResponseNamedValueTokenParsers()
        };

        private static INamedValueTokenParser[] trainPersonGroupCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId),
            new CommonPersonOutputRequestResponseNamedValueTokenParsers(),
            new NamedValueTokenParser(null, "person.wait.timeout", "010", "1;0", null, null, "864000000")
        };

        private static INamedValueTokenParser[] trainStatusPersonGroupCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId),
            new CommonPersonOutputRequestResponseNamedValueTokenParsers(),
            new NamedValueTokenParser(null, "person.wait.timeout", "010", "1;0", null, null, "864000000")
        };

        private static INamedValueTokenParser[] createPersonCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId | Allow.PersonId),
            new CommonPersonOutputRequestResponseNamedValueTokenParsers(),

            new NamedValueTokenParser("--name",             "person.name", "001", "1"),
            new NamedValueTokenParser("--data",             "person.user.data", "011;001", "1"),

            new NamedValueTokenParser(null,                 "person.output.person.id", "0101;0110", "1", "@@", "person.output.id"),
            new NamedValueTokenParser(null,                 "person.output.add.person.id", "01101;01110", "1", "@@", "person.output.add.id"),

        };

        private static INamedValueTokenParser[] updatePersonCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId | Allow.PersonId),
            new CommonPersonOutputRequestResponseNamedValueTokenParsers(),

            new NamedValueTokenParser("--name",             "person.name", "001", "1"),
            new NamedValueTokenParser("--data",             "person.user.data", "0011;0001", "1"),

        };

        private static INamedValueTokenParser[] deletePersonCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId | Allow.PersonId),
            new CommonPersonOutputRequestResponseNamedValueTokenParsers()
        };

        private static INamedValueTokenParser[] addPersonFaceCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId | Allow.PersonId),
            new CommonPersonOutputRequestResponseNamedValueTokenParsers(),

            new NamedValueTokenParser(null,                 "face.detection.model", "001", "1"),
            new NamedValueTokenParser(null,                 "person.face.user.data", "0001", "1"),
            new NamedValueTokenParser(null,                 "person.face.target.rect", "0010;0001", "1"),

            new NamedValueTokenParser("--url",        "vision.input.file", "001", "1", null, null, "file", "vision.input.type"),
            new NamedValueTokenParser("--urls",       "vision.input.files", "001", "1", null, null, "vision.input.file", "x.command.expand.file.name"),
            // new NamedValueTokenParser(null,           "vision.input.camera.device", "0010", "1;0", null, null, "camera", "vision.input.type"),
            new NamedValueTokenParser(null,           "vision.input.type", "011", "1", "file;files" /*"file;files;camera"*/),
            new NamedValueTokenParser(null,           "vision.input.file", "010", "1", null, "vision.input.file", "file", "vision.input.type"),

            new NamedValueTokenParser(null,                 "person.output.face.id", "0101;0110", "1", "@@", "person.output.id"),
            new NamedValueTokenParser(null,                 "person.output.add.face.id", "01101;01110", "1", "@@", "person.output.add.id"),

        };

        private static INamedValueTokenParser[] updatePersonFaceCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId | Allow.PersonId | Allow.PersonFaceId),
            new CommonPersonOutputRequestResponseNamedValueTokenParsers(),

            new NamedValueTokenParser("--data",             "person.face.user.data", "00011;00001", "1"),

        };

        private static INamedValueTokenParser[] deletePersonFaceCommandParsers = {

            new CommonPersonNamedValueTokenParsers(Allow.PersonGroupId | Allow.PersonId | Allow.PersonFaceId),
            new CommonPersonOutputRequestResponseNamedValueTokenParsers()
        };

        #endregion
    }
}
