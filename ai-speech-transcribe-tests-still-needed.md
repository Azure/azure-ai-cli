### Features Not Yet Tested

| Feature            | Description                                             |
|--------------------|---------------------------------------------------------|
| --region           | Specifies the region for a speech resource              |
| --key              | Specifies the subscription key for authentication       |
| --log              | Specifies the file for additional logging information   |
| --output all       | Aggregates specified items into an output file          |
| --output each      | Aggregates specified items into an output file per event|

### Example Tests

#### Test for --region

```yaml
- area: ai speech transcribe region
  tests:
  - name: set and use region
    command: ai speech transcribe --file hello.wav --region westus2
    expect-regex: |
      TRANSCRIPTION STARTED:
      TRANSCRIPTION COMPLETED:
```

#### Test for --key

```yaml
- area: ai speech transcribe key
  tests:
  - name: set and use key
    command: ai speech transcribe --file hello.wav --key 436172626F6E20697320636F6F6C2121
    expect-regex: |
      TRANSCRIPTION STARTED:
      TRANSCRIPTION COMPLETED:
```

#### Test for --log

```yaml
- area: ai speech transcribe log
  tests:
  - name: log transcription details
    command: ai speech transcribe --file hello.wav --log transcription.log
    expect-regex: |
      TRANSCRIPTION STARTED:
      TRANSCRIPTION COMPLETED:
```

#### Test for --output all

```yaml
- area: ai speech transcribe output all
  tests:
  - name: aggregate output
    command: ai speech transcribe --file hello.wav --output all text --output all file output.tsv
    expect-regex: |
      TRANSCRIPTION STARTED:
      TRANSCRIPTION COMPLETED:
```

#### Test for --output each

```yaml
- area: ai speech transcribe output each
  tests:
  - name: output each event
    command: ai speech transcribe --file hello.wav --output each text --output each file output.tsv
    expect-regex: |
      TRANSCRIPTION STARTED:
      TRANSCRIPTION COMPLETED:
```

