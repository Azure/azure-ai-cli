//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    class BatchCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("batch", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("batch.list", true),
            ("batch.download", true),
            ("batch.transcription.create", true),
            ("batch.transcription.update", true),
            ("batch.transcription.delete", true),
            ("batch.transcription.status", true),
            ("batch.transcription.download", true),
            ("batch.transcription.list", false),
            ("batch.transcription.onprem.create", true),
            ("batch.transcription.onprem.status", true),
            ("batch.transcription.onprem.delete", true),
            ("batch.transcription.onprem.endpoints", true),
            ("batch.transcription.onprem.list", false)
        };

        private static readonly string[] _partialCommands = {
            "batch.transcription",
            "batch"
        };

        private static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();
            switch (commandName)
            {
                case "batch.list":
                case "batch.transcription.list":
                    return listCommandParsers;

                case "batch.download":
                case "batch.transcription.download":
                    return downloadCommandParsers;

                case "batch.transcription.create":
                case "batch.transcription.update":
                case "batch.transcription.delete":
                case "batch.transcription.status":
                    return transcriptionCommandParsers;

                case "batch.transcription.onprem.create":
                case "batch.transcription.onprem.status":
                case "batch.transcription.onprem.list":
                case "batch.transcription.onprem.delete":
                case "batch.transcription.onprem.endpoints":
                    return onpremTranscriptionCommandParsers;
            }

            return null;
        }

        #region private data

        private static INamedValueTokenParser[] listCommandParsers = {

            new NamedValueTokenParser(null,           "x.command", "11", "1"),

            new CommonNamedValueTokenParsers(),
            new ExpectConsoleOutputTokenParser(),

            new NamedValueTokenParser(null,                 "batch.api.version", "011", "1"),
            new NamedValueTokenParser(null,                 "batch.api.endpoint", "011", "1"),

            new NamedValueTokenParser("--transcriptions",   "batch.list.transcriptions", "010;001", "0", null, null, "transcriptions", "batch.list.kind"),
            new NamedValueTokenParser(null,                 "batch.list.kind", "001", "1"),

            new NamedValueTokenParser(null,                 "batch.list.transcription.languages", "0001", "0", null, null, "transcription", "batch.list.languages.kind"),
            new NamedValueTokenParser(null,                 "batch.list.languages.kind", "001", "1"),

            new NamedValueTokenParser(null,                 "batch.list.transcription.files", "0001", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null,                 "batch.transcription.id", "001;010", "1"),

            new NamedValueTokenParser(null,                 "batch.top", "01", "1"),
            new NamedValueTokenParser(null,                 "batch.skip", "01", "1"),

            new NamedValueTokenParser(null,                 "batch.output.json.file", "0110", "1", "@@"),
            new NamedValueTokenParser(null,                 "batch.output.request.file", "0110", "1", "@@"),

            new NamedValueTokenParser(null,                 "batch.output.last.id", "0111;0101", "1", "@@", "batch.output.id", "true", "batch.output.list.last"),
            new NamedValueTokenParser(null,                 "batch.output.last.url", "0110;0101", "1", "@@", "batch.output.url", "true", "batch.output.list.last"),
            new NamedValueTokenParser(null,                 "batch.output.list.last", "1111", "1"),

            new NamedValueTokenParser(null,                 "batch.output.transcription.ids", "0101", "1", "@@", "batch.output.ids"),
            new NamedValueTokenParser(null,                 "batch.output.transcription.urls", "0101", "1", "@@", "batch.output.urls"),
        };

        private static INamedValueTokenParser[] downloadCommandParsers = {

            new NamedValueTokenParser(null,           "x.command", "11", "1"),

            new CommonNamedValueTokenParsers(),
            new ExpectConsoleOutputTokenParser(),

            new NamedValueTokenParser(null,                 "batch.api.version", "011", "1"),
            new NamedValueTokenParser(null,                 "batch.api.endpoint", "011", "1"),

            new NamedValueTokenParser(null,                 "batch.download.id", "001", "1"),
            new NamedValueTokenParser(null,                 "batch.transcription.id", "010", "1"),
            new NamedValueTokenParser(null,                 "batch.transcription.file.id", "0010", "1"),

            new NamedValueTokenParser(null,                 "batch.download.url", "001", "1"),
            new NamedValueTokenParser(null,                 "batch.download.file", "001", "1"),

            new NamedValueTokenParser(null,                 "batch.output.file", "011", "1", "@@"),
            new NamedValueTokenParser(null,                 "batch.output.json.file", "0110", "1", "@@"),
            new NamedValueTokenParser(null,                 "batch.output.request.file", "0110", "1", "@@"),
            new NamedValueTokenParser(null,                 "batch.output.url", "011", "1", "@@"),
        };

        private static INamedValueTokenParser[] transcriptionCommandParsers = {

            new NamedValueTokenParser(null,           "x.command", "11", "1"),

            new CommonNamedValueTokenParsers(),
            new ExpectConsoleOutputTokenParser(),

            // new NamedValueTokenParser("--diarization",      "service.output.config.diarization", "0001", "1;0", "true;false", null, "true"),
            // new NamedValueTokenParser("--profanity",        "service.output.config.profanity.option", "00010", "1", "none;removed;tags;masked"),
            // new NamedValueTokenParser("--punctuation",      "service.output.config.punctuation.mode", "00010", "1;0", "none;dictated;automatic;dictatedandautomatic", null, "DictatedAndAutomatic"),
            new NamedValueTokenParser(null,                 "service.output.config.word.level.timing", "000101", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null,                 "batch.api.version", "011", "1"),
            new NamedValueTokenParser(null,                 "batch.api.endpoint", "011", "1"),

            new NamedValueTokenParser(null,                 "batch.project.id", "010", "1"),

            new NamedValueTokenParser("--name",             "batch.transcription.name", "001", "1"),
            new NamedValueTokenParser("--description",      "batch.transcription.description", "001", "1"),
            new NamedValueTokenParser("--language",         "batch.transcription.language", "001", "1"),

            new NamedValueTokenParser(null,                 "batch.transcription.create.model.id", "00010", "1"),
            new NamedValueTokenParser(null,                 "batch.transcription.create.dataset.id", "00010", "1"),
            new NamedValueTokenParser(null,                 "batch.transcription.create.content.urls", "00010", "1", "@;"),
            new NamedValueTokenParser(null,                 "batch.transcription.create.content.url", "00010", "1"),
            new NamedValueTokenParser(null,                 "batch.transcription.create.email", "0001", "1"),

            new NamedValueTokenParser(null,                 "batch.transcription.create.time.to.live", "000111", "1"),
            new NamedValueTokenParser("--property",         "batch.transcription.create.property", "0001", "2;1"),
            new NamedValueTokenParser("--properties",       "batch.transcription.create.properties", "0001", "1", "@;"),

            new NamedValueTokenParser(null,                 "batch.transcription.file.id", "0010", "1"),
            new NamedValueTokenParser("--id",               "batch.transcription.id", "001;010", "1"),

            new NamedValueTokenParser(null,                 "batch.output.file", "011", "1", "@@"),
            new NamedValueTokenParser(null,                 "batch.output.json.file", "0110", "1", "@@"),
            new NamedValueTokenParser(null,                 "batch.output.request.file", "0110", "1", "@@"),
            new NamedValueTokenParser(null,                 "batch.output.transcription.id", "0111;0101", "1", "@@", "batch.output.id"),
            new NamedValueTokenParser(null,                 "batch.output.transcription.url", "0110;0101", "1", "@@", "batch.output.url"),
            new NamedValueTokenParser(null,                 "batch.output.add.transcription.id", "01111;01101", "1", "@@", "batch.output.add.id"),
            new NamedValueTokenParser(null,                 "batch.output.add.transcription.url", "01110;01101", "1", "@@", "batch.output.add.url"),

            new NamedValueTokenParser(null,                 "batch.wait.timeout", "010", "1;0", null, null, "864000000"),
        };

        private static INamedValueTokenParser[] onpremTranscriptionCommandParsers = {

            new NamedValueTokenParser(null,           "x.command", "11", "1"),

            new CommonNamedValueTokenParsers(),
            new ExpectConsoleOutputTokenParser(),

            new NamedValueTokenParser(null,                 "batch.transcription.onprem.create.files", "00001", "1", "@;"),
            new NamedValueTokenParser("--language",         "batch.transcription.language", "001", "1"),
            new NamedValueTokenParser("--diarization",      "batch.transcription.onprem.create.diarization", "00001", "1", "None;Identity;Anonymous"),
            new NamedValueTokenParser("--nbest",            "batch.transcription.onprem.create.nbest", "00001", "1"),
            new NamedValueTokenParser("--profanity",        "batch.transcription.onprem.create.profanity", "00001", "1", "Masked;Raw;Removed"),
            new NamedValueTokenParser("--resume",           "batch.transcription.onprem.create.resume", "00001", "1", "true;false"),
            new NamedValueTokenParser("--combine",          "batch.transcription.onprem.create.combine", "00001", "1", "true;false"),          
            
            new NamedValueTokenParser(null,                 "batch.transcription.onprem.status.waitms", "00001", "1"),

            new NamedValueTokenParser(null,                 "batch.transcription.onprem.id", "0001", "1"),       // list, delete
            new NamedValueTokenParser(null,                 "batch.transcription.onprem.outfile", "0001", "1"),  // list, create

            new NamedValueTokenParser(null,                 "batch.transcription.onprem.endpoints.config", "00001", "1"),

            new NamedValueTokenParser("--host",             "batch.transcription.onprem.api.host", "00011", "1"),
            new NamedValueTokenParser("--port",             "batch.transcription.onprem.api.port", "00011", "1"),
        };

        #endregion
    }
}
