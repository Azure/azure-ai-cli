// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Text;
using System.Text.Json;
using Azure.AI.Chat.SampleService.Services;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Mvc;

namespace Azure.AI.Chat.SampleService;

internal class ChatResponse: IActionResult
{
    private readonly OpenAIClient _client;
    private readonly string _deployment;
    private readonly ChatCompletionOptions _options;

    public ChatResponse(OpenAIClient client, string deployment, ChatCompletionOptions options)
    {
        _client = client;
        _deployment = deployment;
        _options = options;
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        ChatCompletions completions = await _client.GetChatCompletionsAsync(_deployment, new ChatCompletionsOptions(
            messages: _options.Messages.Select(msg => new Azure.AI.OpenAI.ChatMessage(msg.Role, msg.Content)).ToList()));

        ChatCompletion completion = new()
        {
            Choices = completions.Choices.Select(choice => new ChatChoice
            {
                Index = choice.Index,
                Message = new ChatMessage
                {
                    Content = choice.Message.Content,
                    Role = choice.Message.Role.ToString()
                },
                FinishReason = choice.FinishReason?.ToString() ?? ""
            }).ToList()
        };
        var response = context.HttpContext.Response;
        response.StatusCode = (int)HttpStatusCode.OK;
        response.ContentType = "application/json";
        await response.WriteAsync(JsonSerializer.Serialize(completion), Encoding.UTF8);
    }
}

internal class StreamingChatResponse: IActionResult
{
    private readonly OpenAIClient _client;
    private readonly string _deployment;
    private readonly ChatCompletionOptions _options;

    public StreamingChatResponse(OpenAIClient client, string deployment, ChatCompletionOptions options)
    {
        _client = client;
        _deployment = deployment;
        _options = options;
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        StreamingChatCompletions completions = await _client.GetChatCompletionsStreamingAsync(_deployment, new ChatCompletionsOptions(
            messages: _options.Messages.Select(msg => new OpenAI.ChatMessage(msg.Role, msg.Content)).ToList()));

        var response = context.HttpContext.Response;
        response.StatusCode = (int)HttpStatusCode.OK;
        response.ContentType = "text/event-stream";
        await foreach (StreamingChatChoice chunk in completions.GetChoicesStreaming())
        {
            await foreach (var message in chunk.GetMessageStreaming())
            {
                if (!chunk.Index.HasValue)
                {
                    continue;
                }
                ChoiceDelta delta = new()
                {
                    Index = chunk.Index.Value,
                    Delta = new ChatMessageDelta
                    {
                        Content = message.Content,
                        Role = message.Role.ToString()
                    },
                    FinishReason = chunk.FinishReason?.ToString()
                };
                ChatCompletionChunk completion = new()
                {
                    Choices = new List<ChoiceDelta> { delta }
                };
                await response.WriteAsync($"data: {JsonSerializer.Serialize(completion)}\n\n", Encoding.UTF8);
            }
        }
        await response.WriteAsync("data: [DONE]");
    }
}

[ApiController]
[Route("chat")]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;
    private readonly IOpenAIClientProvider _clientProvider;

    public ChatController(ILogger<ChatController> logger, IOpenAIClientProvider clientProvider)
    {
        _logger = logger;
        _clientProvider = clientProvider;
    }

    [HttpPost]
    public IActionResult Create(ChatCompletionOptions options)
    {
        var client = _clientProvider.GetClient();
        var deployment = _clientProvider.GetDeployment();
        if (options.Stream)
        {
            return new StreamingChatResponse(client, deployment, options);
        }
        return new ChatResponse(client, deployment, options);
    }

}
