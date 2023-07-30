//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Runtime.InteropServices;

namespace Azure.AI.Details.Common.CLI.ConsoleGui
{
    public class Screen
    {
        public static Screen Current
        {
            get { return _current; }
        }

        public Screen()
        {
        }

        #region cursor and colors

        public void SetCursorVisible(bool visible)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.CursorVisible = visible;
            }
        }

        public static bool GetCursorVisible()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Console.CursorVisible;
        }

        public Colors ColorsStart
        {
            get { return _initialColors; }
        }

        public Colors ColorsNow
        {
            get { return GetColorsNow(); }
        }

        public void SetColors(Colors colors)
        {
            Console.ForegroundColor = colors.Foreground;
            Console.BackgroundColor = colors.Background;
        }

        #endregion

        #region position and buffer

        public bool IsValidCursorPosition(int x, int y)
        {
            return y <= _biggestYSoFar;
        }

        private static int GetTopRow()
        {
            return Current._initialTop;
        }

        public static int GetBottomRow()
        {
            return GetTopRow() + GetWindowHeight() - 1;
        }

        private static int GetLeftColumn()
        {
            return Current._initialLeft;
        }

        public static int GetRightColumn()
        {
            return GetLeftColumn() + GetWindowWidth() - 1;
        }

        private static int GetWindowHeight()
        {
            return Current._initialHeight;
        }

        private static int GetWindowWidth()
        {
            return Current._initialWidth;
        }


        public bool MoveCursorPosition(int dx, int dy)
        {
            return SetCursorPosition(Console.CursorLeft + dx, Console.CursorTop + dy);
        }

        public int GetCursorSize()
        {
            return Console.CursorSize;
        }

        public void SetCursorSize(int size)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.CursorSize = size;
            }
        }

        public bool SetCursorPosition(int x, int y)
        {
            if (!IsValidCursorPosition(x, y))
            {
                throw new ArgumentOutOfRangeException("y", y, "SetCursor attempted to move Row without ensuring space available. Ensure space is available using MakeSpaceAtCursor()");
            }

            if (x < 0 || x >= GetWindowWidth())
            {
                return false;
            }

            Console.SetCursorPosition(x, y);
            return true;
        }

        public Rect MakeSpaceAtCursor(int width, int height)
        {
            if (height > GetWindowHeight())
            {
                height = GetWindowHeight();
            }
            if (width > GetWindowWidth())
            {
                width = GetWindowWidth();
            }

            var left0 = Console.CursorLeft;
            var top0 = Console.CursorTop;

            var x = left0 + width;
            var y = top0 + height;

            if (x >= GetWindowWidth())
            {
                var tooWide = x - GetWindowWidth();
                width -= tooWide;
            }

            if (y > _biggestYSoFar)
            {
                UpdateBiggestYSoFar();

                var linesNeeded = y - top0 - 1;
                if (linesNeeded > 0)
                {
                    InsertLines(linesNeeded);
                    UpdateBiggestYSoFar();

                    var dx = left0 - Console.CursorLeft;
                    var dy = -linesNeeded;
                    MoveCursorPosition(dx, dy); // move right, and up
                }
            }

            return new Rect(Console.CursorLeft, Console.CursorTop, width, height);
        }

        private void InsertLines(int linesNeeded)
        {
            var beforeReset = ResetColors();
            for (int i = 0; i < linesNeeded; i++)
            {
                Console.WriteLine();
            }
            SetColors(beforeReset);
        }

        private void UpdateBiggestYSoFar()
        {
            var current = Console.CursorTop;
            if (current > _biggestYSoFar) _biggestYSoFar = current;
        }

        #endregion

        #region write char/text

        public void WriteChar(char ch, int count = 1)
        {
            if (count == 1)
            {
                Console.Write(ch);
            }
            else if (count > 1)
            {
                var text = new string(ch, count);
                WriteText(text);
            }
        }

        public void WriteText(string text, int cch = int.MaxValue)
        {
            text = text.Trim('\r', '\n');
            if (text.Length > cch)
            {
                Console.Write(text.Substring(0, cch));
            }
            else
            {
                Console.Write(text);
                if (cch != int.MaxValue)
                {
                    var padding = cch - text.Length;
                    WriteChar(' ', padding);
                }
            }
        }

        public void WriteChar(int x, int y, char ch, int count = 1)
        {
            Cursor saved1 = null;
            Cursor saved2 = null;
            if (x + count > GetRightColumn())
            {
                saved1 = new Cursor(GetLeftColumn(), GetTopRow());
                saved2 = new Cursor(GetRightColumn(), y);
            }

            if (SetCursorPosition(x, y))
            {
                WriteChar(ch, count);
                saved1?.Restore();
                saved2?.Restore();
            }
        }

        public void WriteText(int x, int y, string text, int cch = int.MaxValue)
        {
            if (SetCursorPosition(x, y))
            {
                WriteText(text, cch);
            }
        }

        public void WriteChar(Colors colors, int x, int y, char ch, int count = 1)
        {
            SetColors(colors);
            WriteChar(x, y, ch, count);
        }

        public void WriteText(Colors colors, int x, int y, string text, int cch = int.MaxValue)
        {
            SetColors(colors);
            WriteText(x, y, text, cch);
        }

        public void WriteTextWithHighlight(Colors normal, Colors highlight, int x, int y, string text, int cch = int.MaxValue, char delimiter = '`')
        {
            var colorOn = false;
            var color = normal;
            foreach (var textPiece in text.Split(delimiter))
            {
                WriteText(color, x, y, textPiece, cch);
                cch -= textPiece.Length;
                x += textPiece.Length;

                colorOn = !colorOn;
                color = colorOn ? highlight : normal;
            }

            Console.ResetColor();
            Console.WriteLine();
        }

        #endregion

        #region reset

        public void Reset()
        {
            ResetColors();
            SetCursorVisible(_initialCursorVisible);
            _current = new Screen();
        }

        public Colors ResetColors()
        {
            var beforeReset = ColorsNow;
            SetColors(_initialColors);
            return beforeReset;
        }

        #endregion

        #region private methods and data

        private static Colors GetColorsNow()
        {
            return new Colors(Console.ForegroundColor, Console.BackgroundColor);
        }

        private static Screen _current = new Screen();
        private int _initialWidth = Console.WindowWidth;
        private int _initialHeight = Console.WindowHeight;
        private int _initialTop = Console.CursorTop; // Console.WindowTop;
        private int _initialLeft = Console.WindowLeft;
        private bool _initialCursorVisible = GetCursorVisible();
        private Colors _initialColors = GetColorsNow();
        private int _biggestYSoFar = 0;

        #endregion
    }
}