//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Text;

namespace Azure.AI.Details.Common.CLI.Extensions.HelperFunctions
{
    public static class FileLineEditorHelpers
    {
        public static string OpenFileForLineEditing(string fileName)
        {
            var content = FileHelpers.FileExists(fileName)
                ? FileHelpers.ReadAllText(fileName, new UTF8Encoding(false))
                : null;
            if (content == null) return $"File not found: {fileName}";

            var linePairs = new List<KeyValuePair<int, string?>>();
            _lineEditableFiles[fileName] = linePairs;

            var lines = content.Split('\n').ToList();
            for (var i = 0; i < lines.Count; i++)
            {
                linePairs.Add(new KeyValuePair<int, string?>(i + 1, lines[i]));
            }

            return string.Join("\n", linePairs.Select(p => $"{p.Key}: {p.Value}"));
        }

        public static string RemoveLinesFromFile(string fileName, int firstLineToRemove, int lastLineToRemove)
        {
            if (!_lineEditableFiles.ContainsKey(fileName)) return $"File not opened for line editing: {fileName}; use OpenTextFileForLineEditing() first";

            var linePairs = _lineEditableFiles[fileName];
            var firstToRemove = linePairs.FindIndex(x => x.Key == firstLineToRemove);
            if (firstToRemove < 0) return $"Line number {firstLineToRemove} not found in file {fileName}; re-open the file for line editing";

            var lastToRemove = linePairs.FindIndex(x => x.Key == lastLineToRemove);
            if (lastToRemove < 0) return $"Line number {lastLineToRemove} not found in file {fileName}; re-open the file for line editing";

            for (var i = firstToRemove; i <= lastToRemove; i++)
            {
                var line = linePairs[i];
                linePairs[i] = new KeyValuePair<int, string?>(line.Key, null);
            }

            UpdateFileContent(fileName, linePairs);

            return $"Removed lines {firstLineToRemove} thru {lastLineToRemove}, inclusive";
        }

        public static string InsertLinesIntoFileBeforeOrAfterLine(string fileName, string text, int lineNumber, bool insertBefore, bool insertAfter)
        {
            if (!_lineEditableFiles.ContainsKey(fileName)) return $"File not opened for line editing: {fileName}; use OpenTextFileForLineEditing() first";
            if (insertBefore && insertAfter) throw new ArgumentException("Only one of insertBefore and insertAfter can be true");

            var linePairs = _lineEditableFiles[fileName];
            var insertionPoint = linePairs.FindIndex(x => x.Key == lineNumber);
            if (insertionPoint < 0) return $"Line number {lineNumber} not found in file {fileName}; re-open the file for line editing";

            var pairsPart1 = insertBefore
                ? linePairs.Take(insertionPoint).ToList()
                : linePairs.Take(insertionPoint + 1).ToList();
            var insertPairs = text.Split('\n').Select(x => new KeyValuePair<int, string>(-1, x)).ToList();
            var pairsPart2 = insertAfter
                ? linePairs.Skip(insertionPoint + 1).ToList()
                : linePairs.Skip(insertionPoint).ToList();

            var allPairs = pairsPart1!.Concat(insertPairs).Concat(pairsPart2!).ToList();
            _lineEditableFiles[fileName] = allPairs!;

            UpdateFileContent(fileName, allPairs!);

            return insertBefore
                ? $"Inserted {insertPairs.Count} lines before line {lineNumber}"
                : $"Inserted {insertPairs.Count} lines after line {lineNumber}";
        }

        private static void UpdateFileContent(string fileName, List<KeyValuePair<int, string?>> linePairs)
        {
            var keepNonNull = linePairs.Where(p => p.Value != null).ToList();
            var newContent = string.Join("\n", keepNonNull.Select(p => p.Value));
            FileHelpers.WriteAllText(fileName, newContent, new UTF8Encoding(false));
        }

        private static Dictionary<string, List<KeyValuePair<int, string?>>> _lineEditableFiles = new();
    }
}
