## Use the Fast Transcription API - Speech Service - Azure AI Services

Explore how to use the fast transcription API with Azure AI Speech service. This guide covers all necessary details, parameters, and outputs, focusing on the REST API perspective.

### Overview

The Fast Transcription API is designed to transcribe audio files synchronously, offering quicker results than real-time transcription. Ideal scenarios include:

- Quick audio or video transcription
- Subtitles and edits
- Video translation

Unlike the Batch Transcription API, the Fast Transcription API only provides transcriptions in display form, which includes punctuation and capitalization for easier readability.

### Prerequisites

To use the Fast Transcription API, you need:

- An Azure AI Speech resource available in specific regions (e.g., East US, Australia East).
- An audio file less than 2 hours long and under 200 MB, in a supported format.

### Using the Fast Transcription API

**Basic Steps:**

1. **Locale Specification**: Transcribe an audio file with a known locale to improve accuracy.
2. **Language Identification**: Enable automatic locale identification if uncertain.
3. **Diarization**: Separate different speakers in the conversation.
4. **Multi-channel**: Handle audio files with multiple channels.

#### Request Configuration

To make a transcription request, construct a multipart/form-data POST request to the transcriptions endpoint. Here’s a breakdown of the key properties:

- **Locales**: List of expected audio locales (e.g., `en-US`, `ja-JP`). Helps improve transcription accuracy.
- **Diarization**: Enables speaker separation with options like `{"maxSpeakers": 2, "enabled": true}`.
- **Channels**: Specifies channels to be transcribed separately (e.g., `[0,1]` for stereo files).

#### Example Curl Command

```shell
curl --location 'https://YourServiceRegion.api.cognitive.microsoft.com/speechtotext/transcriptions:transcribe?api-version=2024-11-15' \
--header 'Content-Type: multipart/form-data' \
--header 'Ocp-Apim-Subscription-Key: YourSubscriptionKey' \
--form 'audio=@"YourAudioFile"' \
--form 'definition="{
    "locales":["en-US"], 
    "diarization": {"maxSpeakers": 2,"enabled": true}}'
```

### Response Structure

The API response includes:

- `durationMilliseconds`: Total duration of the audio.
- `combinedPhrases`: Full transcription text, including speaker separations if diarization is enabled.
- `phrases`: Detailed word-level transcription with offsets, durations, and confidence scores.

### Additional Options

- **Profanity Filter Mode**: Manage how profanity is handled in transcriptions (e.g., `Masked`, `Removed`).
- **Output Format**: Transcriptions include JSON structures with phrase details and speaker information.

### Related Content

Explore related documentation and resources to enhance your understanding:

- [Fast Transcription REST API Reference](#)
- [Speech to Text Supported Languages](#)
- [Batch Transcription Overview](#)

By following this guide, you can effectively utilize the Fast Transcription API in Azure AI services, tailoring it to meet your specific transcription needs.

