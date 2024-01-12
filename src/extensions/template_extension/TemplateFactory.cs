using Newtonsoft.Json.Linq;
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

namespace Azure.AI.Details.Common.CLI.Extensions.Templates
{
    public class TemplateFactory
    {
        class NameShortLangItem
        {
            public string LongName { get; set; } = string.Empty;
            public string ShortName { get; set; } = string.Empty;
            public string Language { get; set; } = string.Empty;
            public string UniqueName { get; set; } = string.Empty;
        }

        class GroupedLongNameItem
        {
            public string LongName { get; set; } = string.Empty;
            public string ShortName { get; set; } = String.Empty;
            public string Languages { get { return string.Join(", ", Items.OrderBy(x => x.Language).Select(x => x.Language)); } }
            public List<NameShortLangItem> Items { get; set; } = new List<NameShortLangItem>();
        }

        public static bool ListTemplates()
        {
            var grouped = GetGroupedTemplateItems();

            var longNameLabel = "Name";
            var shortNameLabel = "Short Name";
            var languageLabel = "Language";

            var widths = new int[3];
            widths[0] = Math.Max(longNameLabel.Length, grouped.Max(x => x.LongName.Length));
            widths[1] = Math.Max(shortNameLabel.Length, grouped.Max(x => x.ShortName.Length));
            widths[2] = Math.Max(languageLabel.Length, grouped.Max(x => x.Languages.Length));

            Console.WriteLine($"{longNameLabel.PadRight(widths[0])}    {shortNameLabel.PadRight(widths[1])}    {languageLabel.PadRight(widths[2])}");
            Console.WriteLine($"{"-".PadRight(widths[0], '-')}    {"-".PadRight(widths[1], '-')}    {"-".PadRight(widths[2], '-')}");

            for (int i = 0; i < grouped.Count; i++)
            {
                var longName = grouped[i].LongName;
                var shortName = grouped[i].ShortName.Replace('_', '-');
                var languages = grouped[i].Languages;
                Console.WriteLine($"{longName.PadRight(widths[0])}    {shortName.PadRight(widths[1])}    {languages.PadRight(widths[2])}");
            }

            return true;
        }

        public static bool GenerateTemplateFiles(string templateName, string language, string instructions, string outputDirectory, bool quiet, bool verbose)
        {
            var root = FileHelpers.FileNameFromResourceName("templates") + "/";

            var suffix = ProgrammingLanguageToken.GetSuffix(language);
            templateName += suffix;

            var normalizedTemplateName = templateName.Replace('-', '_');
            var generator = new TemplateGenerator();

            var files = GetTemplateFileNames(normalizedTemplateName, generator).ToList();
            if (files.Count() == 0)
            {
                normalizedTemplateName = normalizedTemplateName.Replace(" ", "_");
                files = GetTemplateFileNames(normalizedTemplateName, generator).ToList();
                if (files.Count() == 0)
                {
                    return false;
                }
            }

            outputDirectory = PathHelpers.NormalizePath(outputDirectory);
            var message = templateName != outputDirectory
                ? $"Generating '{templateName}' in '{outputDirectory}' ({files.Count()} files)..."
                : $"Generating '{templateName}' ({files.Count()} files)...";
            if (!quiet) Console.WriteLine($"{message}\n");

            files.Sort();
            var generated = ProcessTemplates(normalizedTemplateName, generator, files, outputDirectory);
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

        private static List<GroupedLongNameItem> GetGroupedTemplateItems()
        {
            var root = FileHelpers.FileNameFromResourceName("templates") + "/";
            var files = FileHelpers.FindFilesInTemplatePath("*", null).ToList();

            var uniqueNames = files
                .Select(x => x.Replace(root, string.Empty))
                .Where(x => x.EndsWith("_.json"))
                .Select(x => x.Split(new char[] { '\\', '/' }).FirstOrDefault())
                .Where(x => x != null)
                .Select(x => x!)
                .Distinct()
                .ToList();
            uniqueNames.Sort();

            var templates = new List<NameShortLangItem>();
            foreach (var uniqueName in uniqueNames)
            {
                var parameters = GetParameters(uniqueName);
                var longName = parameters["_Name"];
                var shortName = parameters["_Short"];
                var language = parameters["_Language"];

                templates.Add(new NameShortLangItem()
                {
                    LongName = longName,
                    ShortName = shortName,
                    Language = language,
                    UniqueName = uniqueName
                });
            }

            templates.Add(new NameShortLangItem()
            {
                LongName = "Environment Variables",
                ShortName = ".env",
                Language = string.Empty,
                UniqueName = ".env"
            });

            var grouped = templates
                .GroupBy(x => x.LongName)
                .Select(x => new GroupedLongNameItem()
                {
                    LongName = x.Key,
                    ShortName = x.First().ShortName,
                    Items = x.ToList()
                })
                .OrderBy(x => x.ShortName)
                .ToList();
            return grouped;
        }
        private static IEnumerable<string> GetTemplateFileNames(string templateName, TemplateGenerator generator)
        {
            var files = FileHelpers.FindFilesInTemplatePath($"{templateName}/*", null).ToList();
            if (files.Count() == 0) return files;
            
            var parameters = new Dictionary<string, string>();

            var assembly = typeof(TemplateFactory).Assembly;
            var assemblyPath = Path.GetDirectoryName(assembly.Location)!;
            parameters.Add("AICLIExtensionReferencePath", assemblyPath);

            foreach (var item in UpdateParameters(files, parameters))
            {
                var name = item.Key;
                var value = item.Value;
                generator.AddParameter(string.Empty, string.Empty, name, value);
            }

            return files;
        }

        private static Dictionary<string, string> UpdateParameters(List<string> files, Dictionary<string, string> parameters)
        {
            var jsonFile = files.Where(x => x.EndsWith("_.json")).FirstOrDefault();
            if (jsonFile != null)
            {
                files.Remove(jsonFile);
                UpdateParameters(jsonFile, parameters);
            }
            return parameters;
        }

        private static void UpdateParameters(string? jsonFile, Dictionary<string, string> parameters)
        {
            var json = FileHelpers.ReadAllText(jsonFile, new UTF8Encoding(false));
            foreach (var item in JObject.Parse(json))
            {
                var name = item.Key;
                var value = parameters.Keys.Contains(name)
                    ? parameters[name]
                    : item.Value?.ToString();
                parameters[name] = value!;
            }
        }

        private static Dictionary<string, string> GetParameters(string templateName)
        {
            var parameters = new Dictionary<string, string>();

            var files = FileHelpers.FindFilesInTemplatePath($"{templateName}/_.json", null).ToList();
            if (files.Count() == 0) return parameters;

            var jsonFile = files.FirstOrDefault();
            if (jsonFile != null)
            {
                UpdateParameters(jsonFile, parameters);
            }

            return parameters;
        }

        private static IEnumerable<string> ProcessTemplates(string templateName, TemplateGenerator generator, IEnumerable<string> files, string outputDirectory)
        {
            var root = FileHelpers.FileNameFromResourceName("templates") + "/";

            foreach (var file in files)
            {
                if (!file.StartsWith(root)) throw new Exception("Invalid file name");
                var outputFile = file.Substring(root.Length + templateName.Length + 1);
                var outputFileWithPath = PathHelpers.Combine(outputDirectory, outputFile);

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