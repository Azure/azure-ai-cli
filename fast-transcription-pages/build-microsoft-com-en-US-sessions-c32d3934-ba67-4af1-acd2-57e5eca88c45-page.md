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

