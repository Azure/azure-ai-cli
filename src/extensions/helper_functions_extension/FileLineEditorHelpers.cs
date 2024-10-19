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

            var lines = content.Split('\n')
                .Select(x => x.Trim('\r'))
                .ToList();
            for (var i = 0; i < lines.Count; i++)
            {
                linePairs.Add(new KeyValuePair<int, string?>(i + 1, lines[i]));
            }

            return string.Join(Environment.NewLine, linePairs.Select(p => $"{p.Key}: {p.Value}"));
        }

        public static string MoveLineBlockBeforeOrAfterLine(string fileName, int firstLineNumberToMove, int lastLineNumberToMove, int insertBeforeOrAfterLineNumber, bool insertBefore, bool insertAfter)
        {
            if (!_lineEditableFiles.ContainsKey(fileName)) return $"File not opened for line editing: {fileName}; use OpenTextFileForLineEditing() first";

            var linePairs = _lineEditableFiles[fileName];
            var firstToMove = linePairs.FindIndex(x => x.Key == firstLineNumberToMove);
            if (firstToMove < 0) return $"Line number {firstLineNumberToMove} not found in file {fileName}; re-open the file for line editing";

            var lastToMove = linePairs.FindIndex(x => x.Key == lastLineNumberToMove);
            if (lastToMove < 0) return $"Line number {lastLineNumberToMove} not found in file {fileName}; re-open the file for line editing";

            var moved = new List<KeyValuePair<int, string>>();
            for (var i = firstToMove; i <= lastToMove; i++)
            {
                var line = linePairs[i];
                linePairs[i] = new KeyValuePair<int, string?>(line.Key, null);
                if (line.Value != null) moved.Add(line!);
            }

            var insertionPoint = linePairs.FindIndex(x => x.Key == insertBeforeOrAfterLineNumber);
            if (insertionPoint < 0) return $"Line number {insertBeforeOrAfterLineNumber} not found in file {fileName}; re-open the file for line editing";

            var pairsPart1 = insertBefore
                ? linePairs.Take(insertionPoint).ToList()
                : linePairs.Take(insertionPoint + 1).ToList();
            var insertPairs = moved
                .Select(x => new KeyValuePair<int, string>(-1, x.Value))
                .ToList();
            var pairsPart2 = insertAfter
                ? linePairs.Skip(insertionPoint + 1).ToList()
                : linePairs.Skip(insertionPoint).ToList();

            var allPairs = pairsPart1!.Concat(insertPairs).Concat(pairsPart2!).ToList();
            _lineEditableFiles[fileName] = allPairs!;

            return UpdateFileContent(fileName, allPairs!);
        }

        public static string RemoveLinesFromFile(string fileName, int firstLineToRemove, int lastLineToRemove)
        {
            if (!_lineEditableFiles.ContainsKey(fileName)) return $"File not opened for line editing: {fileName}; use OpenTextFileForLineEditing() first";

            var linePairs = _lineEditableFiles[fileName];
            var firstToRemove = linePairs.FindIndex(x => x.Key == firstLineToRemove);
            if (firstToRemove < 0) return $"Line number {firstLineToRemove} not found in file {fileName}; re-open the file for line editing";

            var lastToRemove = linePairs.FindIndex(x => x.Key == lastLineToRemove);
            if (lastToRemove < 0) return $"Line number {lastLineToRemove} not found in file {fileName}; re-open the file for line editing";

            var removed = new List<KeyValuePair<int, string?>>();
            for (var i = firstToRemove; i <= lastToRemove; i++)
            {
                var line = linePairs[i];
                linePairs[i] = new KeyValuePair<int, string?>(line.Key, null);
                removed.Add(line);
            }

            return UpdateFileContent(fileName, linePairs);
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
            var insertPairs = text.Split('\n')
                .Select(x => x.Trim('\r'))
                .Select(x => new KeyValuePair<int, string>(-1, x))
                .ToList();
            var pairsPart2 = insertAfter
                ? linePairs.Skip(insertionPoint + 1).ToList()
                : linePairs.Skip(insertionPoint).ToList();

            var allPairs = pairsPart1!.Concat(insertPairs).Concat(pairsPart2!).ToList();
            _lineEditableFiles[fileName] = allPairs!;

            return UpdateFileContent(fileName, allPairs!);
        }

        private static string UpdateFileContent(string fileName, List<KeyValuePair<int, string?>> linePairs)
        {
            var keepNonNull = linePairs.Where(p => p.Value != null).ToList();
            var newContent = string.Join(Environment.NewLine, keepNonNull.Select(p => p.Value));
            FileHelpers.WriteAllText(fileName, newContent, new UTF8Encoding(false));
            
            return $"Updated `{fileName}`:\n{OpenFileForLineEditing(fileName)}";
        }

        private static Dictionary<string, List<KeyValuePair<int, string?>>> _lineEditableFiles = new();
    }
}
