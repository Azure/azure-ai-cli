## Transcriptions - Transcribe - REST API (Azure AI Services)

### Overview

The Transcriptions API in Azure AI Services allows synchronous transcription of audio files. This API is part of the Speech to Text suite and provides developers with a way to convert audio content into text using the REST API.

### API Endpoint

```http
POST {endpoint}/speechtotext/transcriptions:transcribe?api-version=2024-11-15
```

### URI Parameters

- **endpoint**: (path, required, string) - The Cognitive Services endpoint to use, such as `https://westus.api.cognitive.microsoft.com`.
- **api-version**: (query, required, string) - The API version to use, e.g., `2024-11-15`.

### Request Header

- **Ocp-Apim-Subscription-Key**: (required, string) - Your Cognitive Services account key.
- **Media Types**: "multipart/form-data"

### Request Parameters

- **audio**: (formData, required, binary) - The content of the audio file to be transcribed. Must be shorter than 2 hours and smaller than 250 MB.
- **definition**: (formData, string) - Metadata for a transcription request, containing a JSON-serialized object of type `TranscribeDefinition`.

### Responses

- **200 OK**: The transcription was successful. Returns a `TranscribeResult`.
- **Other Status Codes**: Returns an `Error`.

### Security

- **Ocp-Apim-Subscription-Key**: Type: `apiKey`, In: `header`

### Example Usage

#### Sample Request

```http
POST {endpoint}/speechtotext/transcriptions:transcribe?api-version=2024-11-15
Content-Type: multipart/form-data
Ocp-Apim-Subscription-Key: {YourSubscriptionKey}

--boundary
Content-Disposition: form-data; name="audio"; filename="audiofile.wav"
Content-Type: audio/wav

<binary data>
--boundary--
```

#### Sample Response

```json
{
  "durationMilliseconds": 2000,
  "combinedPhrases": [
    {
      "text": "Weather"
    }
  ],
  "phrases": [
    {
      "offsetMilliseconds": 40,
      "durationMilliseconds": 320,
      "text": "Weather",
      "words": [
        {
          "text": "weather",
          "offsetMilliseconds": 40,
          "durationMilliseconds": 320
        }
      ],
      "locale": "en-US",
      "confidence": 0.78983736
    }
  ]
}
```

### Definitions

- **ChannelCombinedPhrases**: The full transcript per channel.
  - **channel**: (integer) The 0-based channel index.
  - **text**: (string) The transcribed text.

- **Phrase**: A transcribed phrase.
  - **channel**: (integer) The 0-based channel index.
  - **confidence**: (number) The confidence value for the phrase.
  - **durationMilliseconds**: (integer) The duration of the phrase in milliseconds.
  - **locale**: (string) The locale of the phrase.
  - **offsetMilliseconds**: (integer) The start offset of the phrase in milliseconds.
  - **speaker**: (integer) A unique integer for each detected speaker.
  - **text**: (string) The transcribed text.
  - **words**: (Word[]) The words that make up the phrase.

- **Word**: Time-stamped word in the display form.
  - **durationMilliseconds**: (integer) Duration of the word.
  - **offsetMilliseconds**: (integer) Start offset of the word.
  - **text**: (string) The recognized word, including punctuation.

### Error Handling

The API provides detailed error messages with `ErrorCode` and `InnerError` objects for troubleshooting:

- **ErrorCode** examples include `InvalidArgument`, `Unauthorized`, `ServiceUnavailable`, etc.
- **InnerError** contains additional details for deeper insights into the error.

This comprehensive guide allows developers to efficiently integrate and utilize the Transcriptions REST API for converting speech into text within their applications.

