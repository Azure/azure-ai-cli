## Run

Run:
```
ai help speech
ai help speech recognize advanced
ai help speech recognize examples
```

Output:
```
AI - Azure AI CLI, Version 1.0.0-DEV-robc-20241125
Copyright (c) 2024 Microsoft Corporation. All Rights Reserved.

This PUBLIC PREVIEW version may change at any time.
See: https://aka.ms/azure-ai-cli-public-preview

   ___  _____   ___ ___  ___ ___ ____ __ __
  / _ |/_  _/  / __/ _ \/ __/ __/ ___/ // /
 / __ |_/ /_  _\ \/ ___/ _// _// /__/ _  /  
/_/ |_/____/ /___/_/  /___/___/\___/_//_/   

USAGE: ai speech <command> [...]

COMMANDS

  ai speech recognize [...]       (see: ai help speech recognize)
  ai speech synthesize [...]      (see: ai help speech synthesize)

  ai speech intent [...]          (see: ai help speech intent)
  ai speech translate [...]       (see: ai help speech translate)

  ai speech batch [...]           (see: ai help speech batch)
  ai speech csr [...]             (see: ai help speech csr)

  ai speech profile [...]         (see: ai help speech profile)
  ai speech speaker [...]         (see: ai help speech speaker)

ADDITIONAL TOPICS

  ai help speech examples
  ai help find topics speech

AI - Azure AI CLI, Version 1.0.0-DEV-robc-20241125
Copyright (c) 2024 Microsoft Corporation. All Rights Reserved.

This PUBLIC PREVIEW version may change at any time.
See: https://aka.ms/azure-ai-cli-public-preview

RECOGNIZE

  The ai speech recognize command recognizes streaming audio captured
  from a device microphone or stored in local or remote audio files.

USAGE: ai speech recognize [...]

  CONNECTION                      (see: ai help speech recognize connection)
    --key KEY                     (see: ai help speech recognize key)
    --region REGION               (see: ai help speech recognize region)
    --endpoint URI                (see: ai help speech recognize endpoint)
    --token VALUE                 (see: ai help speech recognize token)

  SERVICE
    --traffic type test           (see: ai help speech traffic type)
    --http header name=value      (see: ai help speech http header)
    --query string name=value     (see: ai help speech query string)
    --speech config @file.json    (see: ai help speech websocket messages)
    --speech context @file.json   (see: ai help speech websocket messages)

  LANGUAGE
    --language LANGUAGE           (see: ai help speech recognize language)
    --languages LANG1;LANG2       (see: ai help speech recognize languages)

  INPUT                           (see: ai help speech recognize input)
    --microphone                  (see: ai help speech recognize microphone)
    --file FILE                   (see: ai help speech recognize file)
    --files PATTERN               (see: ai help speech recognize files)
    --files @FILELIST.txt         (see: ai help speech recognize files)
    --url URL                     (see: ai help speech recognize url)
    --urls URL                    (see: ai help speech recognize urls)
    --urls @URLLIST.txt           (see: ai help speech recognize urls)
    --format FORMAT               (see: ai help speech recognize format)
    --id url URL                  (see: ai help speech input id)
    --id ID                       (see: ai help speech input id)

  RECOGNITION
    --once[+]                     (see: ai help speech recognize once)
    --continuous                  (see: ai help speech recognize continuous)
    --timeout MILLISECONDS        (see: ai help speech recognize continuous)
    --keyword FILENAME            (see: ai help speech recognize keyword)

  ACCURACY                        (see: ai help speech recognize improve accuracy)
    --endpoint id ID              (see: ai help speech recognize custom speech)
    --phrases @PHRASELIST.txt     (see: ai help speech recognize phrases)

  TESTING                         (see: ai help speech recognize testing)
    --transcript TEXT             (see: ai help speech recognize transcript)
    --check wer NUMOP NUMBER      (see: ai help speech recognize check wer)
    --check text TEXTOP TEXT      (see: ai help speech recognize check text)
    --check result JMES_STRING    (see: ai help speech recognize check result)

  OUTPUT                          (see: ai help speech recognize output)
    --output all [<item>]         (see: ai help speech recognize output all)
    --output each [<item>]        (see: ai help speech recognize output each)
    --output batch json           (see: ai help speech recognize output batch json)
    --output batch file FILENAME  (see: ai help speech recognize output batch file)
    --output each file FILENAME   (see: ai help speech recognize output each file)
    --output all file FILENAME    (see: ai help speech recognize output all file)
    --output vtt file FILENAME    (see: ai help speech recognize output vtt file)
    --word level timing           (see: ai help speech recognize word level timing)
    --profanity OPTION            (see: ai help speech recognize profanity)

  LOGGING                         (see: ai help speech recognize log)
    --log FILENAME
    --content logging

  PARALLEL PROCESSING
    --threads NUMBER              (see: ai help speech recognize threads)
    --processes NUMBER            (see: ai help speech recognize processes)
    --repeat NUMBER
    --max NUMBER

  ADVANCED
    --connect                     (see: ai help speech recognize connect)
    --disconnect                  (see: ai help speech recognize disconnect)
    --proxy HOSTNAME              (see: ai help speech recognize proxy)
    --property NAME=VALUE         (see: ai help speech recognize property)
    --properties @PROPLIST.txt    (see: ai help speech recognize properties)
    --foreach in @ITEMS.txt       (see: ai help speech recognize foreach)
    --save FILENAME               (see: ai help speech recognize save)
    --zip ZIPFILE                 (see: ai help speech recognize zip)

SEE ALSO

  ai help speech setup
  ai help speech recognize examples
  ai help find topics speech recognize

AI - Azure AI CLI, Version 1.0.0-DEV-robc-20241125
Copyright (c) 2024 Microsoft Corporation. All Rights Reserved.

This PUBLIC PREVIEW version may change at any time.
See: https://aka.ms/azure-ai-cli-public-preview

RECOGNIZE EXAMPLES

  INIT: Automatically setup ai with REGION and KEY default values

    ai init

  SETUP: Manually setup ai with REGION and KEY default values

    ai config speech @region --set westus2
    ai config speech @key --set 436172626F6E20697320636F6F6C2121

  EXAMPLE 1: Recognize speech from a microphone
  
    ai speech recognize --microphone

  EXAMPLE 2: Recognize speech from local WAV file, or remote MP3 file

    ai speech recognize --file hello.wav
    ai speech recognize --file https://crbn.us/hello.mp3 --format mp3

  EXAMPLE 3: Recognize speech from multiple files using wildcards

    ai speech recognize --files *.wav

  EXAMPLE 4: Recognize speech in audio file content piped thru STDIN

    ai speech synthesize --text "Hello" --audio output - --quiet | ai speech recognize --file -

  EXAMPLE 5: Improve speech recognition accuracy with phrase lists

    ai speech recognize --files *.wav --phrases "Hello;Hi;Howya doin"

  EXAMPLE 6: Recognize multiple files from TSV file with file names and transcriptions

    ai speech recognize --foreach file;transcript in @filelist.txt --check wer eq 0

    WHERE: filelist.txt is the filename for a file containing tab delimited content:

      audioFileName1 \t transcript1
      audioFileName2 \t transcript2

  EXAMPLE 7: Recognize multiple files listed in a TSV file with file ids and transcriptions

    ai speech recognize --foreach id;transcript in @filelist.txt --check wer eq 0

    WHERE: filelist.txt is the filename for a file containing tab delimited content:

      audioFileNameNoExtension1 \t transcript1
      audioFileNameNoExtension2 \t transcript2

ADDITIONAL TOPICS

  ai help speech setup
  ai help speech recognize advanced
  ai help speech recognize
```