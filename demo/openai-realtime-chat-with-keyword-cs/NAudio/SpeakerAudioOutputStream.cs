using NAudio.Wave;

public class NotifyingBufferedWaveProvider : BufferedWaveProvider, IWaveProvider
{
    private DateTime? _fireEmptyAfter;
    private TimeSpan _debounceTimeSpan = TimeSpan.FromMilliseconds(650);

    public event EventHandler BufferEmptied;

    public NotifyingBufferedWaveProvider(WaveFormat waveFormat) : base(waveFormat)
    {
    }

    int IWaveProvider.Read(byte[] buffer, int offset, int count)
    {
        int before = BufferedBytes;
        int bytesRead = base.Read(buffer, offset, count);
        int after = BufferedBytes;

        if (before > 0 && after == 0)
        {
            _fireEmptyAfter = DateTime.UtcNow.Add(_debounceTimeSpan);
        }
        else if (before != 0 || after != 0)
        {
            _fireEmptyAfter = null;
        }
        else if (_fireEmptyAfter != null && DateTime.UtcNow > _fireEmptyAfter)
        {
            _fireEmptyAfter = null;
            BufferEmptied?.Invoke(this, EventArgs.Empty);
        }

        return bytesRead;
    }
}

public class SpeakerAudioOutputStream : IDisposable
{
    NotifyingBufferedWaveProvider _waveProvider;
    WaveOutEvent _waveOutEvent;

    public SpeakerAudioOutputStream()
    {
        WaveFormat outputAudioFormat = new(
            rate: 24000,
            bits: 16,
            channels: 1);
        _waveProvider = new(outputAudioFormat)
        {
            BufferDuration = TimeSpan.FromMinutes(2),
        };
        _waveProvider.BufferEmptied += (s, e) => PlaybackFinished?.Invoke(this, EventArgs.Empty);

        _waveOutEvent = new();
        _waveOutEvent.Init(_waveProvider);
        _waveOutEvent.Play();
    }

    public event EventHandler PlaybackFinished;

    public void EnqueueForPlayback(BinaryData audioData)
    {
        byte[] buffer = audioData.ToArray();
        _waveProvider.AddSamples(buffer, 0, buffer.Length);
    }

    public void ClearPlayback()
    {
        _waveProvider.ClearBuffer();
    }

    public void Dispose()
    {
        _waveOutEvent?.Dispose();
    }
}