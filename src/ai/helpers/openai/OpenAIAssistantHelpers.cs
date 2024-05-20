//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Azure.AI.OpenAI.Assistants;
using Azure.Core.Diagnostics;
using System;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ClientModel.Primitives;

namespace Azure.AI.Details.Common.CLI
{
    public class OpenAIAssistantHelpers
    {
        public static async Task<string> CreateAssistant(string key, string endpoint, string name, string deployment, string instructions, bool codeInterpreter, List<string> fileIds)
        {
            var createOptions = new AssistantCreationOptions(deployment) { Name = name, Instructions = instructions };

            if (codeInterpreter)
            {
                createOptions.Tools.Add(new CodeInterpreterToolDefinition());
            }

            if (fileIds.Count() > 0)
            {
                createOptions.Tools.Add(new RetrievalToolDefinition());
                fileIds.ForEach(id => createOptions.FileIds.Add(id));
            }

            var client = CreateOpenAIAssistantsClient(key, endpoint);
            var response = await client.CreateAssistantAsync(createOptions);

            return response.Value.Id;
        }

        public static async Task DeleteAssistant(string key, string endpoint, string id)
        {
            var client = CreateOpenAIAssistantsClient(key, endpoint);
            var response = await client.DeleteAssistantAsync(id);
        }

        public static async Task<string?> GetAssistantJson(string key, string endpoint, string id)
        {
            var client = CreateOpenAIAssistantsClient(key, endpoint);
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
            var order = ListSortOrder.Ascending;
            var client = CreateOpenAIAssistantsClient(key, endpoint);
            var response = await client.GetAssistantsAsync(order: order);

            var pageable = response.Value;
            var list = pageable.ToList();

            while (pageable.HasMore)
            {
                response = await client.GetAssistantsAsync(after: pageable.LastId, order: order);
                pageable = response.Value;
                list.AddRange(pageable);
            }

            return list.ToDictionary(a => a.Id, a => a.Name);
        }

        public static async Task<(string, string)> UploadAssistantFile(string key, string endpoint, string fileName)
        {
            var client = CreateOpenAIAssistantsClient(key, endpoint);
            var response = await client.UploadFileAsync(fileName, OpenAIFilePurpose.Assistants);
            return (response.Value.Id, response.Value.Filename);
        }

        public static async Task<Dictionary<string, string>> ListAssistantFiles(string key, string endpoint)
        {
            var client = CreateOpenAIAssistantsClient(key, endpoint);
            var response = await client.GetFilesAsync(OpenAIFilePurpose.Assistants);
            var files = response.Value.ToList();
            return files.ToDictionary(f => f.Id, f => f.Filename);
        }

        public static async Task DeleteAssistantFile(string key, string endpoint, string id)
        {
            var client = CreateOpenAIAssistantsClient(key, endpoint);
            var response = await client.DeleteFileAsync(id);
        }

        private static AssistantsClient CreateOpenAIAssistantsClient(string key, string endpoint)
        {
            _azureEventSourceListener = new AzureEventSourceListener((e, message) => EventSourceHelpers.EventSourceAiLoggerLog(e, message), System.Diagnostics.Tracing.EventLevel.Verbose);

            var options = new AssistantsClientOptions();
            options.Diagnostics.IsLoggingContentEnabled = true;
            options.Diagnostics.IsLoggingEnabled = true;

            return new AssistantsClient(new Uri(endpoint!), new AzureKeyCredential(key), options);
        }

        private static AzureEventSourceListener _azureEventSourceListener;
    }
}
