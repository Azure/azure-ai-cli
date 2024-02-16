# Bugs found
* `spx recognize --query string fred` doesn't generate an error (it either should, or it should call thru to carbon and let carbon generate the error)
 
# Features needed for testing
* `spx keyword recognize` - add KeywordRecognizer (use `spx recognize` as basis
* `spx keyword model` - add ability to create keyword model using REST APIs, and download the result
* `spx [...] --input push` - add ability to specify audio.input.type==push (and it works with compressed and non-compressed files)
* `spx [...] --input pull` - add ability to specify audio.input.type==pull (and it works with compressed and non-compressed files)
* `spx [...] --input push SIZE` - ability to specify how big each pushed block of audio is in bytes
* `spx [...] --input pull SIZE` - ability to specify how big each pulled block of audio is in bytes
* `spx [...] --input push rnd()` - #MAYBENOTSPX ability to have push and pull sizes be randomly selected (genericize across more parameters?)
* `spx [...] --property CARBON-INTERNAL-MOCK-SdkKwsEngine=true` == `@mock.kws` #MAYBENOTSPX
* `spx [...] --property CARBON-INTERNAL-MOCK-KWS-Keyword=computer` == `@mock.keyword.computer` #MAYBENOTSPX
* `spx [...] --output file stdout` - ability to output to stdout in addition to normal files #MAYBENOTSPX
* `spx [...] --output {each/all} recognized no match reason` - ability to output no match reasons
* `spx [...] --output {each/all} cancelation reason` - ability to output CancellationDetails details
* `spx [...] --query string properties @PROPERTIES-FILE` - allow more than one query string property
* `spx [...] --http headers @HEADERS-FILE` - allow more than one http header value
* `spx [...] --dictation` - SpeechConfig.EnableDictation
* `spx [...] --send message PATH @DATA` - enable Connection.SendMessageAsync (text and binary) (#MAYBENOTSPX)
* `spx [...] --languages bcp47A=endpoint;bcp47B=endpoint2;bcp47C` - enable custom endpoints per bcp47, not just one (like now, w/ --languages A;B --endpoint endpoint)
* `spx [...] --output [...] result language` - ability to output AutoDetectSourceLanguageResult.Language
* `spx [...] --once-` - ability to do recognition prep only (don't do RecoOnce, nor Continuous... just setup, and then tear down) (look for -dont do that pattern w/Az CLI or talk to sinz)
* `spx [...] --output [...] config "blah" property`
* `spx [...] --output [...] connection "blah" property`
* `spx [...] --output [...] connection message "blah" property`
* `spx [...] --output [...] recognizer "blah" property`
* `spx [...] --output [...] result "blah" property`
* need a way to specify `AudioProcessingOptions`?

# Test cases

## speech_recognizer_tests.cpp

**SPXTEST_CASE_BEGIN("compressed stream test")**

These tests use continuous recognition, a push or pull stream, with compressed containers
* SPXTEST_SECTION("push stream works mp3") - `spx recognize --file @SINGLE_UTTERANCE_MP3.FilePath --format mp3 --check recognized text eq @SINGLE_UTTERANCE_MP3.Text --continuous --timeout @WAIT_FOR_RECO_RESULT_TIME --save recognize\stream\push\compressed\mp3.test`
* SPXTEST_SECTION("push stream works mp4") - `spx recognize --file @SINGLE_UTTERANCE_MP4.FilePath --format mp4 --check recognized text eq @SINGLE_UTTERANCE_MP4.Text --continuous --timeout @WAIT_FOR_RECO_RESULT_TIME --save recognize\stream\push\compressed\mp4.test`
* SPXTEST_SECTION("push stream works opus") - `spx recognize --file @SINGLE_UTTERANCE_OPUS.FilePath --format opus --check recognized text eq @SINGLE_UTTERANCE_OPUS.Text --continuous --timeout @WAIT_FOR_RECO_RESULT_TIME --save recognize\stream\push\compressed\opus.test`
* SPXTEST_SECTION("push stream failed with FLAC") - `spx recognize --file @SINGLE_UTTERANCE_FLAC.FilePath --format flac --check recognized text eq @SINGLE_UTTERANCE_FLAC.Text --continuous --timeout @WAIT_FOR_RECO_RESULT_TIME --save recognize\stream\push\compressed\flac.test`
* SPXTEST_SECTION("push stream failed with ALAW") - `spx recognize --file @SINGLE_UTTERANCE_A_LAW.FilePath --format alaw --check recognized text eq @SINGLE_UTTERANCE_A_LAW.Text --continuous --timeout @WAIT_FOR_RECO_RESULT_TIME --save recognize\stream\push\compressed\alaw.test`
* SPXTEST_SECTION("push stream failed with MULAW") - `spx recognize --file @SINGLE_UTTERANCE_MU_LAW.FilePath --format mulaw --check recognized text eq @SINGLE_UTTERANCE_MU_LAW.Text --continuous --timeout @WAIT_FOR_RECO_RESULT_TIME --save recognize\stream\push\compressed\mulaw.test`
- SPXTEST_SECTION("pull stream works mp3") - `spx recognize --file @SINGLE_UTTERANCE_MP3.FilePath --format mp3 --check recognized text eq @SINGLE_UTTERANCE_MP3.Text --input pull --continuous --timeout @WAIT_FOR_RECO_RESULT_TIME --save recognize\stream\pull\compressed\mp3.test`
- SPXTEST_SECTION("pull stream works mp4") - `spx recognize --file @SINGLE_UTTERANCE_MP4.FilePath --format mp4 --check recognized text eq @SINGLE_UTTERANCE_MP4.Text --input pull --continuous --timeout @WAIT_FOR_RECO_RESULT_TIME --save recognize\stream\pull\compressed\mp4.test`
- SPXTEST_SECTION("pull stream works opus") - `spx recognize --file @SINGLE_UTTERANCE_OPUS.FilePath --format opus --check recognized text eq @SINGLE_UTTERANCE_OPUS.Text --input pull --continuous --timeout @WAIT_FOR_RECO_RESULT_TIME --save recognize\stream\pull\compressed\opus.test`
- SPXTEST_SECTION("pull stream failed with FLAC") - `spx recognize --file @SINGLE_UTTERANCE_FLAC.FilePath --format flac --check recognized text eq @SINGLE_UTTERANCE_FLAC.Text --input pull --continuous --timeout @WAIT_FOR_RECO_RESULT_TIME --save recognize\stream\pull\compressed\flac.test`
- SPXTEST_SECTION("pull stream failed with ALAW") - `spx recognize --file @SINGLE_UTTERANCE_A_LAW.FilePath --format alaw --check recognized text eq @SINGLE_UTTERANCE_A_LAW.Text --input pull --continuous --timeout @WAIT_FOR_RECO_RESULT_TIME --save recognize\stream\pull\compressed\alaw.test`
- SPXTEST_SECTION("pull stream failed with MULAW") - `spx recognize --file @SINGLE_UTTERANCE_MU_LAW.FilePath --format mulaw --check recognized text eq @SINGLE_UTTERANCE_MU_LAW.Text --input pull --continuous --timeout @WAIT_FOR_RECO_RESULT_TIME --save recognize\stream\pull\compressed\mulaw.test`

**SPXTEST_CASE_BEGIN("continuousRecognitionAsync using push stream", "[api][cxx]")**
* SPXTEST_SECTION("continuous and once, fixed-size input buffers") - `spx recognize --file @SINGLE_UTTERANCE_ENGLISH.FilePath --check recognized text eq @SINGLE_UTTERANCE_ENGLISH.Text --input push --input push 1000 --continuous --timeout @WAIT_FOR_RECO_RESULT_TIME --save recognize\stream\push\pcm\fixed.size.push.stream.test`
* SPXTEST_SECTION("continuous and once, variable-size input buffers") - `spx recognize --file @SINGLE_UTTERANCE_ENGLISH.FilePath --check recognized text eq @SINGLE_UTTERANCE_ENGLISH.Text --input push --input push rnd() --continuous --timeout @WAIT_FOR_RECO_RESULT_TIME --save recognize\stream\push\pcm\variable.size.push.stream.test`
* SPXTEST_SECTION("continuous and kws") - #MAYBENOTSPX ... does continuous reco (normal) followed by kws reco; also currently #if 0'd out
* SPXTEST_SECTION("continuous start stop 3 times") - #MAYBENOTSPX ... uses mocks to start/stop continuous, after fully recognizing data from push stream

**SPXTEST_CASE_BEGIN("ContinuousRecognitionAsync using file input", "[api][cxx]")**
* SPXTEST_SECTION("start and stop once") - `spx recognize --file @SINGLE_UTTERANCE.FilePath --check recognized text eq @SINGLE_UTTERANCE.Text --continuous --timeout @WAIT_FOR_RECO_RESULT_TIME --save recognize\continuous\start.stop.normal.test`
* SPXTEST_SECTION("start without stop") - #MAYBENOTSPX ... incorrect form (don't call stop ...)
* SPXTEST_SECTION("two starts in a row") - #MAYBENOTSPX ... incorrect form (call start twice, without a stop between, and no trailing stop)
* SPXTEST_SECTION("start and then stop immediately")
  - `spx recognize --file @SINGLE_UTTERANCE.FilePath --continuous --timeout 0 --expect SESSION\sSTARTED:;SESSION\sSTOPPED: --save recognize\continuous\start.stop.timeout.0ms.test`
  - `spx recognize --file @SINGLE_UTTERANCE.FilePath --continuous --timeout 0 --expect SESSION\sSTARTED:;SESSION\sSTOPPED: --fast 0 --not expect RECOGNIZED: --save recognize\continuous\start.stop.timeout.0ms.fast.0.test`
* SPXTEST_SECTION("stop in the middle of reco")
  - `spx recognize --file @SINGLE_UTTERANCE.FilePath --continuous --timeout 500 --expect SESSION\sSTARTED:;SESSION\sSTOPPED: --save recognize\continuous\start.stop.timeout.500ms.test`
  - `spx recognize --file @SINGLE_UTTERANCE.FilePath --continuous --timeout 500 --expect SESSION\sSTARTED:;RECOGNIZED:;SESSION\sSTOPPED: --fast 0 --save recognize\continuous\start.stop.timeout.500ms.fast.0.test`

**SPXTEST_CASE_BEGIN("Single trusted root", "[api][cxx]")**
* SPXTEST_SECTION("fail with invalid single trusted cert") - `spx recognize --file something.wav --property OPENSSL_SINGLE_TRUSTED_CERT=bogus --expect @open.ssl.bogus.cert.cancel.expected`
* SPXTEST_SECTION("pass with DigiCert Global Root as single trusted cert") - `spx recognize --file something.wav --property OPENSSL_SINGLE_TRUSTED_CERT=@digicert --expect @open.ssl.digicert.expected`
* SPXTEST_SECTION("pass with configurable CRL size") - `spx recognize --file something.wav --property CONFIG_MAX_CRL_SIZE_KB=5 --expect 5k.crl.size.cancel.expected`

**SPXTEST_CASE_BEGIN("Recognition from WAV file with throttling", "[api][cxx]")**
* SPXTEST_SECTION("Continuous recognition") - `spx recognize --file something.wav --property OPENSSL_SINGLE_TRUSTED_CERT=bogus --rtf 100 --fast 0 --check text eq SOMETHING --continuous`
* SPXTEST_SECTION("One-shot recognition") - `spx recognize --file something.wav --property OPENSSL_SINGLE_TRUSTED_CERT=bogus --rtf 100 --fast 0 --check text eq SOMETHING --once``

**SPXTEST_CASE_BEGIN("Speech Recognizer basics", "[api][cxx]")**
* SPXTEST_SECTION("push stream with continuous reco") - similar to push stream, continuous, fixed-size input buffers
  - e.Offset > 0 #MAYBENOTSPX
  - e.Result.Offset == e.Offset #MAYBENOTSPX
* SPXTEST_SECTION("KWS throws exception given 11khz sampling rate") - `spx recognize --file 11khz.file.wav --keyword computer.table --log expect @11khz.kws.exception.expected`
* SPXTEST_SECTION("throw exception when the file does not existing") - #MAYBENOTSPX (because spx checks for the file prior to giving it to carbon)
* SPXTEST_SECTION("return an error message in RecognizeOnceAsync given an invalid endpoint") - `spx recognize --file something.wav --once --endpoint https://invaliduri++ --expect @recognize.once.invalid.uri.expected -- log expect @recognize.once.invalid.uri.expected`
* SPXTEST_SECTION("return canceled in StartContinuousRecognitionAsync given an invalid endpoint") - `spx recognize --file something.wav --continuous --timeout 2000 --endpoint https://invaliduri++ --expect @recognize.continuous.invalid.uri.expected -- log expect @recognize.continuous.invalid.uri.expected`
* SPXTEST_SECTION("Check that recognition can set authorization token") - `spx recognize --file https://crbn.us/hello.wav --token 123xyz --log expect "GetStringValue.*AuthToken.*yz'"`
* SPXTEST_GIVEN("Mocks for USP, Microphone, WavFilePump and Reader, and then USP ...") - #MAYBENOTSPX
* SPXTEST_SECTION("Check that recognition result contains original json payload.") - `spx recognize --file \temp\whatstheweatherlike.wav --output each file stdout --output each recognized result json --expect @recognize.json.expected --log expect @recognize.json.expected`
* SPXTEST_SECTION("Check that recognition result contains error details.") - `spx recognize --file \temp\whatstheweatherlike.wav --output each file stdout --output each recognized result json --expect @recognize.error.details.expected`
* SPXTEST_SECTION("Wrong Key triggers Canceled Event") - `spx recognize --file ... --key invalidKey --region @region --output each canceled reason --output each canceled error details --expect @bad.key.canceled.expected`
* SPXTEST_SECTION("German Speech Recognition works") - `spx recognize ... --language de-DE --expect @recognize.german.expected`
* SPXTEST_SECTION("Canceled/EndOfStream works") - `spx recognize ... --once+ --expect @recognize.once.plus.output.expected`

**SPXTEST_CASE_BEGIN("KWS basics", "[api][cxx]")**
* SPXTEST_WHEN("We do a keyword recognition with a speech recognizer") - `spx recognize --properties @mocks --keyword computer.table --expect @recognize.mocks.keyword.expected`

**SPXTEST_CASE_BEGIN("Speech Recognizer is thread-safe.", "[api][cxx]")**
* #MAYBENOTSPX

**SPXTEST_CASE_BEGIN("Speech Recognizer SpeechConfig validations", "[api][cxx]")**
* #MAYBENOTSPX

**SPXTEST_CASE_BEGIN("ConnectionEventsTest", "[api][cxx]")**
* SPXTEST_SECTION("Connected Disconnected Events with RecognizeOnceAsnyc") - `spx recognize --file something.wav --expect recognize.once.connect.disconnect.output.expected --once`
* SPXTEST_SECTION("Connected Disconnected Events with ContinuousRecognition") - `spx recognize --file something.wav --expect recognize.continuous.connect.disconnect.output.expected --continuous`

**SPXTEST_CASE_BEGIN("FromEndpoint without key and token", "[api][cxx]")**
* #MAYBENOTSPX

**SPXTEST_CASE_BEGIN("SetServiceProperty", "[api][cxx]")**
* SPXTEST_SECTION("SetServiceProperty single setting") - `spx recognize [...] --query string language=de-DE --transcript text @whatever --check wer le 10`
* SPXTEST_SECTION("SetServiceProperty property overwrite") - `spx recognize [...] --language en-US --query string language=de-DE --transcript text @whatever --check wer le 10`
* SPXTEST_SECTION("SetServiceProperty 2 properties") - `spx recognize [...] --query string properties language=de-DE;format=detailed --output each file stdout --output each recognized json --expect @recognize.2query.string.output.expected`
* SPXTEST_SECTION("SetServiceProperty FromEndpoint") - `spx recognize [...] --endpoint @this.test.endpoint --query string language=en-US --transcript text @german.transcript --check wer le 10`
* SPXTEST_SECTION("SetServiceProperty SpeechTranslationConfig") - `spx translate [...] --query strings @both.to.and.from.query.string.properties --expect @translated.output.expected`
* SPXTEST_SECTION("SetServiceProperty value with special characters") - `spx recognize [...] --query strings @special.characters.query.string.properties --expected @special.characters.query.string.output.expected`

**SPXTEST_CASE_BEGIN("Dictation Corrections", "[api][cxx]")**
* SPXTEST_SECTION("send_http_header") - `spx recognize [...] --endpoint @office.dictation.endpoint --dictation --http headers @HEADERS --expect send_http_header.output.expected`
* SPXTEST_SECTION("send_event_without_audio") - `spx recognize [...] --endpoint @office.dictation.endpoint --dictation --token abc --send message event @corrections.event.json --log expected @corrects.log.expected`
* SPXTEST_WHEN("send malformed event") - `spx recognize [...] --query string format=corrections --token abc --send message event @bad.event.data --expected @bad.event.data.output.expected --log expected bad.event.data.log.expected`
* SPXTEST_WHEN("sending well formed event") - `spx recognzie [...] --query string format=corrections --token abc --continuous --timeout 60000 --expected @corrections.output.expected`
* SPXTEST_SECTION("set_parameters_in_speech_context_and_config") - `spx recognize [...] --query string properties @set_parameters_in_speech_context_and_config.query.strings --speech context @set_parameters_in_speech_context_and_config.speech.context --speech config @set_parameters_in_speech_context_and_config.speech.config --continous --timeout 60000 --expected @set_parameters_in_speech_context_and_config.output.expected --log expected set_parameters_in_speech_context_and_config.log.expected`
* SPXTEST_SECTION("correction_and_left_right_context") - `spx recognize [...] --query string properties @correction_and_left_right_context.query.strings --dictation --token abc --properties @correction_and_left_right_context.recognizer.properties --continuous --timeout 60000 --expected @correction_and_left_right_context.output.expected --log expected correction_and_left_right_context.log.expected`
* SPXTEST_SECTION("empty_left_right_context") - `spx recognize [...] --endpoint @empty_left_right_context.endpoint --query string properties @empty_left_right_context.query.strings --dictation --token abc --properties @empty_left_right_context.recognizer.properties --continuous --timeout 60000 --expected @empty_left_right_context.output.expected --log expected @empty_left_right_context.log.expected`

**SPXTEST_CASE_BEGIN("Verify auto detect source language config in SpeechRecognizer", "[api][cxx]")**
* SPXTEST_SECTION("auto detect source language config with a vector of string parameters") - `spx recognize [...] --languages en-US;de-DE --log expected @auto.detect.source.languages.log.expected`
* SPXTEST_SECTION("auto detect source language scenario doesn't support single endpointId setting") - #MAYBENOTSPX
* SPXTEST_SECTION("auto detect source language scenario doesn't support open range") - #MAYBENOTSPX

**SPXTEST_CASE_BEGIN("Verify source language config in SpeechRecognizer", "[api][cxx]")**
* similar to SPXTEST_CASE_BEGIN("Verify auto detect source language config in SpeechRecognizer", "[api][cxx]") above

**SPXTEST_CASE_BEGIN("Verify auto detect source language result from speech recognition result", "[api][cxx]")**
* SPXTEST_SECTION("Non Language Id Scenario") - `spx recognize [...] --language en-US --output each file stdout --output each recognized result language --expect @non.lid.language.expected`
* SPXTEST_SECTION("Language Id Scenario") - #MAYBENOTSPX ... this is testing C++ idiom approach to specifying languages
* SPXTEST_SECTION("Language Id with Invalid source languages") - `spx recognize [...] --language en-US;bogus --output each file stdout --output each recognized result language --expect @bogus.lid.language.expected`
* SPXTEST_SECTION("Language Id Scenario From Multiple SourceLanguageConfig") - `spx recognize [...] --language en-US;de-DE --output each file stdout --output each recognized result language --expect @lid.language.expected`

**SPXTEST_CASE_BEGIN("Verify language id detection for continuous speech recognition", "[api][cxx]")**
* similar to -1, -2, and -3 (but here without --once- ... and with --continuous --timeout 60000)

**SPXTEST_CASE_BEGIN("Verify language id detection for multi-lingual continuous speech recognition", "[api][cxx]")**
* similar to -1, -2, -3, and -4 (more languages here)

**SPXTEST_CASE_BEGIN("Connection Message Received Events", "[api][cxx]")**
* SPXTEST_SECTION("RecognizeOnceAsync")
  - `spx recognize [...] --once --output each file stdout --output each message received --expect @once.message.received.output.expected --log expect @once.message.received.log.expected`
  - `spx recognize [...] --once --output each file stdout --output each message received path --output each message received request id --expect @once.message.received.request.ids.match.expected`
* SPXTEST_SECTION("ContinuousRecognition")
  - `spx recognize [...] --continuous --output each file stdout --output each message received --expect @continuous.message.received.output.expected --log expect @continuous.message.received.log.expected`
  - `spx recognize [...] --continuous --output each file stdout --output each message received path --output each message received request id --expect @continuous.message.received.request.ids.match.expected`

**SPXTEST_CASE_BEGIN("Custom speech-to-text endpoints", "[api][cxx]")**
* SPXTEST_SECTION("Host only") - `spx recognize [...] --host @stt.host --once --expect @recognized.output.expected`
* SPXTEST_SECTION("Host with root path") - `spx recognize [...] --host @stt.host.trailing.slash --once --expect @recognized.output.expected`
* SPXTEST_SECTION("Host with parameters") - `spx recognize [...] --host @stt.host.with.params --once --expect @stt.host.with.params.canceled.expected`
* SPXTEST_SECTION("Host with non-root path") - `spx recognize [...] --endpoint @stt.host.with.non.root.path --once --expect @stt.host.with.non.root.path.canceled.expected`
* SPXTEST_SECTION("Invalid url from portal") - `spx recognize [...] --endpoint @regional.stt.issue.token.endpoint --once --expect @regional.stt.issue.token.endpoint.output.expected`

**SPXTEST_CASE_BEGIN("Local speech-to-text endpoints", "[.][api][cxx]")**
* SPXTEST_SECTION("Host only") - `spx recognize [...] --host @test.local.host --once --transcript text @test.single.utterance.text --check wer le @test.single.utterance.le.wer`
* SPXTEST_SECTION("Host with root path") - `spx recognize [...] --host @test.local.host.trailing.slash --once --transcript text @test.single.utterance.text --check wer le @test.single.utterance.le.wer`

**SPXTEST_CASE_BEGIN("SpeechRecognizer::Proxy Tests", "[api][cxx][speech_recognizer][proxy][invalid]")**
* `spx run --command "recognize [...] --proxy 127.0.0.1 --proxy port 12345 --properties @test.invalid.proxy.le.10s --file @single.utterance.filename --expect @connection.failed.output.expected" --expect @run.took.less.than.10s.output.expected`

**SPXTEST_CASE_BEGIN("SpeechRecognizer::Proxy bypass", "[api][cxx][speech_recognizer][proxy][bypass]")**
* `spx run recognize [...] --proxy 127.0.0.1 --proxy port 12345 --properties @bypass.proxy.test.properties --file @single.utterance.filename --expect @recognize.once.single.utterance.output.expected`

**SPXTEST_CASE_BEGIN("SpeechRecognizer::Proxy bypass with Language Id", "[api][cxx][speech_recognizer][proxy][bypass]")**
* similar to -1

**SPXTEST_CASE_BEGIN("KWV via SetProperty", "[api][cxx][kwv][.]")**
* SPXTEST_WHEN("Keyword not present") - `spx recognize [...] --file @single.utterance.filename --properties @kwv.only.test.properties --expect kwv.only.test.no.match.output.expected`
* SPXTEST_WHEN("Keyword present") - `spx recognize [...] --file @COMPUTER_KEYWORD_WITH_SINGLE_UTTERANCE_1.filename --properties @kwv.only.test.properties --expect COMPUTER_KEYWORD_WITH_SINGLE_UTTERANCE_1.recognized.output.expected`


# `spx` For Build 2020

- consider `recognize` `--output.*json` rename to `--output ... response` to match batch and csr?

- non-expert mode?
- output next steps suggestions in non-expert mode
  - spx batch transcription create ... 
      NEXT STEPS: spx tr

- spx setup (as a command), with 


- spx @files should be more like git

  ./.spx
  ../.spx
  ~/.config/spx
  .dll/../.spx

  --local [PATH]
  --user [USER]
  --global

- should `spx csr help` issue command line parsing error? instead, coudl we eat that?
- should spx.exe in zip file have +X in it for linux? 

## batch
- looks like there's a content URLs and content containers url option now
- how does soveriegn cloud work with batch?
- how does batch work with containers?

## bugs
- batch `--word level timing true` doesn't work; need to impl code

## general ui/functionality
* jarno - `--version` should report version (2750051, P3)

## format conversion
* glenn - `--format mp3` consider: check if gstreamer is installed before spx uses, or provide better message (2751601, P2)

## spx config
* **amit - are secrets properly "protected" ? ... like i know how on windows, but not yet linux or mac (2751612, P1)**
* panu - how to set log file permanently in configuration; tried creating a file in the .x\data directory called log, like key and region (2751609, P2)
* panu - `--nodefaults` needs to be first. that's weird (2751610, P2)

## spx translate
* jarno - without `-US` on `en-US` on `--source en-US` didn't work and erorr message didn't help (2751616, P2)

## custom speech
* oliver - `spx csr dataset create` `--text` in examples doesn't exist (2751620, P2)

## spx transcribe
* wei - 8 channel issue on transcriber; should document that it's required to be 8 channels better (2751621, P2)

## security review with oliver
* **oliver - need spx threat model (2751623, P1)**
* **oliver - should run on windows with least privileges (2751625, P1)**
  - CREATEPROCESS_MANIFEST_RESOURCE_ID asInvoker uac dotnet
  - https://docs.microsoft.com/archive/blogs/shawnfa/adding-a-uac-manifest-to-managed-code

## endpoint logs
- for base endpoints, no model

## send spx as custom property like v3 portal sets custom property

## feedback from ralf
- move base id on model create to advanced
- required parameters vs optional

## still to do 2020 04 12 assessment

spx csr project create --name "test csr project" --output id @@proj
spx csr project status --project @proj --output json proj.json

spx csr dataset create --project @proj --name "test csr dataset" --content https://crbn.us/names --kind Language --output url @@ds
spx csr dataset status --dataset @ds --wait --output json ds.json

spx csr model create --project @proj --name "test csr model" --dataset @ds --output url @@mod
spx csr model status --model @mod --wait --output json mod.json

spx csr endpoint create --project @proj --name "test csr endpoint" --model @mod --output url @@ep
spx csr endpoint status --endpoint @ep --wait --output json ep.json --output id @@epid

spx recognize --file mysonsnameiszacchambers.wav --transcript "My sons name is Zac Chambers" --check wer ge 15
echo %errorlevel%
spx recognize --file mysonsnameiszacchambers.wav --transcript "My sons name is Zac Chambers" --check wer eq 0 --endpoint id @epid
echo %errorlevel%

spx csr endpoint update --endpoint @ep --logging enabled --name "test csr endpoint, updated"
spx csr endpoint status --endpoint @ep --output json ep.updated.json
spx csr endpoint list --logs --endpoint @ep

spx recognize --file mysonsnameiszacchambers.wav --transcript "My sons name is Zac Chambers" --check wer ge 25
spx recognize --file mysonsnameiszacchambers.wav --transcript "My sons name is Zac Chambers" --check wer eq 0 --endpoint id @epid

spx csr endpoint status --endpoint @ep --output json ep.updated.json
spx csr endpoint list --logs --endpoint @ep

spx csr endpoint list --project @proj
spx csr model list --project @proj
spx csr dataset list --project @proj
spx csr project status --project @proj

spx csr endpoint delete --endpoint @ep
spx config @ep --clear

spx csr model delete --model @mod
spx config @mod --clear

spx csr dataset delete --dataset @ds
spx config @ds --clear

spx csr project delete --project @proj
spx config @proj --clear


### test batch
- spx batch transcription [...]
- spx batch download [...]
- spx batch list [...]
- spx batch transcription create [...]
- spx batch transcription status [...]
- spx batch transcription list [...]
- spx batch transcription download [...]
- spx batch transcription update [...]
- spx batch transcription delete [...]

### file logs date
- spx csr list --endpoint logs --date blah

### output add
- spx csr [...] --output add id @@FILE
- spx csr [...] --output add url @@FILE

### paging in lists
- spx csr list [without --top X] ... keep pulling until complete

### add host support
- spx csr list --endpoints --host feint2.develop.cris.ai

### list filtering, selecting
- spx csr list [...] --where language eq en-US
- spx csr list [...] --latest
- spx csr list [...] --output id @@FILE
- spx csr list [...] --output url @@FILE
- spx csr list [...] --output add id @@FILE
- spx csr list [...] --output add url @@FILE

spx config @mids --clear
spx csr model create --foreach language in "en-US;de-DE" --name "{csr.model.language} test" --text @{csr.model.language}.txt --output add id @@mids
spx csr model status --foreach id in noheader @mids --wait 

### finish up main commands
- spx csr model copy [...]

### model manifests
- spx csr download --model id ID --manifest

### add properties
- [...] property NAME=VALUE
- [...] properties @FILENAME

## practical examples

### Setup key and region

Set the KEY and REGION in spx configuration datastore, as `@key` and `@region`.  
Then, setup spx to use the REGION and KEY by default.  

````
spx config @key --set KEY
spx config @region --set REGION
spx config @spx.defaults --set @connection.from.region
````

### Ensure that en-US is supported

Visually inspect the output of the first command, or check the errorlevel after searching for a specific language.

````
spx csr languages --list
spx csr languages --list --language en-US
````

### Create lm dataset

Create the language model dataset, using a UTF8-BOM text file named `phrases.txt` located in the `\r\inputs` directory.  
After the language model dataset has been created, store the dataset id in the spx configuration datastore as `@lm.dataset.id`.  

````
spx csr dataset create --name "rob's lm training dataset" --input path \r\inputs --phrases @phrases.txt --wait --config set dataset id @lm.dataset.id
````

### Create am datasets

Split the audio and transcript data into a test dataset (10%) and a training dataset (90%).  
The raw data is made up of `.wav` files and a single `.tsv` file stored in the `\r\inputs` folder.  
Once completed, the datasets will be stored in the `\r\prep\train` and `\r\prep\test` folders, and the 
dataset ids will be stored in spx configuration datastore as `@am.dataset.id` and `@test.dataset.id`.  

NOTEs:
* The input `.tsv` file should contiain the file ids (filename, without path, without extension) followed by the transcription.  
* The input `.wav` files should all be PCM 16Khz, 16bit, mono.  
* The output `.tsv` files will be named `transcripts.txt` (unless `--output transcript tsv file FILENAME` is specified).
* The output audio `.zip` files will be named `audio.zip` (unless `--output audio zip file FILENAME` is specified).

````
spx csr dataset prepare --input path \r\inputs --files *.wav --transcripts @my.file.with.ids.and.transcripts.tsv --split test 10 --output path \r\prep
spx csr dataset create --name "rob's am training dataset" --input path \r\prep\train  --audio audio.zip --transcripts @transcripts.txt --wait --config set dataset id @am.dataset.id
spx csr dataset create --name "rob's am test dataset" --input path \r\prep\test --audio audio.zip --transcripts @transcripts.txt --wait --config set dataset id @test.dataset.id
````

### Create the am and lm models

Both the AM and LM models will be created based on a base model, which is found with the first command, which stores the model id in spx configuration datastore as `@base.model.id`.  
The AM and LM creation commands will wait until completion, and then store the model ids in spx configuration datastore as `@am.model.id` and `@lm.model.id`.  

````
spx csr model find --latest --base --language en-US --config set @base.model.id
spx csr model create --name "rob's am" --audio dataset @am.dataset.id --wait --config set model id @am.model.id
spx csr model create --name "rob's lm" --phrases dataset @lm.dataset.id --wait --config set model id @lm.model.id
````

### Test the am and lm models

````
spx csr evaluation create --name "rob's test" --audio dataset @test.dataset.id --acoustic model @am.model.id --language model @lm.model.id --wait --config set test id @test.id
spx csr evaluation status --id @test.id
spx csr evaluation show --id @test.id
````

### Create the endpoint

The endpoint will be created using the model ids for the am, lm, and base model.

````
spx csr endpoint create --name "robch endpoint" --acoustic model @am.model.id --language model @lm.model.id --base model @base.model.id --config set endpoint id @robch.endpoint.id
````

### Try out the new endpoint

````
spx recognize --endpoint id @robch.endpoint.id --files \r\inputs\*.wav --max 1
````

### Use it via docker container

````
az acr login --name acrbn
docker pull acrbn.azurecr.io/spx:latest
docker pull acrbn.azurecr.io/spx-csr:latest

docker run -v /home/robch/data:/data acrbn.azurecr.io/spx config @key --set YOUR-KEY
docker run -v /home/robch/data:/data acrbn.azurecr.io/spx config @region --set YOUR-REGION
docker run -v /home/robch/data:/data acrbn.azurecr.io/spx config @spx.defaults --set @connection.from.region


docker run -v /home/robch/data:/data acrbn.azurecr.io/spx-csr --files *.wav --transcripts @transcripts.txt

docker run -v /home/robch/data:/data acrbn.azurecr.io/spx recognize --files *.wav --max 1 --endpoint @csr.endpoint.id
````


## spx config / setup

````
spx config @key --set KEY
spx config @region --set REGION
spx config @spx.default.config --set @connection.from.region
````

## spx csr languages

Check to see what languages are supported by Custom Speech Recognition.

````
spx csr languages --list
````

## spx csr dataset

### spx csr dataset create

````
spx csr dataset create

  --name NAME                                       (required)
  --description DESCRIPTION                         (optional)

  --audio audio.zip
  --files *.wav

  --transcripts @transcripts.txt                    (required if --audio or --files)
  --transcript "What's the weather like"

  --phrases @sentences.txt                          
  --phrases "Zac;Nic;Jac;Bec"

  --pronunciations @pronunciations.txt

  --source en-US                                    (optional, defaults to en-US)
  --language en-US
  --source language en-US

  --path .x/datasets/train                          (optional, defaults to ./)
  --path .x/datasets/test

  --properties name=value[;name2=value2...]         (optional)

  --wait [MILLISECONDS-TIMEOUT]                     (optional, defaults to 0)
````

### spx csr dataset list/show/delete

````
spx csr dataset list
spx csr dataset show (--id ID | --name NAME)
spx csr dataset delete (--id ID | --name NAME)
````

## spx csr model

### spx csr model list

````
spx csr model list 

  --base
  --base acoustic
  --base language
  --base batch
  --base online

  --source en-US
  --language en-US
  --source language en-US
````

## spx csr model 

### spx csr model create

````
spx csr model create

  --name NAME                                       (required)
  --description DESCRIPTION

  --source en-US                                    (optional, defaults to en-US)
  --language en-US
  --source language en-US

  --base model MODEL-ID                             (required)

  --audio dataset DATASET-ID
  --phrase dataset DATASET-ID
  --pronunciation dataset DATASET-ID

  --properties name=value[;name2=value2...]         (optional)

  --wait [MILLISECONDS-TIMEOUT]                     (optional, defaults to 0)
````

### spx csr model show/status/delete

````
spx csr model show (--id ID | --name NAME)
spx csr model status (--id ID | --name NAME)
spx csr model delete (--id ID | --name NAME)
````

## spx csr endpoint

### spx csr endpoint create

````
spx csr endpoint create

  --name NAME                                       (required)
  --description DESCRIPTION

  --source en-US                                    (optional, defaults to en-US)
  --language en-US
  --source language en-US

  --acoustic model id ACOUSTIC-MODEL-ID
  --language model id LANGUAGE-MODEL-ID
  
  --concurrent recognitions COUNT

  --log content (true|false)

  --properties name=value[;name2=value2...]         (optional)

  --wait [MILLISECONDS-TIMEOUT]                     (optional, defaults to 0)
````

### spx csr endpoint show/status/delete/data

````
spx csr endpoint show (--endpoint id ID | --name NAME)
spx csr endpoint status (--endpoint id ID | --name NAME)
spx csr endpoint delete (--endpoint id ID | --name NAME)
spx csr endpoint data (--endpoint id ID | --name NAME) --delete
````

## spx csr evaluation

### spx csr evaluation create

````
spx csr evaluation create

  --name NAME                                       (required)
  --description DESCRIPTION

  --audio dataset DATASET-ID
  --acoustic model ACOUSTIC-MODEL-ID
  --language model LANGUAGE-MODEL-ID
  
  --properties name=value[;name2=value2...]         (optional)

  --wait [MILLISECONDS-TIMEOUT]                     (optional, defaults to 0)
````

### spx csr evaluation list/show/status

````
spx csr evaluation list
spx csr evaluation show (--id ID | --name NAME)
spx csr evaluation status (--id ID | --name NAME)
spx csr evaluation delete (--id ID | --name NAME)
````

### spx intent
(formerly spx recognize --intent)
````
spx intent --microphone
spx intent --file FILE --luis intent (ID NAME)
spx intent --file FILE --luis appid (ID)
spx intent --file FILE --luis allintents [true/false]
````
