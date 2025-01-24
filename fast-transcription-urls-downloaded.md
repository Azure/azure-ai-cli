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


## Speech to Text REST API - Azure AI Services

### Overview
The Speech to Text REST API is part of Azure AI Services, providing capabilities for transcribing audio files into text. This service supports both batch transcription and custom speech functionalities, allowing users to work with audio data efficiently.

### Fast Transcription
Fast transcription is designed to transcribe audio files with results returned synchronously, offering a faster-than-real-time transcription experience. This is particularly useful for scenarios requiring quick audio or video transcription or translation.

#### How It Works
- **API Endpoint**: Use the `/speechtotext/transcriptions:transcribe` endpoint.
- **Parameters**: 
  - **Audio File**: Provide the audio file to be transcribed.
  - **Language**: Specify the language of the audio content.
  - **Model**: Choose a base or custom model for transcription.
- **Outputs**:
  - **Transcript**: Receive the transcribed text.
  - **Latency**: Predictable latency ensures quick turnaround.

### Custom Speech
Custom speech allows the creation and deployment of models tailored to specific datasets or requirements.

#### Features
- **Data Upload**: Upload and manage datasets for training and testing models.
- **Model Training**: Train models using specific datasets to improve accuracy.
- **Endpoint Deployment**: Deploy trained models to custom endpoints for real-time or batch processing.

### Batch Transcription
Batch transcription enables the processing of multiple audio files from URLs or Azure storage containers.

#### Operation Groups
- **Models**: Use either base or custom models for transcription.
- **Transcriptions**: Handle large volumes of audio files by batching requests.
- **Web Hooks**: Register for notifications about transcription events such as creation, processing, and completion.

### Using the API in C#
To integrate the Speech to Text REST API into a C# application, follow these steps:

1. **Authentication**: Obtain an Azure subscription key and endpoint URL.
2. **HTTP Requests**: Use `HttpClient` to send POST requests with the necessary headers (e.g., subscription key, content type).
3. **Request Body**: Include the audio data and configuration parameters in the request body.
4. **Handle Responses**: Parse the JSON response to extract the transcribed text and other relevant data.

### Additional Features
- **Logs**: Access logs for each endpoint if enabled.
- **Storage**: Use Azure storage accounts for data management.
- **Webhooks**: Set up webhooks for real-time notifications on transcription and model events.

### Service Health
Monitor the health of the Speech to Text service and its components through the service health dashboard.

### Next Steps
- **Create a Custom Speech Project**: Set up projects for managing models, datasets, and endpoints.
- **Explore Batch Transcription**: Understand how to handle large-scale audio transcription efficiently.

For further details, refer to the official [Speech to Text REST API documentation](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/rest-speech-to-text).


## Implement Language Identification with Azure AI Speech Service

### Overview

Language identification is used to determine the language spoken in audio by comparing it against a list of supported languages. Use cases include:

- **Speech to Text Recognition**: Identify the language in an audio source and transcribe it to text.
- **Speech Translation**: Identify the language in an audio source and translate it into another language.

### Key Concepts

- **Configuration Options**: Define a list of candidate languages expected in the audio. Decide to use at-start or continuous language identification.
- **Candidate Languages**: Specify up to four languages for at-start LID or up to ten for continuous LID. The service will return one of the provided candidate languages even if they weren't spoken in the audio.
- **Recognize Once or Continuous Recognition**: Choose between recognizing once or using continuous recognition methods.

### C# Example: Fast Transcription with Language Identification

#### Setup

1. **Import Necessary Libraries**

   ```csharp
   using Microsoft.CognitiveServices.Speech;
   using Microsoft.CognitiveServices.Speech.Audio;
   ```

2. **Initialize Speech Configuration**

   ```csharp
   var speechConfig = SpeechConfig.FromSubscription("YourSubscriptionKey", "YourServiceRegion");
   ```

3. **Language Configuration**

   ```csharp
   var autoDetectSourceLanguageConfig = 
       AutoDetectSourceLanguageConfig.FromLanguages(new string[] { "en-US", "de-DE", "zh-CN" });
   ```

4. **Audio Configuration**

   ```csharp
   using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
   ```

5. **Recognizer Setup**

   ```csharp
   using (var recognizer = new SpeechRecognizer(speechConfig, autoDetectSourceLanguageConfig, audioConfig))
   {
       var result = await recognizer.RecognizeOnceAsync();
       var detectedLanguage = AutoDetectSourceLanguageResult.FromResult(result).Language;
       Console.WriteLine($"Detected language: {detectedLanguage}");
   }
   ```

### REST API: Language Identification

#### Request

To run a fast transcription using the REST API:

- **Endpoint**: `wss://{region}.stt.speech.microsoft.com/speech/universal/v2`
- **Method**: POST
- **Headers**:
  - `Ocp-Apim-Subscription-Key`: Your subscription key.
  - `Content-Type`: `application/json`
- **Body**:

  ```json
  {
    "languageIdentification": {
      "candidateLocales": ["en-US", "ja-JP", "zh-CN", "hi-IN"]
    },
    "properties": { ... }
  }
  ```

#### Response

- **Success**: Returns the detected language and transcription.
- **Failure**: Provides error details if speech could not be recognized.

### Conclusion

For more examples and details on using Azure Speech Service for language identification, explore the [Azure AI Speech documentation](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-identification?tabs=once&pivots=programming-language-csharp).

### Additional Resources

- [Speech to Text Overview](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/speech-to-text)
- [Speech Translation Overview](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/translation)
- [Azure AI Services Documentation](https://learn.microsoft.com/en-us/azure/ai-services/)


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


## Give your GenAI Apps a Multilingual Voice with Azure AI Speech

### Overview

Azure AI Speech provides robust capabilities to add multilingual voice features to your GenAI applications. This guide will focus on using fast transcription methods, detailing the parameters, outputs, and explaining the process from both the REST API and C# code perspectives.

### Fast Transcription with Azure AI Speech

Fast transcription allows you to quickly convert spoken language into text. This feature is essential for applications that require real-time processing of audio data.

#### Key Parameters

1. **Language**: Specifies the language of the audio input. Use language codes like `en-US`, `fr-FR`, etc.
2. **Format**: Define the output format such as simple text or detailed format with timestamps.
3. **ProfanityFilter**: Enable or disable the filtering of profane words in the output.
4. **Punctuation**: Choose whether to include or exclude punctuation in the transcription.
5. **Voice Activity Detection**: Determines the sensitivity of detecting actual speech versus silence or noise.

#### Outputs

- **Transcription Text**: The primary output, which is the converted text from the audio input.
- **Timestamps**: Time markers indicating when each word was spoken (available in detailed format).
- **Confidence Scores**: Numerical values representing the confidence level of the transcription accuracy.

### REST API Perspective

To use the REST API for fast transcription:

1. **Endpoint**: Use the appropriate Azure Speech service endpoint for your region.
2. **HTTP Method**: Typically `POST` for sending audio data.
3. **Headers**:
   - `Content-Type`: e.g., `audio/wav`
   - `Ocp-Apim-Subscription-Key`: Your Azure subscription key.
4. **Body**: The audio file or stream that needs to be transcribed.
5. **Response Handling**: Parse the JSON response to extract transcription text and other details like confidence scores.

### C# Code Perspective

Integrating fast transcription in a C# application involves:

1. **Setup**: Install the Azure.AI.Speech library via NuGet.
2. **Configuration**:
   ```csharp
   var config = SpeechConfig.FromSubscription("YourSubscriptionKey", "YourServiceRegion");
   config.SpeechRecognitionLanguage = "en-US";
   ```
3. **Audio Input**: Use either a file or microphone as input.
   ```csharp
   using var audioInput = AudioConfig.FromWavFileInput("path/to/audio.wav");
   ```
4. **Recognition**:
   ```csharp
   var recognizer = new SpeechRecognizer(config, audioInput);
   var result = await recognizer.RecognizeOnceAsync();
   ```
5. **Output Handling**:
   ```csharp
   if (result.Reason == ResultReason.RecognizedSpeech)
   {
       Console.WriteLine($"Transcription: {result.Text}");
   }
   else
   {
       Console.WriteLine($"Speech not recognized. Reason: {result.Reason}");
   }
   ```

### Conclusion

By leveraging Azure AI Speech, your GenAI applications can seamlessly incorporate multilingual voice capabilities, enhancing user interaction and accessibility. Whether using the REST API or C#, fast transcription offers a scalable solution for real-time speech-to-text conversion.

