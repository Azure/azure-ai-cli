//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    class WebJobCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("webjob", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("webjob.list", false),
            ("webjob.upload", true),
            ("webjob.run", true),
            ("webjob.status", true),
            ("webjob.download", false),
            ("webjob.delete", true)
        };

        private static readonly string[] _partialCommands = {
            "webjob"
        };

        private static INamedValueTokenParser[] GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();
            switch (commandName)
            {
                case "webjob.list":
                    return listCommandParsers;

                case "webjob.upload":
                    return uploadCommandParsers;

                case "webjob.run":
                    return runCommandParsers;

                case "webjob.status":
                    return statusCommandParsers;

                case "webjob.download":
                    return downloadCommandParsers;

                case "webjob.delete":
                    return deleteCommandParsers;
            }

            return null;
        }

        #region private data

        [Flags]
        public enum Allow
        {
            None = 0,
            InputJobName = 1,
            InputJobRunId = 2, 
            OutputJobName = 4,
            OutputJobRunId = 8,
            OutputUrl = 16,
            OutputJobNames = 32,
            OutputJobRunIds = 64,
            OutputUrls = 128,
            Wait = 256
        }

        public class CommonWebjobNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonWebjobNamedValueTokenParsers(Allow allow) : base(

                new NamedValueTokenParser(null, "x.command", "11", "1"),

                new CommonNamedValueTokenParsers(),
                new ExpectConsoleOutputTokenParser(),
                new ParallelCommandsTokenParser(),

                new NamedValueTokenParser("--ini", "ini.file", "10", "1", "@"),

                new NamedValueTokenParser("--user", "webjob.config.username", "001", "1"),
                new NamedValueTokenParser("--pass", "webjob.config.password", "001", "1"),
                new NamedValueTokenParser(null, "webjob.config.endpoint", "001", "1"),
                new NamedValueTokenParser(null, "webjob.timeout", "01", "1"),

                new NamedValueTokenParser(null, "webjob.output.request.file", "0110", "1", "@@"),
                new NamedValueTokenParser(null, "webjob.output.json.file", "0110", "1", "@@")
            )
            {
                if ((allow & Allow.InputJobName) != 0 && (allow & Allow.InputJobRunId) != 0)
                {
                    Add(new NamedValueTokenParser(null, "webjob.job.name", "001", "1"));
                    Add(new NamedValueTokenParser("--job", "webjob.job.id", "001", "2;1"));
                }
                else if ((allow & Allow.InputJobName) != 0)
                {
                    Add(new NamedValueTokenParser(null, "webjob.job.name", "001;010", "1"));
                }
                else if ((allow & Allow.InputJobRunId) != 0)
                {
                    Add(new NamedValueTokenParser(null, "webjob.job.id", "001", "1"));
                }

                if ((allow & Allow.OutputJobName) != 0)
                {
                    Add(new NamedValueTokenParser(null, "webjob.output.name", "011", "1", "@@"));
                    Add(new NamedValueTokenParser(null, "webjob.output.add.name", "0111", "1", "@@"));
                }

                if ((allow & Allow.OutputJobRunId) != 0)
                {
                    Add(new NamedValueTokenParser(null, "webjob.output.id", "011", "1", "@@"));
                    Add(new NamedValueTokenParser(null, "webjob.output.add.id", "0111", "1", "@@"));
                }

                if ((allow & Allow.OutputUrl) != 0)
                {
                    Add(new NamedValueTokenParser(null, "webjob.output.url", "011", "1", "@@"));
                    Add(new NamedValueTokenParser(null, "webjob.output.add.url", "0111", "1", "@@"));
                }

                if ((allow & Allow.OutputJobNames) != 0)
                {
                    Add(new NamedValueTokenParser(null, "webjob.output.last.name", "0111;0101", "1", "@@", "webjob.output.name", "true", "webjob.output.list.last"));
                    Add(new NamedValueTokenParser(null, "webjob.output.job.names", "0101", "1", "@@", "webjob.output.names"));
                }

                if ((allow & Allow.OutputJobRunIds) != 0)
                {
                    Add(new NamedValueTokenParser(null, "webjob.output.last.id", "0111", "1", "@@", "webjob.output.id", "true", "webjob.output.list.last"));
                    Add(new NamedValueTokenParser(null, "webjob.output.ids", "011", "1", "@@", "webjob.output.ids"));
                }

                if ((allow & Allow.OutputUrls) != 0)
                {
                    Add(new NamedValueTokenParser(null, "webjob.output.last.url", "0110;0101", "1", "@@", "webjob.output.url", "true", "webjob.output.list.last"));
                    Add(new NamedValueTokenParser(null, "webjob.output.job.urls", "0101", "1", "@@", "webjob.output.urls"));
                }

                if ((allow & Allow.Wait) != 0)
                {
                    Add(new NamedValueTokenParser(null, "webjob.wait.timeout", "010", "1;0", null, null, "864000000"));
                }
            }
        }

        private static readonly INamedValueTokenParser[] listCommandParsers = {

            new CommonWebjobNamedValueTokenParsers(
                Allow.InputJobName |
                Allow.InputJobRunId |
                Allow.OutputJobName | Allow.OutputJobNames |
                Allow.OutputJobRunId | Allow.OutputJobRunIds |
                Allow.OutputUrl | Allow.OutputUrls),

            new NamedValueTokenParser("--jobs", "webjob.list.jobs", "001", "0", null, null, "jobs", "webjob.list.kind"),
            new NamedValueTokenParser("--runs", "webjob.list.runs", "001", "0", null, null, "runs", "webjob.list.kind"),
            new NamedValueTokenParser(null, "webjob.list.kind", "001", "1", "jobs;runs"),
        };

        private static readonly INamedValueTokenParser[] uploadCommandParsers = {
            new CommonWebjobNamedValueTokenParsers(
                Allow.InputJobName |
                Allow.OutputJobRunId |
                Allow.OutputUrl |
                Allow.Wait),
            new NamedValueTokenParser(null, "webjob.upload.job.file", "0001", "1"),
            new TrueFalseNamedValueTokenParser("webjob.run.job", "010"),
        };

        private static readonly INamedValueTokenParser[] runCommandParsers = {
            new CommonWebjobNamedValueTokenParsers(
                Allow.InputJobName |
                Allow.OutputJobRunId |
                Allow.OutputUrl |
                Allow.Wait)
        };

        private static readonly INamedValueTokenParser[] statusCommandParsers = {
            new CommonWebjobNamedValueTokenParsers(
                Allow.InputJobName |
                Allow.InputJobRunId |
                Allow.OutputUrl |
                Allow.Wait),
        };

        private static readonly INamedValueTokenParser[] deleteCommandParsers = {
            new CommonWebjobNamedValueTokenParsers(
                Allow.InputJobName)
        };

        private static readonly INamedValueTokenParser[] downloadCommandParsers = {
            new CommonWebjobNamedValueTokenParsers(
                Allow.InputJobName |
                Allow.InputJobRunId),

            new NamedValueTokenParser("--href", "webjob.download.url", "001", "1"),
            new NamedValueTokenParser(null, "webjob.download.file", "001", "1"),
            new NamedValueTokenParser(null, "webjob.output.file", "011", "1", "@@"),
        };

        #endregion
    }
}
