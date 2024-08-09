//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ClientModel.Primitives;
using OpenAI.Assistants;
using OpenAI;
using Azure.AI.OpenAI;
using OpenAI.Files;
using OpenAI.VectorStores;
using System.Threading;
using Scriban.Runtime;

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace Azure.AI.Details.Common.CLI
{
    public class OpenAIAssistantHelpers
    {
        public static async Task<Assistant> CreateAssistantAsync(string key, string endpoint, string name, string deployment, string instructions, bool codeInterpreter)
        {
            var client = CreateOpenAIAssistantClient(key, endpoint);
            return await CreateAssistantAsync(client, key, endpoint, name, deployment, instructions, codeInterpreter);
        }

        private static async Task<Assistant> CreateAssistantAsync(AssistantClient client, string key, string endpoint, string name, string deployment, string instructions, bool codeInterpreter)
        {
            var createOptions = new AssistantCreationOptions()
            {
                Name = name,
                Instructions = instructions,
            };

            if (codeInterpreter)
            {
                createOptions.Tools.Add(new CodeInterpreterToolDefinition());
            }

            return await client.CreateAssistantAsync(deployment, createOptions);
        }

        public static async Task DeleteAssistant(string key, string endpoint, string id)
        {
            var client = CreateOpenAIAssistantClient(key, endpoint);
            await DeleteAssistantAsync(client, id);
        }

        public static async Task DeleteAssistantAsync(AssistantClient client, string id)
        {
            var response = await client.DeleteAssistantAsync(id);
        }

        public static async Task<Assistant> GetAssistantAsync(string key, string endpoint, string id)
        {
            var client = CreateOpenAIAssistantClient(key, endpoint);
            return await GetAssistantAsync(client, id);
        }

        public static async Task<Assistant> GetAssistantAsync(AssistantClient client, string id)
        {
            var response = await client.GetAssistantAsync(id);
            return response.Value;
        }

        public static async Task<string?> GetAssistantJsonAsync(string key, string endpoint, string id)
        {
            var client = CreateOpenAIAssistantClient(key, endpoint);
            return await GetAssistantJsonAsync(client, id);
        }

        public static async Task<string> GetAssistantJsonAsync(AssistantClient client, string id)
        {
            var response = await client.GetAssistantAsync(id);
            var assistant = response.Value;

            var jsonModel = assistant as IJsonModel<Assistant>;
            if (jsonModel != null)
            {
                using var stream = new MemoryStream();
                var writer = new Utf8JsonWriter(stream);
                jsonModel.Write(writer, ModelReaderWriterOptions.Json);
                writer.Flush();

                return Encoding.UTF8.GetString(stream.ToArray());
            }

            return null;
        }

        public static async Task<Dictionary<string, string>> ListAssistantsAsync(string key, string endpoint)
        {
            var client = CreateOpenAIAssistantClient(key, endpoint);
            return await ListAssistantsAsync(client);
        }

        public static async Task<Dictionary<string, string>> ListAssistantsAsync(AssistantClient client)
        {
            var order = ListOrder.OldestFirst;
            var assistants = client.GetAssistantsAsync(order);

            var list = new List<Assistant>();
            await foreach (Assistant assistant in assistants)
            {
                list.Add(assistant);
            }

            return list.ToDictionary(a => a.Id, a => a.Name);
        }

        public static async Task<VectorStore> GetVectorStoreAsync(string key, string endpoint, string id)
        {
            var client = CreateOpenAIVectorStoreClient(key, endpoint);
            return await client.GetVectorStoreAsync(id);
        }

        public static async Task<VectorStore> CreateAssistantVectorStoreAsync(string key, string endpoint, string name, List<string> fileIds)
        {
            var client = CreateOpenAIVectorStoreClient(key, endpoint);
            return await CreateAssistantVectorStoreAsync(client, name, fileIds);
        }

        private static async Task<VectorStore> CreateAssistantVectorStoreAsync(VectorStoreClient client, string name, List<string> fileIds)
        {
            var result = await client.CreateVectorStoreAsync(new VectorStoreCreationOptions()
            {
                Name = name,
                FileIds = fileIds
            });
            return result.Value;
        }

        public static async Task<VectorStore> UpdateAssistantVectorStoreAsync(string key, string endpoint, string id, string name)
        {
            var client = CreateOpenAIVectorStoreClient(key, endpoint);
            return await UpdateAssistantVectorStoreAsync(client, id, name);
        }

        private static async Task<VectorStore> UpdateAssistantVectorStoreAsync(VectorStoreClient client, string id, string name)
        {
            var store = await client.GetVectorStoreAsync(id);
            var result = await client.ModifyVectorStoreAsync(store, new VectorStoreModificationOptions()
            {
                Name = name
            });
            return result.Value;
        }

        public static async Task DeleteAssistantVectorStoreAsync(string key, string endpoint, string id)
        {
            var client = CreateOpenAIVectorStoreClient(key, endpoint);
            await client.DeleteVectorStoreAsync(id);
        }

        public static async Task<Dictionary<string, string>> ListAssistantVectorStoresAsync(string key, string endpoint)
        {
            var client = CreateOpenAIVectorStoreClient(key, endpoint);
            return await ListAssistantVectorStoresAsync(client);
        }

        private static async Task<Dictionary<string, string>> ListAssistantVectorStoresAsync(VectorStoreClient client)
        {
            var vectorStores = client.GetVectorStoresAsync();

            var list = new List<VectorStore>();
            await foreach (VectorStore vectorStore in vectorStores)
            {
                list.Add(vectorStore);
            }

            return list.ToDictionary(vs => vs.Id, vs => vs.Name);
        }

        public static async Task<VectorStore> GetAssistantVectorStoreAsync(string key, string endpoint, string id)
        {
            var client = CreateOpenAIVectorStoreClient(key, endpoint);
            return await GetAssistantVectorStoreAsync(client, id);
        }

        public static async Task<VectorStore> GetAssistantVectorStoreAsync(VectorStoreClient client, string id)
        {
            var response = await client.GetVectorStoreAsync(id);
            return response.Value;
        }

        public static async Task<string> GetAssistantVectorStoreJsonAsync(string key, string endpoint, string id)
        {
            var vectorStore = await GetAssistantVectorStoreAsync(key, endpoint, id);
            return GetAssistantVectorStoreJson(vectorStore);
        }

        public static string GetAssistantVectorStoreJson(VectorStore vectorStore)
        {
            var jsonModel = vectorStore as IJsonModel<VectorStore>;
            if (jsonModel != null)
            {
                using var stream = new MemoryStream();
                var writer = new Utf8JsonWriter(stream);
                jsonModel.Write(writer, ModelReaderWriterOptions.Json);
                writer.Flush();

                return Encoding.UTF8.GetString(stream.ToArray());
            }

            return string.Empty;
        }

        public static async Task<(string, string)> UploadAssistantFile(string key, string endpoint, string fileName)
        {
            var client = CreateOpenAIFileClient(key, endpoint);
            var response = await client.UploadFileAsync(fileName, FileUploadPurpose.Assistants);
            return (response.Value.Id, response.Value.Filename);
        }

        public static async Task<Dictionary<string, string>> ListAssistantFiles(string key, string endpoint)
        {
            var client = CreateOpenAIFileClient(key, endpoint);
            var response = await client.GetFilesAsync(OpenAIFilePurpose.Assistants);
            var files = response.Value.ToList();
            return files.ToDictionary(f => f.Id, f => f.Filename);
        }

        public static async Task DeleteAssistantFile(string key, string endpoint, string id)
        {
            var client = CreateOpenAIFileClient(key, endpoint);
            var response = await client.DeleteFileAsync(id);
        }

        public static async Task<IEnumerable<OpenAIFileInfo>> GetFilesAsync(FileClient fileClient, IEnumerable<string> fileIds, int parallelism = 10, Action<OpenAIFileInfo> callback = null)
        {
            var list = fileIds.ToList();

            var throttler = new SemaphoreSlim(parallelism);
            var tasks = new List<Task>();

            var foundFiles = new List<OpenAIFileInfo>();
            foreach (var file in list)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var foundFile = await fileClient.GetFileAsync(file);
                    foundFiles.Add(foundFile);

                    if (callback != null)
                    {
                        callback(foundFile);
                    }
                }));
            }

            await Task.WhenAll(tasks.ToArray());

            return foundFiles;
        }

        public static async Task<IEnumerable<OpenAIFileInfo>> UploadFilesAsync(FileClient fileClient, IEnumerable<string> files, int parallelism = 10, Action<OpenAIFileInfo> callback = null)
        {
            var list = files.ToList();

            var throttler = new SemaphoreSlim(parallelism);
            var tasks = new List<Task>();

            var uploadedFiles = new List<OpenAIFileInfo>();
            foreach (var file in list)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var uploadedFile = await UploadFileAsync(fileClient, throttler, file);
                    uploadedFiles.Add(uploadedFile);

                    if (callback != null)
                    {
                        callback(uploadedFile);
                    }
                }));
            }

            await Task.WhenAll(tasks.ToArray());

            return uploadedFiles;
        }

        public static async Task<OpenAIFileInfo> UploadFileAsync(FileClient fileClient, SemaphoreSlim throttler, string file)
        {
            await throttler.WaitAsync();
            try
            {
                var stream = new FileStream(file, FileMode.Open);
                var uploaded = await fileClient.UploadFileAsync(stream, file, FileUploadPurpose.Assistants);
                return uploaded.Value;
            }
            finally
            {
                throttler.Release();
            }
        }

        public static OpenAIClient CreateOpenAIClient(string key, string endpoint)
        {
            AzureOpenAIClientOptions options = new();
            options.AddPolicy(new LogTrafficEventPolicy(), PipelinePosition.PerCall);

            return new AzureOpenAIClient(
                new Uri(endpoint!),
                new AzureKeyCredential(key!),
                options);
        }

        public static AssistantClient CreateOpenAIAssistantClient(string key, string endpoint)
        {
            var client = CreateOpenAIClient(key, endpoint);
            return client.GetAssistantClient();
        }

        public static FileClient CreateOpenAIFileClient(string key, string endpoint)
        {
            var client = CreateOpenAIClient(key, endpoint);
            return client.GetFileClient();
        }

        public static VectorStoreClient CreateOpenAIVectorStoreClient(string key, string endpoint)
        {
            var client = CreateOpenAIClient(key, endpoint);
            return client.GetVectorStoreClient();
        }

        public static async Task<VectorStoreBatchFileJob> ProcessBatchFileJob(string key, string endpoint, VectorStore store, IEnumerable<OpenAIFileInfo> uploaded)
        {
            var client = CreateOpenAIVectorStoreClient(key, endpoint);

            var batchJob = await client.CreateBatchFileJobAsync(store, uploaded);
            var completed = false;
            while (!completed)
            {
                Console.Write('.');
                Thread.Sleep(250);

                batchJob = await client.GetBatchFileJobAsync(batchJob);
                var error = batchJob.Value.Status == VectorStoreBatchFileJobStatus.Failed;
                if (error)
                {
                    var message = $"Batch job failed: {batchJob.Value.Status}";
                    var jsonModel = batchJob.Value as IJsonModel<Assistant>;
                    if (jsonModel != null)
                    {
                        using var stream = new MemoryStream();
                        var writer = new Utf8JsonWriter(stream);
                        jsonModel.Write(writer, ModelReaderWriterOptions.Json);
                        writer.Flush();

                        message += Encoding.UTF8.GetString(stream.ToArray());
                    }

                    throw new Exception(message);
                }

                completed = batchJob.Value.Status == VectorStoreBatchFileJobStatus.Completed;
            }

            return batchJob.Value;
        }

        //private static AzureEventSourceListener _azureEventSourceListener;
    }
}
