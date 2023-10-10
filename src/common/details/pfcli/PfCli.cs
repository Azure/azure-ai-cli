//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Azure.AI.Details.Common.CLI;
using System.Collections.Generic;

namespace Azure.AI.Details.Common.CLI
{
    public class PfCli
    {
        public static async Task<ProcessResponse<string>> FlowInit(string flowPath, string entryFile = null, string functionName = null, string promptTemplate = null, string type = null, bool yes = false)
        {
            var cmdPart = "flow init";
            var flowPart = $"--flow {flowPath}";
            var entryPart = string.IsNullOrEmpty(entryFile) ? "" : $"--entry {entryFile}";
            var functionPart = string.IsNullOrEmpty(functionName) ? "" : $"--function {functionName}";
            var promptPart = string.IsNullOrEmpty(promptTemplate) ? "" : $"--prompt-template {promptTemplate}";
            var typePart = string.IsNullOrEmpty(type) ? "" : $"--type {type}";
            var yesPart = yes ? "--yes" : "";

            var process = await ProcessHelpers.ParseShellCommandJson<JObject>("pf", $"{cmdPart} {flowPart} {entryPart} {functionPart} {promptPart} {typePart} {yesPart}");

            var x = new ProcessResponse<string>();
            x.StdOutput = process.StdOutput;
            x.StdError = process.StdError;

            x.Payload = process.StdOutput;

            return x;
        }

        public static async Task<ProcessResponse<string>> FlowTest(string flowPath, string inputs, string node, string variant, bool debug, bool interactive, bool verbose)
        {
            var cmdPart = "flow test";
            var flowPart = $"--flow {flowPath}";
            var inputsPart = string.IsNullOrEmpty(inputs) ? "" : $"--inputs {inputs}";
            var nodePart = string.IsNullOrEmpty(node) ? "" : $"--node {node}";
            var variantPart = string.IsNullOrEmpty(variant) ? "" : $"--variant {variant}";
            var debugPart = debug ? "--debug" : "";
            var interactivePart = interactive ? "--interactive" : "";
            var verbosePart = verbose ? "--verbose" : "";

            var process = await ProcessHelpers.ParseShellCommandJson<JObject>("pf", $"{cmdPart} {flowPart} {inputsPart} {nodePart} {variantPart} {debugPart} {interactivePart} {verbosePart}");

            var x = new ProcessResponse<string>();
            x.StdOutput = process.StdOutput;
            x.StdError = process.StdError;

            x.Payload = process.StdOutput;

            return x;
        }

        public static async Task<ProcessResponse<string>> FlowBuild(string flowPath, string output, string format, string variant, bool verbose, bool debug)
        {
            var cmdPart = "flow build";
            var flowPart = $"--source {flowPath}";
            var outputPart = string.IsNullOrEmpty(output) ? "" : $"--output {output}";
            var formatPart = string.IsNullOrEmpty(format) ? "" : $"--format {format}";
            var variantPart = string.IsNullOrEmpty(variant) ? "" : $"--variant {variant}";
            var verbosePart = verbose ? "--verbose" : "";
            var debugPart = debug ? "--debug" : "";

            var process = await ProcessHelpers.ParseShellCommandJson<JObject>("pf", $"{cmdPart} {flowPart} {outputPart} {formatPart} {variantPart} {verbosePart} {debugPart}");

            var x = new ProcessResponse<string>();
            x.StdOutput = process.StdOutput;
            x.StdError = process.StdError;

            x.Payload = flowPath;

            return x;
        }

        public static async Task<ProcessResponse<string>> FlowServe(string flowPath, string port, string host, string environmentVariables, bool verbose, bool debug)
        {
            var cmdPart = "flow serve";
            var flowPart = $"--source {flowPath}";
            var portPart = string.IsNullOrEmpty(port) ? "" : $"--port {port}";
            var hostPart = string.IsNullOrEmpty(host) ? "" : $"--host {host}";
            var environmentVariablesPart = string.IsNullOrEmpty(environmentVariables) ? "" : $"--environment-variables {environmentVariables}";
            var verbosePart = verbose ? "--verbose" : "";
            var debugPart = debug ? "--debug" : "";

            var process = await ProcessHelpers.ParseShellCommandJson<JObject>("pf", $"{cmdPart} {flowPart} {portPart} {hostPart} {environmentVariablesPart} {verbosePart} {debugPart}");

            var x = new ProcessResponse<string>();
            x.StdOutput = process.StdOutput;
            x.StdError = process.StdError;

            x.Payload = process.StdOutput;

            return x;
        }

        public static async Task<ProcessResponse<string>> RunCreate(string flowPath, string file, string flow, string data, string columnMapping, string run, string variant, bool stream, string environmentVariables, string connections, string set)
        {
            var cmdPart = "run create";
            var filePart = string.IsNullOrEmpty(file) ? "" : $"--file {file}";
            var flowPart = string.IsNullOrEmpty(flow) ? "" : $"--flow {flow}";
            var dataPart = string.IsNullOrEmpty(data) ? "" : $"--data {data}";
            var columnMappingPart = string.IsNullOrEmpty(columnMapping) ? "" : $"--column-mapping {columnMapping}";
            var runPart = string.IsNullOrEmpty(run) ? "" : $"--run {run}";
            var variantPart = string.IsNullOrEmpty(variant) ? "" : $"--variant {variant}";
            var streamPart = stream ? "--stream" : "";
            var environmentVariablesPart = string.IsNullOrEmpty(environmentVariables) ? "" : $"--environment-variables {environmentVariables}";
            var connectionsPart = string.IsNullOrEmpty(connections) ? "" : $"--connections {connections}";
            var setPart = string.IsNullOrEmpty(set) ? "" : $"--set {set}";

            var process = await ProcessHelpers.ParseShellCommandJson<JObject>("pf", $"{cmdPart} {filePart} {flowPart} {dataPart} {columnMappingPart} {runPart} {variantPart} {streamPart} {environmentVariablesPart} {connectionsPart} {setPart}");

            var x = new ProcessResponse<string>();
            x.StdOutput = process.StdOutput;
            x.StdError = process.StdError;

            x.Payload = process.StdOutput;

            return x;
        }

        public static async Task<ProcessResponse<string>> RunUpdate(string name, string set)
        {
            var cmdPart = "run update";
            var namePart = string.IsNullOrEmpty(name) ? "" : $"--name {name}";
            var setPart = string.IsNullOrEmpty(set) ? "" : $"--set {set}";

            var process = await ProcessHelpers.ParseShellCommandJson<JObject>("pf", $"{cmdPart} {namePart} {setPart}");

            var x = new ProcessResponse<string>();
            x.StdOutput = process.StdOutput;
            x.StdError = process.StdError;

            x.Payload = process.StdOutput;

            return x;
        }

        public static async Task<ProcessResponse<string>> RunStream(string name)
        {
            var cmdPart = "run stream";
            var namePart = string.IsNullOrEmpty(name) ? "" : $"--name {name}";

            var process = await ProcessHelpers.ParseShellCommandJson<JObject>("pf", $"{cmdPart} {namePart}");

            var x = new ProcessResponse<string>();
            x.StdOutput = process.StdOutput;
            x.StdError = process.StdError;

            x.Payload = process.StdOutput;

            return x;
        }

        public static async Task<ProcessResponse<string>> RunList(bool allResults, bool archivedOnly, bool includeArchived, int maxResults)
        {
            var cmdPart = "run list";
            var allResultsPart = allResults ? "--all-results" : "";
            var archivedOnlyPart = archivedOnly ? "--archived-only" : "";
            var includeArchivedPart = includeArchived ? "--include-archived" : "";
            var maxResultsPart = maxResults > 0 ? $"--max-results {maxResults}" : "";

            var process = await ProcessHelpers.ParseShellCommandJson<JObject>("pf", $"{cmdPart} {allResultsPart} {archivedOnlyPart} {includeArchivedPart} {maxResultsPart}");

            var x = new ProcessResponse<string>();
            x.StdOutput = process.StdOutput;
            x.StdError = process.StdError;

            x.Payload = process.StdOutput;

            return x;
        }

        public static async Task<ProcessResponse<string>> RunShow(string name)
        {
            var cmdPart = "run show";
            var namePart = string.IsNullOrEmpty(name) ? "" : $"--name {name}";

            var process = await ProcessHelpers.ParseShellCommandJson<JObject>("pf", $"{cmdPart} {namePart}");

            var x = new ProcessResponse<string>();
            x.StdOutput = process.StdOutput;
            x.StdError = process.StdError;

            x.Payload = process.StdOutput;

            return x;
        }

        public static async Task<ProcessResponse<string>> RunShowDetails(string name)
        {
            var cmdPart = "run show-details";
            var namePart = string.IsNullOrEmpty(name) ? "" : $"--name {name}";

            var process = await ProcessHelpers.ParseShellCommandJson<JObject>("pf", $"{cmdPart} {namePart}");

            var x = new ProcessResponse<string>();
            x.StdOutput = process.StdOutput;
            x.StdError = process.StdError;

            x.Payload = process.StdOutput;

            return x;
        }

        public static async Task<ProcessResponse<string>> RunShowMetrics(string name)
        {
            var cmdPart = "run show-metrics";
            var namePart = string.IsNullOrEmpty(name) ? "" : $"--name {name}";

            var process = await ProcessHelpers.ParseShellCommandJson<JObject>("pf", $"{cmdPart} {namePart}");

            var x = new ProcessResponse<string>();
            x.StdOutput = process.StdOutput;
            x.StdError = process.StdError;

            x.Payload = process.StdOutput;

            return x;
        }

        public static async Task<ProcessResponse<string>> RunVisualize(string names)
        {
            var cmdPart = "run visualize";
            var namesPart = string.IsNullOrEmpty(names) ? "" : $"--names {names}";

            var process = await ProcessHelpers.ParseShellCommandJson<JObject>("pf", $"{cmdPart} {namesPart}");

            var x = new ProcessResponse<string>();
            x.StdOutput = process.StdOutput;
            x.StdError = process.StdError;

            x.Payload = process.StdError;

            return x;
        }

        public static async Task<ProcessResponse<string>> RunArchive(string name)
        {
            var cmdPart = "run archive";
            var namePart = string.IsNullOrEmpty(name) ? "" : $"--name {name}";

            var process = await ProcessHelpers.ParseShellCommandJson<JObject>("pf", $"{cmdPart} {namePart}");

            var x = new ProcessResponse<string>();
            x.StdOutput = process.StdOutput;
            x.StdError = process.StdError;

            return x;
        }

        public static async Task<ProcessResponse<string>> RunRestore(string name)
        {
            var cmdPart = "run restore";
            var namePart = string.IsNullOrEmpty(name) ? "" : $"--name {name}";

            var process = await ProcessHelpers.ParseShellCommandJson<JObject>("pf", $"{cmdPart} {namePart}");

            var x = new ProcessResponse<string>();
            x.StdOutput = process.StdOutput;
            x.StdError = process.StdError;

            return x;
        }
    }
}
