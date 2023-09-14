//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Azure.AI.Details.Common.CLI
{
    public class PythonSDKWrapper
    {
        static public string CreateResource(INamedValues values, string subscription, string group, string name, string location, string displayName, string description)
        {
            return DoCreateResourceViaPython(values, subscription, group, name, location, displayName, description);
        }

        static public string CreateProject(INamedValues values, string subscription, string group, string resource, string name, string location, string displayName = null, string description = null, string openAiResourceId = null)
        {
            return DoCreateProjectViaPython(values, subscription, group, resource, name, location, displayName, description, openAiResourceId);
        }

        static public string ListResources(INamedValues values, string subscription)
        {
            return CheckIgnorePythonSdkErrors(() => DoListResourcesViaPython(values, subscription), "[]");
        }

        static public string ListProjects(INamedValues values, string subscription)
        {
            return CheckIgnorePythonSdkErrors(() => DoListProjectsViaPython(values, subscription), "[]");
        }

        static public string ListConnections(INamedValues values, string subscription, string group, string projectName)
        {
            return CheckIgnorePythonSdkErrors(() => DoListConnectionsViaPython(values, subscription, group, projectName), "[]");
        }

        static public string DeleteResource(INamedValues values, string subscription, string group, string name, bool deleteDependentResources)
        {
            return DoDeleteResourceViaPython(values, subscription, group, name, deleteDependentResources);
        }

        static public string DeleteProject(INamedValues values, string subscription, string group, string name, bool deleteDependentResources)
        {
            return DoDeleteProjectViaPython(values, subscription, group, name, deleteDependentResources);
        }

        static public string CreateConnection(INamedValues values, string subscription, string group, string projectName, string connectionName, string connectionType, string endpoint, string key)
        {
            return DoCreateConnectionViaPython(values, subscription, group, projectName, connectionName, connectionType, endpoint, key);
        }

        static public string GetConnection(INamedValues values, string subscription, string group, string projectName, string connectionName)
        {
            return DoGetConnectionViaPython(values, subscription, group, projectName, connectionName);
        }

        private static string DoCreateResourceViaPython(INamedValues values, string subscription, string group, string name, string location, string displayName, string description)
        {
            var createResource = () => RunEmbeddedPythonScript(values,
                    "hub_create",
                    "--subscription", subscription,
                    "--group", group,
                    "--name", name, 
                    "--location", location,
                    "--display-name", displayName,
                    "--description", description);

            var output = TryCatchHelpers.TryCatchNoThrow<string>(() => createResource(), null, out var exception);
            if (!string.IsNullOrEmpty(output)) return output;

            var noResourceGroup = exception.Message.Contains("azure.core.exceptions.ResourceNotFoundError");
            if (noResourceGroup)
            {
                values.Reset("error");
                CreateResourceGroup(values, subscription, location, group);
                return createResource();
            }

            throw exception;
        }

        private static string DoCreateProjectViaPython(INamedValues values, string subscription, string group, string resource, string name, string location, string displayName, string description, string openAiResourceId)
        {
            return RunEmbeddedPythonScript(values,
                    "project_create",
                    "--subscription", subscription,
                    "--group", group,
                    "--resource", resource,
                    "--name", name, 
                    "--location", location,
                    "--display-name", displayName,
                    "--description", description,
                    "--openai-resource-id", openAiResourceId);
        }

        private static string DoListResourcesViaPython(INamedValues values, string subscription)
        {
            return RunEmbeddedPythonScript(values, "hub_list", "--subscription", subscription);
        }

        private static string DoListProjectsViaPython(INamedValues values, string subscription)
        {
            return RunEmbeddedPythonScript(values, "project_list", "--subscription", subscription);
        }

        private static string DoListConnectionsViaPython(INamedValues values, string subscription, string group, string projectName)
        {
            return RunEmbeddedPythonScript(values, "connection_list", "--subscription", subscription, "--group", group, "--project-name", projectName);
        }

        private static string DoDeleteResourceViaPython(INamedValues values, string subscription, string group, string name, bool deleteDependentResources)
        {
            return RunEmbeddedPythonScript(values,
                    "hub_delete",
                    "--subscription", subscription,
                    "--group", group,
                    "--name", name, 
                    "--delete-dependent-resources", deleteDependentResources ? "true" : "false");
        }

        private static string DoDeleteProjectViaPython(INamedValues values, string subscription, string group, string name, bool deleteDependentResources)
        {
            return RunEmbeddedPythonScript(values,
                    "project_delete",
                    "--subscription", subscription,
                    "--group", group,
                    "--name", name, 
                    "--delete-dependent-resources", deleteDependentResources ? "true" : "false");
        }

        private static string DoCreateConnectionViaPython(INamedValues values, string subscription, string group, string projectName, string connectionName, string connectionType, string endpoint, string key)
        {
            return RunEmbeddedPythonScript(values,
                    "api_key_connection_create",
                    "--subscription", subscription,
                    "--group", group,
                    "--project-name", projectName,
                    "--connection-name", connectionName,
                    "--connection-type", connectionType,
                    "--endpoint", endpoint,
                    "--key", key);
        }

        private static string DoGetConnectionViaPython(INamedValues values, string subscription, string group, string projectName, string connectionName)
        {
            return RunEmbeddedPythonScript(values,
                    "api_key_connection_get",
                    "--subscription", subscription,
                    "--group", group,
                    "--project-name", projectName,
                    "--connection-name", connectionName);
        }

        private static string BuildPythonScriptArgs(params string[] args)
        {
            var sb = new StringBuilder();
            for (int i = 0; i + 1 < args.Length; i += 2)
            {
                var argName = args[i];
                var argValue = args[i + 1];

                if (string.IsNullOrWhiteSpace(argValue)) continue;

                sb.Append(argValue.Contains(' ')
                    ? $"{argName} \"{argValue}\""
                    : $"{argName} {argValue}");
                sb.Append(' ');
            }
            return sb.ToString().Trim();
        }

        private static string RunEmbeddedPythonScript(INamedValues values, string scriptName, params string[] args)
        {
            var path = FileHelpers.FindFileInHelpPath($"help/include.python.script.{scriptName}.py");
            var script = FileHelpers.ReadAllHelpText(path, Encoding.UTF8);
            var scriptArgs = BuildPythonScriptArgs(args);

            if (Program.Debug) Console.WriteLine($"DEBUG: {scriptName}.py:\n{script}");
            if (Program.Debug) Console.WriteLine($"DEBUG: PythonRunner.RunScriptAsync: '{scriptName}' {scriptArgs}");

            var dbgOut = script.Replace("\n", "\\n").Replace("\r", "");
            AI.DBG_TRACE_VERBOSE($"RunEmbeddedPythonScript: {scriptName}.py: {dbgOut}");
            AI.DBG_TRACE_VERBOSE($"RunEmbeddedPythonScript: '{scriptName}' {scriptArgs}");

            (var exit, var output)= PythonRunner.RunScriptAsync(script, scriptArgs).Result;
            if (exit != 0) AI.DBG_TRACE_WARNING($"RunEmbeddedPythonScript: exit={exit}");

            dbgOut = output.Replace("\n", "\\n").Replace("\r", "");
            AI.DBG_TRACE_INFO($"RunEmbeddedPythonScript: exit={exit}; output:{dbgOut}");
            if (Program.Debug) Console.WriteLine($"DEBUG: RunEmbeddedPythonScript: exit={exit}; output=\n<---start--->{output}\n<---stop--->");

            if (exit != 0)
            {
                output = output.Trim('\r', '\n', ' ');
                output = "\n\n    " + output.Replace("\n", "\n    ");

                var info = new List<string>();

                if (output.Contains("azure.identity"))
                {
                    info.Add("WARNING:");
                    info.Add("azure-identity Python wheel not found!");
                    info.Add("");
                    info.Add("TRY:");
                    info.Add("pip install azure-identity");
                    info.Add("SEE:");
                    info.Add("https://pypi.org/project/azure-identity/");
                    info.Add("");
                }
                else if (output.Contains("azure.mgmt.resource"))
                {
                    info.Add("WARNING:");
                    info.Add("azure-mgmt-resource Python wheel not found!");
                    info.Add("");
                    info.Add("TRY:");
                    info.Add("pip install azure-mgmt-resource");
                    info.Add("SEE:");
                    info.Add("https://pypi.org/project/azure-mgmt-resource/");
                    info.Add("");
                }
                else if (output.Contains("azure.ai.ml"))
                {
                    info.Add("WARNING:");
                    info.Add("azure-ai-ml Python wheel not found!");
                    info.Add("");
                    info.Add("TRY:");
                    info.Add("pip install azure-ai-ml");
                    info.Add("SEE:");
                    info.Add("https://pypi.org/project/azure-ai-ml/");
                    info.Add("");
                }
                else if (output.Contains("ModuleNotFoundError"))
                {
                    info.Add("WARNING:");
                    info.Add("Python wheel not found!");
                    info.Add("");
                }

                info.Add("ERROR:");
                info.Add($"Python script failed! (exit code={exit})");
                info.Add("");
                info.Add("OUTPUT:");
                info.Add(output);

                values.AddThrowError(info[0], info[1], info.Skip(2).ToArray());
            }

            return ParseOutputAndSkipLinesUntilStartsWith(output, "---").Trim('\r', '\n', ' ');
        }

        private static string ParseOutputAndSkipLinesUntilStartsWith(string output, string startsWith)
        {
            var lines = output.Split('\n');
            var sb = new StringBuilder();
            var skip = true;
            foreach (var line in lines)
            {
                if (skip && line.StartsWith(startsWith))
                {
                    skip = false;
                }
                else if (!skip)
                {
                    sb.AppendLine(line);
                }
            }
            return sb.ToString();
        }

        private static void CreateResourceGroup(INamedValues values, string subscription, string location, string group)
        {
            var quiet = values.GetOrDefault("x.quiet", false);

            var message = $"Creating resource group '{group}'...";
            if (!quiet) Console.WriteLine(message);

            var response = AzCli.CreateResourceGroup(subscription, location, group).Result;
            if (string.IsNullOrEmpty(response.StdOutput) && !string.IsNullOrEmpty(response.StdError))
            {
                values.AddThrowError(
                    "ERROR:", "Creating resource group",
                    "OUTPUT:", response.StdError);
            }

            if (!quiet) Console.WriteLine($"{message} Done!");
        }

        private static string CheckIgnorePythonSdkErrors(Func<string> func, string returnOnError)
        {
            // Check to see if the environment variable "AZAI_IGNORE_PYTHON_SDK_ERRORS" is set
            // If it is, then we will ignore any errors from the Python SDK and return the value of "returnOnError"
            var ignorePythonSdkErrors = Environment.GetEnvironmentVariable("AZAI_IGNORE_PYTHON_SDK_ERRORS");
            return ignorePythonSdkErrors != null && ignorePythonSdkErrors != "false" && ignorePythonSdkErrors != "0"
                ? TryCatchHelpers.TryCatchNoThrow<string>(() => func(), returnOnError, out var exception)
                : func();
        }
    }
}
