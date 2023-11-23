//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Azure.AI.Details.Common.CLI
{
    public class ConfigCommand : Command
    {
        internal static bool RunCommand(ICommandValues values)
        {
            var atFile = values.GetOrDefault("x.config.command.at.file", null);

            var set = values.GetOrDefault("x.config.command.set", null);
            if (!string.IsNullOrEmpty(set)) return DoSetValue(set, atFile, values);

            var add = values.GetOrDefault("x.config.command.add", null);
            if (!string.IsNullOrEmpty(add)) return DoAddValue(add, atFile, values);

            var find = values.GetOrDefault("x.config.command.find", null);
            if (!string.IsNullOrEmpty(find)) return DoFindFiles(find, values);

            var clear = values.GetOrDefault("x.config.command.clear", null);
            if (!string.IsNullOrEmpty(clear)) return DoClearValue(clear, atFile, values);

            if (!string.IsNullOrEmpty(atFile)) return DoAtFile(atFile, values);

            return DoAtFile($"{Program.Name}.defaults", values);
        }

        private static bool DoSetValue(string setValue, string fileName, ICommandValues values)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                setValue = AdjustValue(setValue);
                fileName = AdjustSetValueFileName(fileName, values);
                FileHelpers.WriteAllText(fileName, setValue, Encoding.UTF8);
                return DoShowValue(fileName, "saved at", values);
            }

            string name, value;
            if (StringHelpers.SplitNameValue(setValue, out name, out value))
            {
                fileName = AdjustSetValueFileName(name, values);
                FileHelpers.WriteAllText(fileName, value, Encoding.UTF8);
                return DoShowValue(fileName, "saved at", values);
            }

            values.AddThrowError(
                "WARNING:", $"\"--set {setValue}\" is invalid; missing @NAME, NAME, or VALUE",
                            "",
                    "TRY:", $"{Program.Name} config --set NAME VALUE",
                            $"{Program.Name} config @NAME --set VALUE",
                            $"{Program.Name} config @CONFIG-FILENAME --set NAME VALUE",
                            "",
                    "SEE:", $"{Program.Name} help config set");

            return false;
        }

        private static bool DoAddValue(string addValue, string fileName, ICommandValues values)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                addValue = AdjustValue(addValue);
                fileName = AdjustAddValueFileName(fileName, values);
                FileHelpers.AppendAllText(fileName, "\n" + addValue, Encoding.UTF8);
                return DoShowValue(fileName, "updated at", values);
            }

            string name, value;
            if (StringHelpers.SplitNameValue(addValue, out name, out value))
            {
                fileName = AdjustAddValueFileName(name, values);
                FileHelpers.AppendAllText(fileName, "\n" + value, Encoding.UTF8);
                return DoShowValue(fileName, "updated at", values);
            }

            values.AddThrowError(
                "WARNING:", $"\"--add {addValue}\" is invalid; missing @NAME, NAME, or VALUE",
                            "",
                    "TRY:", $"{Program.Name} config --add NAME VALUE",
                            $"{Program.Name} config @NAME --add VALUE",
                            $"{Program.Name} config @CONFIG-FILENAME --add NAME VALUE",
                            "",
                    "SEE:", $"{Program.Name} help config add");

            return false;
        }

        private static bool DoShowValue(string path, string verbPhrase, ICommandValues values = null)
        {
            var quiet = values.GetOrDefault("x.quiet", false);

            if (!quiet)
            {
                var isStdIn = FileHelpers.IsStandardInputReference(path);
                var isResource = FileHelpers.IsResource(path);
                var isOverride = FileHelpers.IsOverride(path);
                if (isStdIn || isResource || isOverride)
                {
                    var root = isResource
                        ? FileHelpers.FileNameFromResourceName("")
                        : isOverride
                            ? FileHelpers.FileNameFromOverrideName("")
                            : "@stdin";

                    var fileName = path.Replace(root.TrimEnd('/', '_'), "").TrimStart('/', '_');
                    Console.WriteLine(isResource || isStdIn
                        ? $"{fileName} ({verbPhrase} '{root}')\n"
                        : $"{fileName} ({verbPhrase} '{path}')\n");
                }
                else
                {
                    var hive = FileHelpers.HiveFromFileName(path);
                    var printHive = hive == null ? "" : $" ({hive})";

                    var fi = new FileInfo(path);
                    Console.WriteLine($"{fi.Name} ({verbPhrase} '{fi.DirectoryName}'){printHive}\n");
                }
            }

            var contents = FileHelpers.ReadAllText(path, Encoding.UTF8);
            if (!quiet) contents = "  " + contents.Replace("\n", "\n  ");
            Console.WriteLine(contents);

            return true;
        }

        private static bool DoFindFiles(string file, ICommandValues values)
        {
            List<string> files = new List<string>();
            files.AddRange(FileHelpers.FindFilesInConfigPath(file, values));
            files.AddRange(FileHelpers.FindFilesInConfigPath("*." + file, values));
            files.AddRange(FileHelpers.FindFilesInConfigPath(file + ".*", values));
            files.AddRange(FileHelpers.FindFilesInConfigPath("*" + file + "*", values));

            var found = files.Where(x => !x.Contains("/help/") && !x.Contains("/templates/"))
                .Select(x => File.Exists(x)
                    ? new FileInfo(x).FullName
                    : x).ToList();

            if (found.Count() == 0)
            {
                Console.WriteLine($"'{file}' not found!!");
                return false;
            }

            FileHelpers.PrintFoundFiles(found, values);
            return true;
        }

        private static bool DoClearValue(string clearValue, string atFile, ICommandValues values)
        {
            var fileName = !string.IsNullOrEmpty(atFile)
                ? atFile.TrimStart('@') 
                : !string.IsNullOrEmpty(clearValue) && clearValue != "*"
                    ? clearValue.TrimStart('@')
                    : $"{Program.Name}.defaults";
            var existing = FileHelpers.FindFileInConfigPath(fileName, values);
            if (existing == null)
            {
                values.AddThrowError(
                    "WARNING:", $"Cannot delete '@{fileName}'; not found!",
                                "",
                        "USE:", $"{Program.Name} config @{fileName} --set VALUE",
                                $"{Program.Name} config @{fileName} --add VALUE",
                                "",
                        "SEE:", $"{Program.Name} help config");
            }
            else if (existing.Contains($"{Program.Exe}.exe/") || existing.Contains($"${Program.Name.ToUpper()}/"))
            {
                values.AddThrowError(
                    "WARNING:", $"Cannot delete '@{existing}'",
                                "",
                        "USE:", $"{Program.Name} config @{fileName} --set @@none",
                                $"{Program.Name} config @{fileName} --set VALUE",
                                $"{Program.Name} config @{fileName} --add VALUE",
                                "",
                        "SEE:", $"{Program.Name} help config");
            }
            else if (existing.Contains($".{Program.Name}/config") || existing.Contains($".{Program.Name}\\config"))
            {
                var fi = new FileInfo(existing);
                values.AddThrowError(
                    "WARNING:", $"Cannot delete '@{fi.Name}' from '{fi.DirectoryName}'",
                                "",
                        "USE:", $"{Program.Name} config @{fi.Name} --set @@none",
                                $"{Program.Name} config @{fi.Name} --set VALUE",
                                $"{Program.Name} config @{fi.Name} --add VALUE",
                                "",
                        "SEE:", $"{Program.Name} help config");
            }

            DoShowValue(existing, "deleted from", values);
            File.Delete(existing);
            return true;
        }

        private static bool DoAtFile(string atFile, ICommandValues values)
        {
            var fileName = atFile.TrimStart('@');

            var existing = FileHelpers.FindFileInConfigPath(fileName, values);
            if (existing != null) return DoShowValue(existing, "found at", values);

            return DoFindFiles(fileName, values);
        }

        private static string AdjustValue(string setValue)
        {
            return setValue.StartsWith("@@") ? setValue.Substring(1) : setValue;
        }

        private static string AdjustSetValueFileName(string fileName, ICommandValues values)
        {
            return AdjustOutputFileName(fileName, values).Replace("*.", "");
        }

        private static string AdjustAddValueFileName(string fileName, ICommandValues values)
        {
            var existing = FileHelpers.FindFileInConfigPath(fileName, values);
            if (existing != null) return existing;

            return AdjustOutputFileName(fileName, values).Replace("*.", "");
        }

        private static string AdjustOutputFileName(string fileName, ICommandValues values)
        {
            var regionScope = values.GetOrDefault("x.config.scope.region", null);
            var commandScope = values.GetOrDefault("x.config.scope.command", null);
            return AdjustOutputFileNameScope(fileName, regionScope, commandScope, values);
        }

        private static string AdjustOutputFileNameScope(string fileName, string region, string command, ICommandValues values)
        {
            fileName = AdjustFileNameScope(fileName, region, command);
            return FileHelpers.GetOutputConfigFileName(fileName, values);
        }

        private static string AdjustFileNameScope(string fileName, string region, string command)
        {
            fileName = fileName.TrimStart('@');
            if (!string.IsNullOrEmpty(command)) fileName = $"{command}.{fileName}";
            if (!string.IsNullOrEmpty(region)) fileName = $"{region}.{fileName}";
            return fileName;
        }
    }
}
