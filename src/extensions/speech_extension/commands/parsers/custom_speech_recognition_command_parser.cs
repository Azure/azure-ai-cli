//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    public class CustomSpeechRecognitionCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommands(_commands, _partialCommands, tokens, values, x => GetCommandParsers(x));
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("csr", GetCommandParsers(values), tokens, values);
        }

        private static readonly (string name, bool valuesRequired)[] _commands =  {
            ("csr.list", true),
            ("csr.download", true),                          
            ("csr.project.create", true),
            ("csr.project.update", true),
            ("csr.project.delete", true),
            ("csr.project.status", true),
            ("csr.project.list", false),
            ("csr.dataset.create", true),
            ("csr.dataset.upload", true),
            ("csr.dataset.update", true),
            ("csr.dataset.delete", true),
            ("csr.dataset.list", false),
            ("csr.dataset.status", true),
            ("csr.dataset.download", true),
            ("csr.model.create", true),
            ("csr.model.update", true),
            ("csr.model.delete", true),
            ("csr.model.list", false),
            ("csr.model.status", true),
            ("csr.model.download", true),
            ("csr.model.copy", true),
            ("csr.evaluation.create", true),
            ("csr.evaluation.list", false),
            ("csr.evaluation.show", true),
            ("csr.evaluation.status", true),
            ("csr.evaluation.delete", true),
            ("csr.endpoint.create", true),
            ("csr.endpoint.update", true),
            ("csr.endpoint.delete", true),
            ("csr.endpoint.list", false),
            ("csr.endpoint.status", true),
            ("csr.endpoint.download", true)
        };

        private static readonly string[] _partialCommands = {
            "csr.project",
            "csr.dataset",
            "csr.model",
            "csr.evaluation",
            "csr.endpoint",
            "csr"
        };

        public static INamedValueTokenParser[] GetCommandParsers(ICommandValues values)
        {
            var commandName = string.Join('.', values.GetCommand()
                .Split('.')
                .SkipWhile(x => x == "speech")
                .ToArray());

            switch (commandName)
            {
                case "csr.list": return listCommandParsers;
                case "csr.model.list": return listModelCommandParsers;
                case "csr.project.list": return listProjectCommandParsers;
                case "csr.dataset.list": return listDatasetCommandParsers;
                case "csr.evaluation.list": return listEvaluationCommandParsers;
                case "csr.endpoint.list": return listEndpointCommandParsers;

                case "csr.download":
                case "csr.model.download":
                case "csr.dataset.download":
                case "csr.endpoint.download":
                    return downloadCommandParsers;

                case "csr.project.create":
                case "csr.project.update":
                case "csr.project.delete":
                case "csr.project.status":
                    return projectCommandParsers;

                case "csr.dataset.create":
                case "csr.dataset.upload":
                case "csr.dataset.update":
                case "csr.dataset.delete":
                case "csr.dataset.status":
                    return datasetCommandParsers;

                case "csr.model.create":
                case "csr.model.update":
                case "csr.model.delete":
                case "csr.model.status":
                case "csr.model.copy":
                    return modelCommandParsers;

                case "csr.evaluation.create":
                case "csr.evaluation.update":
                case "csr.evaluation.delete":
                case "csr.evaluation.status":
                    return evaluationCommandParsers;

                case "csr.endpoint.create":
                case "csr.endpoint.update":
                case "csr.endpoint.delete":
                case "csr.endpoint.status":
                    return endpointCommandParsers;

            }

            values.AddThrowError("ERROR:", $"Unknown command: {commandName}");
            return Array.Empty<INamedValueTokenParser>();
        }

        #region private data

        private class CommonCustomSpeechValueTokenParsers : NamedValueTokenParserList
        {
            public CommonCustomSpeechValueTokenParsers() : base(
                new Any1ValueNamedValueTokenParser(null, "x.command", "11"),

                new CommonNamedValueTokenParsers(),
                new ExpectConsoleOutputTokenParser(),

                new Any1ValueNamedValueTokenParser(null, "csr.api.version", "011"),
                new Any1ValueNamedValueTokenParser(null, "csr.api.endpoint", "011")
            ) {}
        }

        [Flags]
        public enum AllowList
        {
            None = 0,
            Projects = 1,
            Datasets = 2, 
            Models = 4,
            Endpoints = 8,
            Evaluations = 16,
            All = Projects | Datasets | Models | Endpoints | Evaluations
        }

        private class ListCommonCustomSpeechValueTokenParsers : NamedValueTokenParserList
        {

            public ListCommonCustomSpeechValueTokenParsers(AllowList allow) : base(
                new CommonCustomSpeechValueTokenParsers()
            )
            {
                if ((allow & AllowList.Projects) != 0)
                {
                    Add(new PinnedNamedValueTokenParser("--projects", "csr.list.projects", "001", "projects", "csr.list.kind"));
                    Add(new PinnedNamedValueTokenParser(null, "csr.list.project.languages", "0011", "project", "csr.list.languages.kind"));
                }
                if ((allow & AllowList.Datasets) != 0)
                {
                    var notOnlyDatasets = allow != AllowList.Datasets;
                    Add(new PinnedNamedValueTokenParser("--datasets", "csr.list.datasets", "001", "datasets", "csr.list.kind"));
                    Add(new PinnedNamedValueTokenParser(null, "csr.list.dataset.languages", notOnlyDatasets ? "0011" : "0001", "dataset", "csr.list.languages.kind"));
                    Add(new TrueFalseNamedValueTokenParser(null, "csr.list.dataset.files", notOnlyDatasets ? "0011" : "0001"));
                    Add(new Any1ValueNamedValueTokenParser(null, "csr.dataset.id", notOnlyDatasets ? "010" : "001;010"));
                }
                if ((allow & AllowList.Models) != 0)
                {
                    var notOnlyModels = allow != AllowList.Models;
                    Add(new PinnedNamedValueTokenParser("--models", "csr.list.models", "001", "models", "csr.list.kind"));
                    Add(new PinnedNamedValueTokenParser(null, "csr.list.base.models", "0010", "models/base", "csr.list.kind"));
                    Add(new PinnedNamedValueTokenParser(null, "csr.list.model.languages", notOnlyModels ? "0011" : "0001", "model", "csr.list.languages.kind"));
                }
                if ((allow & AllowList.Endpoints) != 0)
                {
                    var notOnlyEndpoints = allow != AllowList.Endpoints;
                    Add(new PinnedNamedValueTokenParser("--endpoints", "csr.list.endpoints", "001", "endpoints", "csr.list.kind"));
                    Add(new PinnedNamedValueTokenParser(null, "csr.list.endpoint.languages", notOnlyEndpoints ? "0011" : "0001", "endpoint", "csr.list.languages.kind"));
                    Add(new TrueFalseNamedValueTokenParser("csr.list.endpoint.logs", "0001"));
                    Add(new Any1ValueNamedValueTokenParser(null, "csr.endpoint.id", notOnlyEndpoints ? "010" : "001;010"));
                }
                if ((allow & AllowList.Evaluations) != 0)
                {
                    var notOnlyEvaluations = allow != AllowList.Evaluations;
                    Add(new PinnedNamedValueTokenParser("--evaluations", "csr.list.evaluations", "001", "evaluations", "csr.list.kind"));
                    Add(new PinnedNamedValueTokenParser(null, "csr.list.evaluation.languages", notOnlyEvaluations ? "0011" : "0001", "evaluation", "csr.list.languages.kind"));
                    Add(new TrueFalseNamedValueTokenParser(null, "csr.list.evaluation.files", notOnlyEvaluations ? "0011" : "0001"));
                    Add(new Any1ValueNamedValueTokenParser(null, "csr.evaluation.id", notOnlyEvaluations ? "010" : "001;010"));
                }

                Add(new RequiredValidValueNamedValueTokenParser(null, "csr.list.kind", "001", "endpoints;projects;datasets;models;models/base;evaluations"));

                Add(new OptionalValidValueNamedValueTokenParser(null, "csr.list.languages.kind", "0011", "endpoint;project;dataset;model"));
                Add(new TrueFalseNamedValueTokenParser("csr.list.languages", "001"));

                Add(new Any1ValueNamedValueTokenParser(null, "csr.list.id", "001"));
                Add(new Any1ValueNamedValueTokenParser(null, "csr.project.id", "010"));

                Add(new Any1ValueNamedValueTokenParser(null, "csr.top", "01"));
                Add(new Any1ValueNamedValueTokenParser(null, "csr.skip", "01"));

                Add(new OutputFileNameNamedValueTokenParser(null, "csr.output.json.file", "0110"));
                Add(new OutputFileNameNamedValueTokenParser(null, "csr.output.request.file", "0110"));

                Add(new OutputFileNameNamedValueTokenParser(null, "csr.output.last.id", "0111;0101", "csr.output.id", "true", "csr.output.list.last"));
                Add(new OutputFileNameNamedValueTokenParser(null, "csr.output.last.url", "0110;0101", "csr.output.url", "true", "csr.output.list.last"));
                Add(new Any1ValueNamedValueTokenParser(null, "csr.output.list.last", "1111"));

                Add(new OutputFileNameNamedValueTokenParser(null, "csr.output.project.ids", "0101", "csr.output.ids"));
                Add(new OutputFileNameNamedValueTokenParser(null, "csr.output.project.urls", "0101", "csr.output.urls"));
                Add(new OutputFileNameNamedValueTokenParser(null, "csr.output.dataset.ids", "0101", "csr.output.ids"));
                Add(new OutputFileNameNamedValueTokenParser(null, "csr.output.dataset.urls", "0101", "csr.output.urls"));
                Add(new OutputFileNameNamedValueTokenParser(null, "csr.output.model.ids", "0101", "csr.output.ids"));
                Add(new OutputFileNameNamedValueTokenParser(null, "csr.output.model.urls", "0101", "csr.output.urls"));
                Add(new OutputFileNameNamedValueTokenParser(null, "csr.output.endpoint.ids", "0101", "csr.output.ids"));
                Add(new OutputFileNameNamedValueTokenParser(null, "csr.output.endpoint.urls", "0101", "csr.output.urls"));

            }
        }

        private static INamedValueTokenParser[] listCommandParsers = {
            new ListCommonCustomSpeechValueTokenParsers(
                AllowList.Projects |
                AllowList.Datasets |
                AllowList.Models |
                AllowList.Endpoints |
                AllowList.Evaluations)
        };

        private static INamedValueTokenParser[] listProjectCommandParsers = {
            new ListCommonCustomSpeechValueTokenParsers(AllowList.Projects)
        };

        private static INamedValueTokenParser[] listDatasetCommandParsers = {
            new ListCommonCustomSpeechValueTokenParsers(AllowList.Datasets)
        };

        private static INamedValueTokenParser[] listModelCommandParsers = {
            new ListCommonCustomSpeechValueTokenParsers(AllowList.Models)
        };

        private static INamedValueTokenParser[] listEndpointCommandParsers = {
            new ListCommonCustomSpeechValueTokenParsers(AllowList.Endpoints)
        };

        private static INamedValueTokenParser[] listEvaluationCommandParsers = {
            new ListCommonCustomSpeechValueTokenParsers(AllowList.Evaluations)
        };

        private static INamedValueTokenParser[] downloadCommandParsers = {

            new CommonCustomSpeechValueTokenParsers(),

            new Any1ValueNamedValueTokenParser(null, "csr.dataset.file.id", "0110"),
            new Any1ValueNamedValueTokenParser(null, "csr.endpoint.log.id", "0010;0011"),
            new Any1ValueNamedValueTokenParser(null, "csr.evaluation.file.id", "0110"),

            new Any1ValueNamedValueTokenParser(null, "csr.list.id", "001"),
            new Any1ValueNamedValueTokenParser(null, "csr.dataset.id", "010"),
            new Any1ValueNamedValueTokenParser(null, "csr.endpoint.id", "010"),
            new Any1ValueNamedValueTokenParser(null, "csr.evaluation.id", "010"),
            new Any1ValueNamedValueTokenParser(null, "csr.model.id", "010"),

            new Any1ValueNamedValueTokenParser(null, "csr.download.url", "001"),
            new Any1ValueNamedValueTokenParser(null, "csr.download.file", "001"),

            new OutputFileNameNamedValueTokenParser(null, "csr.output.file", "011"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.json.file", "0110"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.request.file", "0110"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.url", "011"),
        };

        private static INamedValueTokenParser[] projectCommandParsers = {

            new CommonCustomSpeechValueTokenParsers(),

            new Any1ValueNamedValueTokenParser("--name", "csr.project.name", "001"),
            new Any1ValueNamedValueTokenParser("--description", "csr.project.description", "001"),
            new Any1ValueNamedValueTokenParser("--language", "csr.project.language", "001"),

            new Any1or2ValueNamedValueTokenParser("--property", "csr.project.create.property", "0001"),
            new AtFileOrListNamedValueTokenParser("--properties", "csr.project.create.properties", "0001"),

            new Any1ValueNamedValueTokenParser("--id", "csr.project.id", "001;010"),

            new OutputFileNameNamedValueTokenParser(null, "csr.output.file", "011"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.json.file", "0110"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.request.file", "0110"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.project.id", "0111;0101", "csr.output.id"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.project.url", "0110;0101", "csr.output.url"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.add.project.id", "01111;01101", "csr.output.add.id"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.add.project.url", "01110;01101", "csr.output.add.url"),
        };

        private static INamedValueTokenParser[] datasetCommandParsers = {

            new CommonCustomSpeechValueTokenParsers(),

            new Any1ValueNamedValueTokenParser(null, "csr.project.id", "010"),
            
            new Any1ValueNamedValueTokenParser(null, "csr.dataset.kind", "001"),
            new Any1ValueNamedValueTokenParser("--name", "csr.dataset.name", "001"),
            new Any1ValueNamedValueTokenParser("--description", "csr.dataset.description", "001"),
            new Any1ValueNamedValueTokenParser("--language", "csr.dataset.language", "001"),

            new Any1ValueNamedValueTokenParser(null, "csr.dataset.create.content.url", "00010"),
            new Any1or2ValueNamedValueTokenParser("--property", "csr.dataset.create.property", "0001"),
            new AtFileOrListNamedValueTokenParser("--properties", "csr.dataset.create.properties", "0001"),

            new Any1ValueNamedValueTokenParser("--data", "csr.dataset.upload.data.file", "00010"),

            new Any1ValueNamedValueTokenParser("--id", "csr.dataset.id", "001;010"),
            new Any1ValueNamedValueTokenParser(null, "csr.dataset.file.id", "0010"),

            new OutputFileNameNamedValueTokenParser(null, "csr.output.file", "011"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.json.file", "0110"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.request.file", "0110"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.dataset.id", "0111;0101", "csr.output.id"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.dataset.url", "0110;0101", "csr.output.url"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.add.dataset.id", "01111;01101", "csr.output.add.id"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.add.dataset.url", "01110;01101", "csr.output.add.url"),

            new OptionalWithDefaultNamedValueTokenParser(null, "csr.wait.timeout", "010", "864000000"),
        };

        private static INamedValueTokenParser[] modelCommandParsers = {

            new CommonCustomSpeechValueTokenParsers(),

            new Any1ValueNamedValueTokenParser(null, "csr.project.id", "010"),

            new Any1ValueNamedValueTokenParser("--name", "csr.model.name", "001"),
            new Any1ValueNamedValueTokenParser("--description", "csr.model.description", "001"),
            new Any1ValueNamedValueTokenParser("--language", "csr.model.language", "001"),

            new Any1ValueNamedValueTokenParser(null, "csr.model.create.base.model.id", "000100"),
            new AtFileOrListNamedValueTokenParser("--datasets", "csr.model.create.dataset.ids", "00011"),
            new Any1ValueNamedValueTokenParser(null, "csr.model.create.dataset.id", "00010"),
            new Any1ValueNamedValueTokenParser("--text", "csr.model.create.text", "0001"),
            new Any1or2ValueNamedValueTokenParser("--property", "csr.model.create.property", "0001"),
            new AtFileOrListNamedValueTokenParser("--properties", "csr.model.create.properties", "0001"),

            new Any1ValueNamedValueTokenParser(null, "csr.model.copy.target.key", "00011"),

            new Any1ValueNamedValueTokenParser("--id", "csr.model.id", "001;010"),

            new OutputFileNameNamedValueTokenParser(null, "csr.output.file", "011"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.json.file", "0110"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.request.file", "0110"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.model.id", "0111;0101", "csr.output.id"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.model.url", "0110;0101", "csr.output.url"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.add.model.id", "01111;01101", "csr.output.add.id"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.add.model.url", "01110;01101", "csr.output.add.url"),

            new OptionalWithDefaultNamedValueTokenParser(null, "csr.wait.timeout", "010", "864000000"),
        };

        private static INamedValueTokenParser[] evaluationCommandParsers = {

            new CommonCustomSpeechValueTokenParsers(),

            new Any1ValueNamedValueTokenParser(null, "csr.project.id", "010"),

            new Any1ValueNamedValueTokenParser("--name", "csr.evaluation.name", "001"),
            new Any1ValueNamedValueTokenParser("--description", "csr.evaluation.description", "001"),
            new Any1ValueNamedValueTokenParser("--language", "csr.evaluation.language", "001"),

            new Any1ValueNamedValueTokenParser("--model1", "csr.evaluation.create.model1.id", "00010"),
            new Any1ValueNamedValueTokenParser("--model2", "csr.evaluation.create.model2.id", "00010"),

            new Any1ValueNamedValueTokenParser("--dataset", "csr.evaluation.create.dataset.id", "00010"),
            new Any1or2ValueNamedValueTokenParser("--property", "csr.evaluation.create.property", "0001"),
            new AtFileOrListNamedValueTokenParser("--properties", "csr.evaluation.create.properties", "0001"),

            new Any1ValueNamedValueTokenParser("--id", "csr.evaluation.id", "001;010"),

            new OutputFileNameNamedValueTokenParser(null, "csr.output.file", "011"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.json.file", "0110"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.request.file", "0110"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.evaluation.id", "0111;0101", "csr.output.id"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.evaluation.url", "0110;0101", "csr.output.url"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.add.evaluation.id", "01111;01101", "csr.output.add.id"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.add.evaluation.url", "01110;01101", "csr.output.add.url"),

            new OptionalWithDefaultNamedValueTokenParser(null, "csr.wait.timeout", "010", "864000000"),
        };

        private static INamedValueTokenParser[] endpointCommandParsers = {

            new CommonCustomSpeechValueTokenParsers(),

            new Any1ValueNamedValueTokenParser(null, "csr.project.id", "010"),

            new Any1ValueNamedValueTokenParser("--name", "csr.endpoint.name", "001"),
            new Any1ValueNamedValueTokenParser("--description", "csr.endpoint.description", "001"),
            new Any1ValueNamedValueTokenParser("--language", "csr.endpoint.language", "001"),
            new TrueFalseNamedValueTokenParser("service.config.content.logging.enabled", "00011;00110"),

            new Any1ValueNamedValueTokenParser("--text", "csr.endpoint.create.text", "0011"),
            new Any1ValueNamedValueTokenParser(null, "csr.endpoint.create.model.id", "00010"),

            new Any1or2ValueNamedValueTokenParser("--property", "csr.endpoint.create.property", "0001"),
            new AtFileOrListNamedValueTokenParser("--properties", "csr.endpoint.create.properties", "0001"),

            new Any1ValueNamedValueTokenParser(null, "csr.endpoint.log.id", "0010"),
            new Any1ValueNamedValueTokenParser("--id", "csr.endpoint.id", "001;010"),

            new OutputFileNameNamedValueTokenParser(null, "csr.output.file", "011"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.json.file", "0110"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.request.file", "0110"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.endpoint.id", "0111;0101", "csr.output.id"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.endpoint.url", "0110;0101", "csr.output.url"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.add.endpoint.id", "01111;01101", "csr.output.add.id"),
            new OutputFileNameNamedValueTokenParser(null, "csr.output.add.endpoint.url", "01110;01101", "csr.outputadd..url"),

            new OptionalWithDefaultNamedValueTokenParser(null, "csr.wait.timeout", "010", "864000000"),
        };

        #endregion
    }
}
