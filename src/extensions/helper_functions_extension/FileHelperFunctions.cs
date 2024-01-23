using System.Text;
using Azure.AI.OpenAI;

namespace Azure.AI.Details.Common.CLI.Extensions.HelperFunctions
{
    public static class FileHelperFunctions
    {
        [HelperFunctionDescription("Checks if file exists")]
        public static bool FileExists(string fileName)
        {
            var exists = FileHelpers.FileExists(fileName);
            return exists;
        }

        [HelperFunctionDescription("Reads text from a file; returns empty string if file does not exist")]
        public static string ReadTextFromFile(string fileName)
        {
            var content = FileHelpers.FileExists(fileName)
                ? FileHelpers.ReadAllText(fileName, new UTF8Encoding(false))
                : string.Empty;
            return content;
        }

        [HelperFunctionDescription("Writes text into a file; if the file exists, it is overwritten")]
        public static bool CreateFileAndSaveText(string fileName, string text)
        {
            FileHelpers.WriteAllText(fileName, text, new UTF8Encoding(false));
            return true;
        }

        [HelperFunctionDescription("Appends text to a file; if the file does not exist, it is created")]
        public static bool AppendTextToFile(string fileName, string text)
        {
            FileHelpers.AppendAllText(fileName, text, new UTF8Encoding(false));
            return true;
        }

        [HelperFunctionDescription("Creates a directory if it doesn't already exist")]
        public static bool DirectoryCreate(string directoryName)
        {
            FileHelpers.EnsureDirectoryForFileExists($"{directoryName}/.");
            return true;
        }


        [HelperFunctionDescription("List files; lists all files regardless of name")]
        public static string FindAllFiles()
        {
            return FindFilesMatchingPattern("**/*");
        }

        [HelperFunctionDescription("List files; lists files matching pattern")]
        public static string FindFilesMatchingPattern([HelperFunctionParameterDescription("The pattern to search for; use '**/*.ext' to search sub-directories")] string pattern)
        {
            var files = FileHelpers.FindFiles(".", pattern);
            return string.Join("\n", files);
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
            var files = FileHelpers.FindFiles(".", pattern);
            var result = new List<string>();
            foreach (var file in files)
            {
                var content = FileHelpers.ReadAllText(file, new UTF8Encoding(false));
                if (content.Contains(text))
                {
                    result.Add(file);
                }
            }
            return string.Join("\n", result);
        }
    }
}
