//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Azure.AI.Details.Common.CLI
{
    public static class ConsoleHelpers
    {
        public static string ReadAllStandardInputText()
        {
            if (stdinText == null)
            {
                stdinText = FileHelpers.ReadAllStreamText(Console.OpenStandardInput(), Encoding.UTF8);
            }
            return stdinText;
        }

        public static byte[] ReadAllStandardInputBytes()
        {
            if (stdinBytes == null)
            {
                stdinBytes = FileHelpers.ReadAllStreamBytes(Console.OpenStandardInput());
            }
            return stdinBytes;
        }

        public static void WriteAllLines(IEnumerable<string> contents)
        {
            foreach (var line in contents)
            {
                Console.WriteLine(line);
            }
        }

        /// <summary>
        /// Writes the text specified using ErrorColor
        /// </summary>
        public static void WriteError(string text)
        {
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0) Console.WriteLine();

                var line = lines[i].TrimEnd();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var x = 0;
                for (; x < line.Length && line[x] == ' '; x++)
                {
                    Console.Write(' ');
                }

                ColorHelpers.SetErrorColors();
                Console.Write(line.Substring(x));
                ColorHelpers.ResetColor();
            }
        }

        /// <summary>
        /// Writes the line specified using ErrorColor
        /// </summary>
        public static void WriteLineError(string text)
        {
            WriteError(text);
            Console.WriteLine();
        }

        /// <summary>
        /// Writes a line to Console.Out with delimited sections rendered in the specified foreground color.
        /// Background color will be automatically selected for maximum readability across console environments.
        /// The default delimiter is backtick ('`') and the default highlight color is Blue.
        /// </summary>
        /// <param name="text"> The text, with or without delimiter pairs, to render </param>
        /// <param name="delimiter"> The character delimiter to use when highlighting </param>
        public static void WriteLineWithHighlight(string text, char delimiter = '`')
        {
            var colorOn = false;
            foreach (var part in text.Split(delimiter))
            {
                if (!colorOn)
                {
                    ColorHelpers.ResetColor();
                    Console.Write(part);
                }
                else if (ColorHelpers.TryParseColorStyleText(part, out var colors, out text))
                {
                    Console.ForegroundColor = colors.Foreground;
                    Console.BackgroundColor = colors.Background;
                    Console.Write(text);
                }
                else
                {
                    ColorHelpers.SetHighlightColors();
                    Console.Write(part);
                }


                colorOn = !colorOn;
            }

            Console.ResetColor();
            Console.WriteLine();
        }

        private static string stdinText = null;
        private static byte[] stdinBytes = null;
    }
}
