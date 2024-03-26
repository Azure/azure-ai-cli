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
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.Memory.AzureCognitiveSearch;

using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure.Storage;
using Azure.Identity;
using Azure.Storage.Blobs.Models;
using System.Diagnostics.Tracing;
using Azure.Core.Diagnostics;

namespace Azure.AI.Details.Common.CLI
{
    public class SearchCommand : Command
    {
        internal SearchCommand(ICommandValues values) : base(values)
        {
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
            StartCommand();

            switch (command)
            {
                case "search.index.create": DoIndexUpdate(); break; // POST-IGNITE: TODO: Implement create separately from update
                case "search.index.update": DoIndexUpdate(); break;

                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
            }

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private void DoIndexUpdate()
        {
            var action = "Updating search index";
            var command = "search index update";

            var pattern = DemandSearchIndexUpdateFilesPattern(action, command);
            var searchIndexName = SearchIndexNameToken.Data().Demand(_values, action, command);
            var blobContainer = BlobContainerToken.Data().GetOrDefault(_values);

            var message = $"{action} '{searchIndexName}' ...";
            if (!_quiet) Console.WriteLine(message);

            var output = string.Empty;

            var useIndexer = !string.IsNullOrEmpty(blobContainer);
            var useSK = !useIndexer && SKIndexNameToken.IsSKIndexKind(_values);

            if (useIndexer)
            {
                var aiServicesApiKey = AiServicesApiKeyToken.Data().Demand(_values, action, command, checkConfig: "services.key");
                var searchEndpoint = DemandSearchEndpointUri(action, command);
                var searchApiKey = DemandSearchApiKey(action, command);
                var embeddingsEndpoint = DemandEmbeddingsEndpointUri(action, command);
                var embeddingsApiKey = DemandEmbeddingsApiKey(action, command);
                var embeddingModelDeployment = SearchEmbeddingModelDeploymentNameToken.Data().Demand(_values, action, command, checkConfig: "embedding.model.deployment.name");
                var dataSourceConnectionName = SearchIndexerDataSourceConnectionNameToken.Data().GetOrDefault(_values, $"{searchIndexName}-datasource");
                var skillsetName = SearchIndexerSkillsetNameToken.Data().GetOrDefault(_values, $"{searchIndexName}-skillset");
                var indexerName = SearchIndexerNameToken.Data().GetOrDefault(_values, $"{searchIndexName}-indexer");

                var idFieldName = IndexIdFieldNameToken.Data().GetOrDefault(_values, "id");
                var contentFieldName = IndexContentFieldNameToken.Data().GetOrDefault(_values, "content");
                var vectorFieldName = IndexVectorFieldNameToken.Data().GetOrDefault(_values, "contentVector");

                output = DoIndexUpdateWithAISearch(aiServicesApiKey, searchEndpoint, searchApiKey, embeddingsEndpoint, embeddingModelDeployment, embeddingsApiKey, searchIndexName, dataSourceConnectionName, blobContainer, pattern, skillsetName, indexerName, idFieldName, contentFieldName, vectorFieldName).Result;
            }
            else if (useSK)
            {
                var searchEndpoint = DemandSearchEndpointUri(action, command);
                var searchApiKey = DemandSearchApiKey(action, command);
                var embeddingsEndpoint = DemandEmbeddingsEndpointUri(action, command);
                var embeddingsDeployment = DemandEmbeddingsDeployment(action, command);
                var embeddingsApiKey = DemandEmbeddingsApiKey(action, command);

                DoIndexUpdateWithSK(searchEndpoint, searchApiKey, embeddingsEndpoint, embeddingsDeployment, embeddingsApiKey, searchIndexName, pattern);
            }
            else // use GenAi
            {
                var subscription = SubscriptionToken.Data().Demand(_values, action, command, checkConfig: "subscription");
                var project = ProjectNameToken.Data().Demand(_values, action, command, checkConfig: "project");
                var group = ResourceGroupNameToken.Data().Demand(_values, action, command, checkConfig: "group");
                var searchEndpoint = DemandSearchEndpointUri(action, command);
                var searchApiKey = DemandSearchApiKey(action, command);
                var embeddingsEndpoint = DemandEmbeddingsEndpointUri(action, command);
                var embeddingsApiKey = DemandEmbeddingsApiKey(action, command);
                var embeddingModelDeployment = SearchEmbeddingModelDeploymentNameToken.Data().Demand(_values, action, command, checkConfig: "embedding.model.deployment.name");
                var embeddingModelName = SearchEmbeddingModelNameToken.Data().Demand(_values, action, command, checkConfig: "embedding.model.name");
                var externalSourceUrl = ExternalSourceToken.Data().GetOrDefault(_values);

                output = DoIndexUpdateWithGenAi(subscription, group, project, searchIndexName, embeddingModelDeployment, embeddingModelName, pattern, externalSourceUrl);

                var parsed = !string.IsNullOrEmpty(output) ? JsonDocument.Parse(output) : null;
                var index = parsed?.GetPropertyElementOrNull("index");
                if (index == null)
                {
                    _values.AddThrowError("ERROR:", $"Failed to update search index '{searchIndexName}'");
                }
            }

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var fi = new FileInfo(ConfigSetHelpers.ConfigSet("search.index.name", searchIndexName));
            if (!_quiet) Console.WriteLine($"{fi.Name} (saved at {fi.DirectoryName})\n\n  {searchIndexName}\n");

            if (!string.IsNullOrEmpty(output)) Console.WriteLine(output);
        }

        private string DoIndexUpdateWithGenAi(string subscription, string groupName, string projectName, string indexName, string embeddingModelDeployment, string embeddingModelName, string dataFiles, string externalSourceUrl)
        {
            // work around issue with Py GenAI SDK needing this var to be set; do not set any additional values... See Hanchi Wang for more info.
            var env = ConfigEnvironmentHelpers.GetEnvironment(_values);
            env = new Dictionary<string, string>(env.Where(x => x.Key == "AZURE_OPENAI_KEY"));
            
            return PythonSDKWrapper.UpdateMLIndex(_values, subscription, groupName, projectName, indexName, embeddingModelDeployment, embeddingModelName, dataFiles, externalSourceUrl, env);
        }

        private async Task<string> DoIndexUpdateWithAISearch(string aiServicesApiKey, string searchEndpoint, string searchApiKey, string embeddingsEndpoint, string embeddingsDeployment, string embeddingsApiKey, string searchIndexName, string dataSourceConnectionName, string blobContainer, string pattern, string skillsetName, string indexerName, string idFieldName, string contentFieldName, string vectorFieldName)
        {
            var (connectionString, containerName) = await UploadFilesToBlobContainer(blobContainer, pattern);

            Console.WriteLine("Connecting to Search ...");
            var datasourceIndex = PrepGetSearchIndex(embeddingsEndpoint, embeddingsDeployment, embeddingsApiKey, searchIndexName, idFieldName, contentFieldName, vectorFieldName);
            var dataSource = PrepGetDataSourceConnection(dataSourceConnectionName, connectionString, containerName);
            var skillset = PrepGetSkillset(skillsetName, aiServicesApiKey, embeddingsEndpoint, embeddingsDeployment, embeddingsApiKey, idFieldName, contentFieldName, vectorFieldName, datasourceIndex);
            var indexer = PrepGetIndexer(indexerName, datasourceIndex, dataSource, skillset);

            Uri endpoint = new Uri(searchEndpoint);
            AzureKeyCredential credential = new AzureKeyCredential(searchApiKey);

            SearchIndexerClient indexerClient = new SearchIndexerClient(endpoint, credential);
            indexerClient.DeleteDataSourceConnection(datasourceIndex.Name);
            indexerClient.DeleteSkillset(skillset.Name);
            indexerClient.DeleteIndexer(indexer.Name);

            SearchIndexClient indexClient = new(endpoint, credential);
            await indexClient.DeleteIndexAsync(datasourceIndex.Name);

            Console.WriteLine("Creating Search index ...");
            await indexClient.CreateIndexAsync(datasourceIndex);
            await indexerClient.CreateOrUpdateDataSourceConnectionAsync(dataSource);
            await indexerClient.CreateSkillsetAsync(skillset);
            await indexerClient.CreateIndexerAsync(indexer);

            Console.Write("Running indexer ...");
            await indexerClient.RunIndexerAsync(indexer.Name);

            var output = string.Empty;
            for (;;)
            {
                var statusResponse = await indexerClient.GetIndexerStatusAsync(indexer.Name);
                var lastResult = statusResponse.Value.LastResult;
                if (lastResult != null && lastResult.Status != IndexerExecutionStatus.InProgress)
                {
                    output = JsonSerializer.Serialize(statusResponse.Value, new JsonSerializerOptions { WriteIndented = true });
                    break;
                }

                Console.Write('.');
                Thread.Sleep(1000);
            }

            return output;
        }

        private async Task<(string connectionString, string containerName)> UploadFilesToBlobContainer(string blobUrlWithContainerName, string pattern)
        {
            int thirdSlash = blobUrlWithContainerName.IndexOf('/', blobUrlWithContainerName.IndexOf('/', blobUrlWithContainerName.IndexOf('/') + 1) + 1);
            var endpoint = blobUrlWithContainerName.Substring(0, thirdSlash);
            var containerName = blobUrlWithContainerName.Substring(thirdSlash + 1);

            Console.WriteLine();
            Console.WriteLine($"Connecting to blob container ...");

            var options = new BlobClientOptions() { Diagnostics = { IsLoggingEnabled = true, IsLoggingContentEnabled = true } };
            var serviceClient = new BlobServiceClient(new Uri(endpoint), new DefaultAzureCredential(), options);
            var connectionString = BuildConnectionString(serviceClient, endpoint, containerName);

            var containerClient = serviceClient.GetBlobContainerClient(containerName);

            if (!containerClient.Exists())
            {
                Console.WriteLine($"\nCreating blob container ...");
                containerClient = serviceClient.CreateBlobContainer(containerName);
                Console.WriteLine($"Creating blob container ... Done!\n");
            }
            Console.WriteLine($"Connecting to blob container ... Done!\n");

            Console.WriteLine("Uploading files to blob container ...");
            await UploadFilesToBlobContainer(containerClient, pattern);
            Console.WriteLine("Uploading files to blob container ... Done!\n");

            if (Program.Debug)
            {
                var blobServiceClient = new BlobServiceClient(connectionString);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
                var blobs = blobContainerClient.GetBlobsAsync();

                Console.WriteLine($"Files in container '{containerName}'");
                await foreach (var blob in blobs)
                {
                    Console.WriteLine($"  {blob.Name} ({blob.Properties.ContentLength} byte(s))");
                }
                Console.WriteLine($"Found: {blobs.CountAsync().Result} file(s)");
            }

            return (connectionString, containerName);
        }

        private async Task UploadFilesToBlobContainer(BlobContainerClient containerClient, string pattern)
        {
            var files = FileHelpers.FindFilesInDataPath(pattern, _values)
                .Where(x => File.Exists(x))
                .Select(x => new FileInfo(x).FullName)
                .Distinct();

            Console.WriteLine();

            var tasks = files.Select(x => UploadFileToBlobContainer(containerClient, x));
            await Task.WhenAll(tasks);

            Console.WriteLine();
        }

        private Task<Response<BlobContentInfo>> UploadFileToBlobContainer(BlobContainerClient containerClient, string fileName)
        {
            var blobName = Path.GetFileName(fileName);
            var blobClient = containerClient.GetBlobClient(blobName);
            var uploaded = blobClient.UploadAsync(fileName, true);

            return uploaded.ContinueWith<Response<BlobContentInfo>>(x => {
                if (x.Exception != null)
                {
                    throw x.Exception;
                }

                var response = x.Result;
                var info = response.Value;
                Console.WriteLine($"  {blobName} ({new FileInfo(fileName).Length} byte(s))");
                return response;
            });
        }

        private static string BuildConnectionString(BlobServiceClient serviceClient, string endpoint, string containerName)
        {
            var accountName = endpoint.Substring(8, endpoint.IndexOf('.') - 8);

            Console.WriteLine("Generating SAS token ...");
            Console.WriteLine();
            Console.WriteLine("  Signing method: User delegation");
            Console.WriteLine("  Expires: " + DateTime.Now.AddDays(1).ToString("yyyy-MM-dd HH:mm:ss"));

            var expiresOn = DateTimeOffset.UtcNow.AddDays(1);
            var userKey = serviceClient.GetUserDelegationKey(null, expiresOn);
            Console.WriteLine("  Key: " + userKey.Value);

            var sasBuilder = new BlobSasBuilder(BlobContainerSasPermissions.All, expiresOn) { BlobContainerName = containerName, Resource = "c", };
            var sasToken = sasBuilder.ToSasQueryParameters(userKey, accountName);

            Console.WriteLine($"  Blob SAS token: {sasToken}");

            return $"BlobEndpoint={endpoint};SharedAccessSignature={sasToken}";
        }

        private static SearchIndex PrepGetSearchIndex(string embeddingsEndpoint, string embeddingsDeployment, string embeddingsApiKey, string searchIndexName, string idFieldName, string contentFieldName, string vectorFieldName)
        {
            SearchIndex datasourceIndex = new(searchIndexName)
            {
                Fields =
                    {
                        new SearchableField("ChunkKey") { IsKey = true, AnalyzerName = LexicalAnalyzerName.Keyword },
                        new SearchableField(idFieldName) { IsFilterable = true},
                        new SearchableField(contentFieldName),
                        new SearchField(vectorFieldName, SearchFieldDataType.Collection(SearchFieldDataType.Single))
                        {
                            IsSearchable = true,
                            VectorSearchDimensions = 1536,
                            VectorSearchProfile = "vectorsearchprofile"
                        }
                    },
                VectorSearch = new()
                {
                    Profiles =
                    {
                        new VectorSearchProfile("vectorsearchprofile", "algoconfig")
                        {
                            Vectorizer = "myvectorizer"
                        }
                    },
                    Algorithms =
                    {
                        new HnswVectorSearchAlgorithmConfiguration("algoconfig")
                    },
                    Vectorizers =
                    {
                        new AzureOpenAIVectorizer("myvectorizer")
                        {
                            AzureOpenAIParameters = new AzureOpenAIParameters()
                            {
                                DeploymentId = embeddingsDeployment,
                                ResourceUri = new Uri(embeddingsEndpoint),
                                ApiKey = embeddingsApiKey
                                //AuthIdentity = new SearchIndexerDataUserAssignedIdentity("randomuser"),
                            }
                        }
                    }
                },
            };
            return datasourceIndex;
        }

        private static SearchIndexerDataSourceConnection PrepGetDataSourceConnection(string dataSourceConnectionName, string dataSourceConnectionString, string dataSourceContainerName)
        {
            return new SearchIndexerDataSourceConnection(
                    dataSourceConnectionName,
                    SearchIndexerDataSourceType.AzureBlob,
                    dataSourceConnectionString,
                    new SearchIndexerDataContainer(dataSourceContainerName));
        }

        private static SearchIndexerSkillset PrepGetSkillset(string skillsetName, string aiServicesApiKey, string embeddingsEndpoint, string embeddingsDeployment, string embeddingsApiKey, string idFieldName, string contentFieldName, string vectorFieldName, SearchIndex datasourceIndex)
        {
            var useOcr = true;

            var ocrSkill = new OcrSkill(
                new List<InputFieldMappingEntry> {
                    new InputFieldMappingEntry("image") { Source = "/document/normalized_images/*" }
                },
                new List<OutputFieldMappingEntry> {
                    new OutputFieldMappingEntry("text") { TargetName = "text"}
                }) {
                    Context = "/document/normalized_images/*",
                    ShouldDetectOrientation = true
                };

            var ocrMergeSkill = new MergeSkill(
                new List<InputFieldMappingEntry> {
                    new InputFieldMappingEntry("text") { Source = "/document/content" },
                    new InputFieldMappingEntry("itemsToInsert") { Source = "/document/normalized_images/*/text" },
                    new InputFieldMappingEntry("offsets") { Source = "/document/normalized_images/*/contentOffset" }
                },
                new List<OutputFieldMappingEntry> {
                    new OutputFieldMappingEntry("mergedText") { TargetName = "mergedText"}
                }) {
                    Context = "/document",
                    InsertPreTag = " ",
                    InsertPostTag = " "
                };

            var splitSkill = new SplitSkill(
                new List<InputFieldMappingEntry> {
                    new InputFieldMappingEntry("text") { Source = useOcr ? "/document/mergedText" : "/document/content" }
                },
                new List<OutputFieldMappingEntry> {
                    new OutputFieldMappingEntry("textItems") { TargetName = "pages"}
                }) {
                    DefaultLanguageCode = SplitSkillLanguage.En,
                    TextSplitMode = TextSplitMode.Pages,
                    MaximumPageLength = 1000,
                    PageOverlapLength = 100,
                    Context = "/document"
                };

            var azureOpenAIEmbeddingSkill = new AzureOpenAIEmbeddingSkill(
                new List<InputFieldMappingEntry> {
                    new InputFieldMappingEntry("text") { Source = "/document/pages/*" }
                },
                new List<OutputFieldMappingEntry> {
                    new OutputFieldMappingEntry("embedding") { TargetName = "vector" }
                }) {
                    Context = "/document/pages/*",
                    ResourceUri = new Uri(embeddingsEndpoint),
                    ApiKey = embeddingsApiKey,
                    DeploymentId = embeddingsDeployment,
                    //AuthIdentity = new SearchIndexerDataUserAssignedIdentity("randomuser")
                };

            var skills = useOcr
                ? new List<SearchIndexerSkill> { ocrSkill, ocrMergeSkill, splitSkill, azureOpenAIEmbeddingSkill }
                : new List<SearchIndexerSkill> { splitSkill, azureOpenAIEmbeddingSkill };

            var indexProjections = new SearchIndexerIndexProjections(
                new List<SearchIndexerIndexProjectionSelector> {
                    new SearchIndexerIndexProjectionSelector(
                        datasourceIndex.Name,
                        idFieldName,
                        "/document/pages/*",
                        new List<InputFieldMappingEntry> {
                            new InputFieldMappingEntry(contentFieldName) { Source = "/document/pages/*" },
                            new InputFieldMappingEntry(vectorFieldName) { Source = "/document/pages/*/vector" }
                })}) {
                    Parameters = new SearchIndexerIndexProjectionsParameters() { ProjectionMode = IndexProjectionMode.SkipIndexingParentDocuments }
                };

            var skillset = !string.IsNullOrEmpty(aiServicesApiKey)
                ? new SearchIndexerSkillset(skillsetName, skills) {
                    CognitiveServicesAccount = new CognitiveServicesAccountKey(aiServicesApiKey),
                    IndexProjections = indexProjections
                }
                : new SearchIndexerSkillset(skillsetName, skills) {
                    IndexProjections = indexProjections
                };

            return skillset;
        }

        private static SearchIndexer PrepGetIndexer(string indexerName, SearchIndex datasourceIndex, SearchIndexerDataSourceConnection dataSource, SearchIndexerSkillset skillset)
        {
            return new SearchIndexer(indexerName, dataSource.Name, datasourceIndex.Name)
            {
                Description = "Data indexer",
                Schedule = new IndexingSchedule(TimeSpan.FromDays(1))
                {
                    StartTime = DateTimeOffset.Now
                },
                Parameters = new IndexingParameters()
                {
                    BatchSize = 10,
                    MaxFailedItems = 0,
                    MaxFailedItemsPerBatch = 0,
                    IndexingParametersConfiguration = new IndexingParametersConfiguration()
                    {
                        DataToExtract = BlobIndexerDataToExtract.ContentAndMetadata,
                        ImageAction = BlobIndexerImageAction.GenerateNormalizedImages
                    }
                },
                SkillsetName = skillset.Name
            };
        }

        private void DoIndexUpdateWithSK(string searchEndpoint, string searchApiKey, string embeddingsEndpoint, string embeddingsDeployment, string embeddingsApiKey, string searchIndexName, string pattern)
        {
            var files = FileHelpers.FindFilesInDataPath(pattern, _values)
                .Where(x => File.Exists(x))
                .Select(x => new FileInfo(x).FullName)
                .Distinct();

            var kvps = files.Select(x => new KeyValuePair<string, string>(x, File.ReadAllText(x))).ToList();

            var kernel = CreateSemanticKernel(searchEndpoint, searchApiKey, embeddingsEndpoint, embeddingsDeployment, embeddingsApiKey);
            StoreMemoryAsync(kernel, searchIndexName, kvps).Wait();
        }

#nullable enable
        private IKernel? CreateSemanticKernel(string searchEndpoint, string searchApiKey, string embeddingsEndpoint, string embeddingsDeployment, string embeddingsApiKey)
        {
            var store = new AzureCognitiveSearchMemoryStore(searchEndpoint, searchApiKey);
            var kernelWithACS = Kernel.Builder
                .WithAzureTextEmbeddingGenerationService(embeddingsDeployment, embeddingsEndpoint, embeddingsApiKey)
                .WithMemoryStorage(store)
                .Build();

            return kernelWithACS;
        }
#nullable disable

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
                    "ERROR:", $"{action}; requires embedding endpoint uri.",
                            "",
                      "TRY:", $"{Program.Name} config search --set embedding.endpoint ENDPOINT",
                              $"{Program.Name} {command} --embedding-endpoint ENDPOINT",
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
                    "ERROR:", $"{action}; requires embedding deployment.",
                            "",
                      "TRY:", $"{Program.Name} config search --set embedding.model.deployment.name DEPLOYMENT",
                              $"{Program.Name} {command} --embedding-deployment DEPLOYMENT",
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
                    "ERROR:", $"{action}; requires embedding api key.",
                            "",
                      "TRY:", $"{Program.Name} config search --set embedding.key KEY",
                              $"{Program.Name} {command} --embedding-key KEY",
                            "",
                      "SEE:", $"{Program.Name} help {command}");
            }
            return embeddingsApiKey;
        }

        private string DemandSearchIndexUpdateFilesPattern(string action, string command)
        {
            var pattern = GetSearchIndexUpdateFiles() ?? GetSearchIndexUpdateFile();
            if (string.IsNullOrEmpty(pattern))
            {
                _values.AddThrowError(
                    "ERROR:", $"{action}; requires data files.",
                      "TRY:", $"{Program.Name} {command} --files PATTERN",
                              $"{Program.Name} {command} --file PATTERN",
                            "",
                      "SEE:", $"{Program.Name} help {command}");
            }
            return pattern;
        }

        private string GetSearchApiKey()
        {
            return _values.Get("service.config.key", true);
        }

        private string GetSearchEndpointUri()
        {
            return ConfigEndpointUriToken.Data().GetOrDefault(_values);
        }

        private string GetEmbeddingsEndpointUri()
        {
            return _values.Get("search.embedding.endpoint.uri", true);
        }

        private string GetEmbeddingsApiKey()
        {
            return _values.Get("search.embedding.api.key", true);
        }

        private string GetEmbeddingsDeployment()
        {
            return _values.Get("search.embedding.model.deployment.name", true);
        }

        private string GetSearchIndexUpdateFile()
        {
            return _values.Get("search.index.update.file", true);
        }

        private string GetSearchIndexUpdateFiles()
        {
            return _values.Get("search.index.update.files", true);
        }

        private void StartCommand()
        {
            CheckPath();
            LogHelpers.EnsureStartLogFile(_values);

            // _display = new DisplayHelper(_values);

            // _output = new OutputHelper(_values);
            // _output!.StartOutput();

            _lock = new SpinLock();
            _lock.StartLock();
        }

        private void StopCommand()
        {
            _lock!.StopLock(5000);

            // LogHelpers.EnsureStopLogFile(_values);
            // _output!.CheckOutput();
            // _output!.StopOutput();

            _stopEvent.Set();
        }
        private SpinLock? _lock = null;
        private readonly bool _quiet = false;
        private readonly bool _verbose = false;
    }
}