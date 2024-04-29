//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Text.Json;
using System.Text;

namespace Azure.AI.Details.Common.CLI.Extensions.Templates
{
    public class DevNewTemplateGroupList
    {
        public DevNewTemplateGroupList()
        {
            Groups = GetTemplateGroups();
        }

        public List<DevNewTemplateGroup> Groups { get; private set; }

        public bool ApplyFilter(string? templateName, string? templateFilter, string? languageFilter)
        {
            var groups = GetFilteredGroups(templateName, templateFilter, languageFilter);
            if (groups.Count == 0) return false;

            Groups = groups;
            return true;
        }

        private static List<DevNewTemplateGroup> GetTemplateGroups()
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

            var templates = new List<DevNewTemplateItem>();
            foreach (var uniqueName in uniqueNames)
            {
                var parameters = GetParameters(uniqueName);
                var longName = parameters["_LongName"];
                var shortName = parameters["_ShortName"];
                var language = parameters["_Language"];

                templates.Add(new DevNewTemplateItem()
                {
                    LongName = longName,
                    ShortName = shortName,
                    Language = language,
                    UniqueName = uniqueName,
                    Parameters = parameters
                });
            }

            templates.Add(new DevNewTemplateItem()
            {
                LongName = "Environment Variables",
                ShortName = ".env",
                Language = string.Empty,
                UniqueName = ".env"
            });

            var grouped = templates
                .GroupBy(x => x.LongName)
                .Select(x => new DevNewTemplateGroup()
                {
                    LongName = x.Key,
                    ShortName = x.First().ShortName,
                    Items = x.ToList()
                })
                .OrderBy(x => x.ShortName)
                .ToList();
            return grouped;
        }

        private List<DevNewTemplateGroup> GetFilteredGroups(string? templateName, string? templateFilter, string? languageFilter)
        {
            if (string.IsNullOrEmpty(templateName) && string.IsNullOrEmpty(templateFilter) && string.IsNullOrEmpty(languageFilter)) return Groups;

            var filtered = Groups
                .Where(x => string.IsNullOrEmpty(templateName) || x.ShortName == templateName || x.LongName == templateName)
                .Where(x => string.IsNullOrEmpty(templateFilter) || x.ShortName.Contains(templateFilter) || x.LongName.Contains(templateFilter))
                .Where(x => string.IsNullOrEmpty(languageFilter) || x.Languages.Split(", ").Contains(languageFilter) || x.Languages == string.Empty)
                .ToList();

            if (filtered.Count > 0 && !string.IsNullOrEmpty(languageFilter))
            {
                var updated = new List<DevNewTemplateGroup>();
                foreach (var item in filtered)
                {
                    updated.Add(new DevNewTemplateGroup()
                    {
                        LongName = item.LongName,
                        ShortName = item.ShortName,
                        Items = item.Items.Where(x => x.Language == languageFilter).ToList()
                    });
                }
                return updated;
            }

            return filtered;
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
    }
}