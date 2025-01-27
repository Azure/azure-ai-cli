
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

            new RequiredValidValueNamedValueTokenParser(null, "x.command", "11", "transcribe;speech.transcribe"),

            new ExpectOutputTokenParser(),
            new DiagnosticLogTokenParser(),
            new CommonNamedValueTokenParsers(),

            new ExpandFileNameNamedValueTokenParser(),

            new SpeechConfigServiceConnectionTokenParser(),
            new TrueFalseNamedValueTokenParser("service.config.content.logging.enabled", "00011;00110"),

            new Any1ValueNamedValueTokenParser("--languages", "source.language.config", "100;010"),
            new Any1ValueNamedValueTokenParser("--channels", "audio.input.channels", "010"),

            new TrueFalseNamedValueTokenParser("--diarization", "transcribe.diarization", "01"),
            new Any1ValueNamedValueTokenParser("--max-speakers", "transcribe.diarization.max.speakers", "0001;0010"),
            new RequiredValidValueNamedValueTokenParser("--profanity", "transcribe.profanity.filter.mode", "0100", "masked;none;tags;removed"),

            new Any1ValueNamedValueTokenParser("--id", "audio.input.id", "001"),
            new Any1PinnedNamedValueTokenParser("--url", "audio.input.file", "001", "file", "audio.input.type"),
            new ExpandFileNameNamedValueTokenParser("--urls", "audio.input.files", "001", "audio.input.file"),
            new RequiredValidValueNamedValueTokenParser(null, "audio.input.type", "011", "file;files"),
            new NamedValueTokenParser(null, "audio.input.file", "010", "1", null, "audio.input.file", "file", "audio.input.type"),

            new OutputFileNameNamedValueTokenParser(null, "transcribe.output.json.file", "0110"),
            new OutputFileNameNamedValueTokenParser(null, "transcribe.output.request.file", "0110"),

            new TrueFalseNamedValueTokenParser("output.overwrite", "11"),
            new TrueFalseNamedValueTokenParser("output.audio.input.id", "1101;1011"),

            new OutputSrtVttRecognizerTokenParser(),

            new TrueFalseNamedValueTokenParser("output.all.transcription.result.text", "10001"),

            new OutputAllRecognizerEventTokenParser(),
            new OutputEachRecognizerEventTokenParser()
        };

        #endregion
    }
}