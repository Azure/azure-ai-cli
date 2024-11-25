using System;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Chat;
{{if {_IS_OPENAI_ASST_FILE_SEARCH_TEMPLATE}}}
using OpenAI.Files;
{{endif}}

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public class {ClassName}
{
    public AssistantThread? Thread;

    public {ClassName}(OpenAIClient client, string assistantId{{if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}, FunctionFactory factory{{endif}})
    {
        {{if {_IS_OPENAI_ASST_FILE_SEARCH_TEMPLATE}}}
        _fileClient = client.GetFileClient();
        {{endif}}
        _assistantClient = client.GetAssistantClient();
        _assistantId = assistantId;
        {{if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}
        _functionFactory = factory;

        _runOptions = new RunCreationOptions();
        foreach (var tool in _functionFactory.GetToolDefinitions())
        {
            _runOptions.ToolsOverride.Add(tool);
        }
        {{endif}}
    }

    public async Task CreateThreadAsync()
    {
        var result = await _assistantClient.CreateThreadAsync();
        Thread = result.Value;
    }

    public async Task RetrieveThreadAsync(string threadId)
    {
        var result = await _assistantClient.GetThreadAsync(threadId);
        Thread = result.Value;
    }

    public async Task GetThreadMessagesAsync(Action<string, string> callback)
    {
        var options = new MessageCollectionOptions() { Order = ListOrder.OldestFirst };
        await foreach (var message in _assistantClient.GetMessagesAsync(Thread, options).GetAllValuesAsync())
        {
            var content = string.Join("", message.Content.Select(c => c.Text));
            var role = message.Role == MessageRole.User ? "user" : "assistant";
            callback(role, content);
        }
    }

    {{if !{_IS_OPENAI_ASST_STREAMING_TEMPLATE}}}
    public async Task<string> GetResponseAsync(string userInput)
    {
        await _assistantClient.CreateMessageAsync(Thread, MessageRole.User, [ userInput ]);
        var assistant = await _assistantClient.GetAssistantAsync(_assistantId);

        var result = await _assistantClient.CreateRunAsync(Thread, assistant);
        var run = result.Value;

        while (!run.Status.IsTerminal)
        {
            System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(100));
            result = _assistantClient.GetRun(run.ThreadId, run.Id);
            run = result.Value;
        }

        var options = new MessageCollectionOptions() { Order = ListOrder.OldestFirst };
        await foreach (var message in _assistantClient.GetMessagesAsync(run.ThreadId, options).GetAllValuesAsync())
        {
            if (message.Role == MessageRole.Assistant)
            {
                var content = string.Join("", message.Content.Select(c => c.Text));
                return content;
            }
        }

        return string.Empty;
    }
    {{else}}
    public async Task GetResponseAsync(string userInput, Action<string> callback)
    {
        await _assistantClient.CreateMessageAsync(Thread, MessageRole.User, [ userInput ]);
        var assistant = await _assistantClient.GetAssistantAsync(_assistantId);
        var stream = _assistantClient.CreateRunStreamingAsync(Thread, assistant.Value{{if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}, _runOptions{{endif}});

        {{if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}
        ThreadRun? run = null;
        List<ToolOutput> toolOutputs = [];
        do
        {
            await foreach (var update in stream)
            {
                if (update is MessageContentUpdate contentUpdate)
                {
                    callback(contentUpdate.Text);
                }
                else if (update is RequiredActionUpdate requiredActionUpdate)
                {
                    if (_functionFactory.TryCallFunction(requiredActionUpdate.FunctionName, requiredActionUpdate.FunctionArguments, out var result))
                    {
                        callback($"\rassistant-function: {requiredActionUpdate.FunctionName}({requiredActionUpdate.FunctionArguments}) => {result}\n");
                        callback("\nAssistant: ");
                        toolOutputs.Add(new ToolOutput(requiredActionUpdate.ToolCallId, result));
                    }
                }

                if (update is RunUpdate runUpdate)
                {
                    run = runUpdate;
                }

                if (run?.Status.IsTerminal == true)
                {
                    callback("\n\n");
                }
            }

            if (toolOutputs.Count > 0 && run != null)
            {
                stream = _assistantClient.SubmitToolOutputsToRunStreamingAsync(run, toolOutputs);
                toolOutputs.Clear();
            }
        }
        while (run?.Status.IsTerminal == false);
        {{else}}
        {{if {_IS_OPENAI_ASST_FILE_SEARCH_TEMPLATE}}}
        var cachedContent = string.Empty;
        {{endif}}
        await foreach (var update in stream) 
        {
            if (update is MessageContentUpdate contentUpdate)
            {
                {{if {_IS_OPENAI_ASST_FILE_SEARCH_TEMPLATE}}}
                var content = contentUpdate.Text;
                var hasContent = !string.IsNullOrEmpty(content);

                var replace = contentUpdate.TextAnnotation?.TextToReplace;
                var hasAnnotation = !string.IsNullOrEmpty(replace);
                
                var hasLenticularBrackets = hasContent && content.Contains("\u3010") && content.Contains("\u3011");
                var shouldCache = hasLenticularBrackets && !hasAnnotation;
                if (shouldCache)
                {
                    cachedContent = cachedContent + content;
                    continue;
                }

                var hasCache = !string.IsNullOrEmpty(cachedContent);
                if (hasCache)
                {
                    content = cachedContent + content;
                    cachedContent = string.Empty;
                }

                if (hasAnnotation)
                {
                    var fileId = contentUpdate.TextAnnotation!.InputFileId;
                    var file = await _fileClient.GetFileAsync(fileId);
                    var fileName = file.Value.Filename ?? fileId;

                    var citation = $"[{contentUpdate.TextAnnotation!.ContentIndex}](file:{fileName})";
                    var hasReplacement = !string.IsNullOrEmpty(content) && content.Contains(replace!);
                    content = hasReplacement
                        ? content.Replace(replace!, citation)
                        : $"{citation} ";
                }

                callback(content);
                {{else}}
                callback(contentUpdate.Text);
                {{endif}}
            }
            {{if {_IS_OPENAI_ASST_CODE_INTERPRETER_TEMPLATE}}}
            else if (update is RunStepDetailsUpdate runStepDetailsUpdate)
            {
                var input = runStepDetailsUpdate.CodeInterpreterInput;
                if (input != null)
                {
                    callback(input);
                }
            }
            {{endif}}

            if (update.UpdateKind == StreamingUpdateReason.RunStepCompleted)
            {
                callback("\n\n");
            }
        }
        {{endif}}
    }
    {{endif}}

    private readonly string _assistantId;
    {{if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}
    private FunctionFactory _functionFactory;
    private RunCreationOptions _runOptions;
    {{endif}}
    private readonly AssistantClient _assistantClient;
    {{if {_IS_OPENAI_ASST_FILE_SEARCH_TEMPLATE}}}
    private readonly FileClient _fileClient;
    {{endif}}
}
