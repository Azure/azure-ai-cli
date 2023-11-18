//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Text;

namespace Azure.AI.Details.Common.CLI.ConsoleGui
{
    public class ConsoleReadMultiLineTextBox : EditBoxControl
    {
        public static string ReadLine(int width , int height, Colors normal, int maxCch = 1024)
        {
            return ReadLines(width, height, normal, true, maxCch);
        }

        public static string ReadLines(int width, int height, Colors normal, int maxCch = 1024)
        {
            return ReadLines(width, height, normal, false, maxCch);
        }

        protected static string ReadLines(int width, int height, Colors normal, bool shiftRequiredToBeMultiline, int maxCch = 1024)
        {
            var lines = new StringBuilder();
            while (true)
            {
                var rect = Screen.Current.MakeSpaceAtCursor(width, height);
                height = Math.Max(height - 1, 1);

                var textBox = new ConsoleReadMultiLineTextBox(null, rect, normal, "", maxCch);
                textBox.Run();

                Console.WriteLine(textBox._readLineText);
                if (textBox._pressedEscape) return null;

                var isFirstLine = lines.Length == 0;
                if (!isFirstLine) lines.AppendLine();
                lines.Append(textBox._readLineText);

                if (shiftRequiredToBeMultiline && isFirstLine && !textBox._pressedShiftEnter) return lines.ToString();

                if (textBox._pressedControlEnter) return lines.ToString();
                if (textBox._pressedAltEnter) return lines.ToString();
            }
        }

        #region protected methods

        protected ConsoleReadMultiLineTextBox(Window parent, Rect rect, Colors colorNormal, string text = "", int maxCch = 1024, string picture = null, string border = null) : base(parent, rect, colorNormal, text, maxCch, picture, border, true)
        {
        }

        public override bool ProcessKey(ConsoleKeyInfo key)
        {
            var processed = false;
            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    _readLineText = null;
                    _pressedEscape = true;
                    processed = true;
                    break;

                case ConsoleKey.Enter:
                    _readLineText = GetText();
                    _pressedEnter = true;
                    _pressedAltEnter = ConsoleModifiers.Alt == (key.Modifiers & ConsoleModifiers.Alt);
                    _pressedShiftEnter = ConsoleModifiers.Shift == (key.Modifiers & ConsoleModifiers.Shift);
                    _pressedControlEnter = ConsoleModifiers.Control == (key.Modifiers & ConsoleModifiers.Control);
                    processed = true;
                    break;
            }

            if (processed)
            {
                Close();
                return true;
            }

            return base.ProcessKey(key);
        }

        #endregion

        #region private data

        private string _readLineText;
        private bool _pressedEscape = false;
        private bool _pressedAltEnter = false;
        private bool _pressedShiftEnter = false;
        private bool _pressedControlEnter = false;
        private bool _pressedEnter;

        #endregion
    }
}
