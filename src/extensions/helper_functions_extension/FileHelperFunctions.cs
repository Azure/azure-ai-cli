//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

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
                : $"File not found: {fileName}";
            return content;
        }

        [HelperFunctionDescription("Writes text into a file; if the file exists, it is overwritten")]
        public static bool CreateFileAndSaveText(string fileName, string text)
        {
            FileHelpers.WriteAllText(fileName, text, new UTF8Encoding(false));
            return true;
        }

        [HelperFunctionDescription("Opens a text file for line editing; returns the file content as a string with line numbers")]
        public static string OpenTextFileForLineEditing(string fileName)
        {
            return FileLineEditorHelpers.OpenFileForLineEditing(fileName);
        }

        [HelperFunctionDescription("Removes lines from a file previously opened for line editing; line numbers must be original line numbers returned from OpenTextFileForLineEditing")]
        public static string RemoveLinesFromFile(string fileName, int firstLineToRemove, int lastLineToRemove)
        {
            return FileLineEditorHelpers.RemoveLinesFromFile(fileName, firstLineToRemove, lastLineToRemove);
        }

        [HelperFunctionDescription("Inserts lines before a line in a file previously opened for line editing; line numbers must be original line numbers returned from OpenTextFileForLineEditing")]
        public static string InsertLinesIntoFileBeforeLine(string fileName, string text, int lineNumber)
        {
            return FileLineEditorHelpers.InsertLinesIntoFileBeforeOrAfterLine(fileName, text, lineNumber, true, false);
        }

        [HelperFunctionDescription("Inserts lines after a line in a file previously opened for line editing; line numbers must be original line numbers returned from OpenTextFileForLineEditing")]
        public static string InsertLinesIntoFileAfterLine(string fileName, string text, int lineNumber)
        {
            return FileLineEditorHelpers.InsertLinesIntoFileBeforeOrAfterLine(fileName, text, lineNumber, false, true);
        }

        [HelperFunctionDescription("Moves a block of lines before a line in a file previously opened for line editing; line numbers must be original line numbers returned from OpenTextFileForLineEditing")]
        public static string MoveLinesBeforeLine(string fileName, int firstLineToMove, int lastLineToMove, int insertBeforeLineNumber)
        {
            return FileLineEditorHelpers.MoveLineBlockBeforeOrAfterLine(fileName, firstLineToMove, lastLineToMove, insertBeforeLineNumber, true, false);
        }

        [HelperFunctionDescription("Moves a block of lines after a line in a file previously opened for line editing; line numbers must be original line numbers returned from OpenTextFileForLineEditing")]
        public static string MoveLinesAfterLine(string fileName, int firstLineToMove, int lastLineToMove, int insertAfterLineNumber)
        {
            return FileLineEditorHelpers.MoveLineBlockBeforeOrAfterLine(fileName, firstLineToMove, lastLineToMove, insertAfterLineNumber, false, true);
        }

        [HelperFunctionDescription("Creates a directory if it doesn't already exist")]
        public static bool DirectoryCreate(string directoryName)
        {
            FileHelpers.EnsureDirectoryForFileExists($"{directoryName}/.");
            return true;
        }

        [HelperFunctionDescription("List files; lists all files matching pattern; searches current directory, and if pattern includes '**', all sub-directories")]
        public static string FindAllFilesMatchingPattern([HelperFunctionParameterDescription("The pattern to search for; use '**/*.ext' to search sub-directories")] string pattern)
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
