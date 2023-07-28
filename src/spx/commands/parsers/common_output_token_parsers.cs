//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.AI.Details.Common.CLI
{
    public class OutputBatchRecognizerTokenParser : NamedValueTokenParserList
    {
        public OutputBatchRecognizerTokenParser() : base(
            new NamedValueTokenParser(null, "output.batch.json", "111", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.batch.file.name", "1110", "1", "@@", null, "true", "output.batch.json")
        ) {}
    }

    public class OutputSrtVttRecognizerTokenParser : NamedValueTokenParserList
    {
        public OutputSrtVttRecognizerTokenParser() : base(
            new NamedValueTokenParser(null, "output.srt.file.name", "1110", "1", "@@", null, "true", "output.type.srt"),
            new NamedValueTokenParser(null, "output.type.srt", "101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.vtt.file.name", "1110", "1", "@@", null, "true", "output.type.vtt"),
            new NamedValueTokenParser(null, "output.type.vtt", "101", "1;0", "true;false", null, "true")
        ) {}
    }
    
    public class OutputAllConnectionEventTokenParser : NamedValueTokenParserList
    {
        public OutputAllConnectionEventTokenParser() : base(

            new NamedValueTokenParser(null, "output.all.connection.message.received.path", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.connection.message.received.is.binary.message", "10010111", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.connection.message.received.binary.message.size", "1001011;10010101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.connection.message.received.binary.message", "1001011;1001011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.connection.message.received.is.text.message", "10010111", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.connection.message.received.text.message", "1001010", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.connection.message.received.request.id", "1001011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.connection.message.received.content.type", "1001011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.connection.message.received.*.property", "1001011", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.connection.connected.sessionid", "10011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.connection.disconnected.sessionid", "10011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.connection.event.sessionid", "10101", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.connection.connected.timestamp", "10011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.connection.disconnected.timestamp", "10011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.connection.event.timestamp", "10101", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.connection.connected.events", "10010", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.connection.disconnected.events", "10010", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.connection.message.received.events", "100110", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.connection.events", "1010", "1;0", "true;false", null, "true")

        ) {}
    }

    public class OutputAllRecognizerEventTokenParser : NamedValueTokenParserList
    {
        public OutputAllRecognizerEventTokenParser(bool includeConnection = true) : base(

            new NamedValueTokenParser(null, "output.all.audio.input.id", "10001", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.recognizer.recognizing.sessionid", "10011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognizing.timestamp", "10011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognizing.result.resultid", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognizing.result.reason", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognizing.result.text", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognizing.result.offset", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognizing.result.duration", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognizing.result.latency", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognizing.result.json", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognizing.result.*.property", "1001011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognizing.recognizer.*.property", "1001111", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.recognizer.recognized.sessionid", "10011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognized.timestamp", "10011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognized.result.resultid", "100001", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognized.result.reason", "100001", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognized.result.text", "100001", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognized.result.itn.text", "1000011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognized.result.lexical.text", "1000011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognized.result.offset", "100001", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognized.result.duration", "100001", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognized.result.latency", "100001", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognized.result.json", "110001;100101;100011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognized.result.*.property", "1001011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognized.recognizer.*.property", "1001111", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.recognizer.canceled.error.code", "100101;100011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.canceled.error.details", "100101;100011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.canceled.error", "10011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.canceled.reason", "10011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.canceled.recognizer.*.property", "1001111", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.recognizer.session.started.recognizer.*.property", "10011011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.session.stopped.recognizer.*.property", "10011011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.session.event.recognizer.*.property", "10011011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.event.recognizer.*.property", "1000111", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.recognizer.session.started.sessionid", "101011;100111;100001", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.session.stopped.sessionid", "101011;100111", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.session.event.sessionid", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.event.sessionid", "10101", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.recognizer.session.started.timestamp", "101011;100111;100001", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.session.stopped.timestamp", "101011;100111", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.session.event.timestamp", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.event.timestamp", "10101", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.recognizer.session.started.events", "101010;100110", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.session.stopped.events", "101010;100110", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.session.events", "10010", "1;0", "true;false", null, "true"),

            !includeConnection ? null : new OutputAllConnectionEventTokenParser(),

            new NamedValueTokenParser(null, "output.all.result.resultid", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.result.reason", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.result.text", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.result.offset", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.result.duration", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.result.latency", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.result.json", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.result.*.property", "10011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.*.property", "10111", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.recognizer.recognizing.events", "10010", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.recognized.events", "10010", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.canceled.events", "10010", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.recognizer.events", "1010", "1;0", "true;false", null, "true"),
            
            new NamedValueTokenParser(null, "output.all.event.sessionid", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.event.timestamp", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.all.events", "111", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.tsv.file.columns", "10001", "1", "@;\t"),
            new NamedValueTokenParser(null, "output.all.tsv.file.has.header", "100111;101011", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.all.file.type", "1011", "1", "tsv;json"),
            new NamedValueTokenParser(null, "output.all.file.name", "1010", "1", "@@")

        ) {}
    }

    public class OutputEachConnectionEventTokenParser : NamedValueTokenParserList
    {
        public OutputEachConnectionEventTokenParser() : base(

            new NamedValueTokenParser(null, "output.each.connection.message.received.path", "110101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.connection.message.received.is.binary.message", "11010111", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.connection.message.received.binary.message.size", "1101011;11010101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.connection.message.received.binary.message", "1101011;1101011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.connection.message.received.is.text.message", "11010111", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.connection.message.received.text.message", "1101010", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.connection.message.received.request.id", "1101011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.connection.message.received.content.type", "1101011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.connection.message.received.*.property", "1101011", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.each.connection.connected.sessionid", "11011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.connection.disconnected.sessionid", "11011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.connection.event.sessionid", "11101", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.each.connection.connected.timestamp", "11011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.connection.disconnected.timestamp", "11011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.connection.event.timestamp", "11101", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.each.connection.connected.event", "11010", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.connection.disconnected.event", "11010", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.connection.message.received.event", "110110", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.connection.event", "1110", "1;0", "true;false", null, "true")

        ) {}
    }

    public class OutputEachRecognizerEventTokenParser : NamedValueTokenParserList
    {
        public OutputEachRecognizerEventTokenParser(bool includeConnection = true) : base(

            new NamedValueTokenParser(null, "output.each.audio.input.id", "11001", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.each.recognizer.recognizing.sessionid", "10011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognizing.timestamp", "10011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognizing.result.resultid", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognizing.result.reason", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognizing.result.text", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognizing.result.offset", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognizing.result.duration", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognizing.result.latency", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognizing.result.json", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognizing.result.*.property", "1001011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognizing.recognizer.*.property", "1001111", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.each.recognizer.recognized.sessionid", "10011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognized.timestamp", "10011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognized.result.resultid", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognized.result.reason", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognized.result.text", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognized.result.itn.text", "1001011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognized.result.lexical.text", "1001011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognized.result.offset", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognized.result.duration", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognized.result.latency", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognized.result.json", "100101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognized.result.*.property", "1001011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognized.recognizer.*.property", "1001111", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.each.recognizer.canceled.error.code", "100101;100011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.canceled.error.details", "100101;100011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.canceled.error", "10011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.canceled.reason", "10011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.canceled.recognizer.*.property", "1001111", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.each.recognizer.session.started.recognizer.*.property", "11011011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.session.stopped.recognizer.*.property", "11011011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.session.event.recognizer.*.property", "11010011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.event.recognizer.*.property", "1100111", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.each.recognizer.session.started.sessionid", "111011;110111", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.session.stopped.sessionid", "111011;110111", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.session.event.sessionid", "110101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.event.sessionid", "11101", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.each.recognizer.session.started.timestamp", "111011;110111", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.session.stopped.timestamp", "111011;110111", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.session.event.timestamp", "110101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.event.timestamp", "11101", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.each.recognizer.session.started.event", "111010;110110", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.session.stopped.event", "111010;110110", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.session.event", "11010", "1;0", "true;false", null, "true"),

            !includeConnection ? null : new OutputEachConnectionEventTokenParser(),

            new NamedValueTokenParser(null, "output.each.result.resultid", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.result.reason", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.result.text", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.result.offset", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.result.duration", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.result.latency", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.result.json", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.result.*.property", "11011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.*.property", "11111", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.each.recognizer.recognizing.event", "11010", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.recognized.event", "11010", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.canceled.event", "11010", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.recognizer.event", "1110", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.each.event.sessionid", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.event.timestamp", "1101", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, "output.each.event", "111", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.each.tsv.file.columns", "11001", "1", "@;\t"),
            new NamedValueTokenParser(null, "output.each.tsv.file.has.header", "110111;111011", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null, "output.each.file.type", "1111", "1", "tsv;json"),
            new NamedValueTokenParser(null, "output.each.file.name", "1110", "1", "@@")

        ) {}
    }
}
