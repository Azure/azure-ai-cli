//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace Azure.AI.Details.Common.CLI
{
    public class PathHelpers
    {
        public static string Combine(string path1, string path2)
        {
            try
            {
                return Path.Combine(path1, path2);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static string Combine(string path1, string path2, string path3)
        {
            try
            {
                return Path.Combine(path1, path2, path3);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static IEnumerable<string> Combine(string path1, IEnumerable<string> path2s)
        {
            var list = new List<string>();
            foreach (var path2 in path2s)
            {
                list.Add(!string.IsNullOrEmpty(path2)
                    ? PathHelpers.Combine(path1, path2)
                    : path1);
            }
            return list;
        }

        public static string NormalizePath(string outputDirectory)
        {
            return new DirectoryInfo(outputDirectory).FullName;
        }
    }

    public class FileHelpers
    {
        public static void UpdatePaths(INamedValues values)
        {
            var outputPath = values.GetOrDefault("x.output.path", "");
            SetOutputPath(outputPath);

            var inputPath = values.GetOrDefault("x.input.path", outputPath);
            SetInputPath(inputPath);
        }

        public static string AppendToFileName(string fileName, string appendBeforeExtension, string appendAfterExtension)
        {
            if (IsStandardInputReference(fileName) || IsStandardOutputReference(fileName)) return fileName;

            var file = new FileInfo(fileName);
            return Path.Combine(file.DirectoryName, $"{Path.GetFileNameWithoutExtension(file.FullName)}{appendBeforeExtension}{file.Extension}{appendAfterExtension}");
        }

        public static IEnumerable<string> FindFiles(string path, string pattern, INamedValues values = null)
        {
            return FindFiles(PathHelpers.Combine(path, pattern), values);
        }

        public static IEnumerable<string> FindFiles(string fileNames, INamedValues values = null)
        {
            var currentDir = Directory.GetCurrentDirectory();
            foreach (var item in fileNames.Split(new char[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var overrides = FindOverrides(item);
                foreach (var name in overrides)
                {
                    yield return name;
                }

                var resources = FindResources(item);
                foreach (var resource in resources)
                {
                    yield return resource;
                }

                if (IsResource(item) || IsOverride(item)) continue;

                if (Program.Debug) Console.WriteLine($"  Searching for files '{fileNames}'");

                var i1 = item.LastIndexOf(Path.DirectorySeparatorChar);
                var i2 = item.LastIndexOf(Path.AltDirectorySeparatorChar);
                var hasPath = i1 >= 0 || i2 >= 0;

                var pathLen = Math.Max(i1, i2);
                var path = !hasPath ? currentDir : item.Substring(0, pathLen);
                var pattern = !hasPath ? item : item.Substring(pathLen + 1);

                EnumerationOptions recursiveOptions = null;
                if (path.EndsWith("**"))
                {
                    path = path.Substring(0, path.Length - 2).TrimEnd('/', '\\');
                    if (string.IsNullOrEmpty(path)) path = ".";
                    recursiveOptions = new EnumerationOptions() { RecurseSubdirectories = true };
                }

                if (!Directory.Exists(path)) continue;

                var files = recursiveOptions != null 
                    ? Directory.EnumerateFiles(path, pattern, recursiveOptions)
                    : Directory.EnumerateFiles(path, pattern);
                foreach (var file in files)
                {
                    yield return file;
                }
            }

            yield break;
        }

        public static IEnumerable<string> FindFilesInConfigPath(string fileNames, INamedValues values)
        {
            if (Program.Debug) Console.WriteLine($"DEBUG: Searching for CONFIG '{fileNames}'\n");
            
            var found = FindFilesInPath(fileNames, values, GetConfigPath(values));

            if (Program.Debug) Console.WriteLine();

            return found;
        }

        public static List<string> FindHelpFiles(string find, INamedValues values)
        {
            find = find.Replace(" ", ".");

            List<string> files = new List<string>();
            files.AddRange(FileHelpers.FindFilesInHelpPath($"help/{find}", values));
            files.AddRange(FileHelpers.FindFilesInHelpPath($"help/*.{find}", values));
            files.AddRange(FileHelpers.FindFilesInHelpPath($"help/{find}.*", values));
            files.AddRange(FileHelpers.FindFilesInHelpPath($"help/*{find}*", values));
            
            var found = files.Where(x => !x.Contains("/include.") && !x.Contains(".include."))
                .Select(x => File.Exists(x)
                    ? new FileInfo(x).FullName
                    : x)
                .OrderBy(x => x)
                .ToList();
            return found;
        }

        public static string ReadAllHelpText(string path, Encoding encoding)
        {
            var help = FileHelpers.ReadAllText(path, encoding);
            return FileHelpers.ExpandHelpIncludes(help) + "\n";
        }

        public static string ExpandHelpIncludes(string help)
        {
            if (help.StartsWith("@"))
            {
                var file = help.Substring(1);
                var existing = FileHelpers.FindFileInHelpPath(file);
                if (existing == null) existing = FileHelpers.FindFileInHelpPath("help/" + file);

                if (existing == null) return help;

                var text = FileHelpers.ReadAllText(existing, Encoding.UTF8);
                var expanded = ExpandHelpIncludes(text);
                return expanded;
            }

            StringBuilder sb = new StringBuilder();
            var lines = help.Split('\n');
            foreach (var line in lines)
            {
                var skipLine = line.StartsWith(';') || line.StartsWith('#');
                if (skipLine) continue;

                var doExpand = line.StartsWith("@");
                var trimmed = line.Trim('\r', '\n');
                sb.AppendLine(doExpand ? ExpandHelpIncludes(trimmed) : trimmed);
            }

            return sb.ToString().TrimEnd('\r', '\n', ' ');
        }

        public static IEnumerable<string> FindFilesInHelpPath(string fileNames, INamedValues values)
        {
            if (Program.Debug) Console.WriteLine($"DEBUG: Searching for HELP '{fileNames}'\n");
            
            var found = FindFilesInPath(fileNames, values, GetHelpPath());

            if (Program.Debug) Console.WriteLine();

            return found;
        }

        public static IEnumerable<string> FindFilesInDataPath(string fileNames, INamedValues values)
        {
            if (Program.Debug) Console.WriteLine($"DEBUG: Searching for DATA '{fileNames}'\n");

            var found = FindFilesInPath(fileNames, values, GetDataPath(values));

            if (Program.Debug) Console.WriteLine();

            return found;
        }

        public static IEnumerable<string> FindFilesInTemplatePath(string fileNames, INamedValues values)
        {
            if (Program.Debug) Console.WriteLine($"DEBUG: Searching for TEMPLATE '{fileNames}'\n");

            var found = FindFilesInPath(fileNames, values, GetTemplatePath());

            if (Program.Debug) Console.WriteLine();

            return found;
        }

        private static IEnumerable<string> FindFilesInPath(string fileNames, INamedValues values, string searchPath)
        {
            var commandActual = values?.GetCommand();
            var commandScope = values?.GetOrDefault("x.config.scope.command", commandActual == "config" ? "" : commandActual);
            if (commandScope != null && commandScope.Contains(".")) commandScope = commandScope.Substring(0, commandScope.IndexOf('.'));

            var regionActual = values?.GetOrDefault("service.config.region", "");
            var regionScope = values?.GetOrDefault("x.config.scope.region", regionActual);

            var paths = searchPath.Split(';');

            List<string> files = new List<string>();
            foreach (var item in fileNames.Split(';', '\n'))
            {
                var fileName = item.ReplaceValues(values);
                foreach (string path in paths)
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        FindFilesInScope(files, regionScope, commandScope, path, fileName, values);
                    }
                }
            }

            return files.Distinct();
        }

        public static void PrintFoundFiles(List<string> found, INamedValues values, bool showLocation = true, string prefix = "")
        {
            var quiet = values.GetOrDefault("x.quiet", false);
            var verbose = values.GetOrDefault("x.verbose", false);

            var slash = @"/\".ToCharArray();
            var lastLocation = "";

            found = found.Distinct().ToList();
            foreach (var item in found)
            {
                var exists = File.Exists(item);
                var fi = exists ? new FileInfo(item) : null;

                var i = item.LastIndexOfAny(slash);
                var name = exists ? fi.Name : item.Substring(i + 1);
                var location = exists ? fi.DirectoryName : item.Substring(0, i > 0 ? i : item.Length);

                if (!quiet && location != lastLocation && showLocation)
                {
                    var hive = HiveFromFileName(item);
                    var printHive = hive == null ? "" : $" ({hive})";

                    var extraCRLF = !string.IsNullOrEmpty(lastLocation) ? "\n" : "";
                    Console.WriteLine($"{extraCRLF}LOCATION: {location}{printHive}\n");

                    lastLocation = location;
                }

                Console.WriteLine(verbose 
                    ? $"  {prefix}{location}/{name}" 
                    : $"  {prefix}{name}");
            }
        }

        public static void PrintFoundHelpFiles(List<string> found, INamedValues values)
        {
            found = found.Select(x => x.Replace('.', ' ')).ToList();
            FileHelpers.PrintFoundFiles(found, values, false, $"{Program.Name} help ");
        }

        public static string ExpandFoundHelpFiles(List<string> found)
        {
            var sb = new StringBuilder();
            foreach (var item in found.Distinct())
            {
                string topic = HelpTopicNameFromHelpFileName(item);
                sb.AppendLine($"`{new string('-', topic.Length)}`");
                sb.AppendLine($"`{topic}`");
                sb.AppendLine($"`{new string('-', topic.Length)}`");

                var text = FileHelpers.ReadAllHelpText(item, Encoding.UTF8);
                sb.AppendLine(text);
            }
            return sb.ToString();
        }

        public static void DumpFoundHelpFiles(List<string> found)
        {
            foreach (var item in found.Distinct())
            {
                string topic = HelpTopicNameFromHelpFileName(item);
                var text = FileHelpers.ReadAllHelpText(item, Encoding.UTF8);

                var fileName = $"{topic}.md";
                FileHelpers.WriteAllText(fileName, text, Encoding.UTF8);

                Console.WriteLine($"File: {fileName}");
            }
        }

        public static void PrintExpandedFoundHelpFiles(List<string> found)
        {
            var expanded = FileHelpers.ExpandFoundHelpFiles(found);
            ConsoleHelpers.WriteLineWithHighlight(expanded);
        }

        public static string HelpTopicNameFromHelpFileName(string item)
        {
            var lastSlash = item.LastIndexOfAny("/\\".ToCharArray());
            var lastPart = lastSlash >= 0 ? item.Substring(lastSlash + 1) : item;
            return $"{Program.Name} help {lastPart.Replace('.', ' ')}";
        }

        public static void PrintHelpFile(string path)
        {
            var text = FileHelpers.ReadAllHelpText(path, Encoding.UTF8);
            ConsoleHelpers.WriteLineWithHighlight(text);
        }

        public static string DemandFindFileInConfigPath(string fileName, INamedValues values, string fileKind)
        {
            if (Program.Debug) Console.WriteLine($"DEBUG: CONFIG '{fileName}' MUST exist! \n");
            
            var found = DemandFindFileInPath(fileName, values, fileKind, GetConfigPath(values));

            if (Program.Debug) Console.WriteLine();

            return found;
        }

        public static string DemandFindFileInDataPath(string fileName, INamedValues values, string fileKind)
        {
            if (Program.Debug) Console.WriteLine($"DEBUG: DATA '{fileName}' MUST exist!'\n");

            var found = DemandFindFileInPath(fileName, values, fileKind, GetDataPath(values));

            if (Program.Debug) Console.WriteLine();

            return found;
        }

        private static string DemandFindFileInPath(string fileName, INamedValues values, string fileKind, string searchPath)
        {
            var existing = FindFileInPath(fileName, values, searchPath);
            if (string.IsNullOrEmpty(existing) || !FileExists(existing))
            {
                values.AddThrowError("ERROR:",
                    string.IsNullOrEmpty(fileKind)
                        ? $"Cannot find input file: \"{fileName}\""
                        : $"Cannot find {fileKind} file: \"{fileName}\"");
            }
            return existing;
        }

        private static string CheckStripDotSlash(string check)
        {
            return check.StartsWith("./")
                ? check.Substring(2)
                : check;
        }

        public static string FindFileInConfigPath(string fileName, INamedValues values)
        {
            if (Program.Debug) Console.WriteLine($"DEBUG: CONFIG '{fileName}' EXIST?\n");

            var found = FindFileInPath(fileName, values, GetConfigPath(values));

            if (Program.Debug) Console.WriteLine();

            return found;
        }

        public static string FindFileInDataPath(string fileName, INamedValues values)
        {
            if (Program.Debug) Console.WriteLine($"DEBUG: DATA '{fileName}' EXIST?\n");

            var found = FindFileInPath(fileName, values, GetDataPath(values));

            if (Program.Debug) Console.WriteLine();

            return found;
        }

        public static string FindFileInTemplatePath(string fileName, INamedValues values)
        {
            if (Program.Debug) Console.WriteLine($"DEBUG: TEMPLATE '{fileName}' EXIST?\n");

            var found = FindFileInPath(fileName, values, GetTemplatePath());

            if (Program.Debug) Console.WriteLine();

            return found;
        }

        public static string FindFileInHelpPath(string fileName)
        {
            if (Program.Debug) Console.WriteLine($"DEBUG: HELP '{fileName}' EXIST?\n");

            var found = FindFileInPath(fileName, null, GetHelpPath());

            if (Program.Debug) Console.WriteLine();

            return found;
        }

        public static string FindFileInOsPath(string fileName)
        {
            return FindFilesInOsPath(fileName).FirstOrDefault();
        }

        public static IEnumerable<string> FindFilesInOsPath(string fileName)
        {
            var lookIn = Environment.GetEnvironmentVariable("PATH")!.Split(System.IO.Path.PathSeparator);
            var found = lookIn.SelectMany(x =>
            {
                try
                {
                    return System.IO.Directory.GetFiles(x, fileName);
                }
                catch (Exception)
                {
                    return Enumerable.Empty<string>();
                }
            });
            return found;
        }

        private static string FindFileInPath(string fileName, INamedValues values, string searchPaths)
        {
            if (IsStandardInputReference(fileName)) return fileName;

            searchPaths += ";.x";
            var commandActual = values?.GetCommand();
            var commandScope = values?.GetOrDefault("x.config.scope.command", commandActual == "config" ? "" : commandActual);
            if (commandScope != null && commandScope.Contains(".")) commandScope = commandScope.Substring(0, commandScope.IndexOf('.'));

            var regionActual = values?.GetOrDefault("service.config.region", "");
            var regionScope = values?.GetOrDefault("x.config.scope.region", regionActual);

            var paths = searchPaths.Split(';');
            foreach (string path in paths)
            {
                var existing = FindFileInScope(regionScope, commandScope, path, fileName, values);
                if (FileExists(existing)) return CheckStripDotSlash(existing);
            }

            return null;
        }

        public static bool FileExistsInConfigPath(string fileName, INamedValues values)
        {
            return FileExistsInPath(fileName, values, GetConfigPath(values));
        }

        public static bool FileExistsInDataPath(string fileName, INamedValues values)
        {
            return FileExistsInPath(fileName, values, GetDataPath(values));
        }

        public static bool FileExistsInTemplatePath(string fileName, INamedValues values)
        {
            return FileExistsInPath(fileName, values, GetTemplatePath());
        }

        public static bool FileExistsInHelpPath(string fileName, INamedValues values)
        {
            return FileExistsInPath(fileName, values, GetHelpPath());
        }

        public static bool FileExistsInOsPath(string fileName)
        {
            var existing = FindFileInOsPath(fileName);
            return existing != null;
        }

        private static bool FileExistsInPath(string fileName, INamedValues values, string searchPath)
        {
            var existing = FindFileInPath(fileName, values, searchPath);
            return existing != null;
        }

        public static string ExpandAtFileValue(string atFileValue, INamedValues values = null)
        {
            if (atFileValue.StartsWith("@") && FileHelpers.FileExistsInConfigPath(atFileValue[1..], values))
            {
                return FileHelpers.ReadAllText(FileHelpers.DemandFindFileInConfigPath(atFileValue[1..], values, "configuration"), Encoding.UTF8);
            }
            else if (atFileValue.StartsWith("@") && FileHelpers.IsStandardInputReference(atFileValue[1..]))
            {
                return ConsoleHelpers.ReadAllStandardInputText();
            }
            else if (atFileValue.StartsWith("@@--"))
            { 
                return atFileValue[2..];
            }
            return atFileValue;
        }

        public static byte[] ReadAllBytes(string fileName)
        {
            byte[] bytes = IsStandardInputReference(fileName)
                ? ConsoleHelpers.ReadAllStandardInputBytes()
                : File.ReadAllBytes(fileName);
            return bytes;
        }

        public static string ReadAllText(string fileName, Encoding encoding)
        {
            var text = IsStandardInputReference(fileName)
                ? ConsoleHelpers.ReadAllStandardInputText()
                : IsOverride(fileName)
                    ? ReadAllOverrideText(fileName)
                    : IsResource(fileName)
                        ? ReadAllResourceText(fileName, encoding ?? Encoding.Default)
                        : File.ReadAllText(fileName, encoding ?? Encoding.Default);
            return text.Trim('\r', '\n');
        }

        public static string GetOutputConfigFileName(string file, INamedValues values = null)
        {
            file = file.TrimStart('@');

            var dir = GetConfigOutputDir(values);
            var outputFile = Path.Combine(dir, file);

            if (Program.Debug) Console.WriteLine($"DEBUG: Output CONFIG file '{file}'='{outputFile}'");

            return outputFile;
        }

        public static bool IsStandardOutputReference(string fileName)
        {
            return fileName == "-" || fileName == "stdout";
        }

        public static string GetOutputDataFileName(string file, INamedValues values = null)
        {
            if (file == null) return null;
            if (file.StartsWith("@")) return GetOutputConfigFileName(file, values);
            if (IsStandardOutputReference(file)) return file;
            
            var i1 = file.LastIndexOf(Path.DirectorySeparatorChar);
            var i2 = file.LastIndexOf(Path.AltDirectorySeparatorChar);
            var hasPath = i1 >= 0 || i2 >= 0;
            var pathLen = hasPath ? Math.Max(i1, i2) : 0;

            var outputDir = values != null ? values.GetOrDefault("x.output.path", _outputPath) : _outputPath;
            var outputFile = !hasPath ? PathHelpers.Combine(outputDir, file) : file;

            if (Program.Debug) Console.WriteLine($"DEBUG: Output DATA file '{file}'='{outputFile}'");

            return outputFile;
        }

        public static Stream Create(string fileName)
        {
            return IsStandardOutputReference(fileName)
                ? Console.OpenStandardOutput()
                : File.Create(fileName);
        }

        public static Stream Open(string fileName, FileMode mode)
        {
            return IsStandardOutputReference(fileName)
                ? Console.OpenStandardOutput()
                : File.Open(fileName, mode);
        }

        public static void AppendAllText(string fileName, string text, Encoding encoding)
        {
            EnsureDirectoryForFileExists(fileName);
            var ex = TryCatchHelpers.TryCatchRetryNoThrow<Exception>(() => {

                if (IsStandardOutputReference(fileName))
                {
                    Console.Write(text);
                }
                else
                {
                    _lockSlim.EnterWriteLock();
                    try
                    {
                        using var file = File.Open(fileName, FileMode.Append, FileAccess.Write, FileShare.None);
                        byte[] byteArray = encoding.GetBytes(text);
                        file.Write(byteArray, 0, byteArray.Length);
                        file.Close();                        
                    }
                    finally
                    {
                        _lockSlim.ExitWriteLock();
                    }
                }
            }, 10);

            if (ex != null)
            {
                throw new IOException($"Cannot write to file '{fileName}'", ex);
            }
        }

        public static void WriteAllText(string fileName, string text, Encoding encoding)
        {
            EnsureDirectoryForFileExists(fileName);
            var ex = TryCatchHelpers.TryCatchRetryNoThrow<Exception>(() => {
                
                if (IsStandardOutputReference(fileName))
                {
                    Console.Write(text);
                    if (!text.EndsWith('\n')) Console.WriteLine();
                }
                else
                {
                    File.WriteAllText(fileName, text, encoding ?? Encoding.Default);
                }
            }, 10);

            if (ex != null)
            {
                throw new IOException($"Cannot write to file '{fileName}'", ex);
            }
        }

        public static void WriteAllLines(string fileName, IEnumerable<string> lines, Encoding encoding)
        {
            EnsureDirectoryForFileExists(fileName);
            var ex = TryCatchHelpers.TryCatchRetryNoThrow<Exception>(() => {

                if (IsStandardOutputReference(fileName))
                {
                    ConsoleHelpers.WriteAllLines(lines);
                }
                else
                {
                    File.WriteAllLines(fileName, lines, encoding ?? Encoding.Default);
                }
            }, 10);

            if (ex != null)
            {
                throw new IOException($"Cannot write to file '{fileName}'", ex);
            }
        }

        public static void WriteAllStream(string fileName, Stream stream)
        {
            EnsureDirectoryForFileExists(fileName);
            var fileStream = FileHelpers.Create(fileName);

            int read = 0;
            int bufferSize = 1024;
            byte[] buffer = new byte[bufferSize];
            while ((read = stream.Read(buffer, 0, bufferSize)) != 0)
            {
                fileStream.Write(buffer, 0, read);
            }

            fileStream.Dispose();
            stream.Dispose();
        }

        public static void WriteAllBytes(string fileName, byte[] bytes)
        {
            using var fileStream = FileHelpers.Open(fileName, FileMode.OpenOrCreate);
            fileStream.Write(bytes, 0, bytes.Length);
        }

        public static void AppendAllBytes(string fileName, byte[] bytes)
        {
            using var fileStream = FileHelpers.Open(fileName, FileMode.Append);
            fileStream.Write(bytes, 0, bytes.Length);
        }

        public static string ReadTextWriteIfJson(Stream stream, bool isJson, INamedValues values, string domain, bool skipWrite = false)
        {
            var text = FileHelpers.ReadAllStreamText(stream, Encoding.UTF8) + Environment.NewLine;
            if (!isJson || skipWrite) return text;

            return CheckOutputJson(text, values, domain);
        }

        public static string CheckOutputJson(string text, INamedValues values, string domain)
        {
            var saveAs = values.GetOrDefault($"{domain}.output.json.file", null);
            if (saveAs == null) return text;

            saveAs = FileHelpers.GetOutputDataFileName(saveAs, values);
            FileHelpers.WriteAllText(saveAs, text, Encoding.UTF8);

            return text;
        }

        public static string ReadWriteAllStream(Stream stream, string fileName, string message, bool returnAsText)
        {
            // we need to know if we're writing to standard output 
            var isStandardOutput = IsStandardOutputReference(fileName);

            // we'll use a temp file if we don't get a fileName passed in, or if it's a standard output reference
            var useTemp = fileName == null || isStandardOutput;
            if (useTemp) fileName = Path.GetTempFileName();

            // don't print the message, if we're using a temporary file
            if (useTemp) message = null;

            // if we have a message, update it with the fileName and print it
            if (message != null)
            {
                message = $"{message} {fileName} ...";
                Console.WriteLine(message);
            }

            // get the file stream
            var fileStream = FileHelpers.Create(fileName);

            // copy data from the source stream to the file stream
            int read;
            int bufferSize = 1024;
            byte[] buffer = new byte[bufferSize];
            while ((read = stream.Read(buffer, 0, bufferSize)) != 0)
            {
                fileStream.Write(buffer, 0, read);
            }

            // if we need to return the text, or we need to print to stdout
            string text = null;
            if (returnAsText || isStandardOutput)
            {
                // seek back and read the text
                fileStream.Seek(0, SeekOrigin.Begin);
                text = FileHelpers.ReadAllStreamText(fileStream, Encoding.UTF8);

                // print the text
                if (isStandardOutput) Console.WriteLine(text);
            }

            // dispose the file stream, and delete the temporary file
            fileStream.Dispose();
            if (useTemp) File.Delete(fileName);

            // print the "I'm done" message...
            if (message != null) Console.WriteLine($"{message} Done!\n");

            // and return the text
            return text;
        }

        public static void EnsureDirectoryForFileExists(string fileName)
        {
            if (IsStandardInputReference(fileName)) return;
            if (IsStandardOutputReference(fileName)) return;

            var s1 = fileName.LastIndexOf(Path.DirectorySeparatorChar);
            var s2 = fileName.LastIndexOf(Path.AltDirectorySeparatorChar);
            var sep = Math.Max(s1, s2);
            if (sep <= 0) return;

            var dir = fileName.Substring(0, sep);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        public static void CopyFile(string path1, string file1, string path2, string file2 = null, bool verbose = true)
        {
            if (file2 == null) file2 = file1;

            var source = PathHelpers.Combine(path1, file1);
            if (!FileExists(source)) source = PathHelpers.Combine(path1, ".." + Path.DirectorySeparatorChar + file1);
            if (!FileExists(source)) source = PathHelpers.Combine(path1, ".." + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar + file1);
            if (!FileExists(source)) return;

            if (verbose) Console.WriteLine($"  Saving {file2} ... ");
            
            var dest = PathHelpers.Combine(path2, file2);
            FileHelpers.EnsureDirectoryForFileExists(dest);

            File.Copy(source, dest, true);
        }

        public static void CopyFiles(IEnumerable<string> sourceLocations, string[] files, string destinationLocation, string dllNewPrefix, string dllNewSuffix, bool verbose)
        {
            var remapDlls = !string.IsNullOrEmpty(dllNewPrefix) && !string.IsNullOrEmpty(dllNewSuffix);
            foreach (var location in sourceLocations)
            {
                foreach (var file in files)
                {
                    var fileName = remapDlls && file.EndsWith(".dll")
                        ? $"{dllNewPrefix}{file.Substring(0, file.Length - 4)}{dllNewSuffix}"
                        : file;

                    FileHelpers.CopyFile(location, fileName, destinationLocation, null, verbose);
                }
            }
        }

        public static FileInfo GetAssemblyFileInfo(Type type)
        {
            // GetAssembly.Location always returns empty when the project is built as 
            // a single-file app (which we do when publishing the Dependency package),
            // warning IL3000
            string assemblyPath = Assembly.GetAssembly(type).Location;
            if (assemblyPath == string.Empty)
            {
                assemblyPath = AppContext.BaseDirectory;
                assemblyPath += Assembly.GetAssembly(type).GetName().Name + ".dll";
            }
            return new FileInfo(assemblyPath);
        }

        public static void LogException(ICommandValues values, Exception ex)
        {
            var file = $"exception.{{run.time}}.{{pid}}.{{time}}.{Program.Name}.error.log";

            var runTime = values.GetOrDefault("x.run.time", "");
            file = file.Replace("{run.time}", runTime);

            var pid = Process.GetCurrentProcess().Id.ToString();
            file = file.Replace("{pid}", pid);

            var time = DateTime.Now.ToFileTime().ToString();
            file = file.Replace("{time}", time);

            file = FileHelpers.GetOutputDataFileName(file, values);
            FileHelpers.WriteAllText(file, ex.ToString(), Encoding.UTF8);
        }

        public static bool FileExists(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;

            var found = IsStandardInputReference(fileName) || OverrideExists(fileName) || ResourceExists(fileName) || File.Exists(fileName);
            if (Program.Debug) Console.WriteLine(found ?  $"  FOUND `{fileName}`" : $"  not found `{fileName}`");

            return found;
        }

        public static bool FileExists(string path, string fileName)
        {
            var check = PathHelpers.Combine(path, fileName);
            return FileExists(check);
        }

        private static bool OverrideExists(string fileName)
        {
            if (!IsOverride(fileName)) return false;

            var name = OverrideNameFromFileName(fileName);
            if (string.IsNullOrEmpty(name)) return false;

            name = name.Replace(overridePrefix, "").TrimStart('/', '_').Replace('_', '.');
            return EnvironmentNamedValues.Current.Contains(name);
        }

        public static bool IsOverride(string fileName)
        {
            return !string.IsNullOrEmpty(fileName) && fileName.StartsWith(overridePrefix);
        }

        private static string OverrideNameFromFileName(string fileName)
        {
            var name = fileName.Replace(overridePrefix, "").TrimStart('/', '_');
            return $"{overridePrefix}_{name}".Replace('/', '_').Replace('\\', '_').Replace('.', '_');
        }

        public static string FileNameFromOverrideName(string name)
        {
            var fileName = name.Replace(overridePrefix, "").TrimStart('/', '_');
            return $"{overridePrefix}/{fileName}".Replace('/', '_').Replace('\\', '_').Replace('.', '_');
        }

        public static IEnumerable<string> FindOverrides(string find)
        {
            if (!IsOverride(find)) yield break;

            if (Program.Debug) Console.WriteLine($"  Searching for overrides '{find}'");

            find = OverrideNameFromFileName(find);
            if (OverrideExists(find))
            {
                yield return FileNameFromOverrideName(find);
                yield break;
            }

            var pattern = find.Replace(@"$", @"\$").Replace(@".", @"\.").Replace(@"?", @".").Replace("*", @"([^\/]+)");
            var names = EnvironmentNamedValues.Current.Names;
            foreach (var name in names)
            {
                var test = $"{overridePrefix}_{name.Replace('.', '_')}";
                var match = Regex.Match(test, pattern);
                if (match.Success) yield return FileNameFromOverrideName(name);
            }

            yield break;            
        }

        private static string ReadAllOverrideText(string fileName)
        {
            if (!IsOverride(fileName)) return null;

            var name = OverrideNameFromFileName(fileName);
            if (string.IsNullOrEmpty(name)) return null;

            name = name.Replace(overridePrefix, "").TrimStart('/', '_').Replace('_', '.');
            return EnvironmentNamedValues.Current[name];
        }

        public static bool ResourceExists(string fileName)
        {
            return IsResource(fileName) && GetResourceStream(fileName) != null;
        }

        public static bool IsResource(string fileName)
        {
            return !string.IsNullOrEmpty(fileName) && fileName.StartsWith(Program.Exe);
        }

        private static string ResourceNameFromFileName(string fileName)
        {
            var phase1 = fileName.Replace(Program.Exe, resourcePrefix).Replace('\\', '/');

            var lastSlash = phase1.LastIndexOf('/');
            var onlyFileName = phase1.Substring(lastSlash + 1);
            var onlyPath = phase1.Substring(0, lastSlash).Replace('/', '.').Replace('-', '_');

            var resourceName = $"{onlyPath}.{onlyFileName}";
            return resourceName;
        }

        public static string FileNameFromResourceName(string name)
        {
            var originalFileNames = GetOriginalResourceFileNames();
            if (originalFileNames.ContainsKey(name))
            {
                name = originalFileNames[name];
                name = name.Replace('\\', '/');
                return name.EndsWith("._") ? name.Substring(0, name.Length - 2) : name;
            }

            var check = name.Replace('-', '_');
            if (originalFileNames.ContainsKey(check))
            {
                name = originalFileNames[check];
                name = name.Replace('\\', '/');
                return name.EndsWith("._") ? name.Substring(0, name.Length - 2) : name;
            }

            name = name.Replace(resourcePrefix, "");

            var subDir = "";
            if (name.StartsWith($"..{Program.Name}.config."))
            {
                subDir = "config/";
                name = name.Replace("." + dotDirectory.Replace("/", ".") + "config", "");
            }

            if (name.StartsWith($"..{Program.Name}.data."))
            {
                subDir = "data/";
                name = name.Replace("." + dotDirectory.Replace("/", ".") + "data", "");
            }

            if (name.StartsWith($"..{Program.Name}.help."))
            {
                subDir = "help/";
                name = name.Replace("." + dotDirectory.Replace("/", ".") + "help", "");
            }

            if (name.StartsWith($"..{Program.Name}.templates"))
            {
                subDir = "templates/";
                name = name.Replace("." + dotDirectory.Replace("/", ".") + "templates", "");
            }

            if (name.StartsWith($"..{Program.Name}.internal"))
            {
                subDir = "internal/";
                name = name.Replace("." + dotDirectory.Replace("/", ".") + "internal", "");
            }

            name = name.Replace("." + dotDirectory.Replace("/", "."), "");
            name = name.Trim('.', '/');

            name = $"{Program.Exe}/{dotDirectory}{subDir}{name}";
            return name.EndsWith("._") ? name.Substring(0, name.Length - 2) : name;
        }

        public static IEnumerable<string> FindResources(string find)
        {
            if (!IsResource(find)) yield break;

            if (Program.Debug) Console.WriteLine($"  Searching for resources '{find}'");

            find = ResourceNameFromFileName(find);
            if (ResourceExists(find))
            {
                yield return FileNameFromResourceName(find);
                yield break;
            }

            var pattern = find.Replace(@".", @"\.").Replace(@"?", @".").Replace("*", @"([^\/]+)");
            var names = Program.ResourceAssembly.GetManifestResourceNames();
            foreach (var name in names)
            {
                var match = Regex.Match(name, pattern);
                if (match.Success) yield return FileNameFromResourceName(name);
            }

            yield break;            
        }

        private static Dictionary<string, string> GetOriginalResourceFileNames()
        {
            if (_origDotXfileNamesDictionary != null) return _origDotXfileNamesDictionary;

            var origDotXfileNamesText = ReadAllResourceText($"{Program.Exe}.{dotDirectory}internal/OriginalDotXfileNames.txt", Encoding.UTF8);
            var origDotXfileNamesLines = origDotXfileNamesText.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var origDotXfileNamesDictionary = origDotXfileNamesLines
                .Select(x => $"{Program.Exe}/{x}")
                .ToDictionary(x => ResourceNameFromFileName(x), x => x);
            _origDotXfileNamesDictionary = origDotXfileNamesDictionary;

            return _origDotXfileNamesDictionary;
        }

        private static Stream GetResourceStream(string fileName)
        {
            var resource = ResourceNameFromFileName(fileName);
            return Program.ResourceAssembly.GetManifestResourceStream(resource)
                ?? Program.ResourceAssembly.GetManifestResourceStream(resource + "._");
        }

        private static string ReadAllResourceText(string fileName, Encoding encoding)
        {
            var stream = GetResourceStream(fileName);
            var length = stream.Length;

            byte[] buffer = new byte[length];
            string text = stream.Read(buffer, 0, (int)length) != 0
                ? encoding.GetString(buffer)
                : "";

            stream.Dispose();
            return text;
        }

        public static bool IsStandardInputReference(string fileName)
        {
            return fileName == "-" || fileName == "stdin";
        }

        public static string ReadAllStreamText(Stream stream, Encoding encoding)
        {
            var tempFile = Path.GetTempFileName();
            FileHelpers.WriteAllStream(tempFile, stream);

            var text = FileHelpers.ReadAllText(tempFile, encoding);
            File.Delete(tempFile);

            return text;
        }

        public static byte[] ReadAllStreamBytes(Stream stream)
        {
            var tempFile = Path.GetTempFileName();
            FileHelpers.WriteAllStream(tempFile, stream);

            var bytes = File.ReadAllBytes(tempFile);
            File.Delete(tempFile);

            return bytes;

        }

        private static void FindFilesInScope(List<string> files, string region, string command, string path1, string path2, string fileName, INamedValues values)
        {
            var path = PathHelpers.Combine(path1, path2);
            FindFilesInScope(files, region, command, path, fileName, values);
        }

        private static void FindFilesInScope(List<string> files, string region, string command, string path, string fileName, INamedValues values)
        {
            if (!string.IsNullOrEmpty(region) && !string.IsNullOrEmpty(command))
            {
                var scoped = $"{region}.{command}.{fileName}";
                files.AddRange(FindFiles(path, scoped, values));
            }

            if (!string.IsNullOrEmpty(region))
            {
                var scoped = $"{region}.{fileName}";
                files.AddRange(FindFiles(path, scoped, values));
            }

            if (!string.IsNullOrEmpty(command))
            {
                var scoped = $"{command}.{fileName}";
                files.AddRange(FindFiles(path, scoped, values));
            }

            files.AddRange(FindFiles(path, fileName, values));
        }

        private static string FindFileInScope(string region, string command, string path1, string path2, string fileName, INamedValues values)
        {
            var path = PathHelpers.Combine(path1, path2);
            return FindFileInScope(region, command, path, fileName, values);
        }

        private static string FindFileInScope(string region, string command, string path, string fileName, INamedValues values)
        {
            fileName = fileName.ReplaceValues(values);
            return FindFileInScope(region, command, path, fileName);
        }

        private static string FindFileInScope(string region, string command, string path, string fileName)
        {
            if (!string.IsNullOrEmpty(region) && !string.IsNullOrEmpty(command))
            {
                var scoped = PathHelpers.Combine(path, $"{region}.{command}.{fileName}");
                if (FileExists(scoped)) return CheckStripDotSlash(scoped);
            }

            if (!string.IsNullOrEmpty(region))
            {
                var scoped = PathHelpers.Combine(path, $"{region}.{fileName}");
                if (FileExists(scoped)) return CheckStripDotSlash(scoped);
            }

            if (!string.IsNullOrEmpty(command))
            {
                var scoped = PathHelpers.Combine(path, $"{command}.{fileName}");
                if (FileExists(scoped)) return CheckStripDotSlash(scoped);
            }

            fileName = PathHelpers.Combine(path, fileName);
            if (FileExists(fileName)) return CheckStripDotSlash(fileName);

            return null;
        }

        private static string ExistingPathOrNull(string check, string root = null, int checkParentDepth = 4)
        {
            if (Directory.Exists(check)) return check;
            if (Path.IsPathFullyQualified(check)) return null;

            var combined = Path.Combine(root, check);
            if (Directory.Exists(combined)) return combined;

            var parent = checkParentDepth > 0 && root != null ? Directory.GetParent(root) : null;
            if (parent == null) return null;

            return ExistingPathOrNull(check, parent.FullName, checkParentDepth - 1);
        }

        private static void SetInputPath(string inputPath)
        {
            var dir = Directory.GetCurrentDirectory();

            var paths = inputPath
                .Split(new char[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Distinct()
                .Select(path => ExistingPathOrNull(path, dir))
                .Where(paths => paths != null);

            _dataPath = string.Join(';', paths) + defaultDataPath;
        }

        private static void SetOutputPath(string outputPath)
        {
            if (!string.IsNullOrEmpty(outputPath))
            {
                _outputPath = outputPath;
                if (!Directory.Exists(_outputPath))
                {
                    Directory.CreateDirectory(_outputPath);
                }
            }
        }

        private static string GetConfigPath(INamedValues values = null)
        {
            var cwd = Directory.GetCurrentDirectory();
            if (_configPathCalculatedFrom != cwd)
            {
                _configPathCalculatedFrom = cwd;
                _configPathScoped = null;
                _configPath = null;
            }

            CheckScopedConfigPath(values);
            if (!string.IsNullOrEmpty(_configPathScoped)) return _configPathScoped;
            if (!string.IsNullOrEmpty(_configPath)) return _configPath;

            var path5 = GetAppResourceConfigDotDir();
            var path4 = GetAssemblyConfigDotDir();
            var path3 = GetGlobalConfigDotDir();
            var path2 = GetUserConfigDotDir();
            var path1 = GetLocalConfigDotDirs();
            var paths = $"{path1};{path2};{path3};{path4};{path5}";

            _configPath = ExpandConfigPath($"{overridePrefix}/;./;", paths);

            return _configPath;
        }

        private static void CheckScopedConfigPath(INamedValues values)
        {
            if (_configPathScoped != null) return;

            var hive = values?.GetOrDefault("x.config.scope.hive", "");
            if (string.IsNullOrEmpty(hive)) return;

            // var path2 = GetAppResourceConfigDotDir();
            // var path1 = GetScopedConfigDotDir(hive, false, false);
            // var paths = $"{path1};{path2}";

            var paths = GetScopedConfigDotDir(hive, false, false);

            _configPathScoped = ExpandConfigPath($"{overridePrefix}/;", paths);
        }

        private static string ExpandConfigPath(string path0, string paths)
        {
            var sb = new StringBuilder();
            foreach (var path in paths.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
            {
                if (Program.Debug) Console.WriteLine($"  CONFIG DATASTORE: '{path}'");
                sb.Append($"{path}data/;{path}config/;{path};");
            }

            if (Program.Debug) Console.WriteLine();

            return path0.TrimEnd(';') + ";" + sb.ToString().TrimEnd(';');
        }

        public static string GetConfigOutputDir(INamedValues values = null)
        {
            CheckScopedConfigOutputDir(values);
            if (!string.IsNullOrEmpty(_configOutputDirScoped)) return _configOutputDirScoped;
            if (!string.IsNullOrEmpty(_configOutputDir)) return _configOutputDir;

            var dir = GetLocalConfigDotDirs();
            if (dir.Contains(';')) dir = dir.Split(';')[0];

            if (string.IsNullOrEmpty(dir)) dir = GetUserConfigDotDir();
            if (string.IsNullOrEmpty(dir)) dir = GetGlobalConfigDotDir();
            if (string.IsNullOrEmpty(dir)) dir = GetAssemblyConfigDotDir();
            if (string.IsNullOrEmpty(dir)) dir = "./";

            _configOutputDir = dir.Contains(dotDirectory) ? $"{dir}data/" : dir;

            return _configOutputDir;
        }

        private static void CheckScopedConfigOutputDir(INamedValues values)
        {
            if (_configOutputDirScoped != null) return;

            var hive = values?.GetOrDefault("x.config.scope.hive", "");
            if (string.IsNullOrEmpty(hive)) return;

            var dir = GetScopedConfigDotDir(hive, true, true);
            if (string.IsNullOrEmpty(dir)) return;

            _configOutputDirScoped = dir.Contains(dotDirectory) ? $"{dir}data/" : dir;
        }

        private static string GetScopedConfigDotDir(string hive, bool mustExist = true, bool createIfDoesnt = false)
        {
            string dotdir;
            switch (hive)
            {
                case "system": dotdir = GetAssemblyConfigDotDir(mustExist, createIfDoesnt); break;
                case "global": dotdir = GetGlobalConfigDotDir(mustExist, createIfDoesnt); break;
                case "user": dotdir = GetUserConfigDotDir(mustExist, createIfDoesnt); break;
                case "local": dotdir = CheckDotDirectory(".", mustExist, createIfDoesnt); break;
                default: dotdir = hive.TrimEnd('/', '\\') + "/"; break;
            }

            if (Program.Debug)
            {
                Console.WriteLine(string.IsNullOrEmpty(dotdir)
                    ? $"  CONFIG DATASTORE ('{hive}'): does NOT exist !!\n"
                    : !Directory.Exists(dotdir)
                        ? $"  CONFIG DATASTORE ('{hive}'): '{dotdir}' does NOT exist !!\n"
                        : $"  CONFIG DATASTORE ('{hive}'): '{dotdir}'\n");
            }

            return dotdir;
        }

        public static string HiveFromFileName(string fileName)
        {
            fileName = fileName.Replace('/', '\\');
            var system = GetAssemblyConfigDotDir(false, false).Replace('/', '\\');
            var global = GetGlobalConfigDotDir(false, false).Replace('/', '\\');
            var user = GetUserConfigDotDir(false, false).Replace('/', '\\');
            var local = CheckDotDirectory(Directory.GetCurrentDirectory(), false, false).Replace('/', '\\');
            var app = GetAppResourceConfigDotDir().Replace('/', '\\');

            if (fileName.StartsWith(local)) return "local";
            if (fileName.StartsWith(system)) return "system";
            if (fileName.StartsWith(global)) return "global";
            if (fileName.StartsWith(user)) return "user";
            if (fileName.StartsWith(app)) return Program.Name;

            return null;
        }

        private static string GetAppResourceConfigDotDir()
        {
            return $"{Program.Exe}/{dotDirectory}";
        }

        private static string GetAssemblyConfigDotDir(bool mustExist = true, bool createIfDoesnt = false)
        {
            return CheckDotDirectory(GetAssemblyDir(), mustExist, createIfDoesnt);
        }

        private static string GetGlobalConfigDotDir(bool mustExist = true, bool createIfDoesnt = false)
        {
            return CheckDotDirectory(GetAppDataDir(), mustExist, createIfDoesnt);
        }

        private static string GetUserConfigDotDir(bool mustExist = true, bool createIfDoesnt = false)
        {
            return CheckDotDirectory(GetAppUserDir(), mustExist, createIfDoesnt);
        }

        private static string GetLocalConfigDotDirs()
        {
            int depth = 6;
            var dirs = new StringBuilder();
            for (var dir = "."; Directory.Exists(dir) && depth > 0; dir = $"{dir}/..", depth--)
            {
                var check = $"{dir}/{dotDirectory}";
                if (Directory.Exists(check)) dirs.Append($";{dir}/{dotDirectory}");
            }

            return dirs.ToString().Trim(';');
        }

        private static string GetDataPath(INamedValues values)
        {
            if (_dataPath.Contains("{config.path}"))
            {
                _dataPath = _dataPath.Replace("{config.path}", GetConfigPath(values));
            }
            return _dataPath;
        }

        private static string GetTemplatePath()
        {
            return GetAppResourceConfigDotDir() + "templates/";
        }

        private static string GetHelpPath()
        {
            return GetAppResourceConfigDotDir();
        }

        private static string GetAssemblyDir()
        {
            return AppContext.BaseDirectory;
        }

        private static string GetAppDataDir()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return path.TrimEnd('/', '\\') + "/";
        }

        private static string GetAppUserDir()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return path.TrimEnd('/', '\\') + "/";
        }

        private static string CheckDotDirectory(string checkPath, bool mustExist = true, bool createIfDoesnt = false)
        {
            checkPath = checkPath.TrimEnd('/', '\\');
            checkPath = $"{checkPath}/{dotDirectory}".TrimEnd('/', '\\');

            return Directory.Exists(checkPath)
                ? $"{checkPath}/"
                : createIfDoesnt
                    ? Directory.CreateDirectory(checkPath).FullName + "/"
                    : mustExist
                        ? ""
                        : $"{checkPath}/";
        }

        private const string resourcePrefix = "Azure.AI.Details.Common.CLI.resources";
        private static readonly string overridePrefix = $"${Program.Name.ToUpper()}";

        private const string defaultDataPath = @";./;../;../../;../../../;../../../../;{config.path};";
        
        private static string _configPathCalculatedFrom = null;
        private static string _configPath = null;
        private static string _configPathScoped = null;

        private static string _configOutputDir = null;
        private static string _configOutputDirScoped = null;

        private static string _dataPath = defaultDataPath;
        private static string _outputPath = "";

        private static readonly string dotDirectory = $".{Program.Name}/";

        private static ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();
        private static Dictionary<string, string> _origDotXfileNamesDictionary;
    }
}
