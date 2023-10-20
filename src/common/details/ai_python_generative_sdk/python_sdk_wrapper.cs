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

        static public string DeleteConnection(INamedValues values, string subscription, string group, string projectName, string connectionName)
        {
            return DoDeleteConnectionViaPython(values, subscription, group, projectName, connectionName);
        }

        static public string CreateConnection(INamedValues values, string subscription, string group, string projectName, string connectionName, string connectionType, string endpoint, string key)
        {
            return DoCreateConnectionViaPython(values, subscription, group, projectName, connectionName, connectionType, endpoint, key);
        }

        static public string GetConnection(INamedValues values, string subscription, string group, string projectName, string connectionName)
        {
            return DoGetConnectionViaPython(values, subscription, group, projectName, connectionName);
        }

        static public string UpdateMLIndex(INamedValues values, string subscription, string group, string projectName, string indexName, string embeddingModelDeployment, string embeddingModelName, string dataFiles, string externalSourceUrl)
        {
            return PythonRunner.RunEmbeddedPythonScript(values,
                    "ml_index_update",
                    "--subscription", subscription,
                    "--group", group,
                    "--project-name", projectName,
                    "--index-name", indexName,
                    "--embedding-model-deployment", embeddingModelDeployment,
                    "--embedding-model-name", embeddingModelName,
                    "--data-files", dataFiles,
                    "--external-source-url", externalSourceUrl);
        }

        private static string DoCreateResourceViaPython(INamedValues values, string subscription, string group, string name, string location, string displayName, string description)
        {
            var createResource = () => PythonRunner.RunEmbeddedPythonScript(values,
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
            return PythonRunner.RunEmbeddedPythonScript(values,
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
            return PythonRunner.RunEmbeddedPythonScript(values, "hub_list", "--subscription", subscription);
        }

        private static string DoListProjectsViaPython(INamedValues values, string subscription)
        {
            return PythonRunner.RunEmbeddedPythonScript(values, "project_list", "--subscription", subscription);
        }

        private static string DoListConnectionsViaPython(INamedValues values, string subscription, string group, string projectName)
        {
            return PythonRunner.RunEmbeddedPythonScript(values, "connection_list", "--subscription", subscription, "--group", group, "--project-name", projectName);
        }

        private static string DoDeleteResourceViaPython(INamedValues values, string subscription, string group, string name, bool deleteDependentResources)
        {
            return PythonRunner.RunEmbeddedPythonScript(values,
                    "hub_delete",
                    "--subscription", subscription,
                    "--group", group,
                    "--name", name, 
                    "--delete-dependent-resources", deleteDependentResources ? "true" : "false");
        }

        private static string DoDeleteProjectViaPython(INamedValues values, string subscription, string group, string name, bool deleteDependentResources)
        {
            return PythonRunner.RunEmbeddedPythonScript(values,
                    "project_delete",
                    "--subscription", subscription,
                    "--group", group,
                    "--name", name, 
                    "--delete-dependent-resources", deleteDependentResources ? "true" : "false");
        }

        private static string DoDeleteConnectionViaPython(INamedValues values, string subscription, string group, string projectName, string connectionName)
        {
            return PythonRunner.RunEmbeddedPythonScript(values,
                    "connection_delete",
                    "--subscription", subscription,
                    "--group", group,
                    "--project-name", projectName, 
                    "--connection-name", connectionName);
        }

        private static string DoCreateConnectionViaPython(INamedValues values, string subscription, string group, string projectName, string connectionName, string connectionType, string endpoint, string key)
        {
            return PythonRunner.RunEmbeddedPythonScript(values,
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
            return PythonRunner.RunEmbeddedPythonScript(values,
                    "api_key_connection_get",
                    "--subscription", subscription,
                    "--group", group,
                    "--project-name", projectName,
                    "--connection-name", connectionName);
        }

        private static void CreateResourceGroup(INamedValues values, string subscription, string location, string group)
        {
            var quiet = values.GetOrDefault("x.quiet", false);

            var message = $"Creating resource group '{group}'...";
            if (!quiet) Console.WriteLine(message);

            var response = AzCli.CreateResourceGroup(subscription, location, group).Result;
            if (string.IsNullOrEmpty(response.Output.StdOutput) && !string.IsNullOrEmpty(response.Output.StdError))
            {
                values.AddThrowError(
                    "ERROR:", "Creating resource group",
                    "OUTPUT:", response.Output.StdError);
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
