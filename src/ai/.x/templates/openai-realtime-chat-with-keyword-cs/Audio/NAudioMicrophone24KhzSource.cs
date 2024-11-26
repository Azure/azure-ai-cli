using NAudio.Wave;

public class NAudioMicrophone24KhzSource : IDisposable
{
    public NAudioMicrophone24KhzSource()
    {
        _waveInEvent = new()
        {
            WaveFormat = new WaveFormat(SAMPLES_PER_SECOND, BYTES_PER_SAMPLE * 8, CHANNELS),
        };
        _waveInEvent.DataAvailable += (_, e) =>
        {
            AudioDataAvailable?.Invoke(this, new AudioDataAvailableEventArgs(e.Buffer, e.BytesRecorded));
        };
    }

    public event EventHandler<AudioDataAvailableEventArgs>? AudioDataAvailable;

    public void Start()
    {
        _waveInEvent.StartRecording();
    }

    public void Stop()
    {
        _waveInEvent.StopRecording();
    }

    public void Dispose()
    {
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _waveInEvent?.Dispose();
        }
    }

    private const int SAMPLES_PER_SECOND = 24000;
    private const int BYTES_PER_SAMPLE = 2;
    private const int CHANNELS = 1;

    private readonly WaveInEvent _waveInEvent;
}
