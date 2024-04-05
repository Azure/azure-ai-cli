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
            ("init.openai.chat", false),
            ("init.openai.embeddings", false),
            ("init.openai.evaluations", false),
            ("init.openai.deployments", false),
            ("init.openai", false),
            ("init.search", false),
            ("init.speech", false),
            ("init.vision", false),
            ("init.project.new", false),
            ("init.project.select", false),
            ("init.project", false),
            ("init.resource", false),
            ("init", false)
        };

        private static readonly string[] _partialCommands = {
            "init",
            "init.openai"
        };

        private static INamedValueTokenParser[] GetCommandParsers(ICommandValues values)
        {
            var commandName = values.GetCommandOrEmpty();
            switch (commandName)
            {
                case "init.aiservices":
                case "init.cognitiveservices":
                case "init.openai":
                case "init.openai.chat":
                case "init.openai.embeddings":
                case "init.openai.evaluations":
                case "init.openai.deployments":
                case "init.search":
                case "init.speech":
                case "init.vision":
                case "init.project":
                case "init.project.new":
                case "init.project.select":
                case "init.resource":

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

                new NamedValueTokenParser(null, "x.command", "11", "1"),

                new ExpectOutputTokenParser(),
                new DiagnosticLogTokenParser(),
                new CommonNamedValueTokenParsers(false),

                new NamedValueTokenParser("--ini", "ini.file", "10", "1", "@"),

                SubscriptionToken.Parser(),
                new NamedValueTokenParser("--region", "init.service.resource.region.name", "00010", "1"),
                new NamedValueTokenParser("--group", "init.service.resource.group.name", "00010", "1"),
                new NamedValueTokenParser("--name", "init.service.cognitiveservices.resource.name", "00001", "1"),
                new NamedValueTokenParser("--kind", "init.service.cognitiveservices.resource.kind", "00001", "1"),
                new NamedValueTokenParser("--sku", "init.service.cognitiveservices.resource.sku", "00001", "1"),
                new NamedValueTokenParser("--yes", "init.service.cognitiveservices.terms.agree", "00001", "1;0", "true;false", null, "true"),

                new NamedValueTokenParser(null, "init.chat.model.deployment.name", "01010;00010", "1"),
                new NamedValueTokenParser(null, "init.embeddings.model.deployment.name", "01010", "1"),
                new NamedValueTokenParser(null, "init.evaluation.model.deployment.name", "01010", "1"),

                new NamedValueTokenParser(null, "init.chat.model.name", "0110;0010", "1"),
                new NamedValueTokenParser(null, "init.embeddings.model.name", "0110", "1"),
                new NamedValueTokenParser(null, "init.evaluation.model.name", "0110", "1"),

                new NamedValueTokenParser(null, "init.output.env.file", "0110", "1;0", "true;false", null, "true"),

                new NamedValueTokenParser("--interactive", "init.service.interactive", "001", "1;0", "true;false", null, "true")

            // new NamedValueTokenParser(null, "init.output.azcli.command.file", "01100", "1", "@@"),
            // new NamedValueTokenParser(null, "init.output.azcli.json.file", "01110", "1", "@@")
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
