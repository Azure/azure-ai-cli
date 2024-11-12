using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Threading.Tasks;

public class KeywordGatedAudio16khzSource
{
    public KeywordGatedAudio16khzSource(string keywordModelFile = "keyword.table")
    {
        _audioConfig = AudioConfig.FromDefaultMicrophoneInput(
            AudioProcessingOptions.Create(
                AudioProcessingConstants.AUDIO_INPUT_PROCESSING_ENABLE_DEFAULT | AudioProcessingConstants.AUDIO_INPUT_PROCESSING_DISABLE_ECHO_CANCELLATION,
                PresetMicrophoneArrayGeometry.Linear2,
                SpeakerReferenceChannel.LastChannel));
        _model = KeywordRecognitionModel.FromFile(keywordModelFile);
    }

    public event EventHandler? KeywordRecognized;
    public event EventHandler<AudioDataAvailableEventArgs>? AudioDataAvailable;

    public bool Start()
    {
        return StartAsync().Result;
    }

    public void Stop()
    {
        StopAsync().Wait();
    }

    public async Task<bool> StartAsync()
    {
        await StopAsync();

        lock (_syncLock)
        {
            if (_isRunning) throw new InvalidOperationException("The source is already running.");
            _isRunning = true;

            _requestStop = new TaskCompletionSource<bool>();
            _startedKeywordSpotting = new TaskCompletionSource<bool>();
            _stoppedKeywordSpotting = new TaskCompletionSource<bool>();

            _ = Task.Run(async () =>
            {
                try
                {
                    await StartKeywordSpottingAudioProcessing();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    lock (_syncLock)
                    {
                        _isRunning = false;
                        _startedKeywordSpotting.TrySetResult(false);
                        _stoppedKeywordSpotting.SetResult(true);
                    }
                }
            });
        }

        return await _startedKeywordSpotting.Task;
    }

    public async Task<bool> StopAsync()
    {
        lock (_syncLock)
        {
            if (!_isRunning || _requestStop == null)
                return false;

            _requestStop.TrySetResult(true);
        }

        return await _stoppedKeywordSpotting!.Task;
    }

    private async Task StartKeywordSpottingAudioProcessing()
    {
        var recognizedKeyword = await RecognizeKeywordAsync();
        if (recognizedKeyword)
        {
            await ProcessKeywordAudioStreamAsync();
        }
    }

    private async Task<bool> RecognizeKeywordAsync()
    {
        while (true)
        {
            _recognizer = new KeywordRecognizer(_audioConfig);

            var recognizeKeyword = _recognizer.RecognizeOnceAsync(_model);
            _startedKeywordSpotting?.TrySetResult(true);

            var completedTask = await Task.WhenAny(recognizeKeyword, _requestStop!.Task);
            if (completedTask == _requestStop.Task)
            {
                await _recognizer.StopRecognitionAsync();
                return false;
            }

            _keywordResult = await recognizeKeyword;
            if (_keywordResult.Reason == ResultReason.RecognizedKeyword)
            {
                _audioDataStream = AudioDataStream.FromResult(_keywordResult);
                KeywordRecognized?.Invoke(this, EventArgs.Empty);
                return true;
            }
        }
    }

    private async Task ProcessKeywordAudioStreamAsync()
    {
        while (_audioDataStream != null)
        {
            if (_audioDataStream.CanReadData(BYTES_PER_PACKET))
            {
                var buffer = new byte[BYTES_PER_PACKET];
                var bytes = _audioDataStream.ReadData(buffer);
                if (bytes > 0)
                {
                    var args = new AudioDataAvailableEventArgs(buffer, (int)bytes);
                    AudioDataAvailable?.Invoke(this, args);
                }
            }

            var stopTask = _requestStop!.Task;
            var delayTask = Task.Delay(_checkStopTimeSpanInMilliseconds);
            if (await Task.WhenAny(stopTask, delayTask) == stopTask)
            {
                _audioDataStream.DetachInput();
                return;
            }
        }
    }

    private const int SAMPLES_PER_SECOND = 16000;
    private const int BYTES_PER_SAMPLE = 2;
    private const int CHANNELS = 1;
    private const int PACKETS_PER_SECOND = 20;
    private const int BYTES_PER_PACKET = SAMPLES_PER_SECOND * BYTES_PER_SAMPLE * CHANNELS / PACKETS_PER_SECOND;
    private const int CHECK_STOP_TIMES_PER_PACKET = 10;
    private const int CHECK_STOP_TIMESPAN_MS = 1000 / PACKETS_PER_SECOND / CHECK_STOP_TIMES_PER_PACKET;
    private readonly TimeSpan _checkStopTimeSpanInMilliseconds = TimeSpan.FromMilliseconds(CHECK_STOP_TIMESPAN_MS);

    private AudioConfig _audioConfig;
    private KeywordRecognizer _recognizer;
    private KeywordRecognitionModel _model;

    private TaskCompletionSource<bool>? _requestStop;
    private TaskCompletionSource<bool>? _startedKeywordSpotting;
    private TaskCompletionSource<bool>? _stoppedKeywordSpotting;

    private KeywordRecognitionResult _keywordResult;
    private AudioDataStream? _audioDataStream;

    private readonly object _syncLock = new();
    private bool _isRunning = false;
}
