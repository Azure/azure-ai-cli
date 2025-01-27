### Features Not Yet Tested

| Feature            | Description                                             |
|--------------------|---------------------------------------------------------|
| --region           | Specifies the region for a speech resource              |
| --key              | Specifies the subscription key for authentication       |
| --foreach          | Repeats a specific command multiple times               |
| --language         | Specifies a single spoken language                      |
| --languages        | Auto-detect from a set of languages                     |
| --log              | Specifies the file for additional logging information   |
| --output all       | Aggregates specified items into an output file          |
| --output each      | Aggregates specified items into an output file per event|
| --threads          | Specifies number of threads for parallel processing     |
| --processes        | Specifies number of sub-processes for parallel processing|
| --save             | Packages command line and config data into a file       |
| --zip              | Packages data and dependencies into a ZIP file          |

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

#### Test for --foreach

```yaml
- area: ai speech transcribe foreach
  tests:
  - name: transcribe multiple files
    command: ai speech transcribe --foreach file in @filelist.txt
    expect-regex: |
      TRANSCRIPTION STARTED:
      TRANSCRIPTION COMPLETED:
```

#### Test for --language

```yaml
- area: ai speech transcribe language
  tests:
  - name: set and use language
    command: ai speech transcribe --file hello.wav --language en-US
    expect-regex: |
      TRANSCRIPTION STARTED:
      TRANSCRIPTION COMPLETED:
```

#### Test for --languages

```yaml
- area: ai speech transcribe languages
  tests:
  - name: auto-detect language
    command: ai speech transcribe --file hello.wav --languages en-US;de-DE
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

#### Test for --threads

```yaml
- area: ai speech transcribe threads
  tests:
  - name: use multiple threads
    command: ai speech transcribe --file hello.wav --threads 4
    expect-regex: |
      TRANSCRIPTION STARTED:
      TRANSCRIPTION COMPLETED:
```

#### Test for --processes

```yaml
- area: ai speech transcribe processes
  tests:
  - name: use multiple processes
    command: ai speech transcribe --file hello.wav --processes 4
    expect-regex: |
      TRANSCRIPTION STARTED:
      TRANSCRIPTION COMPLETED:
```

#### Test for --save

```yaml
- area: ai speech transcribe save
  tests:
  - name: save configuration
    command: ai speech transcribe --file hello.wav --save config.job
    expect-regex: |
      CONFIGURATION SAVED:
```

#### Test for --zip

```yaml
- area: ai speech transcribe zip
  tests:
  - name: package into zip
    command: ai speech transcribe --file hello.wav --zip config.zip
    expect-regex: |
      ZIP PACKAGE CREATED:
```

