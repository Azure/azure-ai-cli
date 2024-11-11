using Azure.AI.OpenAI;
using OpenAI.RealtimeConversation;
using System.ClientModel;
using System.Text;

#pragma warning disable OPENAI002

public class OpenAIRealtimeConversationSessionHelperClass
{
    [HelperFunctionDescription("Stops listening, but switching to 'keyword activation' mode; If the user asks to 'Go to sleep', you should call this function.")]
    public static string StopListening()
    {
        Task.Run(() => Current?.StopListeningInternalAkaGoToSleep());
        return "Tell the user that they can wake you back up using your name (but don't tell them your name, as it might wake you up)";
    }

    public OpenAIRealtimeConversationSessionHelperClass(string apiKey, string endpoint, string model, string instructions, FunctionFactory factory, AudioSourceController audioSourceController, AudioSourceControlStream audioSourceControlStream, SpeakerAudioOutputStream speaker)
    {
        _client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
        _conversationClient = _client.GetRealtimeConversationClient(model);

        _functionFactory = factory;
        factory.AddFunctions(typeof(OpenAIRealtimeConversationSessionHelperClass));

        _audioSourceController = audioSourceController;
        _audioSourceControlStream = audioSourceControlStream;

        _speaker = speaker;
        _speaker.PlaybackFinished += Speaker_PlaybackFinished;

        _sessionOptions = new ConversationSessionOptions() 
        {
            Instructions = instructions,
            InputTranscriptionOptions = new()
            {
                Model = "whisper-1",
            },
            Voice = "dan"
            // 'amuch' - sounds like a dj
            // , 'dan', 'elan', 'marilyn', 'meadow', 'breeze', 'cove', 'ember', 'jupiter', 'alloy', 'echo', and 'shimmer'
        };

        foreach (var tool in _functionFactory.GetTools())
        {
            _sessionOptions.Tools.Add(tool);
        }

        Current = this;
    }

    public async Task StartSessionAsync()
    {
        if (Program.Debug) Console.WriteLine("Starting session...");

        _session = await _conversationClient.StartConversationSessionAsync();
        await _session.ConfigureSessionAsync(_sessionOptions);

        _audioSourceController.TransitionToOpenMic();
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

                case ConversationResponseStartedUpdate responseStartedUpdate:
                    HandleResponseStarted(callback, responseStartedUpdate);
                    break;

                case ConversationOutputTranscriptionDeltaUpdate outputTranscriptionDeltaUpdate:
                    HandleOutputTranscriptionDelta(callback, outputTranscriptionDeltaUpdate);
                    break;

                case ConversationOutputTranscriptionFinishedUpdate:
                    HandleOutputTranscriptionFinished(callback);
                    break;

                case ConversationItemFinishedUpdate itemFinishedUpdate:
                    await HandleItemFinished(itemFinishedUpdate);
                    break;

                case ConversationResponseFinishedUpdate responseFinishedUpdate:
                    await HandleResponseFinishedUpdate(callback, responseFinishedUpdate);
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
            // callback("assistant", "Listening...\n");
            await _session.SendAudioAsync(_audioSourceControlStream);
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
        if (_audioSourceController.State == AudioSourceState.OpenMic)
        {
            _audioSourceController.Mute();
        }
        else if (_audioSourceController.State == AudioSourceState.KeywordArmed)
        {
            _audioSourceController.TransitionToKeywordArmed();
        }

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

    private async Task HandleItemFinished(ConversationItemFinishedUpdate itemFinishedUpdate)
    {
        if (_functionFactory.TryCallFunction(itemFinishedUpdate.FunctionName, itemFinishedUpdate.FunctionCallArguments, out var result))
        {
            await _session.AddItemAsync(ConversationItem.CreateFunctionCallOutput(
                callId: itemFinishedUpdate.FunctionCallId,
                output: result));
        }
    }

    private void HandleResponseStarted(Action<string, string> callback, ConversationResponseStartedUpdate responseStartedUpdate)
    {
        _responseStartedTime = DateTime.UtcNow;
    }

    private async Task HandleResponseFinishedUpdate(Action<string, string> callback, ConversationResponseFinishedUpdate responseFinishedUpdate)
    {
        _responseFinishedTime = DateTime.UtcNow;

        if (Program.Debug) Console.WriteLine($"  -- Model turn generation finished. Status: {responseFinishedUpdate.Status}");

        if (responseFinishedUpdate.CreatedItems.Any(item => item.FunctionName?.Length > 0))
        {
            if (Program.Debug) Console.WriteLine($"  -- Ending client turn for pending tool responses");
            await _session.StartResponseTurnAsync();
        }
        else
        {
            callback("user", "");
        }
    }

    private void Speaker_PlaybackFinished(object? sender, EventArgs e)
    {
        var playbackFinishedTime = DateTime.UtcNow;
        if (playbackFinishedTime >= _responseFinishedTime && _responseFinishedTime >= _responseStartedTime)
        {
            var unMute = !_ignoreNextFinishedAudioCue && _audioSourceController.IsMuted;
            if (unMute)
            {
                _audioSourceController.UnMute();
            }

            if (_audioSourceController.State == AudioSourceState.KeywordArmed)
            {
                _audioSourceController.TransitionToKeywordArmed();
            }

            _ignoreNextFinishedAudioCue = false;
        }
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

    private void StopListeningInternalAkaGoToSleep()
    {
        _ignoreNextFinishedAudioCue = true;
        _audioSourceController.TransitionToKeywordArmed();
    }

    private static OpenAIRealtimeConversationSessionHelperClass? Current;

    private readonly AzureOpenAIClient _client;
    private RealtimeConversationClient _conversationClient;

    private RealtimeConversationSession? _session;
    private ConversationSessionOptions _sessionOptions;

    private FunctionFactory _functionFactory;

    private AudioSourceController _audioSourceController;
    private AudioSourceControlStream _audioSourceControlStream;
    private SpeakerAudioOutputStream _speaker;

    private DateTime _responseStartedTime;
    private DateTime _responseFinishedTime;

    private StringBuilder? _bufferOutputTranscriptionDeltas;
    private bool _ignoreNextFinishedAudioCue;
}