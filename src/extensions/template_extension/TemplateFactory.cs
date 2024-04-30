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
        public class Item
        {
            public string LongName { get; set; } = string.Empty;
            public string ShortName { get; set; } = string.Empty;
            public string Language { get; set; } = string.Empty;
            public string UniqueName { get; set; } = string.Empty;
        }

        public class Group
        {
            public string LongName { get; set; } = string.Empty;
            public string ShortName { get; set; } = String.Empty;
            public string Languages { get { return string.Join(", ", Items.OrderBy(x => x.Language).Select(x => x.Language)); } }
            public List<Item> Items { get; set; } = new List<Item>();
        }

        public static bool ListTemplates(string? templateFilter, string? languageFilter)
        {
            var groups = GetFilteredTemplateGroups(templateFilter, languageFilter);
            if (groups.Count == 0)
            {
                ConsoleHelpers.WriteLineError($"No matching templates found\n");
                groups = GetTemplateGroups();
            }
            if (InOutPipeServer.IsInOutPipeServer)
            {
                string json = JsonSerializer.Serialize(new { groups });
                Console.WriteLine(json);
                return true;
            }
            var longNameLabel = "Name";
            var shortNameLabel = "Short Name";
            var languageLabel = "Language";

            var widths = new int[3];
            widths[0] = Math.Max(longNameLabel.Length, groups.Max(x => x.LongName.Length));
            widths[1] = Math.Max(shortNameLabel.Length, groups.Max(x => x.ShortName.Length));
            widths[2] = Math.Max(languageLabel.Length, groups.Max(x => x.Languages.Length));

            var hideLongName = !Console.IsOutputRedirected && Screen.GetRightColumn() < widths.Sum() + 4 * 2 + 1;

            if (!hideLongName) Console.Write($"{longNameLabel.PadRight(widths[0])}    ");
            Console.WriteLine($"{shortNameLabel.PadRight(widths[1])}    {languageLabel.PadRight(widths[2])}");

            if (!hideLongName) Console.Write($"{"-".PadRight(widths[0], '-')}    ");
            Console.WriteLine($"{"-".PadRight(widths[1], '-')}    {"-".PadRight(widths[2], '-')}");

            for (int i = 0; i < groups.Count; i++)
            {
                var longName = groups[i].LongName;
                var shortName = groups[i].ShortName.Replace('_', '-');
                var languages = groups[i].Languages;

                if (!hideLongName) Console.Write($"{longName.PadRight(widths[0])}    ");
                Console.WriteLine($"{shortName.PadRight(widths[1])}    {languages.PadRight(widths[2])}");
            }

            return true;
        }

        public static object? GenerateTemplateFiles(string templateName, string language, string instructions, string outputDirectory, bool quiet, bool verbose)
        {
            var groups = GetTemplateGroups();
            var groupFound = groups.Where(x => x.ShortName == templateName).FirstOrDefault()
                          ?? groups.Where(x => x.LongName == templateName).FirstOrDefault();
            if (groupFound == null) return null;

            var templateFound = !string.IsNullOrEmpty(language)
                ? groupFound.Items.Where(x => x.Language == language).FirstOrDefault()
                : groupFound.Items.Count != 1
                    ? groupFound.Items.Where(x => x.Language == string.Empty).FirstOrDefault()
                    : groupFound.Items.FirstOrDefault();
            if (templateFound == null) return groupFound;

            templateName = templateFound.UniqueName;

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

        private static List<Group> GetTemplateGroups()
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

            var templates = new List<Item>();
            foreach (var uniqueName in uniqueNames)
            {
                var parameters = GetParameters(uniqueName);
                var longName = parameters["_LongName"];
                var shortName = parameters["_ShortName"];
                var language = parameters["_Language"];

                templates.Add(new Item()
                {
                    LongName = longName,
                    ShortName = shortName,
                    Language = language,
                    UniqueName = uniqueName
                });
            }

            templates.Add(new Item()
            {
                LongName = "Environment Variables",
                ShortName = ".env",
                Language = string.Empty,
                UniqueName = ".env"
            });

            var grouped = templates
                .GroupBy(x => x.LongName)
                .Select(x => new Group()
                {
                    LongName = x.Key,
                    ShortName = x.First().ShortName,
                    Items = x.ToList()
                })
                .OrderBy(x => x.ShortName)
                .ToList();
            return grouped;
        }

        private static List<Group> GetFilteredTemplateGroups(string? templateFilter, string? languageFilter)
        {
            var groups = GetTemplateGroups();
            if (string.IsNullOrEmpty(templateFilter) && string.IsNullOrEmpty(languageFilter)) return groups;

            var filtered = groups
                .Where(x => string.IsNullOrEmpty(templateFilter) || x.ShortName.Contains(templateFilter) || x.LongName.Contains(templateFilter))
                .Where(x => string.IsNullOrEmpty(languageFilter) || x.Languages.Split(", ").Contains(languageFilter) || x.Languages == string.Empty)
                .ToList();

            if (filtered.Count > 0 && !string.IsNullOrEmpty(languageFilter))
            {
                groups.Clear();
                foreach (var item in filtered)
                {
                    groups.Add(new Group()
                    {
                        LongName = item.LongName,
                        ShortName = item.ShortName,
                        Items = item.Items.Where(x => x.Language == languageFilter).ToList()
                    });
                }
                return groups;
            }

            return filtered;
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

        private static void UpdateParameters(string jsonFile, Dictionary<string, string> parameters)
        {
            var json = FileHelpers.ReadAllText(jsonFile, new UTF8Encoding(false));
            foreach (var item in JsonDocument.Parse(json).RootElement.EnumerateObject())
            {
                var name = item.Name;
                var value = parameters.ContainsKey(name)
                    ? parameters[name]
                    : item.Value.ToString();
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