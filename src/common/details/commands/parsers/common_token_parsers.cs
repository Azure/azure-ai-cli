//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.AI.Details.Common.CLI
{
    public class CommonNamedValueTokenParsers : NamedValueTokenParserList
    {
        public CommonNamedValueTokenParsers(bool includeKeyAndRegion = true) : base(

            new NamedValueTokenParser("--help",       "--?", "1", "0", null, null, "true", "display.help"),
            new NamedValueTokenParser("--cls",        "x.cls", "01", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser("--pause",      "x.pause", "01", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser("--quiet",      "x.quiet", "01", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser("--verbose",    "x.verbose", "01", "1;0", "true;false", null, "true"),

            new NamedValueTokenParser(null,                 "x.input.path", "001", "1"),
            new NamedValueTokenParser(null,                 "x.output.path", "011", "1"),
            new NamedValueTokenParser(null,                 "x.run.time", "111", "1"),

            new NamedValueTokenParser("--save",             "x.command.save.as.file", "00100", "1"),

            new NamedValueTokenParser(null,                 "x.command.zip.target", "0011", "1;0", "webjob", null, "webjob"),
            new NamedValueTokenParser("--zip",              "x.command.zip.as.file", "00100", "1"),
            new NamedValueTokenParser(null,                 "output.zip.file", "110", "1"),

            new NamedValueTokenParser("--max",              "x.command.max", "001", "1"),
            new NamedValueTokenParser("--repeat",           "x.command.repeat", "001", "1;0", null, null, "10"),

            new NamedValueTokenParser(null,                 "check.result.jmes", "110", "1"),
            new ParallelCommandsTokenParser(),
            new ReplaceForEachTokenParser(),
            new ForEachTokenParser()
        )
        {
            if (includeKeyAndRegion)
            {
                Add(new NamedValueTokenParser("--key",              "service.config.key", "001", "1"));
                Add(new NamedValueTokenParser("--region",           "service.config.region", "001", "1"));
            }

        }
    }

    public class ExpectConsoleOutputTokenParser : NamedValueTokenParserList
    {
        public ExpectConsoleOutputTokenParser() : this("x.command")
        {
        }

        public ExpectConsoleOutputTokenParser(string prefix) : base(
            new NamedValueTokenParser(null, $"{prefix}.output.expect", $"{NotRequired(prefix)}01", "1"),
            new NamedValueTokenParser(null, $"{prefix}.output.not.expect", $"{NotRequired(prefix)}011", "1"),
            new NamedValueTokenParser(null, $"{prefix}.output.auto.expect", $"{NotRequired(prefix)}011", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, $"{prefix}.output.ignore.check.failures", $"{NotRequired(prefix)}0111", "1;0", null, null, "true", "x.command.output.ignore.check.failures")
        ) {}
    }

    public class ExpectDiagnosticOutputTokenParser : NamedValueTokenParserList
    {
        public ExpectDiagnosticOutputTokenParser() : this("x.command")
        {
        }

        public ExpectDiagnosticOutputTokenParser(string prefix) : base(
            new NamedValueTokenParser(null, $"{prefix}.diagnostics.log.expect", $"{NotRequired(prefix)}011", "1"),
            new NamedValueTokenParser(null, $"{prefix}.diagnostics.log.not.expect", $"{NotRequired(prefix)}0111", "1"),
            new NamedValueTokenParser(null, $"{prefix}.diagnostics.log.auto.expect.filter", $"{NotRequired(prefix)}01111", "1", null, null, "true", "x.command.diagnostics.log.auto.expect"),
            new NamedValueTokenParser(null, $"{prefix}.diagnostics.log.auto.expect", $"{NotRequired(prefix)}0111", "1;0", "true;false", null, "true")
        ) {}
    }

    public class ExpectOutputTokenParser : NamedValueTokenParserList
    {
        public ExpectOutputTokenParser() : this("x.command")
        {
        }

        public ExpectOutputTokenParser(string prefix) : base(
            new ExpectConsoleOutputTokenParser(prefix),
            new ExpectDiagnosticOutputTokenParser(prefix)
        ) {}
    }

    public class ParallelThreadTokenParser : NamedValueTokenParserList
    {
        public ParallelThreadTokenParser() : base(
            new NamedValueTokenParser("--threads", "x.command.parallel.thread.count", "00011", "1"),
            new NamedValueTokenParser(null, "x.command.parallel.ramp.threads.every", "000110", "1")
        ) {}
    }

    public class ParallelProcessTokenParser : NamedValueTokenParserList
    {
        public ParallelProcessTokenParser() : base(
            new NamedValueTokenParser("--processes", "x.command.parallel.process.count", "00011", "1"),
            new NamedValueTokenParser(null, "x.command.parallel.ramp.processes.every", "000110", "1")
        ) {}
    }

    public class ParallelCommandsTokenParser : NamedValueTokenParserList
    {
        public ParallelCommandsTokenParser() : base(
            new ParallelThreadTokenParser(),
            new ParallelProcessTokenParser()
        ) {}
    }

    public class DiagnosticLogTokenParser : NamedValueTokenParserList
    {
        public DiagnosticLogTokenParser() : base(
            new NamedValueTokenParser(null, "diagnostics.config.log.file", "0011;0010", "1")
        ) {}
    }
}
