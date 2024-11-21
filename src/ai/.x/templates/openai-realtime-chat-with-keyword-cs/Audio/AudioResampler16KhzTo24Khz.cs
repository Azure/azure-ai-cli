using NAudio.Wave;
using System;

public class AudioResampler16KhzTo24Khz : IDisposable
{
    private readonly WaveFormat _sourceFormat;
    private readonly WaveFormat _targetFormat;
    private readonly BufferedWaveProvider _bufferedWaveProvider;
    private readonly MediaFoundationResampler _resampler;

    public AudioResampler16KhzTo24Khz()
    {
        _sourceFormat = new WaveFormat(16000, 16, 1); // 16 kHz, 16-bit, mono
        _targetFormat = new WaveFormat(24000, 16, 1); // 24 kHz, 16-bit, mono

        _bufferedWaveProvider = new BufferedWaveProvider(_sourceFormat)
        {
            DiscardOnBufferOverflow = true,
            ReadFully = false // Do not block when no data is available
        };

        _resampler = new MediaFoundationResampler(_bufferedWaveProvider, _targetFormat)
        {
            ResamplerQuality = 60 // High-quality resampling
        };
    }

    public byte[] Resample(byte[] audioData)
    {
        _bufferedWaveProvider.AddSamples(audioData, 0, audioData.Length);

        int bytesAvailable = _resampler.WaveFormat.AverageBytesPerSecond * audioData.Length / _sourceFormat.AverageBytesPerSecond;
        byte[] resampledData = new byte[bytesAvailable];

        int bytesRead = _resampler.Read(resampledData, 0, resampledData.Length);
        if (bytesRead < resampledData.Length)
        {
            Array.Resize(ref resampledData, bytesRead);
        }

        return resampledData;
    }

    public byte[] Flush()
    {
        using (var outputStream = new System.IO.MemoryStream())
        {
            byte[] buffer = new byte[4096];
            int bytesRead;

            while ((bytesRead = _resampler.Read(buffer, 0, buffer.Length)) > 0)
            {
                outputStream.Write(buffer, 0, bytesRead);
            }

            return outputStream.ToArray();
        }
    }

    public void Dispose()
    {
        _resampler?.Dispose();
    }
}
