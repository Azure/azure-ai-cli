//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    public class IntentCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommand("intent", intentCommandParsers, tokens, values);
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("intent", intentCommandParsers, tokens, values);
        }

        public static IEnumerable<INamedValueTokenParser> GetCommandParsers()
        {
            return intentCommandParsers;
        }

        #region private data

        private static INamedValueTokenParser[] intentCommandParsers = {

            new RequiredValidValueNamedValueTokenParser(null, "x.command", "11", "intent"),

            new ExpectOutputTokenParser(),
            new DiagnosticLogTokenParser(),
            new CommonNamedValueTokenParsers(),

            new ExpandFileNameNamedValueTokenParser(),

            new SpeechConfigServiceConnectionTokenParser(),
            new TrueFalseNamedValueTokenParser("service.config.content.logging.enabled", "00011;00110"),

            new TrueFalseNamedValueTokenParser("--embedded", "embedded.config.embedded", "001"),
            new Any1ValueNamedValueTokenParser("--embeddedModelKey", "embedded.config.model.key", "0011"),
            new Any1ValueNamedValueTokenParser("--embeddedModelPath", "embedded.config.model.path", "0011"),

            // new Any1ValueNamedValueTokenParser("--target", "target.language.config", "100"),
            new Any1ValueNamedValueTokenParser("--languages", "source.language.config", "100;010"),
            new RequiredValidValueNamedValueTokenParser("--profanity", "service.output.config.profanity.option", "00010", "masked;raw;removed"),
            new TrueFalseNamedValueTokenParser("service.output.config.word.level.timing", "000101"),

            new Any1or2ValueNamedValueTokenParser("--property", "config.string.property", "001"),
            new AtFileOrListNamedValueTokenParser("--properties", "config.string.properties", "001"),

            new Any1ValueNamedValueTokenParser(null, "audio.input.id.url", "0011"),

            new Any1ValueNamedValueTokenParser("--id", "audio.input.id", "001"),
            new Any1PinnedNamedValueTokenParser("--url", "audio.input.file", "001", "file", "audio.input.type"),
            new ExpandFileNameNamedValueTokenParser("--urls", "audio.input.files", "001", "audio.input.file"),
            new NamedValueTokenParser("--format",     "audio.input.format", "001", "1", "any;mp3;ogg;flac;alaw;opus", null, "file", "audio.input.type"),
            new Any1PinnedNamedValueTokenParser(null, "audio.input.microphone.geometry", "0001", "microphone", "audio.input.type"),
            new OptionalWithDefaultNamedValueTokenParser(null, "audio.input.microphone.device", "0010", "microphone", "audio.input.type"),
            // new Any1PinnedNamedValueTokenParser(null, "audio.input.push.stream.file", "00100;01100", "push", "audio.input.type"),
            // new Any1PinnedNamedValueTokenParser(null, "audio.input.pull.stream.file", "00100;01100", "pull", "audio.input.type"),
            // new RequiredValidValueNamedValueTokenParser(null, "audio.input.type", "011", "file;files;microphone;push;pull;blob"),
            new RequiredValidValueNamedValueTokenParser(null, "audio.input.type", "011", "file;files;microphone"),
            new NamedValueTokenParser(null,           "audio.input.file", "010", "1", null, "audio.input.file", "file", "audio.input.type"),
            new Any1ValueNamedValueTokenParser("--rtf", "audio.input.real.time.factor", "00110"),
            new Any1ValueNamedValueTokenParser("--fast", "audio.input.fast.lane", "0010"),

            new AtFileOrListNamedValueTokenParser("--phrases", "grammar.phrase.list", "011"),
            new Any1ValueNamedValueTokenParser(null, "grammar.recognition.factor.phrase", "0110"),
            
            new Any1ValueNamedValueTokenParser(null, "luis.key", "11"),
            new Any1ValueNamedValueTokenParser(null, "luis.region", "11"),
            new Any1ValueNamedValueTokenParser(null, "luis.appid", "11"),
            new Any1or2ValueNamedValueTokenParser(null, "luis.intent", "11"),
            new TrueFalseNamedValueTokenParser("--allintents", "luis.allintents", "01"),
            new Any1or2ValueNamedValueTokenParser("--pattern", "intent.pattern", "01;10"),
            new AtFileOrListNamedValueTokenParser("--patterns", "intent.patterns", "01"),

            new ConnectDisconnectNamedValueTokenParser(),

            new Any1ValueNamedValueTokenParser(null, "usp.speech.config", "011"),
            new Any1ValueNamedValueTokenParser(null, "usp.speech.context", "011"),

            // new Any1or2ValueNamedValueTokenParser(null, "recognizer.property", "11"),
            new Any1PinnedNamedValueTokenParser(null, "recognize.keyword.file", "010", "keyword", "recognize.method"),
            new Any1ValueNamedValueTokenParser(null, "recognize.timeout", "01"),
            new RequiredValidValueNamedValueTokenParser("--recognize", "recognize.method", "10", "keyword;continuous;once+;once"),
            new PinnedNamedValueTokenParser("--continuous", "recognize.method", "10", "continuous"),
            new PinnedNamedValueTokenParser("--once+", "recognize.method", "10", "once+"),
            new PinnedNamedValueTokenParser("--once", "recognize.method", "10", "once"),

            new IniFileNamedValueTokenParser(),

            new NamedValueTokenParser(null, "wer.sr.url", "101", "1", null, "wer.sr.url"),
            new Any1ValueNamedValueTokenParser(null, "transcript.lexical.text", "110"),
            new Any1ValueNamedValueTokenParser(null, "transcript.itn.text", "110"),
            new Any1ValueNamedValueTokenParser(null, "transcript.text", "10"),

            new Any1or2ValueNamedValueTokenParser(null, "check.sr.transcript.text.wer", "10001", "check.sr.transcript.text.wer", "true", "output.all.recognizer.recognized.result.text"),
            new Any1or2ValueNamedValueTokenParser(null, "check.sr.transcript.itn.text.wer", "100101", "check.sr.transcript.itn.text.wer", "true", "output.all.recognizer.recognized.result.itn.text"),
            new Any1or2ValueNamedValueTokenParser(null, "check.sr.transcript.lexical.text.wer", "100101", "check.sr.transcript.lexical.text.wer", "true", "output.all.recognizer.recognized.result.lexical.text"),

            new AtFileOrListWithPinnedValueNamedValueTokenParser(null, "check.sr.transcript.text.in", "10011", "check.sr.transcript.text.in", "true", "output.all.recognizer.recognized.result.text"),
            new NamedValueTokenParser(null, "check.sr.transcript.text.contains", "10011", "1", null, "check.sr.transcript.text.contains", "true", "output.all.recognizer.recognized.result.text"),
            new AtFileOrListWithPinnedValueNamedValueTokenParser(null, "check.sr.transcript.text.not.in", "100111", "check.sr.transcript.text.not.in", "true", "output.all.recognizer.recognized.result.text"),
            new NamedValueTokenParser(null, "check.sr.transcript.text.not.contains", "100111", "1", null, "check.sr.transcript.text.not.contains", "true", "output.all.recognizer.recognized.result.text"),
            new Any1or2ValueNamedValueTokenParser(null, "check.sr.transcript.text", "1001", "check.sr.transcript.text", "true", "output.all.recognizer.recognized.result.text"),

            new AtFileOrListWithPinnedValueNamedValueTokenParser(null, "check.sr.transcript.itn.text.in", "100101", "check.sr.transcript.itn.text.in", "true", "output.all.recognizer.recognized.result.itn.text"),
            new NamedValueTokenParser(null, "check.sr.transcript.itn.text.contains", "100101", "1", null, "check.sr.transcript.itn.text.contains", "true", "output.all.recognizer.recognized.result.itn.text"),
            new AtFileOrListWithPinnedValueNamedValueTokenParser(null, "check.sr.transcript.itn.text.not.in", "1001011", "check.sr.transcript.itn.text.not.in", "true", "output.all.recognizer.recognized.result.itn.text"),
            new NamedValueTokenParser(null, "check.sr.transcript.itn.text.not.contains", "1001011", "1", null, "check.sr.transcript.itn.text.not.contains", "true", "output.all.recognizer.recognized.result.itn.text"),
            new Any1or2ValueNamedValueTokenParser(null, "check.sr.transcript.itn.text", "10010", "check.sr.transcript.itn.text", "true", "output.all.recognizer.recognized.result.itn.text"),

            new AtFileOrListWithPinnedValueNamedValueTokenParser(null, "check.sr.transcript.lexical.text.in", "100101", "check.sr.transcript.lexical.text.in", "true", "output.all.recognizer.recognized.result.lexical.text"),
            new NamedValueTokenParser(null, "check.sr.transcript.lexical.text.contains", "100101", "1", null, "check.sr.transcript.lexical.text.contains", "true", "output.all.recognizer.recognized.result.lexical.text"),
            new AtFileOrListWithPinnedValueNamedValueTokenParser(null, "check.sr.transcript.lexical.text.not.in", "1001011", "check.sr.transcript.lexical.text.not.in", "true", "output.all.recognizer.recognized.result.lexical.text"),
            new NamedValueTokenParser(null, "check.sr.transcript.lexical.text.not.contains", "1001011", "1", null, "check.sr.transcript.lexical.text.not.contains", "true", "output.all.recognizer.recognized.result.lexical.text"),
            new Any1or2ValueNamedValueTokenParser(null, "check.sr.transcript.lexical.text", "10010", "check.sr.transcript.lexical.text", "true", "output.all.recognizer.recognized.result.lexical.text"),

            new Any1ValueNamedValueTokenParser(null, "check.jmes", "10"),

            new TrueFalseNamedValueTokenParser("output.overwrite", "11"),
            new TrueFalseNamedValueTokenParser("output.audio.input.id", "1101;1011"),

            new OutputBatchRecognizerTokenParser(),
            new OutputSrtVttRecognizerTokenParser(),

            new TrueFalseNamedValueTokenParser("output.all.recognizer.recognized.result.intentid", "100001"),
            new TrueFalseNamedValueTokenParser("output.each.recognizer.recognized.result.intentid", "110001"),

            new TrueFalseNamedValueTokenParser("output.all.result.intentid", "1101"),
            new TrueFalseNamedValueTokenParser("output.each.result.intentid", "1101"),

            new TrueFalseNamedValueTokenParser("output.all.recognizer.recognized.result.luis.json", "1000011"),
            new TrueFalseNamedValueTokenParser("output.each.recognizer.recognized.result.luis.json", "1100011"),

            new TrueFalseNamedValueTokenParser("output.all.result.luis.json", "11011"),
            new TrueFalseNamedValueTokenParser("output.each.result.luis.json", "11011"),

            new TrueFalseNamedValueTokenParser("output.all.recognizer.recognized.result.entity.json", "1000011"),
            new TrueFalseNamedValueTokenParser("output.each.recognizer.recognized.result.entity.json", "1100011"),

            new TrueFalseNamedValueTokenParser("output.all.result.entity.json", "11011"),
            new TrueFalseNamedValueTokenParser("output.each.result.entity.json", "11011"),

            new TrueFalseNamedValueTokenParser(null, "output.all.recognizer.recognized.result.*.entity", "1000011"),
            new TrueFalseNamedValueTokenParser(null, "output.each.recognizer.recognized.result.*.entity", "1100011"),

            new TrueFalseNamedValueTokenParser(null, "output.all.result.*.entity", "11011"),
            new TrueFalseNamedValueTokenParser(null, "output.each.result.*.entity", "11011"),

            new OutputAllRecognizerEventTokenParser(),
            new OutputEachRecognizerEventTokenParser()
        };

        #endregion
    }
}
