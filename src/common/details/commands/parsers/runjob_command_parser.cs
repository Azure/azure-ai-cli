using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    public class RunJobCommandParser : CommandParser
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
            new RequiredValidValueNamedValueTokenParser(null, "x.command", "11", "run"),

            new CommonNamedValueTokenParsers(),
            new ExpectConsoleOutputTokenParser("run"),
            new ParallelCommandsTokenParser(),

            new ExpandFileNameNamedValueTokenParser(),

            new OptionalWithDefaultNamedValueTokenParser("--retry", "run.retries", "01", "1"),
            new Any1ValueNamedValueTokenParser("--timeout", "run.timeout", "01"),

            new Any1PinnedNamedValueTokenParser(null, $"run.input.{Program.Name}.pre.command.args", "000110", Program.Name, "run.input.process"),
            new Any1PinnedNamedValueTokenParser(null, $"run.input.{Program.Name}.post.command.args", "000110;000011", Program.Name, "run.input.process"),
            new Any1PinnedNamedValueTokenParser(null, $"run.input.{Program.Name}.command", "0001", Program.Name, "run.input.process"),

            new Any1PinnedNamedValueTokenParser(null, $"run.input.{Program.Name}.pre.job.args", "000110", Program.Name, "run.input.process"),
            new Any1PinnedNamedValueTokenParser(null, $"run.input.{Program.Name}.post.job.args", "000110;000011", Program.Name, "run.input.process"),
            new Any1PinnedNamedValueTokenParser("--job", $"run.input.{Program.Name}.job", "0001", Program.Name, "run.input.process"),
            new ExpandFileNameNamedValueTokenParser("--jobs", $"run.input.{Program.Name}.jobs", "0001", $"run.input.{Program.Name}.job"),

            new PinnedNamedValueTokenParser($"--{Program.Name}", $"run.input.{Program.Name}", "001", Program.Name, "run.input.process"),

            new PinnedNamedValueTokenParser("--cmd", "run.input.shell.cmd", "0001", "cmd", "run.input.process"),
            new PinnedNamedValueTokenParser("--bash", "run.input.shell.bash", "0001", "bash", "run.input.process"),
            new PinnedNamedValueTokenParser("--wsl", "run.input.shell.wsl", "0001", "wsl", "run.input.process"),
            new Any1ValueNamedValueTokenParser("--process", "run.input.process", "001"),

            new Any1ValueNamedValueTokenParser(null, "run.input.pre.script.args", "00110"),
            new Any1ValueNamedValueTokenParser(null, "run.input.post.script.args", "00110;00011"),
            new Any1ValueNamedValueTokenParser("--script", "run.input.script", "001"),
            new ExpandFileNameNamedValueTokenParser("--scripts", "run.input.scripts", "001", "run.input.script"),

            new Any1ValueNamedValueTokenParser(null, "run.input.pre.file.args", "00110"),
            new Any1ValueNamedValueTokenParser(null, "run.input.post.file.args", "00110;00011"),
            new Any1ValueNamedValueTokenParser("--file", "run.input.file", "001"),
            new ExpandFileNameNamedValueTokenParser("--files", "run.input.files", "001", "run.input.file"),

            new Any1ValueNamedValueTokenParser(null, "run.input.pre.line.args", "00110"),
            new Any1ValueNamedValueTokenParser(null, "run.input.post.line.args", "00110;00011"),
            new Any1ValueNamedValueTokenParser("--line", "run.input.line", "001"),

            new Any1ValueNamedValueTokenParser(null, "run.input.pre.item.args", "00110"),
            new Any1ValueNamedValueTokenParser(null, "run.input.post.item.args", "00110;00011"),
            new Any1ValueNamedValueTokenParser("--item", "run.input.item", "001"),

            new Any1ValueNamedValueTokenParser(null, "run.input.pre.args", "0010"),
            new Any1ValueNamedValueTokenParser("--args", "run.input.post.args", "0001;0010"),
        };

        #endregion
    }
}
