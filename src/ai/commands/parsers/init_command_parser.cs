//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    public class InitCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("init", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("init.aiservices", false),
            ("init.cognitiveservices", false),
            ("init.inference", false),
            ("init.github", false),
            ("init.openai.chat", false),
            ("init.openai.embeddings", false),
            ("init.openai.realtime", false),
            ("init.openai.deployments", false),
            ("init.openai", false),
            ("init.search", false),
            ("init.speech", false),
            ("init.vision", false),
#if USE_PYTHON_HUB_PROJECT_CONNECTION_OR_RELATED
            ("init.project.new", false),
            ("init.project.select", false),
            ("init.project", false),
            ("init.resource", false),
#endif
            ("init", false)
        };

        private static readonly string[] _partialCommands = {
            "init",
            "init.openai"
        };

        private static INamedValueTokenParser[] GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommand();
            switch (commandName)
            {
                case "init.aiservices":
                case "init.cognitiveservices":
                case "init.inference":
                case "init.github":
                case "init.openai":
                case "init.openai.chat":
                case "init.openai.embeddings":
                case "init.openai.realtime":
                case "init.openai.deployments":
                case "init.search":
                case "init.speech":
                case "init.vision":
#if USE_PYTHON_HUB_PROJECT_CONNECTION_OR_RELATED
                case "init.project":
                case "init.project.new":
                case "init.project.select":
                case "init.resource":
#endif

                case "init":
                    return initCommandParsers;
            }

            values.AddThrowError("ERROR:", $"Unknown command: {commandName}");
            return Array.Empty<INamedValueTokenParser>();
        }

        #region private data

        public class CommonInitNamedValueTokenParsers : NamedValueTokenParserList
        {
            public CommonInitNamedValueTokenParsers() : base(

                new Any1ValueNamedValueTokenParser(null, "x.command", "11"),

                new ExpectOutputTokenParser(),
                new DiagnosticLogTokenParser(),
                new CommonNamedValueTokenParsers(false),

                new IniFileNamedValueTokenParser(),

                SubscriptionToken.Parser(),
                new Any1ValueNamedValueTokenParser("--region", "init.service.resource.region.name", "00010"),
                new Any1ValueNamedValueTokenParser("--group", "init.service.resource.group.name", "00010"),
                new Any1ValueNamedValueTokenParser("--name", "init.service.cognitiveservices.resource.name", "00001"),
                new Any1ValueNamedValueTokenParser("--kind", "init.service.cognitiveservices.resource.kind", "00001"),
                new Any1ValueNamedValueTokenParser("--sku", "init.service.cognitiveservices.resource.sku", "00001"),
                new TrueFalseNamedValueTokenParser("--yes", "init.service.cognitiveservices.terms.agree", "00001"),

                new Any1ValueNamedValueTokenParser(null, "init.chat.model.deployment.name", "01010;00010"),
                new Any1ValueNamedValueTokenParser(null, "init.embeddings.model.deployment.name", "01010"),
                new Any1ValueNamedValueTokenParser(null, "init.realtime.model.deployment.name", "01010"),

                new Any1ValueNamedValueTokenParser(null, "init.chat.model.name", "0110;0010"),
                new Any1ValueNamedValueTokenParser(null, "init.embeddings.model.name", "0110"),
                new Any1ValueNamedValueTokenParser(null, "init.realtime.model.name", "0110"),

                new TrueFalseNamedValueTokenParser(null, "init.output.env.file", "0110"),

                new TrueFalseNamedValueTokenParser("--interactive", "init.service.interactive", "001")

            // new OutputFileNameNamedValueTokenParser(null, "init.output.azcli.command.file", "01100"),
            // new OutputFileNameNamedValueTokenParser(null, "init.output.azcli.json.file", "01110")
            )
            {
            }
        }

        private static readonly INamedValueTokenParser[] initCommandParsers = {
            new CommonInitNamedValueTokenParsers()
        };

        #endregion
    }
}
