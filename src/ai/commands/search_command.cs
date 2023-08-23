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
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.Memory.AzureCognitiveSearch;

namespace Azure.AI.Details.Common.CLI
{
    public class SearchCommand : Command
    {
        internal SearchCommand(ICommandValues values)
        {
            _values = values.ReplaceValues();
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", true);
        }

        internal bool RunCommand()
        {
            try
            {
                RunSearchCommand();
            }
            catch (WebException ex)
            {
                ConsoleHelpers.WriteLineError($"\n  ERROR: {ex.Message}");
                JsonHelpers.PrintJson(HttpHelpers.ReadWriteJson(ex.Response, _values, "search"));
            }

            return _values.GetOrDefault("passed", true);
        }

        private bool RunSearchCommand()
        {
            DoCommand(_values.GetCommand());
            return _values.GetOrDefault("passed", true);
        }

        private void DoCommand(string command)
        {
            CheckPath();

            switch (command)
            {
                case "search.index.update": DoIndexUpdate(); break;

                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
            }
        }

        private void DoIndexUpdate()
        {
            var action = "Updating search index";
            var command = "search index update";
            var searchEndpoint = DemandSearchEndpointUri(action, command);
            var searchApiKey = DemandSearchApiKey(action, command);
            var embeddingsEndpoint = DemandEmbeddingsEndpointUri(action, command);
            var embeddingsDeployment = DemandEmbeddingsDeployment(action, command);
            var embeddingsApiKey = DemandEmbeddingsApiKey(action, command);
            var searchIndexName = GetSearchIndexName() ?? "MyIndex";

            var message = $"{action} '{searchIndexName}'";

            if (!_quiet) Console.WriteLine(message);
            DoIndexUpdateWithSK(searchEndpoint, searchApiKey, embeddingsEndpoint, embeddingsDeployment, embeddingsApiKey, searchIndexName);
            if (!_quiet) Console.WriteLine($"{message} Done!\n");
        }

        private void DoIndexUpdateWithSK(string searchEndpoint, string searchApiKey, string embeddingsEndpoint, string embeddingsDeployment, string embeddingsApiKey, string searchIndexName)
        {
            var pattern = GetSearchIndexUpdateFiles() ?? GetSearchIndexUpdateFile();
            var files = FileHelpers.FindFilesInDataPath(pattern, _values)
                .Where(x => File.Exists(x))
                .Select(x => new FileInfo(x).FullName)
                .Distinct();

            var kvps = files.Select(x => new KeyValuePair<string, string>(x, File.ReadAllText(x))).ToList();

            var kernel = CreateSemanticKernel(searchEndpoint, searchApiKey, embeddingsEndpoint, embeddingsDeployment, embeddingsApiKey);
            StoreMemoryAsync(kernel, searchIndexName, kvps).Wait();
        }

        private IKernel? CreateSemanticKernel(string searchEndpoint, string searchApiKey, string embeddingsEndpoint, string embeddingsDeployment, string embeddingsApiKey)
        {
            var store = new AzureCognitiveSearchMemoryStore(searchEndpoint, searchApiKey);
            var kernelWithACS = Kernel.Builder
                .WithAzureTextEmbeddingGenerationService(embeddingsDeployment, embeddingsEndpoint, embeddingsApiKey)
                .WithMemoryStorage(store)
                .Build();

            return kernelWithACS;
        }

        private static async Task StoreMemoryAsync(IKernel kernel, string index, IEnumerable<KeyValuePair<string, string>> kvps)
        {
            var list = kvps.ToList();
            if (list.Count() == 0) return;

            foreach (var entry in list)
            {
                await kernel.Memory.SaveInformationAsync(
                    collection: index,
                    text: entry.Value,
                    id: entry.Key);

                 Console.WriteLine($"{entry.Key}: {entry.Value.Length} bytes");
            }
            Console.WriteLine();
        }

        private string DemandSearchEndpointUri(string action, string command)
        {
            var endpointUri = GetSearchEndpointUri();
            if (string.IsNullOrEmpty(endpointUri) || endpointUri.Contains("rror"))
            {
                _values.AddThrowError(
                    "ERROR:", $"{action}; requires search endpoint uri.",
                            "",
                      "TRY:", $"{Program.Name} config search --set endpoint ENDPOINT",
                              $"{Program.Name} {command} --search-endpoint ENDPOINT",
                            "",
                      "SEE:", $"{Program.Name} help {command}");
            }
            return endpointUri;
        }

        private string DemandSearchApiKey(string action, string command)
        {
            var searchApiKey = GetSearchApiKey();
            if (string.IsNullOrEmpty(searchApiKey) || searchApiKey.Contains("rror"))
            {
                _values.AddThrowError(
                    "ERROR:", $"{action}; requires search api key.",
                            "",
                      "TRY:", $"{Program.Name} config search --set api.key KEY",
                              $"{Program.Name} {command} --search-api-key KEY",
                            "",
                      "SEE:", $"{Program.Name} help {command}");
            }
            return searchApiKey;
        }

        private string DemandEmbeddingsEndpointUri(string action, string command)
        {
            var endpointUri = GetEmbeddingsEndpointUri();
            if (string.IsNullOrEmpty(endpointUri) || endpointUri.Contains("rror"))
            {
                _values.AddThrowError(
                    "ERROR:", $"{action}; requires embeddings endpoint uri.",
                            "",
                      "TRY:", $"{Program.Name} config search --set embeddings.endpoint ENDPOINT",
                              $"{Program.Name} {command} --embeddings-endpoint ENDPOINT",
                            "",
                      "SEE:", $"{Program.Name} help {command}");
            }
            return endpointUri;
        }

        private string DemandEmbeddingsDeployment(string action, string command)
        {
            var deployment = GetEmbeddingsDeployment();
            if (string.IsNullOrEmpty(deployment) || deployment.Contains("rror"))
            {
                _values.AddThrowError(
                    "ERROR:", $"{action}; requires embeddings deployment.",
                            "",
                      "TRY:", $"{Program.Name} config search --set embeddings.deployment DEPLOYMENT",
                              $"{Program.Name} {command} --embeddings-deployment DEPLOYMENT",
                            "",
                      "SEE:", $"{Program.Name} help {command}");
            }
            return deployment;
        }

        private string DemandEmbeddingsApiKey(string action, string command)
        {
            var embeddingsApiKey = GetEmbeddingsApiKey();
            if (string.IsNullOrEmpty(embeddingsApiKey) || embeddingsApiKey.Contains("rror"))
            {
                _values.AddThrowError(
                    "ERROR:", $"{action}; requires embeddings api key.",
                            "",
                      "TRY:", $"{Program.Name} config search --set embeddings.api.key KEY",
                              $"{Program.Name} {command} --embeddings-api-key KEY",
                            "",
                      "SEE:", $"{Program.Name} help {command}");
            }
            return embeddingsApiKey;
        }

        private string GetSearchApiKey()
        {
            return _values.Get("service.config.search.api.key", true);
        }

        private string GetSearchEndpointUri()
        {
            return _values.Get("service.config.search.endpoint.uri", true);
        }

        private string GetEmbeddingsEndpointUri()
        {
            return _values.Get("search.embeddings.endpoint.uri", true);
        }

        private string GetEmbeddingsApiKey()
        {
            return _values.Get("search.embeddings.api.key", true);
        }

        private string GetEmbeddingsDeployment()
        {
            return _values.Get("search.embeddings.deployment", true);
        }

        private string GetSearchIndexName()
        {
            return _values.Get("search.embeddings.index.name", true);
        }

        private string GetSearchIndexUpdateFile()
        {
            return _values.Get("search.index.update.file", true);
        }

        private string GetSearchIndexUpdateFiles()
        {
            return _values.Get("search.index.update.files", true);
        }

        private bool _quiet = false;
        private bool _verbose = false;
    }
}
