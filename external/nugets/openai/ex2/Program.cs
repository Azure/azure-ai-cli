using System;
using System.ClientModel;
using System.IO;
using System.Linq;
using Azure.AI.OpenAI;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Files;
using OpenAI.VectorStores;

#pragma warning disable OPENAI001

class Program
{
    public async static Task Main(string[] args)
    {
        var instructions = "You are search assistant. You search documents that have been previously uploaded.";

        // Get the required environment variables for Azure OpenAI Assistants API
        var ASSISTANT_ID = Environment.GetEnvironmentVariable("ASSISTANT_ID");
        var VECTOR_STORE_ID = Environment.GetEnvironmentVariable("VECTOR_STORE_ID");
        var VECTOR_STORE_NAME = Environment.GetEnvironmentVariable("VECTOR_STORE_NAME");
        var AZURE_OPENAI_API_KEY = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        var AZURE_OPENAI_API_VERSION = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_VERSION");
        var AZURE_OPENAI_ENDPOINT = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var AZURE_OPENAI_CHAT_DEPLOYMENT = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT");

        // Get the required environment variables for OpenAI Assistants API
        var OPENAI_API_KEY = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var OPENAI_MODEL_NAME = Environment.GetEnvironmentVariable("OPENAI_MODEL_NAME");

        // Check if the required environment variables are set
        var azureOk = !string.IsNullOrEmpty(AZURE_OPENAI_API_KEY) && !AZURE_OPENAI_API_KEY.StartsWith("<insert") &&
            !string.IsNullOrEmpty(AZURE_OPENAI_API_VERSION) && !AZURE_OPENAI_API_VERSION.StartsWith("<insert") &&
            !string.IsNullOrEmpty(AZURE_OPENAI_CHAT_DEPLOYMENT) && !AZURE_OPENAI_CHAT_DEPLOYMENT.StartsWith("<insert") &&
            !string.IsNullOrEmpty(AZURE_OPENAI_ENDPOINT) && !AZURE_OPENAI_ENDPOINT.StartsWith("<insert");
        var openAIOk = !string.IsNullOrEmpty(OPENAI_API_KEY) && !OPENAI_API_KEY.StartsWith("<insert") &&
            !string.IsNullOrEmpty(OPENAI_MODEL_NAME) && !OPENAI_MODEL_NAME.StartsWith("<insert");

        var ok = azureOk || openAIOk;
        if (!ok)
        {
            Console.WriteLine("To use OpenAI, set the following environment variables:\n" +
                "\n  ASSISTANT_ID" +
                "\n  OPENAI_API_KEY" +
                "\n  OPENAI_ORG_ID" +
                "\n  OPENAI_MODEL_NAME");
            Console.WriteLine();
            Console.WriteLine("To use Azure OpenAI, set the following environment variables:\n" +
                "\n  ASSISTANT_ID" +
                "\n  AZURE_OPENAI_API_KEY" +
                "\n  AZURE_OPENAI_API_VERSION" +
                "\n  AZURE_OPENAI_CHAT_DEPLOYMENT" +
                "\n  AZURE_OPENAI_ENDPOINT");
            Console.WriteLine();
            Console.WriteLine("\nYou can easily do that using the Azure AI CLI by doing one of the following:\n" +
              "\n  ai init" +
              "\n  ai dev shell" +
              "\n  dotnet run" +
              "\n" +
              "\n  or" +
              "\n" +
              "\n  ai init" +
              "\n  ai dev shell --run \"dotnet run\"");
            Environment.Exit(1);
        }

        // Get the client objects required
        var client = azureOk
            ? CreateAzureOpenAIClient(AZURE_OPENAI_API_KEY!, AZURE_OPENAI_ENDPOINT!)
            : CreateOpenAIClient(OPENAI_API_KEY!);
        var assistantClient = client.GetAssistantClient();
        var vectorStoreClient = client.GetVectorStoreClient();
        var fileClient = client.GetFileClient();

        // Create or get the Assistant
        var createAssistant = string.IsNullOrEmpty(ASSISTANT_ID);
        var assistant = createAssistant
            ? CreateAssistant(assistantClient, azureOk ? AZURE_OPENAI_CHAT_DEPLOYMENT! : OPENAI_MODEL_NAME!, instructions)
            : GetAssistant(assistantClient, ASSISTANT_ID!);
        PrintAssistant(assistant);

        // Create or get the Vector Store
        var vectorStoreId = VECTOR_STORE_ID ?? assistant.ToolResources.FileSearch.VectorStoreIds.FirstOrDefault();
        var createVectorStore = createAssistant || string.IsNullOrEmpty(vectorStoreId);
        var vectorStore = createVectorStore
            ? CreateVectorStore(vectorStoreClient, VECTOR_STORE_NAME ?? "(unnamed)")
            : GetVectorStore(vectorStoreClient, vectorStoreId!);
        PrintVectorStore(client, vectorStore, vectorStoreClient);

        // Upload files to the Vector Store
        var files = FindFiles(args);
        if (files.Any())
        {
            var uploadedFiles = UploadFiles(fileClient, files);
            ProcessBatchFileJob(vectorStoreClient, vectorStore, uploadedFiles);
            PrintVectorStore(client, vectorStore, vectorStoreClient);
        }

        // Update the assistant to use the vector store
        if (createVectorStore)
        {
            assistant = UpdateAssistant(assistantClient, assistant, vectorStore);
            PrintAssistant(assistant);
        }

        var thread = assistantClient.CreateThread();
        while (true)
        {
            Console.Write("User: ");
            var input = Console.ReadLine();
            if (string.IsNullOrEmpty(input) || input == "exit") break;

            Console.Write("\nAssistant: ");
            await GetResponse(assistantClient, fileClient, assistant, thread, input, (content) => {
                Console.Write(content);
            });
            Console.WriteLine("\n");
        }
    }

    private static AzureOpenAIClient CreateAzureOpenAIClient(string key, string endpoint)
    {
        Console.WriteLine("Using Azure OpenAI (w/ API Key)...");
        return new AzureOpenAIClient(new Uri(endpoint), new System.ClientModel.ApiKeyCredential(key));
    }

    private static OpenAIClient CreateOpenAIClient(string key)
    {
        Console.WriteLine("Using OpenAI (w/ API Key)...");
        return new OpenAIClient(new ApiKeyCredential(key));
    }

    private static Assistant CreateAssistant(AssistantClient assistantClient, string model, string instructions)
    {
        Console.Write("Creating assistant ...");
        var assistant = assistantClient.CreateAssistant(model, new AssistantCreationOptions()
        {
            Tools = { new FileSearchToolDefinition() },
            Instructions = instructions
        });

        Console.WriteLine("Done!");
        return assistant;
    }

    private static Assistant GetAssistant(AssistantClient assistantClient, string id)
    {
        Console.Write("Getting assistant ...");
        var assistant = assistantClient.GetAssistant(id).Value;

        Console.WriteLine("Getting assistant ... Done!");
        return assistant;
    }

    private static Assistant UpdateAssistant(AssistantClient assistantClient, Assistant assistant, VectorStore vectorStore)
    {
        Console.Write("Updating assistant ...");

        var updateOptions = new AssistantModificationOptions()
        {
            ToolResources = new()
            {
                FileSearch = new()
                {
                    VectorStoreIds = { vectorStore.Id }
                },
            }
        };
        updateOptions.DefaultTools.Add(new FileSearchToolDefinition());
        assistant = assistantClient.ModifyAssistant(assistant, updateOptions).Value;

        Console.WriteLine(" Done!");
        return assistant;
    }

    private static void PrintAssistant(Assistant assistant)
    {
        Console.WriteLine("Updated Assistant:");
        Console.WriteLine($"  ID: {assistant.Id}");
        Console.WriteLine($"  Name: {assistant.Name}");
        Console.WriteLine($"  Model: {assistant.Model}");
        Console.WriteLine($"  Instructions: {assistant.Instructions}");

        var toolNames = string.Join(", ", assistant.Tools.Select(x => x.GetType().Name));
        Console.WriteLine($"  Tools: {toolNames}");

        var vectorStoreIds = string.Join(", ", assistant.ToolResources.FileSearch.VectorStoreIds);
        Console.WriteLine($"  Vector stores: {vectorStoreIds}");

        Console.WriteLine();
    }

    private static VectorStore CreateVectorStore(VectorStoreClient vectorStoreClient, string? name)
    {
        Console.Write("Creating vector store ...");
        var vectorStore = vectorStoreClient.CreateVectorStore(new VectorStoreCreationOptions() { Name = name });

        Console.WriteLine("Done!");
        return vectorStore;
    }

    private static VectorStore GetVectorStore(VectorStoreClient vectorStoreClient, string id)
    {
        Console.Write("Getting vector store ...");
        var vectorStore = vectorStoreClient.GetVectorStore(id);

        Console.WriteLine(" Done!");
        return vectorStore;
    }

    private static void PrintVectorStore(OpenAIClient client, VectorStore vectorStore, VectorStoreClient vectorStoreClient)
    {
        Console.WriteLine("Vector Store:");
        Console.WriteLine($"  ID: {vectorStore.Id}");
        Console.WriteLine($"  Name: {vectorStore.Name}");

        var fileClient = client.GetFileClient();

        Console.WriteLine("  Files:");
        var associations = vectorStoreClient.GetFileAssociations(vectorStore);
        foreach (var association in associations)
        {
            var file = fileClient.GetFile(association.FileId);
            Console.WriteLine($"    {file.Value.Filename} ({file.Value.SizeInBytes} byte(s))");
        }
        Console.WriteLine();
    }

    private static List<string> FindFiles(string[] patterns)
    {
        Console.WriteLine("Files:");
        var files = new List<string>();
        for (int i = 0; i < patterns.Length; i++)
        {
            var fi = new FileInfo(patterns[i]);
            var path = Path.GetDirectoryName(fi.FullName) ?? ".";
            var pattern = Path.GetFileName(fi.FullName);
            var found = Directory.GetFiles(path, pattern);
            if (found != null)
            {
                foreach (var file in found)
                {
                    files.Add(file);
                    Console.WriteLine($"  {file}");
                }
            }
        }

        if (files.Count() == 0)
        {
            Console.WriteLine("No files found.");
        }

        Console.WriteLine();

        return files;
    }

    private static IEnumerable<OpenAIFileInfo> UploadFiles(FileClient fileClient, IEnumerable<string> files)
    {
        Console.Write("Uploading files ...");

        List<Task<ClientResult<OpenAIFileInfo>>> uploadTasks = [];
        foreach (var file in files)
        {
            var stream = new FileStream(file, FileMode.Open);
            var uploaded = fileClient.UploadFileAsync(stream, file, OpenAIFilePurpose.Assistants);
            uploadTasks.Add(uploaded);
        }

        Task.WaitAll(uploadTasks.ToArray());
        Console.WriteLine(" Done!");

        foreach (var task in uploadTasks)
        {
            var file = task.Result.Value;
            Console.WriteLine($"  {file.Filename} ({file.SizeInBytes} byte(s))");
        }
        Console.WriteLine();

        return uploadTasks.Select(x => x.Result.Value);
    }

    private static VectorStoreBatchFileJob ProcessBatchFileJob(VectorStoreClient vectorStoreClient, VectorStore vectorStore, IEnumerable<OpenAIFileInfo> uploadedFiles)
    {
        Console.Write("Processing vector store files ...");

        var batchJob = vectorStoreClient.CreateBatchFileJob(vectorStore, uploadedFiles);
        var completed = false;
        while (!completed)
        {
            Console.Write('.');
            Thread.Sleep(250);

            batchJob = vectorStoreClient.GetBatchFileJob(batchJob);
            completed = batchJob.Value.Status == VectorStoreBatchFileJobStatus.Completed;
        }
        Console.WriteLine("\n");

        Console.WriteLine($"Batch job: {batchJob.Value.BatchId}");
        Console.WriteLine($"File Completed/Total: {batchJob.Value.FileCounts.Completed} out of {batchJob.Value.FileCounts.Total}");
        Console.WriteLine();

        return batchJob;
    }

    private static async Task GetResponse(AssistantClient assistantClient, FileClient fileClient, Assistant assistant, AssistantThread thread, string? input, Action<string> callback)
    {
        var message = assistantClient.CreateMessage(thread, [input]);
        var updates = assistantClient.CreateRunStreaming(thread, assistant).Value;
        await foreach (var update in updates)
        {
            if (update is MessageContentUpdate contentUpdate)
            {
                HandleMessageContentUpdate(contentUpdate, callback);
            }
            else if (update is MessageStatusUpdate statusUpdate)
            {
                if (update.UpdateKind == StreamingUpdateReason.MessageCompleted)
                {
                    HandleMessageCompletedStatusUpdate(statusUpdate, fileClient, callback);
                }
            }
        }
    }

    private static void HandleMessageContentUpdate(MessageContentUpdate contentUpdate, Action<string> callback)
    {
        var content = contentUpdate.Text;
        if (contentUpdate.TextAnnotation != null)
        {
            var annotation = contentUpdate.TextAnnotation;
            if (!string.IsNullOrEmpty(content))
            {
                content = content.Replace(annotation.TextToReplace, $"[{annotation.ContentIndex}]");
            }
        }
        callback(content);
    }

    private static void HandleMessageCompletedStatusUpdate(MessageStatusUpdate statusUpdate, FileClient fileClient, Action<string> callback)
    {
        var index = 0;
        var citations = new List<string>();
        foreach (var content in statusUpdate.Value.Content)
        {
            foreach (var annotation in content.TextAnnotations)
            {
                var fileId = annotation.InputFileId;
                if (!string.IsNullOrEmpty(fileId))
                {
                    var file = fileClient.GetFile(fileId);
                    citations.Add($"[{index++}] {file.Value.Filename}");
                }
            }
        }

        if (citations.Any())
        {
            callback("\n\n" + string.Join("\n", citations.ToArray()));
        }
    }
}
