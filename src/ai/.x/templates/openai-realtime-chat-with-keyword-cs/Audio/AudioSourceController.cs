public class AudioSourceController
{
    public AudioSourceState State { get; private set; }

    public event EventHandler<AudioSourceState>? StateChanged;
    public event EventHandler<AudioDataAvailableEventArgs>? AudioDataAvailable;
    public event EventHandler<string>? DisplayOutput;
    public event EventHandler<SoundEventArgs>? SoundOutput;

    public AudioSourceController()
    {
        _naudioMicSource = new NAudioMicrophone24KhzSource();
        _keywordAudioSource = new KeywordGatedAudio24KhzSource();

        _naudioMicSource.AudioDataAvailable += OnAudioDataAvailable;
        _keywordAudioSource.AudioDataAvailable += OnAudioDataAvailable;
        _keywordAudioSource.KeywordRecognized += OnKeywordRecognized;

        State = AudioSourceState.Off;
        _isMuted = false;
    }

    public void TransitionToOff(bool playSound = true)
    {
        State = AudioSourceState.Off;

        _naudioMicSource.Stop();
        _keywordAudioSource.Stop();

        OnStateChanged(State);

        DisplayOutput?.Invoke(this, "Off");
        if (playSound) SoundOutput?.Invoke(this, SoundEventArgs.CueForOff());
    }

    public void TransitionToOpenMic(bool playSound = true)
    {
        State = AudioSourceState.OpenMic;

        _naudioMicSource.Start();
        _keywordAudioSource.Stop();

        OnStateChanged(State);
        UnMute(playSound);
    }

    public void TransitionToKeywordArmed(bool playSound = true)
    {
        State = AudioSourceState.KeywordArmed;

        _keywordAudioSource.Start();
        _naudioMicSource.Stop();

        OnStateChanged(State);
        UnMute(playSound);
    }

    public void ToggleKeywordState(bool playSound = true)
    {
        switch (State)
        {
            case AudioSourceState.Off:
            case AudioSourceState.OpenMic:
                TransitionToKeywordArmed(playSound);
                break;

            case AudioSourceState.KeywordArmed:
                TransitionToOff(playSound);
                break;
        }
    }

    public void ToggleState(bool playSound = true)
    {
        switch (State)
        {
            case AudioSourceState.Off:
            case AudioSourceState.KeywordArmed:
                TransitionToOpenMic(playSound);
                break;

            case AudioSourceState.OpenMic:
                if (IsMuted)
                {
                    UnMute(playSound);
                }
                else
                {
                    TransitionToOff(playSound);
                }
                break;
        }
    }

    public void ToggleMute(bool playSound = true)
    {
        if (_isMuted)
        {
            UnMute(playSound);
        }
        else
        {
            Mute();
        }
    }

    public bool IsMuted { get { return _isMuted; } }

    public void Mute()
    {
        var wasMuted = _isMuted;
        _isMuted = true;

        switch (State)
        {
            case AudioSourceState.KeywordArmed:
                DisplayOutput?.Invoke(this, "Sleeping (muted)");
                break;

            case AudioSourceState.OpenMic:
                DisplayOutput?.Invoke(this, "Listening (muted)");
                break;

            case AudioSourceState.Off:
                DisplayOutput?.Invoke(this, "Off (muted)");
                break;
        }
    }

    public void UnMute(bool playSound = true)
    {
        var wasMuted = _isMuted;
        _isMuted = false;

        switch (State)
        {
            case AudioSourceState.KeywordArmed:
                DisplayOutput?.Invoke(this, "Sleeping");
                if (playSound) SoundOutput?.Invoke(this, SoundEventArgs.CueForArmedKeyword());
                break;

            case AudioSourceState.OpenMic:
                DisplayOutput?.Invoke(this, "Listening");
                if (playSound) SoundOutput?.Invoke(this, SoundEventArgs.CueForOpenMic());
                break;

            case AudioSourceState.Off:
                if (playSound) DisplayOutput?.Invoke(this, "Off");
                break;
        }
    }

    private void OnAudioDataAvailable(object? sender, AudioDataAvailableEventArgs e)
    {
        if (_isMuted)
        {
            e.AudioData = new byte[e.AudioData.Length];
        }

        AudioDataAvailable?.Invoke(this, e);
    }

    private void OnKeywordRecognized(object? sender, EventArgs e)
    {
        if (State == AudioSourceState.KeywordArmed)
        {
            DisplayOutput?.Invoke(this, "Listening (keyword)");
        }
    }

    protected virtual void OnStateChanged(AudioSourceState newState)
    {
        StateChanged?.Invoke(this, newState);
    }

    private bool _isMuted;
    private NAudioMicrophone24KhzSource _naudioMicSource;
    private KeywordGatedAudio24KhzSource _keywordAudioSource;
}
