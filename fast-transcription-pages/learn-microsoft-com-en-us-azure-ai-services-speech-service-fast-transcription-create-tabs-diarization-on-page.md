## Use the Fast Transcription API - Speech Service - Azure AI Services

### Overview

The Fast Transcription API is designed to transcribe audio files with results returned synchronously and faster than real-time. This API is useful in scenarios where you need the transcript of an audio recording quickly, such as for subtitles or video translation. Unlike the batch transcription API, the fast transcription API produces transcriptions in the display form, which includes punctuation and capitalization for a more human-readable output.

### Prerequisites

1. **Azure AI Speech Resource**: You need a Speech resource in an Azure region where the fast transcription API is available. Supported regions include:
   - Australia East
   - Brazil South
   - Central India
   - East US
   - East US 2
   - French Central
   - Japan East
   - North Central US
   - North Europe
   - South Central US
   - Southeast Asia
   - Sweden Central
   - West Europe
   - West US
   - West US 2
   - West US 3

2. **Audio File**: The audio file must be less than 2 hours long and under 200 MB, in a format supported by the batch transcription API.

### Using the Fast Transcription API

#### Request Configuration Options

- **Locales**: Specify expected locales to improve transcription accuracy and minimize latency. Supported locales include: `de-DE`, `en-IN`, `en-US`, `es-ES`, `es-MX`, `fr-FR`, `hi-IN`, `it-IT`, `ja-JP`, `ko-KR`, `pt-BR`, `zh-CN`.
- **Diarization**: Enable diarization to distinguish between different speakers in an audio channel. Example: `"diarization": {"maxSpeakers": 2, "enabled": true}`.
- **Channels**: Specify channels to transcribe separately. For stereo audio, use `[0,1]`, `[0]`, or `[1]`. Diarization is not supported for multiple channels.

#### Example Requests

1. **Transcribe with Known Locale**

   ```bash
   curl --location 'https://YourServiceRegion.api.cognitive.microsoft.com/speechtotext/transcriptions:transcribe?api-version=2024-11-15' \
   --header 'Content-Type: multipart/form-data' \
   --header 'Ocp-Apim-Subscription-Key: YourSubscriptionKey' \
   --form 'audio=@"YourAudioFile"' \
   --form 'definition="{"locales":["en-US"]}"'
   ```

2. **Transcribe with Language Identification**

   ```bash
   curl --location 'https://YourServiceRegion.api.cognitive.microsoft.com/speechtotext/transcriptions:transcribe?api-version=2024-11-15' \
   --header 'Content-Type: multipart/form-data' \
   --header 'Ocp-Apim-Subscription-Key: YourSubscriptionKey' \
   --form 'audio=@"YourAudioFile"' \
   --form 'definition="{"locales":["en-US","ja-JP"]}"'
   ```

3. **Transcribe with Diarization Enabled**

   ```bash
   curl --location 'https://YourServiceRegion.api.cognitive.microsoft.com/speechtotext/transcriptions:transcribe?api-version=2024-11-15' \
   --header 'Content-Type: multipart/form-data' \
   --header 'Ocp-Apim-Subscription-Key: YourSubscriptionKey' \
   --form 'audio=@"YourAudioFile"' \
   --form 'definition="{"locales":["en-US"], "diarization": {"maxSpeakers": 2,"enabled": true}}"'
   ```

4. **Transcribe Multi-Channel Audio**

   ```bash
   curl --location 'https://YourServiceRegion.api.cognitive.microsoft.com/speechtotext/transcriptions:transcribe?api-version=2024-11-15' \
   --header 'Content-Type: multipart/form-data' \
   --header 'Ocp-Apim-Subscription-Key: YourSubscriptionKey' \
   --form 'audio=@"YourAudioFile"' \
   --form 'definition="{"locales":["en-US"], "channels": [0,1]}"'
   ```

### Response Structure

- **durationMilliseconds**: Total duration of the audio in milliseconds.
- **combinedPhrases**: Full transcriptions for all speakers, combining all channels if applicable.
- **phrases**: Array containing detailed transcription results, including:
  - `offsetMilliseconds`: Start time of the phrase in the audio.
  - `durationMilliseconds`: Duration of the phrase.
  - `text`: Transcribed text.
  - `words`: Array of individual words with timing and confidence.
  - `locale`: Locale used for the transcription.
  - `confidence`: Confidence level of the transcription.
  - `speaker`: Speaker identification (when diarization is enabled).
  - `channel`: Channel identification (when multi-channel is enabled).

### Additional Configuration

- **Profanity Filter Mode**: Options include `None`, `Masked`, `Removed`, or `Tags`. Default is `Masked`.

### Related Resources

- Fast transcription REST API reference
- Speech to text supported languages
- Batch transcription overview

This documentation provides a comprehensive guide on setting up and using the Fast Transcription API, including detailed instructions on request configurations and interpreting responses.

