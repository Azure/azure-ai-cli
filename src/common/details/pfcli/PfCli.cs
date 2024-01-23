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
            var argsPart = CliHelpers.BuildCliArgs(
                "--flow", flowPath,
                "--entry", entryFile,
                "--function", functionName,
                "--prompt-template", promptTemplate,
                "--type", type) +
                (yes ? " --yes" : "");

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {argsPart}", null, StdOutputHandler(), StandardErrorHandler());
        }

        public static async Task<ProcessOutput> FlowTest(string flowPath, string inputs, string node = null, string variant = null, bool debug = false, bool interactive = false, bool verbose = false)
        {
            var cmdPart = "flow test";
            var argsPart = CliHelpers.BuildCliArgs(
                "--config", "connection.provider=azureml",
                "--flow", flowPath,
                "--inputs", inputs,
                "--node", node,
                "--variant", variant) +
                (debug ? " --debug" : "") +
                (interactive ? " --interactive" : "") +
                (verbose ? " --verbose" : "");

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {argsPart}", null, StdOutputHandler(), StandardErrorHandler());
        }

        public static Task FlowUpload(string subscription, string resourceGroup, string projectName, string flowName)
        {
            var cmdPart = "flow create";
            var argsPart = CliHelpers.BuildCliArgs(
                "--subscription", subscription,
                "--resource-group", resourceGroup,
                "--workspace-name", projectName,
                "--flow", flowName);

            return ProcessHelpers.RunShellCommandAsync("pfazure", $"{cmdPart} {argsPart} --set name={flowName} type=chat", null, StdOutputHandler(), StandardErrorHandler());
        }

        public static async Task<ProcessOutput> FlowBuild(string flowPath, string output, string format, string variant = null, bool verbose = false, bool debug = false)
        {
            var cmdPart = "flow build";
            var argsPart = CliHelpers.BuildCliArgs(
                "--source", flowPath,
                "--output", output,
                "--format", format,
                "--variant", variant) +
                (verbose ? " --verbose" : "") +
                (debug ? " --debug" : "");

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {argsPart}", null, StdOutputHandler(), StandardErrorHandler());
        }

        public static async Task<ProcessOutput> FlowServe(string flowPath, string port = null, string host = null, string environmentVariables = null, bool verbose = false, bool debug = false)
        {
            var cmdPart = "flow serve";
            var argsPart = CliHelpers.BuildCliArgs(
                "--config", "connection.provider=azureml",
                "--source", flowPath,
                "--port", port,
                "--host", host,
                "--environment-variables", environmentVariables) +
                (verbose ? " --verbose" : "") +
                (debug ? " --debug" : "");

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {argsPart}", null, StdOutputHandler(), StandardErrorHandler());
        }

        public static async Task<ProcessOutput> RunCreate(string flowPath, string file, string data, string columnMapping, string run, string variant, bool stream, string environmentVariables, string connections, string set)
        {
            var cmdPart = "run create";
            var argsPart = CliHelpers.BuildCliArgs(
                "--flow", flowPath,
                "--file", file,
                "--data", data,
                "--column-mapping", columnMapping,
                "--run", run,
                "--variant", variant,
                "--environment-variables", environmentVariables,
                "--connections", connections,
                "--set", set) +
                (stream ? " --stream" : "");

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {argsPart}", null, StdOutputHandler(), StandardErrorHandler());
        }

        public static async Task<ProcessOutput> RunUpdate(string name, string set)
        {
            var cmdPart = "run update";
            var argsPart = CliHelpers.BuildCliArgs(
                "--name", name,
                "--set", set);

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {argsPart}", null, StdOutputHandler(), StandardErrorHandler());
        }

        public static async Task<ProcessOutput> RunStream(string name)
        {
            var cmdPart = "run stream";
            var argsPart = CliHelpers.BuildCliArgs("--name", name);

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {argsPart}", null, StdOutputHandler(), StandardErrorHandler());
        }

        public static async Task<ProcessOutput> RunList(bool allResults, bool archivedOnly, bool includeArchived, int maxResults)
        {
            var cmdPart = "run list";
            var argsPart = CliHelpers.BuildCliArgs(
                "--max-results", maxResults.ToString()) +
                (allResults ? " --all-results" : "") +
                (archivedOnly ? " --archived-only" : "") +
                (includeArchived ? " --include-archived" : "");

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {argsPart}", null, StdOutputHandler(), StandardErrorHandler());
        }

        public static async Task<ProcessOutput> RunShow(string name)
        {
            var cmdPart = "run show";
            var argsPart = CliHelpers.BuildCliArgs("--name", name);

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {argsPart}", null, StdOutputHandler(), StandardErrorHandler());
        }

        public static async Task<ProcessOutput> RunShowDetails(string name)
        {
            var cmdPart = "run show-details";
            var argsPart = CliHelpers.BuildCliArgs("--name", name);

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {argsPart}", null, StdOutputHandler(), StandardErrorHandler());
        }

        public static async Task<ProcessOutput> RunShowMetrics(string name)
        {
            var cmdPart = "run show-metrics";
            var argsPart = CliHelpers.BuildCliArgs("--name", name);

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {argsPart}", null, StdOutputHandler(), StandardErrorHandler());
        }

        public static async Task<ProcessOutput> RunVisualize(string names)
        {
            var cmdPart = "run visualize";
            var argsPart = CliHelpers.BuildCliArgs("--names", names);

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {argsPart}", null, StdOutputHandler(), StandardErrorHandler());
        }

        public static async Task<ProcessOutput> RunArchive(string name)
        {
            var cmdPart = "run archive";
            var argsPart = CliHelpers.BuildCliArgs("--name", name);

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {argsPart}", null, StdOutputHandler(), StandardErrorHandler());
        }

        public static async Task<ProcessOutput> RunRestore(string name)
        {
            var cmdPart = "run restore";
            var argsPart = CliHelpers.BuildCliArgs("--name", name);

            return await ProcessHelpers.RunShellCommandAsync("pf", $"{cmdPart} {argsPart}", null, StdOutputHandler(), StandardErrorHandler());
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
