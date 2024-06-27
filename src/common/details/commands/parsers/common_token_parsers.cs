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

    public class CommonNamedValueTokenParsers : NamedValueTokenParserList
    {
        public CommonNamedValueTokenParsers(bool includeKeyAndRegion = true) : base(

            new PinnedNamedValueTokenParser("--help", "--?", "1", "true", "display.help"),
            new PinnedNamedValueTokenParser("--version", "--v", "1", "true", "display.version"),
            new TrueFalseNamedValueTokenParser("--cls", "x.cls", "01"),
            new TrueFalseNamedValueTokenParser("--pause", "x.pause", "01"),
            new TrueFalseNamedValueTokenParser("--quiet", "x.quiet", "01"),
            new TrueFalseNamedValueTokenParser("--verbose", "x.verbose", "01"),

            new Any1ValueNamedValueTokenParser(null, "x.input.path", "001"),
            new Any1ValueNamedValueTokenParser(null, "x.output.path", "011"),
            new Any1ValueNamedValueTokenParser(null, "x.run.time", "111"),

            new Any1ValueNamedValueTokenParser("--save", "x.command.save.as.file", "00100"),

            new NamedValueTokenParser(null, "x.command.zip.target", "0011", "1;0", "webjob", null, "webjob"),
            new Any1ValueNamedValueTokenParser("--zip", "x.command.zip.as.file", "00100"),
            new Any1ValueNamedValueTokenParser(null, "output.zip.file", "110"),

            new Any1ValueNamedValueTokenParser("--max", "x.command.max", "001"),
            new OptionalWithDefaultNamedValueTokenParser("--repeat", "x.command.repeat", "001", "10"),

            new Any1ValueNamedValueTokenParser(null, "check.result.jmes", "110"),
            new ParallelCommandsTokenParser(),
            new ReplaceForEachTokenParser(),
            new ForEachTokenParser()
        )
        {
            if (includeKeyAndRegion)
            {
                Add(new Any1ValueNamedValueTokenParser("--key", "service.config.key", "001"));
                Add(new Any1ValueNamedValueTokenParser("--region", "service.config.region", "001"));
            }

        }
    }

    public class ExpectConsoleOutputTokenParser : NamedValueTokenParserList
    {
        public ExpectConsoleOutputTokenParser() : this("x.command")
        {
        }

        public ExpectConsoleOutputTokenParser(string prefix) : base(
            new Any1ValueNamedValueTokenParser(null, $"{prefix}.output.expect.regex", $"{NotRequired(prefix)}010"),
            new Any1ValueNamedValueTokenParser(null, $"{prefix}.output.not.expect.regex", $"{NotRequired(prefix)}0110"),
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
            new Any1ValueNamedValueTokenParser(null, $"{prefix}.diagnostics.log.expect.regex", $"{NotRequired(prefix)}0110"),
            new Any1ValueNamedValueTokenParser(null, $"{prefix}.diagnostics.log.not.expect.regex", $"{NotRequired(prefix)}01110"),
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
            new Any1ValueNamedValueTokenParser("--threads", "x.command.parallel.thread.count", "00011"),
            new Any1ValueNamedValueTokenParser(null, "x.command.parallel.ramp.threads.every", "000110")
        ) {}
    }

    public class ParallelProcessTokenParser : NamedValueTokenParserList
    {
        public ParallelProcessTokenParser() : base(
            new Any1ValueNamedValueTokenParser("--processes", "x.command.parallel.process.count", "00011"),
            new Any1ValueNamedValueTokenParser(null, "x.command.parallel.ramp.processes.every", "000110")
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
            new Any1ValueNamedValueTokenParser(null, "diagnostics.config.log.file", "0011;0010")
        ) {}
    }
}
