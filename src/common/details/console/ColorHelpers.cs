//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;
using Azure.AI.Details.Common.CLI;

namespace Azure.AI.Details.Common.CLI
{
    public static class ColorHelpers
    {
        public static void ResetColor()
        {
            Console.ResetColor();
        }

        public static void SetHighlightColors()
        {
            Console.BackgroundColor = colors[0];
            Console.ForegroundColor = colors[1];
        }

        public static Colors GetHighlightColors()
        {
            return new Colors(colors[1], colors[0]);
        }

        public static Colors GetHighlightColors(ConsoleColor fg, ConsoleColor bg)
        {
            var colors = GetBestColors(fg, bg);
            return new Colors(colors[1], colors[0]);
        }

        public static void SetErrorColors()
        {
            Console.BackgroundColor = colors[2];
            Console.ForegroundColor = colors[3];
        }

        public static Colors GetErrorColors()
        {
            return new Colors(colors[3], colors[2]);
        }

        public static Colors GetErrorColors(ConsoleColor fg, ConsoleColor bg)
        {
            var colors = GetBestColors(fg, bg);
            return new Colors(colors[3], colors[2]);
        }

        public static bool TryParseColorStyleText(string text, out Colors parsedStyle, out string parsedText)
        {
            if (text.Length > 2 && text[0] == '#')
            {
                if (text.StartsWith("#example;"))
                {
                    parsedStyle = new Colors(ConsoleColor.Yellow, ConsoleColor.Black);
                    parsedText = text.Substring(9);
                    return true;
                }
                else if (text.StartsWith("#error;"))
                {
                    parsedStyle = GetErrorColors();
                    parsedText = text.Substring(7);
                    return true;
                }
                else if (text.Length > 4 && text[3] == ';')
                {
                    var fg = text[1] == '_' ? Console.ForegroundColor : (ConsoleColor)(int.Parse(text.Substring(1, 1), System.Globalization.NumberStyles.HexNumber));
                    var bg = text[2] == '_' ? Console.BackgroundColor : (ConsoleColor)(int.Parse(text.Substring(2, 1), System.Globalization.NumberStyles.HexNumber));
                    parsedStyle = new Colors(fg, bg);
                    parsedText = text.Substring(4);
                    return true;
                }
            }
            parsedStyle = null;
            parsedText = null;
            return false;
        }

        private static ConsoleColor[] GetBestColors()
        {
            var bg = Console.BackgroundColor;
            var fg = Console.ForegroundColor;
            return GetBestColors(fg, bg);
        }

        private static ConsoleColor[] GetBestColors(ConsoleColor fg, ConsoleColor bg)
        {
            var fgIsWhite = fg == ConsoleColor.White || fg == ConsoleColor.Gray;
            var fgIsBlack = fg == ConsoleColor.Black;
            ConsoleColor[] colors = { ConsoleColor.White, ConsoleColor.Black, ConsoleColor.DarkRed, ConsoleColor.White };
            switch (bg)
            {
                case ConsoleColor.Black:
                    colors[0] = ConsoleColor.DarkGray;
                    colors[1] = ConsoleColor.White;
                    break;

                case ConsoleColor.DarkBlue:
                    colors[0] = ConsoleColor.Gray;
                    colors[1] = ConsoleColor.Black;
                    break;

                case ConsoleColor.DarkGreen:
                    colors[0] = ConsoleColor.Gray;
                    colors[1] = ConsoleColor.Black;
                    colors[2] = ConsoleColor.Gray;
                    colors[3] = ConsoleColor.DarkRed;
                    break;

                case ConsoleColor.DarkCyan:
                    colors[0] = ConsoleColor.Yellow;
                    colors[1] = ConsoleColor.Black;
                    colors[2] = ConsoleColor.White;
                    colors[3] = ConsoleColor.DarkRed;
                    break;

                case ConsoleColor.DarkRed:
                    colors[0] = ConsoleColor.Yellow;
                    colors[1] = ConsoleColor.Black;
                    colors[2] = ConsoleColor.White;
                    colors[3] = ConsoleColor.DarkRed;
                    break;

                case ConsoleColor.DarkMagenta:
                    colors[0] = ConsoleColor.Yellow;
                    colors[1] = ConsoleColor.Black;
                    colors[2] = ConsoleColor.White;
                    colors[3] = ConsoleColor.DarkRed;
                    break;

                case ConsoleColor.DarkYellow:
                    colors[0] = ConsoleColor.Gray;
                    colors[1] = ConsoleColor.Black;
                    colors[2] = ConsoleColor.Gray;
                    colors[3] = ConsoleColor.DarkRed;
                    break;

                case ConsoleColor.Gray:
                    colors[0] = ConsoleColor.White;
                    colors[1] = ConsoleColor.Black;
                    colors[2] = ConsoleColor.White;
                    colors[3] = ConsoleColor.DarkRed;
                    break;

                case ConsoleColor.DarkGray:
                    colors[0] = ConsoleColor.White;
                    colors[1] = ConsoleColor.Black;
                    colors[2] = ConsoleColor.Gray;
                    colors[3] = ConsoleColor.DarkRed;
                    break;

                case ConsoleColor.Blue:
                    colors[0] = fgIsBlack ? ConsoleColor.Gray : ConsoleColor.White;
                    colors[1] = ConsoleColor.Black;
                    break;

                case ConsoleColor.Green:
                    colors[0] = ConsoleColor.Gray;
                    colors[1] = ConsoleColor.Black;
                    colors[2] = ConsoleColor.White;
                    colors[3] = ConsoleColor.DarkRed;
                    break;

                case ConsoleColor.Cyan:
                    colors[0] = ConsoleColor.White;
                    colors[1] = ConsoleColor.Black;
                    break;

                case ConsoleColor.Red:
                    colors[0] = ConsoleColor.White;
                    colors[1] = ConsoleColor.Black;
                    colors[2] = ConsoleColor.White;
                    colors[3] = ConsoleColor.DarkRed;
                    break;

                case ConsoleColor.Magenta:
                    colors[0] = ConsoleColor.Yellow;
                    colors[1] = ConsoleColor.Black;
                    colors[2] = ConsoleColor.Gray;
                    colors[3] = ConsoleColor.DarkRed;
                    break;

                case ConsoleColor.Yellow:
                    colors[0] = ConsoleColor.Gray;
                    colors[1] = ConsoleColor.Black;
                    colors[2] = ConsoleColor.DarkRed;
                    colors[3] = ConsoleColor.White;
                    break;

                case ConsoleColor.White:
                    colors[0] = ConsoleColor.DarkGray;
                    colors[1] = ConsoleColor.White;
                    colors[2] = ConsoleColor.DarkRed;
                    colors[3] = ConsoleColor.White;
                    break;
            }

            return colors;
        }

        public static void ShowColorChart()
        {
            Type type = typeof(ConsoleColor);
            Console.ForegroundColor = ConsoleColor.White;
            foreach (var bg in Enum.GetNames(type))
            {
                var bgc = (ConsoleColor)Enum.Parse(type, bg);
                
                Console.ResetColor();
                Console.Write($"{bg,15}: {(int)bgc,-2}: ");
                Console.BackgroundColor = bgc;

                foreach (var fg in Enum.GetNames(type))
                {
                    var fgc = (ConsoleColor)Enum.Parse(type, fg);
                    Console.ForegroundColor = fgc;
                    Console.Write($"  {(int)fgc,-2}  ");
                }

                Console.WriteLine();
            }

            Console.ResetColor();
        }

        private static ConsoleColor[] InitBestColors()
        {
            Console.ResetColor();
            return GetBestColors();
        }

        private static ConsoleColor[] colors = InitBestColors();

    }
}
