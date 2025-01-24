## Use the Fast Transcription API - Speech Service

The Fast Transcription API in Azure AI's Speech service allows you to transcribe audio files synchronously, providing results faster than real-time. This API is ideal for scenarios where you need a transcript of an audio recording with predictable latency, such as quick audio or video transcription, subtitles, and video translation. 

### Key Features

- **Display Form Transcription**: Unlike the batch transcription API, the fast transcription API produces transcriptions in display form, which is more human-readable with punctuation and capitalization.

### Prerequisites

1. **Azure AI Speech Resource**: You need a speech resource in one of the supported regions: Australia East, Brazil South, Central India, East US, East US 2, French Central, Japan East, North Central US, North Europe, South Central US, Southeast Asia, Sweden Central, West Europe, West US, West US 2, West US 3.

2. **Audio File**: The audio file should be less than 2 hours long and less than 200 MB in size, in a supported format and codec.

### Usage Scenarios

1. **Known Locale Specified**: Transcribe an audio file with a specified locale to improve accuracy and reduce latency.
2. **Language Identification On**: Identify the locale automatically if the audio file's locale is unknown.
3. **Diarization On**: Distinguish between different speakers in the conversation.
4. **Multi-Channel On**: Transcribe audio files with one or two channels separately.

### Request Configuration Options

- **Channels**: Zero-based indices of the channels to be transcribed separately. Up to two channels can be specified unless diarization is enabled.
- **Diarization**: Recognize and separate multiple speakers in one audio channel.
- **Locales**: List of locales matching the expected locale of the audio data.
- **ProfanityFilterMode**: Handle profanity in recognition results. Options are None, Masked, Removed, or Tags.

### REST API Example

To transcribe an audio file using REST API:

```bash
curl --location 'https://<YourServiceRegion>.api.cognitive.microsoft.com/speechtotext/transcriptions:transcribe?api-version=2024-11-15' \
--header 'Content-Type: multipart/form-data' \
--header 'Ocp-Apim-Subscription-Key: <YourSubscriptionKey>' \
--form 'audio=@"<YourAudioFile>"' \
--form 'definition="{
    "locales":["en-US"],
    "diarization": {"maxSpeakers": 2,"enabled": true},
    "channels": [0,1]}"'
```

### Response Structure

- **durationMilliseconds**: Total duration of the audio.
- **combinedPhrases**: Full transcriptions for all speakers or channels.
- **phrases**: Detailed information for each phrase, including offset, duration, text, words, locale, and confidence.

### Example JSON Response

```json
{
    "durationMilliseconds": 182439,
    "combinedPhrases": [
        {
            "text": "Good afternoon. This is Sam. Thank you for calling Contoso. How can I help? ..."
        }
    ],
    "phrases": [
        {
            "offsetMilliseconds": 960,
            "durationMilliseconds": 640,
            "text": "Good afternoon.",
            "words": [
                {"text": "Good", "offsetMilliseconds": 960, "durationMilliseconds": 240},
                {"text": "afternoon.", "offsetMilliseconds": 1200, "durationMilliseconds": 400}
            ],
            "locale": "en-US",
            "confidence": 0.93616915
        }
        // More phrases...
    ]
}
```

### Additional Information

For more details on locales, diarization, and other properties, refer to the request configuration options section in the documentation. The Fast Transcription API is part of the Azure AI services, providing advanced speech-to-text capabilities.

