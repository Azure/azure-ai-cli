public class SoundEventArgs : EventArgs
{
    public SoundEventArgs()
    {
    }

    public void PlaySound()
    {
        foreach (var sound in sounds)
        {
            #pragma warning disable CA1416 // Validate platform compatibility
            Console.Beep(sound.Item2, sound.Item1);
        }
    }

    public static SoundEventArgs CueForOff()
    {
        var args = new SoundEventArgs();
        args.sounds.Add(new Tuple<int, int>(NoteDurationMs, LowNoteFrequncy));
        return args;
    }

    public static SoundEventArgs CueForArmedKeyword()
    {
        var args = new SoundEventArgs();
        args.sounds.Add(new Tuple<int, int>(NoteDurationMs, LowNoteFrequncy));
        args.sounds.Add(new Tuple<int, int>(NoteDurationMs, LowNoteFrequncy));
        return args;
    }

    public static SoundEventArgs CueForOpenMic()
    {
        var args = new SoundEventArgs();
        args.sounds.Add(new Tuple<int, int>(NoteDurationMs, HighNoteFrequency));
        return args;
    }

    private List<Tuple<int, int>> sounds = new();

    private const int NoteDurationMs = 80;
    private const int LowNoteFrequncy = 329;
    private const int HighNoteFrequency = 660;
}
