// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Text;
using System.Text.Json;
using Azure.AI.Chat.SampleService.Services;
using Azure.AI.OpenAI;
using Azure.Core.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Tracing;

namespace Azure.AI.Chat.SampleService;

internal class ChatResponseBaseClass
{
    protected readonly OpenAIClient _client;
    private readonly string _deployment;
    private readonly ChatCompletionOptions _options;
    private readonly AzureEventSourceListener _listener;

    internal ChatResponseBaseClass(OpenAIClient client, string deployment, ChatCompletionOptions options)
    {
        _client = client;
        _deployment = deployment;
        _options = options;
        _listener = AzureEventSourceListener.CreateConsoleLogger(EventLevel.Verbose);
    }

    internal ChatCompletionsOptions GetChatCompletionsOptions()
    {
        var messages = new List<ChatRequestMessage>();

        foreach (ChatMessage chatMessage in _options.Messages)
        {
            switch (chatMessage.Role)
            {
                // See https://learn.microsoft.com/dotnet/api/azure.ai.openai.chatrequestmessage?view=azure-dotnet-preview
                case "system":
                    messages.Add(new ChatRequestSystemMessage(chatMessage.Content));
                    break;
                case "user":
                    messages.Add(new ChatRequestUserMessage(chatMessage.Content));
                    break;
                case "assistant":
                    messages.Add(new ChatRequestAssistantMessage(chatMessage.Content));
                    break;
                case "tool": // TODO 
                case "function": // TODO (but deprecated)
                default:
                    // This will result in "HTTP/1.1 500 Internal Server Error" response to the client
                    throw new Exception("Invalid role value");
            }
        }

        return new ChatCompletionsOptions(_deployment, messages);
    }
}

internal class ChatResponse: ChatResponseBaseClass, IActionResult
{
    internal ChatResponse(OpenAIClient client, string deployment, ChatCompletionOptions options) 
        : base(client, deployment, options)
    {
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        // Throws Azure.RequestFailedException if the response is not successful (HTTP status code is not in the 200s)
        Response<ChatCompletions> chatCompletionsResponse =
            await _client.GetChatCompletionsAsync(GetChatCompletionsOptions());

        ChatCompletion completion = new()
        {
            Choices = chatCompletionsResponse.Value.Choices.Select(choice => new ChatChoice
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

        HttpResponse httpResponse = context.HttpContext.Response;
        httpResponse.StatusCode = (int)HttpStatusCode.OK;
        httpResponse.ContentType = "application/json";
        await httpResponse.WriteAsync(JsonSerializer.Serialize(completion), Encoding.UTF8);
    }
}

internal class StreamingChatResponse: ChatResponseBaseClass, IActionResult
{
    internal StreamingChatResponse(OpenAIClient client, string deployment, ChatCompletionOptions options)
        : base(client, deployment, options)
    {
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        // Throws Azure.RequestFailedException if the response is not successful (HTTP status code is not in the 200s)
        StreamingResponse<StreamingChatCompletionsUpdate> streamingChatCompletionsUpdateResponse =
            await _client.GetChatCompletionsStreamingAsync(GetChatCompletionsOptions());

        HttpResponse httpResponse = context.HttpContext.Response;
        httpResponse.StatusCode = (int)HttpStatusCode.OK;
        httpResponse.ContentType = "text/event-stream";

        await foreach (StreamingChatCompletionsUpdate chatUpdate in streamingChatCompletionsUpdateResponse)
        {
            if (!chatUpdate.ChoiceIndex.HasValue)
            {
                continue;
            }

            int choiceIndex = chatUpdate.ChoiceIndex.Value;

            ChoiceDelta delta = new()
            {
                Index = choiceIndex,
                Delta = new ChatMessageDelta
                {
                    Content = chatUpdate.ContentUpdate,
                    Role = chatUpdate.Role?.ToString()
                },
                FinishReason = chatUpdate.FinishReason?.ToString()
            };

            ChatCompletionChunk completion = new()
            {
                Choices = new List<ChoiceDelta> { delta }
            };

            await httpResponse.WriteAsync($"{JsonSerializer.Serialize(completion)}\n", Encoding.UTF8);
        }
        await httpResponse.WriteAsync("[DONE]\n");
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
