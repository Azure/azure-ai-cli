public class KeywordGatedAudio24KhzSource
{
    public KeywordGatedAudio24KhzSource()
    {
        _keywordAudioSource = new KeywordGatedAudio16khzSource();
        _resample16khzTo24Khz = new AudioResampler16KhzTo24Khz();

        _keywordAudioSource.KeywordRecognized += (s, e) => KeywordRecognized?.Invoke(this, EventArgs.Empty);
        _keywordAudioSource.AudioDataAvailable += OnKeywordAudioDataAvailable;
    }

    public event EventHandler<AudioDataAvailableEventArgs>? AudioDataAvailable;
    public event EventHandler? KeywordRecognized;

    public void Start()
    {
        _keywordAudioSource.Start();
    }

    public void Stop()
    {
        _keywordAudioSource.Stop();
    }

    private void OnKeywordAudioDataAvailable(object? sender, AudioDataAvailableEventArgs e)
    {
        byte[] resampledData = _resample16khzTo24Khz.Resample(e.AudioData);
        AudioDataAvailable?.Invoke(this, new AudioDataAvailableEventArgs(resampledData, resampledData.Length));
    }

    private KeywordGatedAudio16khzSource _keywordAudioSource;
    private AudioResampler16KhzTo24Khz _resample16khzTo24Khz;

}
