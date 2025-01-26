To add WebVTT and SRT output support to the `TranscribeCommand` class, we need to implement a few new methods and modify existing ones to ensure the generated transcription can be output in these formats. Here's the complete modification:

### New Methods to Add

1. **GenerateSrtFile**
2. **GenerateVttFile**

```csharp
private void GenerateSrtFile(string transcriptionJson, string srtFileName)
{
    var json = JsonDocument.Parse(transcriptionJson);

    var sb = new StringBuilder();
    int sequence = 1;

    foreach (var phrase in json.RootElement.GetProperty("phrases").EnumerateArray())
    {
        var startTime = TimeSpan.FromMilliseconds(phrase.GetProperty("offsetMilliseconds").GetInt32());
        var duration = TimeSpan.FromMilliseconds(phrase.GetProperty("durationMilliseconds").GetInt32());
        var endTime = startTime + duration;

        sb.AppendLine($"{sequence}");
        sb.AppendLine($"{startTime:hh\\:mm\\:ss\\,fff} --> {endTime:hh\\:mm\\:ss\\,fff}");
        sb.AppendLine(phrase.GetProperty("text").GetString());
        sb.AppendLine();

        sequence++;
    }

    FileHelpers.WriteAllText(srtFileName, sb.ToString().Trim(), Encoding.UTF8);
}

private void GenerateVttFile(string transcriptionJson, string vttFileName)
{
    var json = JsonDocument.Parse(transcriptionJson);

    var sb = new StringBuilder("WEBVTT\n");

    foreach (var phrase in json.RootElement.GetProperty("phrases").EnumerateArray())
    {
        var startTime = TimeSpan.FromMilliseconds(phrase.GetProperty("offsetMilliseconds").GetInt32());
        var duration = TimeSpan.FromMilliseconds(phrase.GetProperty("durationMilliseconds").GetInt32());
        var endTime = startTime + duration;

        sb.AppendLine();
        sb.AppendLine($"{startTime:hh\\:mm\\:ss\\.fff} --> {endTime:hh\\:mm\\:ss\\.fff}");
        sb.AppendLine(phrase.GetProperty("text").GetString());
    }

    FileHelpers.WriteAllText(vttFileName, sb.ToString().Trim(), Encoding.UTF8);
}
```

### Modifications to Existing Methods

#### 1. **ProcessResponse**
We need to call the new methods to generate SRT and VTT files if the corresponding options are enabled.

```csharp
private void ProcessResponse(string jsonResponse)
{
    if (Program.Debug) Console.WriteLine(jsonResponse);

    var json = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

    var transcription = string.Join("\n", json
        .GetProperty("combinedPhrases")
        .EnumerateArray()
        .Select(p => p.GetProperty("text").GetString())
        .ToArray());

    var phrases = json.GetProperty("phrases").EnumerateArray();
    var hasSpeakerInfo = phrases.Any(p => p.TryGetProperty("speaker", out var speaker) && speaker.GetInt32() > 0);
    var withSpeakerInfo = hasSpeakerInfo
        ? string.Join("\n", phrases.Select(p => {
            var speaker = p.GetProperty("speaker").GetInt32();
            var text = p.GetProperty("text").GetString();
            return $"Speaker {speaker}: {text}";
            }))
        : transcription;

    if (!_quiet) Console.WriteLine(withSpeakerInfo);

    _output!.EnsureOutputAll("transcription.result", withSpeakerInfo);
    _output!.CheckOutput();

    var srtFileName = _values.GetOrEmpty("transcribe.output.srt.file");
    if (!string.IsNullOrEmpty(srtFileName))
    {
        GenerateSrtFile(jsonResponse, srtFileName);
    }

    var vttFileName = _values.GetOrEmpty("transcribe.output.vtt.file");
    if (!string.IsNullOrEmpty(vttFileName))
    {
        GenerateVttFile(jsonResponse, vttFileName);
    }
}
```

#### 2. **StartCommand**
We need to initialize output settings for SRT and VTT.

```csharp
private void StartCommand()
{
    CheckPath();
    CheckAudioInput();

    _output = new OutputHelper(_values);
    _output!.StartOutput();

    var id = _values["audio.input.id"]!;
    _output!.EnsureOutputAll("audio.input.id", id);
    _output!.EnsureOutputEach("audio.input.id", id);
    _output!.EnsureCacheProperty("audio.input.id", id);

    var file = _values["audio.input.file"];
    _output!.EnsureCacheProperty("audio.input.file", file);

    // Initialize SRT and VTT output file settings
    var srtFileName = _values.GetOrEmpty("transcribe.output.srt.file");
    if (!string.IsNullOrEmpty(srtFileName))
    {
        _output!.EnsureOutputAll("transcribe.output.srt.file", srtFileName);
        _output!.EnsureOutputEach("transcribe.output.srt.file", srtFileName);
    }

    var vttFileName = _values.GetOrEmpty("transcribe.output.vtt.file");
    if (!string.IsNullOrEmpty(vttFileName))
    {
        _output!.EnsureOutputAll("transcribe.output.vtt.file", vttFileName);
        _output!.EnsureOutputEach("transcribe.output.vtt.file", vttFileName);
    }
}
```

These changes ensure that the `TranscribeCommand` class can handle SRT and VTT output formats as specified.

