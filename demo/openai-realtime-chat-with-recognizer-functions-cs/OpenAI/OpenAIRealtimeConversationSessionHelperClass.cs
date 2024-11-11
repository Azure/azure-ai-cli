using Azure.AI.OpenAI;
using OpenAI.RealtimeConversation;
using System.ClientModel;
using System.Text;

#pragma warning disable OPENAI002

public class OpenAIRealtimeConversationSessionHelperClass
{
    public OpenAIRealtimeConversationSessionHelperClass(string apiKey, string endpoint, string model, string instructions, FunctionFactory factory, MicrophoneAudioInputStream microphone, SpeakerAudioOutputStream speaker)
    {
        _client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
        _conversationClient = _client.GetRealtimeConversationClient(model);

        _functionFactory = factory;
        _microphone = microphone;
        _speaker = speaker;

        _sessionOptions = new ConversationSessionOptions() 
        {
            Instructions = instructions, 
            TurnDetectionOptions = ConversationTurnDetectionOptions.CreateDisabledTurnDetectionOptions(),
        };

        foreach (var tool in _functionFactory.GetTools())
        {
            _sessionOptions.Tools.Add(tool);
        }
    }

    public async Task StartSessionAsync()
    {
        if (Program.Debug) Console.WriteLine("Starting session...");

        _session = await _conversationClient.StartConversationSessionAsync();
        await _session.ConfigureSessionAsync(_sessionOptions);
    }

    public async Task<string> GetSessionUpdateAsync(string text, Action<string> callback)
    {
        if (_session == null) throw new InvalidOperationException("Session has not been started.");

        var parts = new List<ConversationContentPart>() { ConversationContentPart.FromInputText(text) };
        await _session.AddItemAsync(ConversationItem.CreateUserMessage(parts));

        await _session.StartResponseAsync();
        await GetSessionUpdatesAsync((role, content) => callback(content));

        return string.Empty;
    }

    public async Task GetSessionUpdatesAsync(Action<string, string> callback)
    {
        if (_session == null) throw new InvalidOperationException("Session has not been started.");

        await foreach (var update in _session.ReceiveUpdatesAsync())
        {
            if (Program.Debug)
            {
                var raw = update.GetRawContent().ToString();
                raw = raw.Substring(0, Math.Min(1000, raw.Length)) + "...";
                Console.WriteLine($"\n**{update.GetType().Name}: {raw}");
            }

            switch (update)
            {
                case ConversationSessionStartedUpdate:
                    HandleSessionStarted(callback);
                    break;

                case ConversationItemStreamingPartDeltaUpdate deltaUpdate:
                    Console.Write(deltaUpdate.AudioTranscript);
                    Console.Write(deltaUpdate.Text);
                    if (deltaUpdate.AudioBytes != null) _speaker.EnqueueForPlayback(deltaUpdate.AudioBytes);
                    break;

                case ConversationItemStreamingFinishedUpdate itemFinishedUpdate:
                    await HandleItemFinished(callback, itemFinishedUpdate);
                    break;

                case ConversationResponseFinishedUpdate turnFinishedUpdate:
                    if (await HandleTurnFinished(turnFinishedUpdate)) return;
                    break;

                case ConversationErrorUpdate errorUpdate:
                    Console.WriteLine($"ERROR: {errorUpdate.Message}");
                    return;
            }
        }
    }

    private void HandleSessionStarted(Action<string, string> callback)
    {
        if (Program.Debug) Console.WriteLine("Connected: session started");
    }

    private async Task HandleItemFinished(Action<string, string> callback, ConversationItemStreamingFinishedUpdate itemFinishedUpdate)
    {
        if (_functionFactory.TryCallFunction(itemFinishedUpdate.FunctionName, itemFinishedUpdate.FunctionCallArguments, out var result))
        {
            callback?.Invoke("assistant-function", $"{itemFinishedUpdate}({itemFinishedUpdate.FunctionCallArguments}) => {result ?? string.Empty}");
            await _session.AddItemAsync(ConversationItem.CreateFunctionCallOutput(
                callId: itemFinishedUpdate.FunctionCallId,
                output: result));
        }
    }

    private async Task<bool> HandleTurnFinished(ConversationResponseFinishedUpdate turnFinishedUpdate)
    {
        if (Program.Debug) Console.WriteLine($"  -- Model turn generation finished. Status: {turnFinishedUpdate.Status}");

        if (turnFinishedUpdate.CreatedItems.Any(item => item.FunctionName?.Length > 0))
        {
            if (Program.Debug) Console.WriteLine($"  -- Ending client turn for pending tool responses");
            await _session.StartResponseAsync();
            return false;
        }

        return true;
    }

    private readonly AzureOpenAIClient _client;
    private RealtimeConversationClient _conversationClient;

    private RealtimeConversationSession? _session;
    private ConversationSessionOptions _sessionOptions;

    private FunctionFactory _functionFactory;
    private MicrophoneAudioInputStream _microphone;
    private SpeakerAudioOutputStream _speaker;
}