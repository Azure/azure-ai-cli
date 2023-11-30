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
        public static bool ListTemplates()
        {
            var root = FileHelpers.FileNameFromResourceName("templates") + "/";

            var templateShortNames = FileHelpers.FindFilesInTemplatePath("*", null)
                .Select(x => x.Replace(root, string.Empty))
                .Select(x => x.Split('.').FirstOrDefault())
                .Where(x => x != null)
                .Select(x => x!)
                .Distinct()
                .ToList();
            templateShortNames.Sort();

            var templateLongNames = new List<string>();
            var languages = new List<string>();
            foreach (var item in templateShortNames)
            {
                var parameters = GetParameters(item);
                var longName = parameters["_Name"];
                var language = parameters["_Language"];

                templateLongNames.Add(longName);
                languages.Add(language);
            }

            templateShortNames.Insert(0, ".env");
            templateLongNames.Insert(0, "Environment Variables");
            languages.Insert(0, "");

            var longNameLabel = "Name";
            var shortNameLabel = "Short Name";
            var languageLabel = "Language";

            var widths = new int[3];
            widths[0] = Math.Max(longNameLabel.Length, templateLongNames.Max(x => x.Length));
            widths[1] = Math.Max(shortNameLabel.Length, templateShortNames.Max(x => x.Length));
            widths[2] = Math.Max(languageLabel.Length, languages.Max(x => x.Length));

            Console.WriteLine($"{longNameLabel.PadRight(widths[0])}    {shortNameLabel.PadRight(widths[1])}    {languageLabel.PadRight(widths[2])}");
            Console.WriteLine($"{"-".PadRight(widths[0], '-')}    {"-".PadRight(widths[1], '-')}    {"-".PadRight(widths[2], '-')}");

            for (int i = 0; i < templateShortNames.Count; i++)
            {
                var longName = templateLongNames[i];
                var shortName = templateShortNames[i].Replace('_', '-');
                var language = languages[i];
                Console.WriteLine($"{longName.PadRight(widths[0])}    {shortName.PadRight(widths[1])}    {language.PadRight(widths[2])}");
            }

            return true;
        }

        public static bool GenerateTemplateFiles(string templateName, string outputDirectory)
        {
            templateName = templateName.Replace('-', '_');
            var generator = new TemplateGenerator();
            
            var files = GetTemplateFileNames(templateName, generator);
            if (files.Count() == 0)
            {
                templateName = templateName.Replace(" ", "_");
                files = GetTemplateFileNames(templateName, generator);
                if (files.Count() == 0)
                {
                    return false;
                }
            }

            var processed = ProcessTemplates(templateName, generator, files);
            foreach (var item in processed)
            {
                var file = item.Key;
                var text = item.Value;
                Console.WriteLine($"FILE: {file}:\n```\n{text}\n```");

                FileHelpers.WriteAllText(PathHelpers.Combine(outputDirectory, file), text, new UTF8Encoding(false));
                Console.WriteLine();
            }

            return true;
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

        private static Dictionary<string, string> ProcessTemplates(string templateName, TemplateGenerator generator, IEnumerable<string> files)
        {
            var processed = new Dictionary<string, string>();
            foreach (var file in files)
            {
                var text = FileHelpers.ReadAllText(file, new UTF8Encoding(false));
                if (Program.Debug) Console.WriteLine($"```{file}\n{text}\n```");

                var i = file.IndexOf(templateName);
                var outputFile = file.Substring(i + templateName.Length + 1);

                var parsed = generator.ParseTemplate(file, text);
                var settings = TemplatingEngine.GetSettings(generator, parsed);
                settings.CompilerOptions = "-nullable:enable";

                (string generatedFileName, string generatedContent) = generator.ProcessTemplateAsync(parsed, file, text, outputFile, settings).Result;
                if (Program.Debug) Console.WriteLine($"```{generatedFileName}\n{generatedContent}\n```");

                processed.Add(generatedFileName, generatedContent);
            }

            return processed;
        }
    }
}