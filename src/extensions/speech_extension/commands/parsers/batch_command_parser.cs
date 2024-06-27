//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    public class BatchCommandParser : CommandParser
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

        public static IEnumerable<INamedValueTokenParser> GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();
            switch (commandName.Replace("speech.", ""))
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

            new Any1ValueNamedValueTokenParser(null, "x.command", "11"),

            new CommonNamedValueTokenParsers(),
            new ExpectConsoleOutputTokenParser(),

            new Any1ValueNamedValueTokenParser(null, "batch.api.version", "011"),
            new Any1ValueNamedValueTokenParser(null, "batch.api.endpoint", "011"),

            new PinnedNamedValueTokenParser("--transcriptions", "batch.list.transcriptions", "010;001", "transcriptions", "batch.list.kind"),
            new Any1ValueNamedValueTokenParser(null, "batch.list.kind", "001"),

            new PinnedNamedValueTokenParser(null, "batch.list.transcription.languages", "0001", "transcription", "batch.list.languages.kind"),
            new Any1ValueNamedValueTokenParser(null, "batch.list.languages.kind", "001"),

            new TrueFalseNamedValueTokenParser("batch.list.transcription.files", "0001"),
            new Any1ValueNamedValueTokenParser(null, "batch.transcription.id", "001;010"),

            new Any1ValueNamedValueTokenParser(null, "batch.top", "01"),
            new Any1ValueNamedValueTokenParser(null, "batch.skip", "01"),

            new OutputFileNameNamedValueTokenParser(null, "batch.output.json.file", "0110"),
            new OutputFileNameNamedValueTokenParser(null, "batch.output.request.file", "0110"),

            new OutputFileNameNamedValueTokenParser(null, "batch.output.last.id", "0111;0101", "batch.output.id", "true", "batch.output.list.last"),
            new OutputFileNameNamedValueTokenParser(null, "batch.output.last.url", "0110;0101", "batch.output.url", "true", "batch.output.list.last"),
            new Any1ValueNamedValueTokenParser(null, "batch.output.list.last", "1111"),

            new OutputFileNameNamedValueTokenParser(null, "batch.output.transcription.ids", "0101", "batch.output.ids"),
            new OutputFileNameNamedValueTokenParser(null, "batch.output.transcription.urls", "0101", "batch.output.urls"),
        };

        private static INamedValueTokenParser[] downloadCommandParsers = {

            new Any1ValueNamedValueTokenParser(null, "x.command", "11"),

            new CommonNamedValueTokenParsers(),
            new ExpectConsoleOutputTokenParser(),

            new Any1ValueNamedValueTokenParser(null, "batch.api.version", "011"),
            new Any1ValueNamedValueTokenParser(null, "batch.api.endpoint", "011"),

            new Any1ValueNamedValueTokenParser(null, "batch.download.id", "001"),
            new Any1ValueNamedValueTokenParser(null, "batch.transcription.id", "010"),
            new Any1ValueNamedValueTokenParser(null, "batch.transcription.file.id", "0010"),

            new Any1ValueNamedValueTokenParser(null, "batch.download.url", "001"),
            new Any1ValueNamedValueTokenParser(null, "batch.download.file", "001"),

            new OutputFileNameNamedValueTokenParser(null, "batch.output.file", "011"),
            new OutputFileNameNamedValueTokenParser(null, "batch.output.json.file", "0110"),
            new OutputFileNameNamedValueTokenParser(null, "batch.output.request.file", "0110"),
            new OutputFileNameNamedValueTokenParser(null, "batch.output.url", "011"),
        };

        private static INamedValueTokenParser[] transcriptionCommandParsers = {

            new Any1ValueNamedValueTokenParser(null, "x.command", "11"),

            new CommonNamedValueTokenParsers(),
            new ExpectConsoleOutputTokenParser(),

            new TrueFalseNamedValueTokenParser("service.output.config.word.level.timing", "000101"),

            new Any1ValueNamedValueTokenParser(null, "batch.api.version", "011"),
            new Any1ValueNamedValueTokenParser(null, "batch.api.endpoint", "011"),

            new Any1ValueNamedValueTokenParser(null, "batch.project.id", "010"),

            new Any1ValueNamedValueTokenParser("--name", "batch.transcription.name", "001"),
            new Any1ValueNamedValueTokenParser("--description", "batch.transcription.description", "001"),
            new Any1ValueNamedValueTokenParser("--language", "batch.transcription.language", "001"),

            new Any1ValueNamedValueTokenParser(null, "batch.transcription.create.model.id", "00010"),
            new Any1ValueNamedValueTokenParser(null, "batch.transcription.create.dataset.id", "00010"),
            new AtFileOrListNamedValueTokenParser(null, "batch.transcription.create.content.urls", "00010"),
            new Any1ValueNamedValueTokenParser(null, "batch.transcription.create.content.url", "00010"),
            new Any1ValueNamedValueTokenParser(null, "batch.transcription.create.email", "0001"),

            new Any1ValueNamedValueTokenParser(null, "batch.transcription.create.time.to.live", "000111"),
            new Any1or2ValueNamedValueTokenParser("--property", "batch.transcription.create.property", "0001"),
            new AtFileOrListNamedValueTokenParser("--properties", "batch.transcription.create.properties", "0001"),

            new Any1ValueNamedValueTokenParser(null, "batch.transcription.file.id", "0010"),
            new Any1ValueNamedValueTokenParser("--id", "batch.transcription.id", "001;010"),

            new OutputFileNameNamedValueTokenParser(null, "batch.output.file", "011"),
            new OutputFileNameNamedValueTokenParser(null, "batch.output.json.file", "0110"),
            new OutputFileNameNamedValueTokenParser(null, "batch.output.request.file", "0110"),
            new OutputFileNameNamedValueTokenParser(null, "batch.output.transcription.id", "0111;0101", "batch.output.id"),
            new OutputFileNameNamedValueTokenParser(null, "batch.output.transcription.url", "0110;0101", "batch.output.url"),
            new OutputFileNameNamedValueTokenParser(null, "batch.output.add.transcription.id", "01111;01101", "batch.output.add.id"),
            new OutputFileNameNamedValueTokenParser(null, "batch.output.add.transcription.url", "01110;01101", "batch.output.add.url"),

            new OptionalWithDefaultNamedValueTokenParser(null, "batch.wait.timeout", "010", "864000000"),
        };

        private static INamedValueTokenParser[] onpremTranscriptionCommandParsers = {

            new Any1ValueNamedValueTokenParser(null, "x.command", "11"),

            new CommonNamedValueTokenParsers(),
            new ExpectConsoleOutputTokenParser(),

            new AtFileOrListNamedValueTokenParser(null, "batch.transcription.onprem.create.files", "00001"),
            new Any1ValueNamedValueTokenParser("--language", "batch.transcription.language", "001"),
            new RequiredValidValueNamedValueTokenParser("--diarization", "batch.transcription.onprem.create.diarization", "00001", "None;Identity;Anonymous"),
            new Any1ValueNamedValueTokenParser("--nbest", "batch.transcription.onprem.create.nbest", "00001"),
            new RequiredValidValueNamedValueTokenParser("--profanity", "batch.transcription.onprem.create.profanity", "00001", "Masked;Raw;Removed"),
            new TrueFalseNamedValueTokenParser("--resume", "batch.transcription.onprem.create.resume", "00001"),
            new TrueFalseNamedValueTokenParser("--combine", "batch.transcription.onprem.create.combine", "00001"),          
            
            new Any1ValueNamedValueTokenParser(null, "batch.transcription.onprem.status.waitms", "00001"),

            new Any1ValueNamedValueTokenParser(null, "batch.transcription.onprem.id", "0001"),       // list, delete
            new Any1ValueNamedValueTokenParser(null, "batch.transcription.onprem.outfile", "0001"),  // list, create

            new Any1ValueNamedValueTokenParser(null, "batch.transcription.onprem.endpoints.config", "00001"),

            new Any1ValueNamedValueTokenParser("--host", "batch.transcription.onprem.api.host", "00011"),
            new Any1ValueNamedValueTokenParser("--port", "batch.transcription.onprem.api.port", "00011"),
        };

        #endregion
    }
}
