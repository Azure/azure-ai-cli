# Fast Transcription API Guide for Azure AI Speech

## Overview

The Fast Transcription API allows for synchronous audio file transcription using Azure AI Speech services. This API is designed to quickly convert spoken language into text, making it ideal for applications requiring immediate transcription, such as subtitles or video translation.

## Prerequisites

- **Azure AI Speech Resource**: Ensure you have a Speech resource in a supported Azure region.
- **Audio File Requirements**: Must be less than 2 hours in duration and smaller than 200 MB. Supported formats include those compatible with the Batch Transcription API.

## API Endpoint

- **Endpoint Format**: `https://<YourServiceRegion>.api.cognitive.microsoft.com/speechtotext/transcriptions:transcribe?api-version=2024-11-15`
- **HTTP Method**: `POST`

## Authentication

- **Ocp-Apim-Subscription-Key**: Your Azure subscription key is required in the request header for authentication.

## Request Headers

- **Content-Type**: `multipart/form-data`
- **Ocp-Apim-Subscription-Key**: Your subscription key

## Request Body Parameters

The request consists of a `multipart/form-data` POST request including:

1. **audio**: Binary audio content to be transcribed.
2. **definition**: JSON object containing transcription configuration:

| Parameter           | Description                                                                                  | Example Value                                          |
|---------------------|----------------------------------------------------------------------------------------------|--------------------------------------------------------|
| `locales`           | Specifies expected locales to improve transcription accuracy.                                | `["en-US", "ja-JP"]`                                   |
| `diarization`       | Enables speaker separation. Configure max speakers and enable status.                        | `{"maxSpeakers": 2, "enabled": true}`                  |
| `channels`          | Specifies channels for transcription in stereo files.                                        | `[0,1]`                                                |
| `profanityFilterMode` | Determines how to handle profanity in the transcriptions. Options: `None`, `Masked`, `Removed`, `Tags`. | `"Masked"`                                             |

## Example Request Configurations

### Known Locale Specified

```bash
curl --location 'https://YourServiceRegion.api.cognitive.microsoft.com/speechtotext/transcriptions:transcribe?api-version=2024-11-15' \
--header 'Content-Type: multipart/form-data' \
--header 'Ocp-Apim-Subscription-Key: YourSubscriptionKey' \
--form 'audio=@"YourAudioFile"' \
--form 'definition="{
    "locales": ["en-US"]
}"'
```

### Language Identification Enabled

```bash
curl --location 'https://YourServiceRegion.api.cognitive.microsoft.com/speechtotext/transcriptions:transcribe?api-version=2024-11-15' \
--header 'Content-Type: multipart/form-data' \
--header 'Ocp-Apim-Subscription-Key: YourSubscriptionKey' \
--form 'audio=@"YourAudioFile"' \
--form 'definition="{
    "locales": ["en-US", "ja-JP"]
}"'
```

### Diarization Enabled

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

### Multi-Channel Transcription

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

## API Response Structure

- **`durationMilliseconds`**: Total duration of the audio.
- **`combinedPhrases`**: Full transcription text, including speaker separations if diarization is enabled.
- **`phrases`**: Array detailing individual transcription results with attributes such as `text`, `offsetMilliseconds`, `durationMilliseconds`, `locale`, and `confidence`.

### Example JSON Response

```json
{
  "durationMilliseconds": 182439,
  "combinedPhrases": [
    {
      "text": "Good afternoon. This is Sam. Thank you for calling Contoso. How can I help?"
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
  ]
}
```

## Additional Configuration

- **Profanity Filter Mode**: Options include `None`, `Masked`, `Removed`, or `Tags`. Default is `Masked`.

## Error Handling

The API provides detailed error messages with `ErrorCode` and `InnerError` objects for troubleshooting:
- **Error Code Examples**: `InvalidArgument`, `Unauthorized`, `ServiceUnavailable`.
- **InnerError**: Provides additional insights into the error. 

This guide details the necessary steps and configurations to effectively utilize the Fast Transcription API for real-time speech-to-text conversion in applications.

