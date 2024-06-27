//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    public class SynthesizeCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommand("synthesize", synthesizeCommandParsers, tokens, values);
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("synthesize", synthesizeCommandParsers, tokens, values);
        }

        public static IEnumerable<INamedValueTokenParser> GetCommandParsers()
        {
            return synthesizeCommandParsers;
        }

        #region private data

        private static INamedValueTokenParser[] synthesizeCommandParsers = {

            new RequiredValidValueNamedValueTokenParser(null, "x.command", "11", "synthesize"),

            new ExpectOutputTokenParser(),
            new DiagnosticLogTokenParser(),
            new CommonNamedValueTokenParsers(),

            new ExpandFileNameNamedValueTokenParser(),

            new SpeechConfigServiceConnectionTokenParser(),
            // new TrueFalseNamedValueTokenParser(null, "service.config.content.logging.enabled", "00011;00110"),

            new TrueFalseNamedValueTokenParser("--embedded", "embedded.config.embedded", "001"),
            new Any1ValueNamedValueTokenParser("--embeddedModelKey", "embedded.config.model.key", "0011"),
            new Any1ValueNamedValueTokenParser("--embeddedModelPath", "embedded.config.model.path", "0011"),

            new Any1or2ValueNamedValueTokenParser("--property", "config.string.property", "001"),
            new AtFileOrListNamedValueTokenParser("--properties", "config.string.properties", "001"),

            new ExpandFileNameNamedValueTokenParser(null, "synthesizer.input.text.files", "0011", "synthesizer.input.text.file"),
            new ExpandFileNameNamedValueTokenParser(null, "synthesizer.input.ssml.files", "0001", "synthesizer.input.ssml.file"),

            new Any1ValueNamedValueTokenParser(null, "synthesizer.input.id.url", "0011"),

            new Any1ValueNamedValueTokenParser("--id", "synthesizer.input.id", "001"),
            new Any1PinnedNamedValueTokenParser("--text", "synthesizer.input.text", "001", "text", "synthesizer.input.type"),
            new Any1PinnedNamedValueTokenParser("--ssml", "synthesizer.input.ssml", "001", "ssml", "synthesizer.input.type"),
            new Any1PinnedNamedValueTokenParser(null, "synthesizer.input.text.file", "0011", "text.file", "synthesizer.input.type"),
            new Any1PinnedNamedValueTokenParser(null, "synthesizer.input.ssml.file", "0001", "ssml.file", "synthesizer.input.type"),
            new PinnedNamedValueTokenParser("--interactive", "synthesizer.input.interactive", "001", "interactive", "synthesizer.input.type"),
            new PinnedNamedValueTokenParser("--interactive+", "synthesizer.input.interactive+", "001", "interactive+", "synthesizer.input.type"),
            new RequiredValidValueNamedValueTokenParser(null, "synthesizer.input.type", "111", "interactive;interactive+;text;ssml;text.file;ssml.file"),

            new TrueFalseNamedValueTokenParser("synthesizer.list.voices", "001"),
            new PinnedNamedValueTokenParser(null, "synthesizer.list.languages", "011", "true", "synthesizer.list.voices"),
            new PinnedNamedValueTokenParser(null, "synthesizer.list.voice.names", "0101;0011", "true", "synthesizer.list.voices"),
            new Any1ValueNamedValueTokenParser(null, "synthesizer.output.voice.name", "0010"),

            new PinnedNamedValueTokenParser("--speakers", "audio.output.speaker.device", "0010", "speaker", "audio.output.type"),
            new OutputFileNameNamedValueTokenParser(null, "audio.output.file", "110", null, "file", "audio.output.type"),
            new RequiredValidValueNamedValueTokenParser(null, "audio.output.type", "111", "file;speaker"),
            new Any1ValueNamedValueTokenParser("--format", "audio.output.format", "001"),

            new TrueFalseNamedValueTokenParser("output.overwrite", "11"),

            new TrueFalseNamedValueTokenParser("output.all.synthesizer.input.id", "10001"),
            new TrueFalseNamedValueTokenParser("output.each.synthesizer.input.id", "11001"),

            new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesis.started.result.resultid", "1000101"),
            new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesis.started.result.reason", "1000101"),
            new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesis.started.result.audio.data", "10001011"),
            new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesis.started.result.audio.length", "10001011"),

            new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesizing.result.resultid", "100101"),
            new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesizing.result.reason", "100101"),
            new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesizing.result.audio.data", "1001011"),
            new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesizing.result.audio.length", "1001011"),
            // new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesizing.result.latency", "100101"),

            new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesis.completed.result.resultid", "1000001"),
            new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesis.completed.result.reason", "1000001"),
            new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesis.completed.result.audio.data", "10000011"),
            new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesis.completed.result.audio.length", "10000011"),
            // new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesis.completed.result.latency", "100001"),

            new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesis.canceled.error.code", "1000101;100011"),
            new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesis.canceled.error.details", "1000101;100011"),
            new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesis.canceled.error", "100011"),
            new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesis.canceled.reason", "100011"),

            new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesis.started.events", "100010"),
            new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesizing.events", "10010"),
            new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesis.completed.events", "100010"),
            new TrueFalseNamedValueTokenParser("output.all.synthesizer.synthesis.canceled.events", "100010"),
            new TrueFalseNamedValueTokenParser("output.all.synthesizer.events", "1010"),
            new TrueFalseNamedValueTokenParser("output.all.events", "111"),

            new TrueFalseNamedValueTokenParser("output.all.result.resultid", "1101"),
            new TrueFalseNamedValueTokenParser("output.all.result.reason", "1101"),
            new TrueFalseNamedValueTokenParser("output.all.result.audio.data", "11011"),
            new TrueFalseNamedValueTokenParser("output.all.result.audio.length", "11011"),
            // new TrueFalseNamedValueTokenParser("output.all.result.latency", "1101"),

            new TrueFalseNamedValueTokenParser("output.each.synthesizer.synthesis.started.result.resultid", "1100101"),
            new TrueFalseNamedValueTokenParser("output.each.synthesizer.synthesis.started.result.reason", "1100101"),
            new TrueFalseNamedValueTokenParser("output.each.synthesizer.synthesis.started.result.audio.data", "11001011"),
            new TrueFalseNamedValueTokenParser("output.each.synthesizer.synthesis.started.result.audio.length", "11001011"),

            new TrueFalseNamedValueTokenParser("output.each.synthesizer.synthesizing.result.resultid", "100101"),
            new TrueFalseNamedValueTokenParser("output.each.synthesizer.synthesizing.result.reason", "100101"),
            new TrueFalseNamedValueTokenParser("output.each.synthesizer.synthesizing.result.audio.data", "1001011"),
            new TrueFalseNamedValueTokenParser("output.each.synthesizer.synthesizing.result.audio.length", "1001011"),
            // new TrueFalseNamedValueTokenParser("output.each.synthesizer.synthesizing.result.latency", "100101"),

            new TrueFalseNamedValueTokenParser("output.each.synthesizer.synthesis.completed.result.resultid", "1000001"),
            new TrueFalseNamedValueTokenParser("output.each.synthesizer.synthesis.completed.result.reason", "1000001"),
            new TrueFalseNamedValueTokenParser("output.each.synthesizer.synthesis.completed.result.audio.data", "10000011"),
            new TrueFalseNamedValueTokenParser("output.each.synthesizer.synthesis.completed.result.audio.length", "10000011"),
            // new TrueFalseNamedValueTokenParser("output.each.synthesizer.synthesis.completed.result.latency", "100001"),

            new TrueFalseNamedValueTokenParser("output.each.synthesizer.canceled.error.code", "100101;100011"),
            new TrueFalseNamedValueTokenParser("output.each.synthesizer.canceled.error.details", "100101;100011"),
            new TrueFalseNamedValueTokenParser("output.each.synthesizer.canceled.error", "10011"),
            new TrueFalseNamedValueTokenParser("output.each.synthesizer.canceled.reason", "10011"),

            new TrueFalseNamedValueTokenParser("output.each.synthesizer.synthesis.started.event", "110110"),
            new TrueFalseNamedValueTokenParser("output.each.synthesizer.synthesizing.event", "11010"),
            new TrueFalseNamedValueTokenParser("output.each.synthesizer.synthesis.completed.event", "110110"),
            new TrueFalseNamedValueTokenParser("output.each.synthesizer.synthesis.canceled.event", "110010"),
            new TrueFalseNamedValueTokenParser("output.each.synthesizer.event", "1110"),
            new TrueFalseNamedValueTokenParser("output.each.event", "111"),

            new TrueFalseNamedValueTokenParser("output.each.result.resultid", "1101"),
            new TrueFalseNamedValueTokenParser("output.each.result.reason", "1101"),
            new TrueFalseNamedValueTokenParser("output.each.result.audio.data", "11011"),
            new TrueFalseNamedValueTokenParser("output.each.result.audio.length", "11011"),
            // new TrueFalseNamedValueTokenParser("output.each.result.latency", "1101"),

            new RequiredValidValueNamedValueTokenParser(null, "output.all.tsv.file.columns", "10001", "@;\t"),
            new RequiredValidValueNamedValueTokenParser(null, "output.each.tsv.file.columns", "11001", "@;\t"),

            new TrueFalseNamedValueTokenParser("output.all.tsv.file.has.header", "100111;101011"),
            new TrueFalseNamedValueTokenParser("output.each.tsv.file.has.header", "110111;111011"),

            new RequiredValidValueNamedValueTokenParser(null, "output.all.file.type", "1011", "tsv"),
            new RequiredValidValueNamedValueTokenParser(null, "output.each.file.type", "1111", "tsv"),
            new OutputFileNameNamedValueTokenParser(null, "output.all.file.name", "1010"),
            new OutputFileNameNamedValueTokenParser(null, "output.each.file.name", "1110"),

            new Any1ValueNamedValueTokenParser(null, "check.jmes", "10"),
        };

        #endregion
    }
}
