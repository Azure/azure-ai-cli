using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    class SynthesizeCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommand("synthesize", synthesizeCommandParsers, tokens, values);
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("synthesize", synthesizeCommandParsers, tokens, values);
        }

        #region private data

        private static INamedValueTokenParser[] synthesizeCommandParsers = {

            new NamedValueTokenParser(null,           "x.command", "11", "1", "synthesize"),

            new ExpectOutputTokenParser(),
            new DiagnosticLogTokenParser(),
            new CommonNamedValueTokenParsers(),

            new NamedValueTokenParser(null,           "x.command.expand.file.name", "11111", "1"),

            new SpeechConfigServiceConnectionTokenParser(),
            // new NamedValueTokenParser(null,           "service.config.content.logging.enabled", "00011;00110", "1", "true;false"),

            new NamedValueTokenParser("--embedded",   "embedded.config.embedded", "001", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser("--embeddedModelKey", "embedded.config.model.key", "0011", "1"),
            new NamedValueTokenParser("--embeddedModelPath", "embedded.config.model.path", "0011", "1"),

            new NamedValueTokenParser("--property",   "config.string.property", "001", "2;1"),
            new NamedValueTokenParser("--properties", "config.string.properties", "001", "1", "@;"),

            new NamedValueTokenParser(null,           "synthesizer.input.text.files", "0011", "1", null, null, "synthesizer.input.text.file", "x.command.expand.file.name"),
            new NamedValueTokenParser(null,           "synthesizer.input.ssml.files", "0001", "1", null, null, "synthesizer.input.ssml.file", "x.command.expand.file.name"),

            new NamedValueTokenParser(null,           "synthesizer.input.id.url", "0011", "1"),

            new NamedValueTokenParser("--id",           "synthesizer.input.id", "001", "1"),
            new NamedValueTokenParser("--text",         "synthesizer.input.text", "001", "1", null, null, "text", "synthesizer.input.type"),
            new NamedValueTokenParser("--ssml",         "synthesizer.input.ssml", "001", "1", null, null, "ssml", "synthesizer.input.type"),
            new NamedValueTokenParser(null,             "synthesizer.input.text.file", "0011", "1", null, null, "text.file", "synthesizer.input.type"),
            new NamedValueTokenParser(null,             "synthesizer.input.ssml.file", "0001", "1", null, null, "ssml.file", "synthesizer.input.type"),
            new NamedValueTokenParser("--interactive",  "synthesizer.input.interactive", "001", "0", null, null, "interactive", "synthesizer.input.type"),
            new NamedValueTokenParser("--interactive+", "synthesizer.input.interactive+", "001", "0", null, null, "interactive+", "synthesizer.input.type"),
            new NamedValueTokenParser(null,             "synthesizer.input.type", "111", "1", "interactive;interactive+;text;ssml;text.file;ssml.file"),

            new NamedValueTokenParser(null,           "synthesizer.list.voices", "001", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null,           "synthesizer.list.languages", "011", "0", null, null, "true", "synthesizer.list.voices"),
            new NamedValueTokenParser(null,           "synthesizer.list.voice.names", "0101;0011", "0", null, null, "true", "synthesizer.list.voices"),
            new NamedValueTokenParser(null,           "synthesizer.output.voice.name", "0010", "1"),

            new NamedValueTokenParser("--speakers",   "audio.output.speaker.device", "0010", "0", null, null, "speaker", "audio.output.type"),
            new NamedValueTokenParser(null,           "audio.output.file", "110", "1", "@@", null, "file", "audio.output.type"),
            new NamedValueTokenParser(null,           "audio.output.type", "111", "1", "file;speaker"),
            new NamedValueTokenParser("--format",     "audio.output.format", "001", "1"),

            new NamedValueTokenParser(null, "output.overwrite", "11", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.synthesizer.input.id", "10001", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.synthesizer.input.id", "11001", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.synthesizer.synthesis.started.result.resultid", "1000101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.synthesizer.synthesis.started.result.reason", "1000101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.synthesizer.synthesis.started.result.audio.data", "10001011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.synthesizer.synthesis.started.result.audio.length", "10001011", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.synthesizer.synthesizing.result.resultid", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.synthesizer.synthesizing.result.reason", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.synthesizer.synthesizing.result.audio.data", "1001011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.synthesizer.synthesizing.result.audio.length", "1001011", "1;0", "true;false", null, "true"),
            // new NamedValueTokenParser(null, "output.all.synthesizer.synthesizing.result.latency", "100101", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.synthesizer.synthesis.completed.result.resultid", "1000001", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.synthesizer.synthesis.completed.result.reason", "1000001", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.synthesizer.synthesis.completed.result.audio.data", "10000011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.synthesizer.synthesis.completed.result.audio.length", "10000011", "1;0", "true;false", null, "true"),
            // new NamedValueTokenParser(null, "output.all.synthesizer.synthesis.completed.result.latency", "100001", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.synthesizer.synthesis.canceled.error.code", "1000101;100011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.synthesizer.synthesis.canceled.error.details", "1000101;100011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.synthesizer.synthesis.canceled.error", "100011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.synthesizer.synthesis.canceled.reason", "100011", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.synthesizer.synthesis.started.events", "100010", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.synthesizer.synthesizing.events", "10010", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.synthesizer.synthesis.completed.events", "100010", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.synthesizer.synthesis.canceled.events", "100010", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.synthesizer.events", "1010", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.events", "111", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.result.resultid", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.result.reason", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.result.audio.data", "11011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.result.audio.length", "11011", "1;0", "true;false", null, "true"),
            // new NamedValueTokenParser(null, "output.all.result.latency", "1101", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.each.synthesizer.synthesis.started.result.resultid", "1100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.synthesizer.synthesis.started.result.reason", "1100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.synthesizer.synthesis.started.result.audio.data", "11001011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.synthesizer.synthesis.started.result.audio.length", "11001011", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.each.synthesizer.synthesizing.result.resultid", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.synthesizer.synthesizing.result.reason", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.synthesizer.synthesizing.result.audio.data", "1001011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.synthesizer.synthesizing.result.audio.length", "1001011", "1;0", "true;false", null, "true"),
            // new NamedValueTokenParser(null, "output.each.synthesizer.synthesizing.result.latency", "100101", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.each.synthesizer.synthesis.completed.result.resultid", "1000001", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.synthesizer.synthesis.completed.result.reason", "1000001", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.synthesizer.synthesis.completed.result.audio.data", "10000011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.synthesizer.synthesis.completed.result.audio.length", "10000011", "1;0", "true;false", null, "true"),
            // new NamedValueTokenParser(null, "output.each.synthesizer.synthesis.completed.result.latency", "100001", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.each.synthesizer.canceled.error.code", "100101;100011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.synthesizer.canceled.error.details", "100101;100011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.synthesizer.canceled.error", "10011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.synthesizer.canceled.reason", "10011", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.each.synthesizer.synthesis.started.event", "110110", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.synthesizer.synthesizing.event", "11010", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.synthesizer.synthesis.completed.event", "110110", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.synthesizer.synthesis.canceled.event", "110010", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.synthesizer.event", "1110", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.event", "111", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.each.result.resultid", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.result.reason", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.result.audio.data", "11011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.result.audio.length", "11011", "1;0", "true;false", null, "true"),
            // new NamedValueTokenParser(null, "output.each.result.latency", "1101", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.tsv.file.columns", "10001", "1", "@;\t"),
            new NamedValueTokenParser(null, "output.each.tsv.file.columns", "11001", "1", "@;\t"),

            new NamedValueTokenParser(null, "output.all.tsv.file.has.header", "100111;101011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.tsv.file.has.header", "110111;111011", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.file.type", "1011", "1", "tsv"),
            new NamedValueTokenParser(null, "output.each.file.type", "1111", "1", "tsv"),
            new NamedValueTokenParser(null, "output.all.file.name", "1010", "1", "@@"),
            new NamedValueTokenParser(null, "output.each.file.name", "1110", "1", "@@"),

            new NamedValueTokenParser(null, "check.jmes", "10", "1"),
        };

        #endregion
    }
}
