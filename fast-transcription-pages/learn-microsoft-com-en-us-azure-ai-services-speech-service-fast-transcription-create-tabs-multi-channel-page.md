## Use the Fast Transcription API - Speech Service - Azure AI Services

The Fast Transcription API is designed for quickly transcribing audio files with predictable latency, returning results faster than real-time processing. It is ideal for scenarios where immediate transcription is crucial, such as creating subtitles or translating video content.

### Prerequisites

- **Azure AI Speech Resource**: Ensure your Azure account has a Speech resource in a supported region.
- **Audio File**: The file should be less than 2 hours long and smaller than 200 MB. Supported formats are those compatible with the Batch Transcription API.

### Supported Regions

Fast Transcription is available in multiple regions, including:
- Australia East
- Brazil South
- Central India
- East US
- French Central
- Japan East
- North Europe
- South Central US
- West Europe
- West US

### API Usage

To use the Fast Transcription API, make a `multipart/form-data POST` request to the transcriptions endpoint, including the audio file and request body properties.

#### Request Configuration Options

1. **Locales**: Specify expected locales for the audio to improve accuracy and reduce latency. Supported locales include `en-US`, `fr-FR`, `de-DE`, `ja-JP`, etc.
   
2. **Diarization**: This option enables speaker separation in single-channel audio files. Specify as `{"maxSpeakers": 2, "enabled": true}`.

3. **Channels**: Specify channels for transcription. For stereo files, channels `[0,1]` can be specified unless diarization is on. The API default merges channels.

4. **Profanity Filter Mode**: Handle profanity in transcriptions using the options `None`, `Masked`, `Removed`, or `Tags`.

### Example Usages

#### Known Locale Specified

Transcribe an audio file by specifying a known locale:

```bash
curl --location 'https://YourServiceRegion.api.cognitive.microsoft.com/speechtotext/transcriptions:transcribe?api-version=2024-11-15' \
--header 'Content-Type: multipart/form-data' \
--header 'Ocp-Apim-Subscription-Key: YourSubscriptionKey' \
--form 'audio=@"YourAudioFile"' \
--form 'definition="{
    "locales": ["en-US"]
}"'
```

#### Language Identification On

Enable language identification if the audio file's locale is unknown:

```bash
curl --location 'https://YourServiceRegion.api.cognitive.microsoft.com/speechtotext/transcriptions:transcribe?api-version=2024-11-15' \
--header 'Content-Type: multipart/form-data' \
--header 'Ocp-Apim-Subscription-Key: YourSubscriptionKey' \
--form 'audio=@"YourAudioFile"' \
--form 'definition="{
    "locales": ["en-US", "ja-JP"]
}"'
```

#### Diarization Enabled

Distinguish between speakers using diarization:

```bash
curl --location 'https://YourServiceRegion.api.cognitive.microsoft.com/speechtotext/transcriptions:transcribe?api-version=2024-11-15' \
--header 'Content-Type: multipart/form-data' \
--header 'Ocp-Apim-Subscription-Key: YourSubscriptionKey' \
--form 'audio=@"YourAudioFile"' \
--form 'definition="{
    "locales": ["en-US"],
    "diarization": {"maxSpeakers": 2, "enabled": true}
}"'
```

#### Multi-Channel Transcription

Transcribe an audio file with multiple channels separately:

```bash
curl --location 'https://YourServiceRegion.api.cognitive.microsoft.com/speechtotext/transcriptions:transcribe?api-version=2024-11-15' \
--header 'Content-Type: multipart/form-data' \
--header 'Ocp-Apim-Subscription-Key: YourSubscriptionKey' \
--form 'audio=@"YourAudioFile"' \
--form 'definition="{
    "locales": ["en-US"],
    "channels": [0, 1]
}"'
```

### API Response

The API response includes:
- `durationMilliseconds`: Total duration of the audio.
- `combinedPhrases`: Full transcription text.
- `phrases`: Array of objects detailing individual phrases with attributes like `text`, `offsetMilliseconds`, `durationMilliseconds`, `locale`, and `confidence`.

For more detailed information about locales and other properties, refer to the [Speech Service Language Support Documentation](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-support).

### Additional Resources

- **[Speech to Text Overview](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/speech-to-text-overview)**: Understand the capabilities of the Speech Service.
- **[Speech to Text Quickstart](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/quickstart-speech-to-text)**: Get started with real-time speech-to-text conversion.
- **[Batch Transcription Overview](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/batch-transcription-overview)**: Learn about transcribing large audio files in storage. 

These resources will guide you through effectively using the Fast Transcription API and other related Azure AI services.

