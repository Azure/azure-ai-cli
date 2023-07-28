using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    class RunJobCommandParser : CommandParser
    {
        public static bool ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommand("run", runCommandParsers, tokens, values);
        }

        public static bool ParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return ParseCommandValues("run", runCommandParsers, tokens, values);
        }

        #region private data

        private static INamedValueTokenParser[] runCommandParsers = {
            new NamedValueTokenParser(null,           "x.command", "11", "1", "run"),

            new CommonNamedValueTokenParsers(),
            new ExpectConsoleOutputTokenParser("run"),
            new ParallelCommandsTokenParser(),

            new NamedValueTokenParser(null,           "x.command.expand.file.name", "11111", "1"),

            new NamedValueTokenParser("--retry",      "run.retries", "01", "1;0", null, null, "1"),
            new NamedValueTokenParser("--timeout",    "run.timeout", "01", "1"),

            new NamedValueTokenParser(null,           $"run.input.{Program.Name}.pre.command.args", "000110", "1", null, null, Program.Name, "run.input.process"),
            new NamedValueTokenParser(null,           $"run.input.{Program.Name}.post.command.args", "000110;000011", "1", null, null, Program.Name, "run.input.process"),
            new NamedValueTokenParser(null,           $"run.input.{Program.Name}.command", "0001", "1", null, null, Program.Name, "run.input.process"),

            new NamedValueTokenParser(null,           $"run.input.{Program.Name}.pre.job.args", "000110", "1", null, null, Program.Name, "run.input.process"),
            new NamedValueTokenParser(null,           $"run.input.{Program.Name}.post.job.args", "000110;000011", "1", null, null, Program.Name, "run.input.process"),
            new NamedValueTokenParser("--job",        $"run.input.{Program.Name}.job", "0001", "1", null, null, Program.Name, "run.input.process"),
            new NamedValueTokenParser("--jobs",       $"run.input.{Program.Name}.jobs", "0001", "1", null, null, $"run.input.{Program.Name}.job", "x.command.expand.file.name"),

            new NamedValueTokenParser($"--{Program.Name}",       $"run.input.{Program.Name}", "001", "0", null, null, Program.Name, "run.input.process"),

            new NamedValueTokenParser("--cmd",        "run.input.shell.cmd", "0001", "0", null, null, "cmd", "run.input.process"),
            new NamedValueTokenParser("--bash",       "run.input.shell.bash", "0001", "0", null, null, "bash", "run.input.process"),
            new NamedValueTokenParser("--wsl",        "run.input.shell.wsl", "0001", "0", null, null, "wsl", "run.input.process"),
            new NamedValueTokenParser("--process",    "run.input.process", "001", "1"),

            new NamedValueTokenParser(null,           "run.input.pre.script.args", "00110", "1", null, null),
            new NamedValueTokenParser(null,           "run.input.post.script.args", "00110;00011", "1", null, null),
            new NamedValueTokenParser("--script",     "run.input.script", "001", "1"),
            new NamedValueTokenParser("--scripts",    "run.input.scripts", "001", "1", null, null, "run.input.script", "x.command.expand.file.name"),

            new NamedValueTokenParser(null,           "run.input.pre.file.args", "00110", "1", null, null),
            new NamedValueTokenParser(null,           "run.input.post.file.args", "00110;00011", "1", null, null),
            new NamedValueTokenParser("--file",       "run.input.file", "001", "1"),
            new NamedValueTokenParser("--files",      "run.input.files", "001", "1", null, null, "run.input.file", "x.command.expand.file.name"),

            new NamedValueTokenParser(null,           "run.input.pre.line.args", "00110", "1"),
            new NamedValueTokenParser(null,           "run.input.post.line.args", "00110;00011", "1"),
            new NamedValueTokenParser("--line",       "run.input.line", "001", "1"),

            new NamedValueTokenParser(null,           "run.input.pre.item.args", "00110", "1"),
            new NamedValueTokenParser(null,           "run.input.post.item.args", "00110;00011", "1"),
            new NamedValueTokenParser("--item",       "run.input.item", "001", "1"),

            new NamedValueTokenParser(null,           "run.input.pre.args", "0010", "1"),
            new NamedValueTokenParser("--args",       "run.input.post.args", "0001;0010", "1"),
        };

        #endregion
    }
}
