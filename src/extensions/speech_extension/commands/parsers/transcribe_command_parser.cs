
namespace Azure.AI.Details.Common.CLI
{
    public class TranscribeCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommand("transcribe", transcribeCommandParsers, tokens, values);
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("transcribe", transcribeCommandParsers, tokens, values);
        }

        public static IEnumerable<INamedValueTokenParser> GetCommandParsers()
        {
            return transcribeCommandParsers;
        }

        #region private data

        private static INamedValueTokenParser[] transcribeCommandParsers = {

            new RequiredValidValueNamedValueTokenParser(null, "x.command", "11", "transcribe"),

            new ExpectOutputTokenParser(),
            new DiagnosticLogTokenParser(),
            new CommonNamedValueTokenParsers(),

            new ExpandFileNameNamedValueTokenParser(),

            new SpeechConfigServiceConnectionTokenParser(),
            new TrueFalseNamedValueTokenParser("service.config.content.logging.enabled", "00011;00110"),

            new Any1ValueNamedValueTokenParser("--languages", "source.language.config", "100;010"),
            new Any1ValueNamedValueTokenParser("--channels", "audio.input.channels", "010"),
            new Any1or2ValueNamedValueTokenParser("--property", "config.string.property", "001"),
            new AtFileOrListNamedValueTokenParser("--properties", "config.string.properties", "001"),

            new TrueFalseNamedValueTokenParser("--diarization", "transcribe.diarization", "01"),
            new Any1ValueNamedValueTokenParser("--max-speakers", "transcribe.diarization.max.speakers", "0001;0010"),

            new Any1ValueNamedValueTokenParser("--id", "audio.input.id", "001"),
            new Any1PinnedNamedValueTokenParser("--url", "audio.input.file", "001", "file", "audio.input.type"),
            new ExpandFileNameNamedValueTokenParser("--urls", "audio.input.files", "001", "audio.input.file"),
            new NamedValueTokenParser("--format", "audio.input.format", "001", "1", "any;mp3;ogg;flac;alaw;opus", null, "file", "audio.input.type"),
            new Any1PinnedNamedValueTokenParser(null, "audio.input.microphone.geometry", "0001", "microphone", "audio.input.type"),
            new OptionalWithDefaultNamedValueTokenParser(null, "audio.input.microphone.device", "0010", "microphone", "audio.input.type"),

            new OutputFileNameNamedValueTokenParser(null, "transcribe.output.json.file", "0110"),
            new OutputFileNameNamedValueTokenParser(null, "transcribe.output.request.file", "0110"),

            new ConnectDisconnectNamedValueTokenParser(),

            new Any1ValueNamedValueTokenParser(null, "usp.speech.config", "011"),
            new Any1ValueNamedValueTokenParser(null, "usp.speech.context", "011"),

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

            new NamedValueTokenParser(null, "check.jmes.verbose.failures", "1010", "1;0", "true;false", null, "false"),
            new Any1ValueNamedValueTokenParser(null, "check.jmes", "10"),

            new TrueFalseNamedValueTokenParser("output.overwrite", "11"),
            new TrueFalseNamedValueTokenParser("output.audio.input.id", "1101;1011"),

            new OutputBatchRecognizerTokenParser(),
            new OutputSrtVttRecognizerTokenParser(),

            new OutputAllRecognizerEventTokenParser(),
            new OutputEachRecognizerEventTokenParser()
        };

        #endregion
    }
}