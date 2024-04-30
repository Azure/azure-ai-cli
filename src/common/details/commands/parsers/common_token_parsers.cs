//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.AI.Details.Common.CLI
{
    public enum FileDirection
    {
        input,
        output
    }

    public class FileNamedValueTokenParser : NamedValueTokenParser
    {
        public FileNamedValueTokenParser(string fullName, FileDirection direction, string requiredParts, string defaultValue = "-") :
            base(null, $"{direction}.{fullName}.file", $"1{requiredParts}0", "1;0", "@@", null, defaultValue)
        {
        }
    }

    public class InputFileNamedValueTokenParser : FileNamedValueTokenParser
    {
        public InputFileNamedValueTokenParser(string fullName, string requiredParts, string defaultValue = "-") :
            base(fullName, FileDirection.input, requiredParts, defaultValue)
        {
        }
    }

    public class InputFileOptionalPrefixNamedValueTokenParser : InputFileNamedValueTokenParser
    {
        public InputFileOptionalPrefixNamedValueTokenParser(string optionalPrefix, string fullName, string fullNameRequiredParts, string defaultValue = "-") :
            base($"{optionalPrefix}.{fullName}", $"{NotRequired(optionalPrefix)}{fullNameRequiredParts}", defaultValue)
        {
        }
    }

    public class OutputFileNamedValueTokenParser : FileNamedValueTokenParser
    {
        public OutputFileNamedValueTokenParser(string fullName, string requiredParts, string defaultValue = "-") :
            base(fullName, FileDirection.output, requiredParts, defaultValue)
        {
        }
    }

    public class OutputFileOptionalPrefixNamedValueTokenParser : OutputFileNamedValueTokenParser
    {
        public OutputFileOptionalPrefixNamedValueTokenParser(string optionalPrefix, string fullName, string fullNameRequiredParts, string defaultValue = "-") :
            base($"{optionalPrefix}.{fullName}", $"{NotRequired(optionalPrefix)}{fullNameRequiredParts}",  defaultValue)
        {
        }
    }

    public class OutputFileRequiredPrefixNamedValueTokenParser : OutputFileNamedValueTokenParser
    {
        public OutputFileRequiredPrefixNamedValueTokenParser(string requiredPrefix, string fullName, string fullNameRequiredParts, string defaultValue = "-") :
            base($"{requiredPrefix}.{fullName}.output.file", $"{Required(requiredPrefix)}{fullNameRequiredParts}", defaultValue)
        {
        }
    }

    public class OutputFileOptionalAndRequiredPrefixNamedValueTokenParser : OutputFileNamedValueTokenParser
    {
        public OutputFileOptionalAndRequiredPrefixNamedValueTokenParser(string optionalPrefix, string requiredPrefix, string fullName, string fullNameRequiredParts, string defaultValue = "-") :
            base($"{optionalPrefix}.{requiredPrefix}.{fullName}", $"{NotRequired(optionalPrefix)}{Required(requiredPrefix)}{fullNameRequiredParts}",  defaultValue)
        {
        }
    }

    public class TrueFalseNamedValueTokenParser : NamedValueTokenParser
    {
        public TrueFalseNamedValueTokenParser(string fullName, string requiredParts, bool defaultValue = true) :
            base(null, fullName, requiredParts, "1;0", "true;false", null, defaultValue ? "true" : "false")
        {
        }
    }

    public class TrueFalseRequiredPrefixNamedValueTokenParser : TrueFalseNamedValueTokenParser
    {
        public TrueFalseRequiredPrefixNamedValueTokenParser(string prefix, string fullName, string requiredParts, bool defaultValue = true) :
            base($"{prefix}.{fullName}", $"{Required(prefix)}{requiredParts}", defaultValue)
        {
            Console.WriteLine($"{FullName}, {RequiredParts}");
        }
    }

    public class CommonNamedValueTokenParsers : NamedValueTokenParserList
    {
        public CommonNamedValueTokenParsers(bool includeKeyAndRegion = true) : base(

            new NamedValueTokenParser("--help",       "--?", "1", "0", null, null, "true", "display.help"),
            new NamedValueTokenParser("--version",       "--v", "1", "0", null, null, "true", "display.version"),
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
            new NamedValueTokenParser(null, $"{prefix}.output.expect.regex", $"{NotRequired(prefix)}010", "1"),
            new NamedValueTokenParser(null, $"{prefix}.output.not.expect.regex", $"{NotRequired(prefix)}0110", "1"),
            new NamedValueTokenParser(null, $"{prefix}.output.auto.expect.regex", $"{NotRequired(prefix)}0110", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null, $"{prefix}.output.ignore.check.failures", $"{NotRequired(prefix)}0111", "1;0", null, null, "true", "x.command.output.ignore.check.failures")
        ) {}
    }

    public class ExpectDiagnosticOutputTokenParser : NamedValueTokenParserList
    {
        public ExpectDiagnosticOutputTokenParser() : this("x.command")
        {
        }

        public ExpectDiagnosticOutputTokenParser(string prefix) : base(
            new NamedValueTokenParser(null, $"{prefix}.diagnostics.log.expect.regex", $"{NotRequired(prefix)}0110", "1"),
            new NamedValueTokenParser(null, $"{prefix}.diagnostics.log.not.expect.regex", $"{NotRequired(prefix)}01110", "1"),
            new NamedValueTokenParser(null, $"{prefix}.diagnostics.log.auto.expect.regex.filter", $"{NotRequired(prefix)}011101", "1", null, null, "true", "x.command.diagnostics.log.auto.expect.regex"),
            new NamedValueTokenParser(null, $"{prefix}.diagnostics.log.auto.expect.regex", $"{NotRequired(prefix)}01110", "1;0", "true;false", null, "true")
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
