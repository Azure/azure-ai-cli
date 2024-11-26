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
        Current?.StopListeningInternalAkaGoToSleep();
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
            // Voice = "dan"
            Voice = "cove"
            // Current choices: 'amuch', 'dan', 'elan', 'marilyn', 'meadow', 'breeze', 'cove', 'ember', 'jupiter', 'alloy', 'echo', and 'shimmer'
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

                case ConversationItemStreamingPartDeltaUpdate itemStreamingPartDeltaUpdate:
                    HandleItemStreamingPartDeltaUpdate(callback, itemStreamingPartDeltaUpdate);
                    break;

                case ConversationItemStreamingPartFinishedUpdate itemStreamingPartFinishedUpdate:
                    HandleItemStreamingPartFinishedUpdate(callback, itemStreamingPartFinishedUpdate);
                    break;

                case ConversationItemStreamingFinishedUpdate itemStreamingFinishedUpdate:
                    await HandleItemStreamingFinishedUpdateAsync(callback, itemStreamingFinishedUpdate);
                    break;

                case ConversationResponseFinishedUpdate responseFinishedUpdate:
                    await HandleResponseFinishedUpdate(callback, responseFinishedUpdate);
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
        _ = Task.Run(async () =>
        {
            await _session.SendInputAudioAsync(_audioSourceControlStream);
            callback("user", "");
        });
    }

    private void HandleInputSpeechStarted(Action<string, string> callback)
    {
        if (Program.Debug) Console.WriteLine("Start of speech detected");
        _speaker.ClearPlayback();
    }

    private void HandleInputSpeechFinished()
    {
        if (Program.Debug) Console.WriteLine("End of speech detected");
        StartBufferingAssistantTextOutputs();
    }

    private void HandleInputTranscriptionFinished(Action<string, string> callback, ConversationInputTranscriptionFinishedUpdate transcriptionUpdate)
    {
        callback.Invoke("user", $"{transcriptionUpdate.Transcript}");
        StopBufferingAssistantTextOutputs(callback);

        if (_audioSourceController.State == AudioSourceState.OpenMic)
        {
            _audioSourceController.Mute();
        }
        else if (_audioSourceController.State == AudioSourceState.KeywordArmed)
        {
            _audioSourceController.TransitionToKeywordArmed(playSound: false);
        }
    }

    private void HandleResponseStarted(Action<string, string> callback, ConversationResponseStartedUpdate responseStartedUpdate)
    {
        _responseStartedTime = DateTime.UtcNow;
    }

    private void HandleItemStreamingPartDeltaUpdate(Action<string, string> callback, ConversationItemStreamingPartDeltaUpdate update)
    {
        if (update.AudioBytes != null)
        {
            _speaker.EnqueueForPlayback(update.AudioBytes);
        }

        if (IsBufferingAssistantTextOutputs())
        {
            if (!string.IsNullOrEmpty(update.AudioTranscript)) BufferAssistantTextOutput(update.AudioTranscript);
            if (!string.IsNullOrEmpty(update.Text)) BufferAssistantTextOutput(update.Text);
        }
        else
        {
            callback?.Invoke("assistant", update.AudioTranscript);
            callback?.Invoke("assistant", update.Text);
        }

    }

    private void HandleItemStreamingPartFinishedUpdate(Action<string, string> callback, ConversationItemStreamingPartFinishedUpdate update)
    {
        var textIsQuestion = update.Text != null && update.Text.Trim(new [] { '\r', '\n', ' ' }).EndsWith('?');
        var transcriptIsQuestion = update.AudioTranscript != null && update.AudioTranscript.Trim(new [] { '\r', '\n', ' ' }).EndsWith('?');

        _openMicWhenPlaybackIsFinished = textIsQuestion || transcriptIsQuestion;
    }

    private async Task HandleItemStreamingFinishedUpdateAsync(Action<string, string> callback, ConversationItemStreamingFinishedUpdate update)
    {
        if (_functionFactory.TryCallFunction(update.FunctionName, update.FunctionCallArguments, out var result))
        {
            var abbreviated = update.FunctionName.Contains("Listening");
            var fnCall = !abbreviated
                ? $"{update.FunctionName}({string.Join(", ", update.FunctionCallArguments)})"
                : $"{update.FunctionName}()";
            var output = !abbreviated
                ? $"[{fnCall} => {result}]"
                : $"[{fnCall}]";
            HandleAssistantFunctionCallOutput(callback, output);

            await _session.AddItemAsync(ConversationItem.CreateFunctionCallOutput(
                callId: update.FunctionCallId,
                output: result));
        }
    }


    private async Task HandleResponseFinishedUpdate(Action<string, string> callback, ConversationResponseFinishedUpdate responseFinishedUpdate)
    {
        _responseFinishedTime = DateTime.UtcNow;

        if (Program.Debug) Console.WriteLine($"  -- Model turn generation finished. Status: {responseFinishedUpdate.Status}");

        if (responseFinishedUpdate.CreatedItems.Any(item => item.FunctionName?.Length > 0))
        {
            if (Program.Debug) Console.WriteLine($"  -- Ending client turn for pending tool responses");
            await _session.StartResponseAsync();
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
            if (!_ignoreNextFinishedAudioCue && _audioSourceController.IsMuted)
            {
                _audioSourceController.UnMute();
            }

            if (_openMicWhenPlaybackIsFinished)
            {
                _audioSourceController.TransitionToOpenMic();
            }
            else if (_audioSourceController.State == AudioSourceState.KeywordArmed)
            {
                _audioSourceController.TransitionToKeywordArmed();
            }

            _ignoreNextFinishedAudioCue = false;
        }
    }

    private bool IsBufferingAssistantTextOutputs()
    {
        return _bufferAssistantTextOutputs != null;
    }

    private void StartBufferingAssistantTextOutputs()
    {
        _bufferAssistantTextOutputs = new StringBuilder();
        _bufferAssistantFunctionCallOutputs = new List<string>();
    }

    private void BufferAssistantTextOutput(string delta)
    {
        _bufferAssistantTextOutputs?.Append(delta);
    }

    private void HandleAssistantFunctionCallOutput(Action<string, string> callback, string output)
    {
        if (IsBufferingAssistantTextOutputs())
        {
            BufferAssistantFunctionOutputs(output);
        }
        else
        {
            callback?.Invoke("assistant", "");

            var previous = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;

            callback?.Invoke("assistant", output);
            Console.ForegroundColor = previous;
        }
    }

    private void BufferAssistantFunctionOutputs(string output)
    {
        _bufferAssistantFunctionCallOutputs?.Add(output);
    }

    private void StopBufferingAssistantTextOutputs(Action<string, string> callback)
    {
        if (_bufferAssistantFunctionCallOutputs != null)
        {
            if (_bufferAssistantFunctionCallOutputs.Count() > 0)
            {
                callback?.Invoke("assistant", "");

                var previous = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkGray;

                foreach (var output in _bufferAssistantFunctionCallOutputs)
                {
                    callback?.Invoke("assistant", output);
                }

                Console.ForegroundColor = previous;
            }
            _bufferAssistantFunctionCallOutputs = null;
        }

        if (_bufferAssistantTextOutputs != null)
        {
            callback?.Invoke("assistant", _bufferAssistantTextOutputs.ToString());
            _bufferAssistantTextOutputs = null;
        }
    }

    // private void StartListeningInternalAkaWakeUp()
    // {
    //     _audioSourceController.TransitionToOpenMic();
    // }

    private void StopListeningInternalAkaGoToSleep()
    {
        _ignoreNextFinishedAudioCue = true;
        _audioSourceController.TransitionToKeywordArmed(playSound: false);
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

    private StringBuilder? _bufferAssistantTextOutputs;
    private List<string>? _bufferAssistantFunctionCallOutputs;
    private bool _ignoreNextFinishedAudioCue;
    private bool _openMicWhenPlaybackIsFinished;
}