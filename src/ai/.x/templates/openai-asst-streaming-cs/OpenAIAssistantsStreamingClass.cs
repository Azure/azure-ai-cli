using System;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Chat;

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public class OpenAIAssistantsStreamingClass
{
    public AssistantThread? Thread;

    public OpenAIAssistantsStreamingClass(OpenAIClient client, string assistantId)
    {
        _client = client.GetAssistantClient();
        _assistantId = assistantId;
    }

    public async Task CreateThreadAsync()
    {
        var result = await _client.CreateThreadAsync();
        Thread = result.Value;
    }

    public async Task RetrieveThreadAsync(string threadId)
    {
        var result = await _client.GetThreadAsync(threadId);
        Thread = result.Value;
    }

    public async Task GetThreadMessagesAsync(Action<string, string> callback)
    {
        var messages = new List<ThreadMessage>();
        await foreach (var message in _client.GetMessagesAsync(Thread))
        {
            messages.Add(message);
        }

        messages.Reverse();
        foreach (var message in messages)
        {
            var content = string.Join("", message.Content.Select(c => c.Text));
            var role = message.Role == MessageRole.User ? "user" : "assistant";
            callback(role, content);
        }
    }

    public async Task GetResponseAsync(string userInput, Action<string> callback)
    {
        await _client.CreateMessageAsync(Thread, [ userInput ]);
        var assistant = await _client.GetAssistantAsync(_assistantId);
        var stream = _client.CreateRunStreamingAsync(Thread, assistant.Value);

        await foreach (var update in stream) 
        {
            if (update is MessageContentUpdate contentUpdate)
            {
                callback(contentUpdate.Text);
            }
        }
    }

    private readonly string _assistantId;
    private readonly AssistantClient _client;
}