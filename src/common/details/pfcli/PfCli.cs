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
        public static async Task<ProcessOutput> FlowInit(string flowPath, string entryFile = null, string functionName = null, string promptTemplate = null, string type = null, bool yes = false)
        {
            var cmdPart = "flow init";
            var flowPart = $"--flow {flowPath}";
            var entryPart = string.IsNullOrEmpty(entryFile) ? "" : $"--entry {entryFile}";
            var functionPart = string.IsNullOrEmpty(functionName) ? "" : $"--function {functionName}";
            var promptPart = string.IsNullOrEmpty(promptTemplate) ? "" : $"--prompt-template {promptTemplate}";
            var typePart = string.IsNullOrEmpty(type) ? "" : $"--type {type}";
            var yesPart = yes ? "--yes" : "";

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {flowPart} {entryPart} {functionPart} {promptPart} {typePart} {yesPart}", null, StdOutputHandler(), StandardErrorHandler());
        }

        public static async Task<ProcessOutput> FlowTest(string flowPath, string inputs, string node = null, string variant = null, bool debug = false, bool interactive = false, bool verbose = false)
        {
            var cmdPart = "flow test";
            var flowPart = $"--flow {flowPath}";
            var inputsPart = string.IsNullOrEmpty(inputs) ? "" : $"--inputs {inputs}";
            var nodePart = string.IsNullOrEmpty(node) ? "" : $"--node {node}";
            var variantPart = string.IsNullOrEmpty(variant) ? "" : $"--variant {variant}";
            var debugPart = debug ? "--debug" : "";
            var interactivePart = interactive ? "--interactive" : "";
            var verbosePart = verbose ? "--verbose" : "";

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {flowPart} {inputsPart} {nodePart} {variantPart} {debugPart} {interactivePart} {verbosePart}", null, StdOutputHandler(), StandardErrorHandler());
        }

        public static async Task<ProcessOutput> FlowBuild(string flowPath, string output, string format, string variant = null, bool verbose = false, bool debug = false)
        {
            var cmdPart = "flow build";
            var flowPart = $"--source {flowPath}";
            var outputPart = string.IsNullOrEmpty(output) ? "" : $"--output {output}";
            var formatPart = string.IsNullOrEmpty(format) ? "" : $"--format {format}";
            var variantPart = string.IsNullOrEmpty(variant) ? "" : $"--variant {variant}";
            var verbosePart = verbose ? "--verbose" : "";
            var debugPart = debug ? "--debug" : "";

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {flowPart} {outputPart} {formatPart} {variantPart} {verbosePart} {debugPart}", null, StdOutputHandler(), StandardErrorHandler());
        }

        public static async Task<ProcessOutput> FlowServe(string flowPath, string port = null, string host = null, string environmentVariables = null, bool verbose = false, bool debug = false)
        {
            var cmdPart = "flow serve";
            var flowPart = $"--source {flowPath}";
            var portPart = string.IsNullOrEmpty(port) ? "" : $"--port {port}";
            var hostPart = string.IsNullOrEmpty(host) ? "" : $"--host {host}";
            var environmentVariablesPart = string.IsNullOrEmpty(environmentVariables) ? "" : $"--environment-variables {environmentVariables}";
            var verbosePart = verbose ? "--verbose" : "";
            var debugPart = debug ? "--debug" : "";

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {flowPart} {portPart} {hostPart} {environmentVariablesPart} {verbosePart} {debugPart}", null, StdOutputHandler(), StandardErrorHandler());
        }

        public static async Task<ProcessOutput> RunCreate(string flowPath, string file, string flow, string data, string columnMapping, string run, string variant, bool stream, string environmentVariables, string connections, string set)
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

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {filePart} {flowPart} {dataPart} {columnMappingPart} {runPart} {variantPart} {streamPart} {environmentVariablesPart} {connectionsPart} {setPart}");
        }

        public static async Task<ProcessOutput> RunUpdate(string name, string set)
        {
            var cmdPart = "run update";
            var namePart = string.IsNullOrEmpty(name) ? "" : $"--name {name}";
            var setPart = string.IsNullOrEmpty(set) ? "" : $"--set {set}";

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {namePart} {setPart}");
        }

        public static async Task<ProcessOutput> RunStream(string name)
        {
            var cmdPart = "run stream";
            var namePart = string.IsNullOrEmpty(name) ? "" : $"--name {name}";

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {namePart}");
        }

        public static async Task<ProcessOutput> RunList(bool allResults, bool archivedOnly, bool includeArchived, int maxResults)
        {
            var cmdPart = "run list";
            var allResultsPart = allResults ? "--all-results" : "";
            var archivedOnlyPart = archivedOnly ? "--archived-only" : "";
            var includeArchivedPart = includeArchived ? "--include-archived" : "";
            var maxResultsPart = maxResults > 0 ? $"--max-results {maxResults}" : "";

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {allResultsPart} {archivedOnlyPart} {includeArchivedPart} {maxResultsPart}");
        }

        public static async Task<ProcessOutput> RunShow(string name)
        {
            var cmdPart = "run show";
            var namePart = string.IsNullOrEmpty(name) ? "" : $"--name {name}";

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {namePart}");
        }

        public static async Task<ProcessOutput> RunShowDetails(string name)
        {
            var cmdPart = "run show-details";
            var namePart = string.IsNullOrEmpty(name) ? "" : $"--name {name}";

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {namePart}");
        }

        public static async Task<ProcessOutput> RunShowMetrics(string name)
        {
            var cmdPart = "run show-metrics";
            var namePart = string.IsNullOrEmpty(name) ? "" : $"--name {name}";

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {namePart}");
        }

        public static async Task<ProcessOutput> RunVisualize(string names)
        {
            var cmdPart = "run visualize";
            var namesPart = string.IsNullOrEmpty(names) ? "" : $"--names {names}";

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {namesPart}");
        }

        public static async Task<ProcessOutput> RunArchive(string name)
        {
            var cmdPart = "run archive";
            var namePart = string.IsNullOrEmpty(name) ? "" : $"--name {name}";

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {namePart}");
        }

        public static async Task<ProcessOutput> RunRestore(string name)
        {
            var cmdPart = "run restore";
            var namePart = string.IsNullOrEmpty(name) ? "" : $"--name {name}";

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {namePart}");
        }

        private static Action<string> StandardErrorHandler()
        {
            return x => Console.Error.WriteLine(RemapStrings(x));
        }

        private static Action<string> StdOutputHandler()
        {
            return x => Console.WriteLine(RemapStrings(x));
        }

        private static string RemapStrings(string x)
        {
            var check = "You can execute this command to test the flow, pf flow test";
            if (x.StartsWith(check))
            {
                x = x.Replace(check, "You can execute this command to test the flow, ai flow invoke");
                x = x.Replace(" --interactive", "");
            }
            return x;
        }
    }
}
