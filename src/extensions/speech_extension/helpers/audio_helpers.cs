//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Collections.Generic;

namespace Azure.AI.Details.Common.CLI
{
    public class AudioHelpers : Command
    {
        public AudioHelpers(ICommandValues values) : base(values)
        {
            // weird that this is a command... it's never instantiated
        }

        public static AudioConfig CreateAudioConfigFromFile(string file, string? format)
        {
            return !string.IsNullOrEmpty(format)
                ? AudioConfig.FromStreamInput(CreatePushStream(file, format))
                : FileHelpers.IsStandardInputReference(file)
                    ? AudioConfig.FromStreamInput(CreatePushStream(file))
                    : AudioConfig.FromWavFileInput(file);
        }

        public static AudioConfig CreateMicrophoneAudioConfig(string? device)
        {
            return !string.IsNullOrEmpty(device)
                ? AudioConfig.FromMicrophoneInput(device)
                : AudioConfig.FromDefaultMicrophoneInput();
        }

        static public PushAudioInputStream CreatePushStream(string file, string format)
        {
            return !string.IsNullOrEmpty(format)
                ? CreatePushStream(file, ContainerFormatFrom(format))
                : CreatePushStream(file);
        }

        static public AudioStreamContainerFormat ContainerFormatFrom(string format)
        {
            return format switch {
                "any" => AudioStreamContainerFormat.ANY,
                "alaw" => AudioStreamContainerFormat.ALAW,
                "amrnb" => AudioStreamContainerFormat.AMRNB,
                "amrwb" => AudioStreamContainerFormat.AMRWB,
                "flac" => AudioStreamContainerFormat.FLAC,
                "mp3" => AudioStreamContainerFormat.MP3,
                "ogg" => AudioStreamContainerFormat.OGG_OPUS,
                "mulaw" => AudioStreamContainerFormat.MULAW,
                _ => AudioStreamContainerFormat.ANY
            };
        }

        static public PushAudioInputStream CreatePushStream(string file, AudioStreamContainerFormat containerFormat)
        {
            var pushFormat = AudioStreamFormat.GetCompressedFormat(containerFormat);
            var push = AudioInputStream.CreatePushStream(pushFormat);

            push.Write(FileHelpers.ReadAllBytes(file));
            push.Close();
            
            return push;
        }

        static public PushAudioInputStream CreatePushStream(string file)
        {
            var push = AudioInputStream.CreatePushStream();

            push.Write(FileHelpers.ReadAllBytes(file));
            push.Close();
            
            return push;
        }
    }

    public class AudioOutputHelpers : Command
    {
        public AudioOutputHelpers(ICommandValues values) : base(values)
        {
            // weird that this is a command... it's never instantiated
        }

        public static AudioConfig CreateAudioConfigForFile(string file)
        {
            return FileHelpers.IsStandardOutputReference(file)
                ? AudioConfig.FromStreamOutput(CreateStandardOutputPushStream())
                : AudioConfig.FromWavFileOutput(file);
        }

        public static AudioConfig CreateAudioConfigForSpeaker(string? device = null)
        {
            return !string.IsNullOrEmpty(device)
                ? AudioConfig.FromSpeakerOutput(device)
                : AudioConfig.FromDefaultSpeakerOutput();
        }

        static public SpeechSynthesisOutputFormat OutputFormatFrom(string format)
        {
            return format switch {

                "mp3" => SpeechSynthesisOutputFormat.Audio16Khz128KBitRateMonoMp3,
                "audio-16khz-128kbitrate-mono-mp3" => SpeechSynthesisOutputFormat.Audio16Khz128KBitRateMonoMp3,
                "audio-16khz-32kbitrate-mono-mp3" => SpeechSynthesisOutputFormat.Audio16Khz32KBitRateMonoMp3,
                "audio-16khz-64kbitrate-mono-mp3" => SpeechSynthesisOutputFormat.Audio16Khz64KBitRateMonoMp3,
                "audio-24khz-160kbitrate-mono-mp3" => SpeechSynthesisOutputFormat.Audio24Khz160KBitRateMonoMp3,
                "audio-24khz-48kbitrate-mono-mp3" => SpeechSynthesisOutputFormat.Audio24Khz48KBitRateMonoMp3,
                "audio-24khz-96kbitrate-mono-mp3" => SpeechSynthesisOutputFormat.Audio24Khz96KBitRateMonoMp3,
                "audio-48khz-192kbitrate-mono-mp3" => SpeechSynthesisOutputFormat.Audio48Khz192KBitRateMonoMp3,
                "audio-48khz-96kbitrate-mono-mp3" => SpeechSynthesisOutputFormat.Audio48Khz96KBitRateMonoMp3,

                "opus" => SpeechSynthesisOutputFormat.Audio16Khz16Bit32KbpsMonoOpus,
                "audio-16khz-16bit-32kbps-mono-opus" => SpeechSynthesisOutputFormat.Audio16Khz16Bit32KbpsMonoOpus,
                "audio-24khz-16bit-24kbps-mono-opus" => SpeechSynthesisOutputFormat.Audio24Khz16Bit24KbpsMonoOpus,
                "audio-24khz-16bit-48kbps-mono-opus" => SpeechSynthesisOutputFormat.Audio24Khz16Bit48KbpsMonoOpus,

                "siren" => SpeechSynthesisOutputFormat.Audio16Khz16KbpsMonoSiren,
                "audio-16khz-16kbps-mono-siren" => SpeechSynthesisOutputFormat.Audio16Khz16KbpsMonoSiren,
                "riff-16khz-16kbps-mono-siren" => SpeechSynthesisOutputFormat.Riff16Khz16KbpsMonoSiren,

                "ogg" => SpeechSynthesisOutputFormat.Ogg16Khz16BitMonoOpus,
                "ogg-16khz-16bit-mono-opus" => SpeechSynthesisOutputFormat.Ogg16Khz16BitMonoOpus,
                "ogg-24khz-16bit-mono-opus" => SpeechSynthesisOutputFormat.Ogg24Khz16BitMonoOpus,
                "ogg-48khz-16bit-mono-opus" => SpeechSynthesisOutputFormat.Ogg48Khz16BitMonoOpus,

                "raw" => SpeechSynthesisOutputFormat.Raw16Khz16BitMonoPcm,
                "raw-16khz-16bit-mono-pcm" => SpeechSynthesisOutputFormat.Raw16Khz16BitMonoPcm,
                "raw-16khz-16bit-mono-truesilk" => SpeechSynthesisOutputFormat.Raw16Khz16BitMonoTrueSilk,
                "raw-24khz-16bit-mono-pcm" => SpeechSynthesisOutputFormat.Raw24Khz16BitMonoPcm,
                "raw-24khz-16bit-mono-truesilk" => SpeechSynthesisOutputFormat.Raw24Khz16BitMonoTrueSilk,
                "raw-48khz-16bit-mono-pcm" => SpeechSynthesisOutputFormat.Raw48Khz16BitMonoPcm,
                "raw-8khz-16bit-mono-pcm" => SpeechSynthesisOutputFormat.Raw8Khz16BitMonoPcm,
                "raw-8khz-8bit-mono-alaw" => SpeechSynthesisOutputFormat.Raw8Khz8BitMonoALaw,
                "raw-8khz-8bit-mono-mulaw" => SpeechSynthesisOutputFormat.Raw8Khz8BitMonoMULaw,

                "wav" => SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm,
                "riff-16khz-16bit-mono-pcm" => SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm,
                "riff-24khz-16bit-mono-pcm" => SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm,
                "riff-48khz-16bit-mono-pcm" => SpeechSynthesisOutputFormat.Riff48Khz16BitMonoPcm,
                "riff-8khz-16bit-mono-pcm" => SpeechSynthesisOutputFormat.Riff8Khz16BitMonoPcm,
                "riff-8khz-8bit-mono-alaw" => SpeechSynthesisOutputFormat.Riff8Khz8BitMonoALaw,
                "riff-8khz-8bit-mono-mulaw" => SpeechSynthesisOutputFormat.Riff8Khz8BitMonoMULaw,

                "alaw" => SpeechSynthesisOutputFormat.Riff8Khz8BitMonoALaw,
                "mulaw" => SpeechSynthesisOutputFormat.Riff8Khz8BitMonoMULaw,

                "webm" => SpeechSynthesisOutputFormat.Webm16Khz16BitMonoOpus,
                "webm-16khz-16bit-mono-opus" => SpeechSynthesisOutputFormat.Webm16Khz16BitMonoOpus,
                "webm-24khz-16bit-24kbps-mono-opus" => SpeechSynthesisOutputFormat.Webm24Khz16Bit24KbpsMonoOpus,
                "webm-24khz-16bit-mono-opus" => SpeechSynthesisOutputFormat.Webm24Khz16BitMonoOpus,

                _ => SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm
            };
        }

        private static AudioOutputStream CreateStandardOutputPushStream()
        {
            return AudioOutputStream.CreatePushStream(new PushAudioToStandardOutputCallback());
        }

        private sealed class PushAudioToStandardOutputCallback : PushAudioOutputStreamCallback
        {
            public PushAudioToStandardOutputCallback()
            {
                stream = Console.OpenStandardOutput();
            }

            public override uint Write(byte[] dataBuffer)
            {
                stream.Write(dataBuffer, 0, dataBuffer.Length);
                return (uint)dataBuffer.Length;
            }

            public override void Close()
            {
                stream.Close();
            }

            private readonly Stream stream;
        }
    }
}
