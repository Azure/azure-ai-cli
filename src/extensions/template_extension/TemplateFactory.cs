//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Mono.TextTemplating;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Azure.AI.Details.Common.CLI;
using Azure.AI.Details.Common.CLI.ConsoleGui;
using System.Text.Json;

namespace Azure.AI.Details.Common.CLI.Extensions.Templates
{
    public class TemplateFactory
    {
        public static bool ListTemplates(string? templateFilter, string? languageFilter)
        {
            var list = new DevNewTemplateGroupList();
            if (!list.ApplyFilter(null, templateFilter, languageFilter))
            {
                ConsoleHelpers.WriteLineError($"No matching templates found\n");
            }

            var longNameLabel = "Name";
            var shortNameLabel = "Short Name";
            var languageLabel = "Language";

            var widths = new int[3];
            widths[0] = Math.Max(longNameLabel.Length, list.Groups.Max(x => x.LongName.Length));
            widths[1] = Math.Max(shortNameLabel.Length, list.Groups.Max(x => x.ShortName.Length));
            widths[2] = Math.Max(languageLabel.Length, list.Groups.Max(x => x.Languages.Length));

            var hideLongName = !Console.IsOutputRedirected && Screen.GetRightColumn() < widths.Sum() + 4 * 2 + 1;

            if (!hideLongName) Console.Write($"{longNameLabel.PadRight(widths[0])}    ");
            Console.WriteLine($"{shortNameLabel.PadRight(widths[1])}    {languageLabel.PadRight(widths[2])}");

            if (!hideLongName) Console.Write($"{"-".PadRight(widths[0], '-')}    ");
            Console.WriteLine($"{"-".PadRight(widths[1], '-')}    {"-".PadRight(widths[2], '-')}");

            for (int i = 0; i < list.Groups.Count; i++)
            {
                var longName = list.Groups[i].LongName;
                var shortName = list.Groups[i].ShortName.Replace('_', '-');
                var languages = list.Groups[i].Languages;

                if (!hideLongName) Console.Write($"{longName.PadRight(widths[0])}    ");
                Console.WriteLine($"{shortName.PadRight(widths[1])}    {languages.PadRight(widths[2])}");
            }

            return true;
        }

        public static object? GenerateTemplateFiles(string templateName, string language, string instructions, string outputDirectory, bool quiet, bool verbose)
        {
            var list = new DevNewTemplateGroupList();
            var filterApplied = list.ApplyFilter(templateName, null, null)
                             || list.ApplyFilter(null, templateName, null);
            if (!filterApplied) return null;

            var groupFound = list.Groups.FirstOrDefault();
            if (groupFound == null) return null;

            var templateFound = !string.IsNullOrEmpty(language)
                ? groupFound.Items.Where(x => x.Language == language).FirstOrDefault()
                : groupFound.Items.Count != 1
                    ? groupFound.Items.Where(x => x.Language == string.Empty).FirstOrDefault()
                    : groupFound.Items.FirstOrDefault();
            if (templateFound == null) return groupFound;

            var normalizedTemplateName = templateFound.UniqueName.Replace('-', '_');
            var files = FileHelpers.FindFilesInTemplatePath($"{normalizedTemplateName}/*", null).ToList();

            files.Sort();
            files.RemoveAll(x => x.EndsWith("_.json"));
            if (files.Count() == 0) return null;

            var parameters = new Dictionary<string, string>(templateFound.Parameters);
            parameters["AICLIExtensionReferencePath"] = Path.GetDirectoryName(typeof(TemplateFactory).Assembly.Location)!;

            outputDirectory = PathHelpers.NormalizePath(outputDirectory);
            var message = templateName != outputDirectory
                ? $"Generating '{templateName}' in '{outputDirectory}' ({files.Count()} files)..."
                : $"Generating '{templateName}' ({files.Count()} files)...";
            if (!quiet) Console.WriteLine($"{message}\n");

            var generated = ProcessTemplates(normalizedTemplateName, files, parameters, outputDirectory);
            foreach (var item in generated)
            {
                var file = item.Replace(outputDirectory, string.Empty).Trim('\\', '/');
                if (!quiet) Console.WriteLine($"  {file}");
            }

            if (!quiet)
            {
                Console.WriteLine();
                Console.WriteLine($"\r{message} DONE!\n");
            }

            var instructionsOk = !string.IsNullOrEmpty(instructions);
            var promptsOk = File.Exists($"{outputDirectory}/.ai/system.md") && File.Exists($"{outputDirectory}/.ai/prompt.md");

            if (instructionsOk && promptsOk)
            {
                var saved = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(outputDirectory);

                Console.WriteLine("Applying instructions...\n");
                var exitCode = Program.RunInternal("chat",
                    "--quiet", "true",
                    "--built-in-functions", "true",
                    "--index", "@none",
                    "--system", "@system.md",
                    "--user", "@prompt.md",
                    "--var", "scenario", instructions
                );
                Directory.SetCurrentDirectory(saved);

                if (exitCode != 0)
                {
                    Console.WriteLine($"ERROR: chat failed with exit code {exitCode}");
                    return false;
                }
            }

            return true;
        }

        private static IEnumerable<string> ProcessTemplates(string templateName, IEnumerable<string> files, Dictionary<string, string> parameters, string outputDirectory)
        {
            var generator = new TemplateGenerator();
            foreach (var item in parameters)
            {
                var name = item.Key;
                var value = item.Value;
                generator.AddParameter(string.Empty, string.Empty, name, value);
            }

            var root = FileHelpers.FileNameFromResourceName("templates") + "/";

            foreach (var file in files)
            {
                if (!file.StartsWith(root)) throw new Exception("Invalid file name");
                var outputFile = file.Substring(root.Length + templateName.Length + 1);
                var outputFileWithPath = PathHelpers.Combine(outputDirectory, outputFile)!;

                var isBinary = file.EndsWith(".png") || file.EndsWith(".ico");
                if (!isBinary)
                {
                    ProcessTemplate(generator, file, outputFileWithPath, out var generatedFileName, out var generatedContent);
                    FileHelpers.WriteAllText(generatedFileName, generatedContent, new UTF8Encoding(false));
                    yield return generatedFileName;
                }
                else
                {
                    var bytes = FileHelpers.ReadAllBytes(file);
                    FileHelpers.WriteAllBytes(outputFileWithPath, bytes);
                    yield return outputFileWithPath;
                }
            }
        }

        private static void ProcessTemplate(TemplateGenerator generator, string file, string outputFile, out string generatedFileName, out string generatedContent)
        {
            var text = FileHelpers.ReadAllText(file, new UTF8Encoding(false));
            if (Program.Debug) Console.WriteLine($"```{file}\n{text}\n```");

            var parsed = generator.ParseTemplate(file, text);
            var settings = TemplatingEngine.GetSettings(generator, parsed);
            settings.CompilerOptions = "-nullable:enable";

            (generatedFileName, generatedContent) = generator.ProcessTemplateAsync(parsed, file, text, outputFile, settings).Result;
            if (Program.Debug) Console.WriteLine($"```{generatedFileName}\n{generatedContent}\n```");
        }
    }
}