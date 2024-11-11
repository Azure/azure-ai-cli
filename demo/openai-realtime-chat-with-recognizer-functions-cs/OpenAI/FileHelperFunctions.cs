//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Text;
using Azure.AI.OpenAI;

public static class FileHelperFunctions
{
    [HelperFunctionDescription("Checks if file exists")]
    public static bool FileExists(string fileName)
    {
        var exists = File.Exists(fileName);
        return exists;
    }

    [HelperFunctionDescription("Reads text from a file; returns empty string if file does not exist")]
    public static string ReadTextFromFile(string fileName)
    {
        var content = File.Exists(fileName)
            ? File.ReadAllText(fileName, new UTF8Encoding(false))
            : $"File not found: {fileName}";
        return content;
    }

    [HelperFunctionDescription("Writes text into a file; if the file exists, it is overwritten")]
    public static bool CreateFileAndSaveText(string fileName, string text)
    {
        File.WriteAllText(fileName, text, new UTF8Encoding(false));
        return true;
    }

    [HelperFunctionDescription("Appends text to a file; if the file does not exist, it is created")]
    public static bool AppendTextToFile(string fileName, string text)
    {
        File.AppendAllText(fileName, text, new UTF8Encoding(false));
        return true;
    }

    [HelperFunctionDescription("Creates a directory if it doesn't already exist")]
    public static bool DirectoryCreate(string directoryName)
    {
        if (!Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }
        return true;
    }

    [HelperFunctionDescription("List files; lists all files matching pattern; searches current directory, and if pattern includes '**', all sub-directories")]
    public static string FindAllFilesMatchingPattern([HelperFunctionParameterDescription("The pattern to search for; use '**/*.ext' to search sub-directories")] string pattern)
    {
        var sb = new StringBuilder();
        var currentDir = Directory.GetCurrentDirectory();
        foreach (var item in pattern.Split(new char[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var i1 = item.LastIndexOf(Path.DirectorySeparatorChar);
            var i2 = item.LastIndexOf(Path.AltDirectorySeparatorChar);
            var hasPath = i1 >= 0 || i2 >= 0;

            var pathLen = Math.Max(i1, i2);
            var path = !hasPath ? currentDir : item.Substring(0, pathLen);
            var filePattern = !hasPath ? item : item.Substring(pathLen + 1);

            EnumerationOptions? recursiveOptions = null;
            if (path.EndsWith("**"))
            {
                path = path.Substring(0, path.Length - 2).TrimEnd('/', '\\');
                if (string.IsNullOrEmpty(path)) path = ".";
                recursiveOptions = new EnumerationOptions() { RecurseSubdirectories = true };
            }

            if (!Directory.Exists(path)) continue;

            var files = recursiveOptions != null 
                ? Directory.EnumerateFiles(path, filePattern, recursiveOptions)
                : Directory.EnumerateFiles(path, filePattern);
            foreach (var file in files)
            {
                sb.AppendLine(file);
            }
        }

        return sb.ToString();
    }

    [HelperFunctionDescription("Find files containing text; searches all files")]
    public static string FindTextInAllFiles([HelperFunctionParameterDescription("The text to find")] string text)
    {
        return FindTextInFilesMatchingPattern(text, "**/*");
    }

    [HelperFunctionDescription("Find files containing text; searches files matching a pattern")]
    public static string FindTextInFilesMatchingPattern(
        [HelperFunctionParameterDescription("The text to find")] string text,
        [HelperFunctionParameterDescription("The pattern to search for; use '**/*.ext' to search sub-directories")] string pattern)
    {
        var files = FindAllFilesMatchingPattern(pattern).Split(new char[] { '/', '\\'}, StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();
        foreach (var file in files)
        {
            var content = File.ReadAllText(file, new UTF8Encoding(false));
            if (content.Contains(text))
            {
                result.Add(file);
            }
        }
        return string.Join("\n", result);
    }
}
