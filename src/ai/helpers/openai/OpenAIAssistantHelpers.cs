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

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace Azure.AI.Details.Common.CLI
{
    public class OpenAIAssistantHelpers
    {
        public static async Task<string> CreateAssistant(string key, string endpoint, string name, string deployment, string instructions, bool codeInterpreter, List<string> fileIds)
        {
            var hasFiles = fileIds.Count() > 0;
            var createOptions = new AssistantCreationOptions()
            {
                Name = name,
                Instructions = instructions,
                ToolResources = !hasFiles ? new() : new()
                {
                    FileSearch = new()
                    {
                        NewVectorStores =
                        {
                            new(fileIds)
                        }
                    }
                }
            };

            if (codeInterpreter)
            {
                createOptions.Tools.Add(new CodeInterpreterToolDefinition());
            }

            if (hasFiles)
            {
                createOptions.Tools.Add(new FileSearchToolDefinition());
            }

            var client = CreateOpenAIAssistantClient(key, endpoint);
            var response = await client.CreateAssistantAsync(deployment, createOptions);

            return response.Value.Id;
        }

        public static async Task DeleteAssistant(string key, string endpoint, string id)
        {
            var client = CreateOpenAIAssistantClient(key, endpoint);
            var response = await client.DeleteAssistantAsync(id);
        }

        public static async Task<string?> GetAssistantJson(string key, string endpoint, string id)
        {
            var client = CreateOpenAIAssistantClient(key, endpoint);
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

        public static async Task<Dictionary<string, string>> ListAssistants(string key, string endpoint)
        {
            var order = ListOrder.OldestFirst;
            var client = CreateOpenAIAssistantClient(key, endpoint);
            var assistants = client.GetAssistantsAsync(order);

            var list = new List<Assistant>();
            await foreach (Assistant assistant in assistants)
            {
                list.Add(assistant);
            }

            return list.ToDictionary(a => a.Id, a => a.Name);
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

        private static OpenAIClient CreateOpenAIClient(string key, string endpoint)
        {
            //_azureEventSourceListener = new AzureEventSourceListener((e, message) => EventSourceHelpers.EventSourceAiLoggerLog(e, message), System.Diagnostics.Tracing.EventLevel.Verbose);

            var options = new AzureOpenAIClientOptions();
            return new AzureOpenAIClient(
                new Uri(endpoint!),
                new AzureKeyCredential(key!),
                options);
        }

        private static AssistantClient CreateOpenAIAssistantClient(string key, string endpoint)
        {
            var client = CreateOpenAIClient(key, endpoint);
            return client.GetAssistantClient();
        }

        private static FileClient CreateOpenAIFileClient(string key, string endpoint)
        {
            var client = CreateOpenAIClient(key, endpoint);
            return client.GetFileClient();
        }

        //private static AzureEventSourceListener _azureEventSourceListener;
    }
}
