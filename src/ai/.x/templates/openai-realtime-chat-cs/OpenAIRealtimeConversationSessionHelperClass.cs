using Azure.AI.OpenAI;
using OpenAI.RealtimeConversation;
using System.ClientModel;
using System.Text;

#pragma warning disable OPENAI002

public class {ClassName}
{
    public {ClassName}(string apiKey, string endpoint, string model, string instructions, MicrophoneAudioInputStream microphone, SpeakerAudioOutputStream speaker)
    {
        _client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
        _conversationClient = _client.GetRealtimeConversationClient(model);

        _microphone = microphone;
        _speaker = speaker;

        _sessionOptions = new ConversationSessionOptions() 
        {
            Instructions = instructions,
            InputTranscriptionOptions = new()
            {
                Model = "whisper-1",
            }
        };
    }

    public async Task StartSessionAsync()
    {
        if (Program.Debug) Console.WriteLine("Starting session...");

        _session = await _conversationClient.StartConversationSessionAsync();
        await _session.ConfigureSessionAsync(_sessionOptions);
    }

    public async Task GetSessionUpdatesAsync(Action<string, string> callback)
    {
        if (_session == null) throw new InvalidOperationException("Session has not been started.");

        await foreach (var update in _session.ReceiveUpdatesAsync())
        {
            if (Program.Debug) Console.WriteLine($"Received update: {update.GetType().Name}");

            switch (update)
            {
                case ConversationSessionStartedUpdate:
                    HandleSessionStarted(callback);
                    break;

                case ConversationAudioDeltaUpdate audioDeltaUpdate:
                    HandleAudioDelta(audioDeltaUpdate);
                    break;

                case ConversationInputSpeechStartedUpdate:
                    HandleInputSpeechStarted(callback);
                    break;

                case ConversationInputSpeechFinishedUpdate:
                    HandleInputSpeechFinished();
                    break;

                case ConversationInputTranscriptionFinishedUpdate transcriptionFinishedUpdate:
                    HandleInputTranscriptionFinished(callback, transcriptionFinishedUpdate);
                    break;

                case ConversationOutputTranscriptionDeltaUpdate outputTranscriptionDeltaUpdate:
                    HandleOutputTranscriptionDelta(callback, outputTranscriptionDeltaUpdate);
                    break;

                case ConversationOutputTranscriptionFinishedUpdate:
                    HandleOutputTranscriptionFinished(callback);
                    break;

                case ConversationErrorUpdate errorUpdate:
                    Console.WriteLine($"ERROR: {errorUpdate.ErrorMessage}");
                    return;
            }
        }
    }

    private void HandleSessionStarted(Action<string, string> callback)
    {
        if (Program.Debug) Console.WriteLine("Connected: session started");
        _ = Task.Run(async () =>
        {
            callback("assistant", "Listening...\n");
            await _session.SendAudioAsync(_microphone);
            callback("user", "");
        });
    }

    private void HandleAudioDelta(ConversationAudioDeltaUpdate audioUpdate)
    {
        _speaker.EnqueueForPlayback(audioUpdate.Delta);
    }

    private void HandleInputSpeechStarted(Action<string, string> callback)
    {
        if (Program.Debug) Console.WriteLine("Start of speech detected");
        _speaker.ClearPlayback();
        callback("user", "");
    }

    private void HandleInputSpeechFinished()
    {
        if (Program.Debug) Console.WriteLine("End of speech detected");
        StartBufferingOutputTranscriptionDeltas();
    }

    private void HandleInputTranscriptionFinished(Action<string, string> callback, ConversationInputTranscriptionFinishedUpdate transcriptionUpdate)
    {
        callback?.Invoke("user", $"{transcriptionUpdate.Transcript}");
        StopBufferingOutputTranscriptionDeltas(callback);
    }

    private void HandleOutputTranscriptionDelta(Action<string, string> callback, ConversationOutputTranscriptionDeltaUpdate transcriptionUpdate)
    {
        if (IsBufferingOutputTranscriptionDeltas())
        {
            BufferOutputTranscriptionDelta(transcriptionUpdate.Delta);
        }
        else
        {
            callback?.Invoke("assistant", transcriptionUpdate.Delta);
        }
    }

    private void HandleOutputTranscriptionFinished(Action<string, string> callback)
    {
        callback?.Invoke("assistant", "\n");
    }

    private bool IsBufferingOutputTranscriptionDeltas()
    {
        return _bufferOutputTranscriptionDeltas != null;
    }

    private void StartBufferingOutputTranscriptionDeltas()
    {
        _bufferOutputTranscriptionDeltas = new StringBuilder();
    }

    private void BufferOutputTranscriptionDelta(string delta)
    {
        _bufferOutputTranscriptionDeltas?.Append(delta);
    }

    private void StopBufferingOutputTranscriptionDeltas(Action<string, string> callback)
    {
        if (_bufferOutputTranscriptionDeltas != null)
        {
            callback?.Invoke("assistant", _bufferOutputTranscriptionDeltas.ToString());
            _bufferOutputTranscriptionDeltas = null;
        }
    }

    private readonly AzureOpenAIClient _client;
    private RealtimeConversationClient _conversationClient;

    private RealtimeConversationSession? _session;
    private ConversationSessionOptions _sessionOptions;

    private MicrophoneAudioInputStream _microphone;
    private SpeakerAudioOutputStream _speaker;

    private StringBuilder? _bufferOutputTranscriptionDeltas;
}
