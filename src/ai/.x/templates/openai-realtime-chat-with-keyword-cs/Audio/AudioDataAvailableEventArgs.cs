public class AudioDataAvailableEventArgs : EventArgs
{
    public byte[] AudioData { get; set; }
    public int BytesRecorded { get; set; }

    public AudioDataAvailableEventArgs(byte[] audioData, int bytesRecorded)
    {
        AudioData = audioData;
        BytesRecorded = bytesRecorded;
    }
}
