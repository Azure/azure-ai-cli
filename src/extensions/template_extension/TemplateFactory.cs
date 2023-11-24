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
        public static void GenerateTemplateFiles(string templateName)
        {
            var generator = new TemplateGenerator();
            var files = GetTemplateFileNames(templateName, generator);

            var processed = ProcessTemplates(templateName, generator, files);
            foreach (var item in processed)
            {
                var file = item.Key;
                var text = item.Value;
                Console.WriteLine($"```{file}\n{text}\n```");
            }
        }

        private static IEnumerable<string> GetTemplateFileNames(string templateName, TemplateGenerator generator)
        {
            var files = FileHelpers.FindFilesInTemplatePath($"{templateName}/*", null).ToList();
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
                var json = FileHelpers.ReadAllText(jsonFile, Encoding.UTF8);
                foreach (var item in JObject.Parse(json))
                {
                    var name = item.Key;
                    var value = parameters.Keys.Contains(name)
                        ? parameters[name]
                        : item.Value?.ToString();
                    parameters[name] = value!;
                }
            }
            return parameters;
        }

        private static Dictionary<string, string> ProcessTemplates(string templateName, TemplateGenerator generator, IEnumerable<string> files)
        {
            var processed = new Dictionary<string, string>();
            foreach (var file in files)
            {
                var text = FileHelpers.ReadAllText(file, Encoding.UTF8);
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