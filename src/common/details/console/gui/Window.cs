//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;

namespace Azure.AI.Details.Common.CLI.ConsoleGui
{
    public class Window
    {
        public class Borders
        {
            public static string SingleLine = "\U0000250C\U00002500\U00002510\U00002502 \U00002502\U00002514\U00002500\U00002518";
        }

        public Window(Window? parent, Rect rect, Colors colors, string? border = null)
        {
            this._parent = parent;

            this._colors = colors;
            this._border = border;

            SetRect(rect);
        }

        #region parent and position

        public Window? GetParent()
        {
            return _parent;
        }

        public int X
        {
            get { return _rect.X; }
        }

        public int Y
        {
            get { return _rect.Y; }
        }
        
        public int Width
        {
            get { return _rect.Width; }
        }

        public int Height
        {
            get { return _rect.Height; }
        }

        public Rect ClientRect
        {
            get { return _rectClient; }
        }

        #endregion

        #region color and border

        public Colors Colors
        {
            get { return _colors; }
        }

        public bool IsBorder()
        {
            return _border != null;
        }

        public string? Border
        {
            get { return _border; }
        }

        #endregion

        #region open/close/focus

        public virtual bool Open()
        {
            _open = true;
            PaintWindow(Colors, Border);
            return true;
        }

        public virtual bool Close()
        {
            if (IsOpen())
            {
                RestoreScreen();
                _open = false;
            }

            return true;
        }

        public bool IsOpen()
        {
            return _open;
        }

        public virtual bool SetFocus()
        {
            if (!_focus)
            {
                _focus = true;

                _killFocusCursorVisible = Screen.GetCursorVisible();
                Screen.Current.SetCursorVisible(false);

                return true;
            }

            return false;
        }

        public virtual void KillFocus()
        {
            if (_focus)
            {
                _focus = false;
                Screen.Current.SetCursorVisible(_killFocusCursorVisible);
            }
        }

        public bool IsFocus()
        {
            return _focus;
        }

        #endregion

        #region run/process keys

        public bool Run()
        {
            SetFocus();

            if (!IsOpen())
            {
                Open();
            }

            while (IsOpen())
            {
                DispatchEvents();
            }
            KillFocus();

            return true;
        }

        public virtual bool ProcessKey(ConsoleKeyInfo key)
        {
            return false;
        }

        public virtual bool ProcessHotKey(ConsoleKeyInfo key)
        {
            return false;
        }

        #endregion

        #region write text/char

        public void WriteChar(char ch, int count = 1)
        {
            if (IsOpen() && Height > 0)
            {
                Screen.Current.WriteChar(ch, count);
            }
        }

        public void WriteText(string text, int cch = int.MaxValue)
        {
            if (IsOpen() && Height > 0)
            {
                Screen.Current.WriteText(text, cch);
            }
        }

        public void WriteChar(int x, int y, char ch, int count = 1)
        {
            if (IsOpen() && Height > 0)
            {
                Screen.Current.WriteChar(X + x, Y + y, ch, count);
            }
        }

        public void WriteText(int x, int y, string text, int cch = int.MaxValue)
        {
            if (IsOpen() && Height > 0)
            {
                Screen.Current.WriteText(X + x, Y + y, text, cch);
            }
        }

        public void WriteChar(Colors colors, int x, int y, char ch, int count = 1)
        {
            if (IsOpen() && Height > 0)
            {
                Screen.Current.WriteChar(colors, X + x, Y + y, ch, count);
            }
        }

        public void WriteText(Colors colors, int x, int y, string text, int cch = int.MaxValue)
        {
            if (IsOpen() && Height > 0)
            {
                Screen.Current.WriteText(colors, X + x, Y + y, text, cch);
            }
        }

        public void WriteTextWithHighlight(Colors colors, Colors highlight, int x, int y, string text, int cch = int.MaxValue)
        {
            if (IsOpen() && Height > 0)
            {
                Screen.Current.WriteTextWithHighlight(colors, highlight, X + x, Y + y, text, cch);
            }
        }

        #endregion

        #region write client text/char
        
        public void WriteClientChar(int x, int y, char ch, int count = 1)
        {
            if (IsOpen() && Height > 0)
            {
                Screen.Current.WriteChar(_rectClient.X + x, _rectClient.Y + y, ch, count);
            }
        }

        public void WriteClientText(int x, int y, string text, int cch = int.MaxValue)
        {
            if (IsOpen() && Height > 0)
            {
                Screen.Current.WriteText(_rectClient.X + x, _rectClient.Y + y, text, cch);
            }
        }

        public void WriteClientChar(Colors colors, int x, int y, char ch, int count = 1)
        {
            if (IsOpen() && Height > 0)
            {
                Screen.Current.WriteChar(colors, _rectClient.X + x, _rectClient.Y + y, ch, count);
            }
        }

        public void WriteClientText(Colors colors, int x, int y, string text, int cch = int.MaxValue)
        {
            if (IsOpen() && Height > 0)
            {
                Screen.Current.WriteText(colors, _rectClient.X + x, _rectClient.Y + y, text, cch);
            }
        }

        public void WriteClientTextWithHighlight(Colors colors, Colors highlight, int x, int y, string text, int cch = int.MaxValue)
        {
            if (IsOpen() && Height > 0)
            {
                Screen.Current.WriteTextWithHighlight(colors, highlight, _rectClient.X + x, _rectClient.Y + y, text, cch);
            }
        }

        #endregion

        #region protected methods

        protected virtual void PaintWindow(Colors colors, string? border = null)
        {
            if (border != null && border.Length == 9)
            {
                if (border[0] >= 32) WriteChar(colors, 0, 0, border[0]);
                if (border[1] >= 32) WriteChar(colors, 1, 0, border[1], _rectClient.Width);
                if (border[2] >= 32) WriteChar(colors, _rectClient.Width + 1, 0, border[2]);

                for (int y = 1; y <= _rectClient.Height; y++)
                {
                    if (border[3] >= 32) WriteChar(colors, 0, y, border[3]);
                    if (border[4] >= 32) WriteChar(colors, 1, y, border[4], _rectClient.Width);
                    if (border[5] >= 32) WriteChar(colors, _rectClient.Width + 1, y, border[5]);
                }

                if (border[6] >= 32) WriteChar(colors, 0, _rectClient.Height + 1, border[6]);
                if (border[7] >= 32) WriteChar(colors, 1, _rectClient.Height + 1, border[7], _rectClient.Width);
                if (border[8] >= 32) WriteChar(colors, _rectClient.Width + 1, _rectClient.Height + 1, border[8]);
            }
            else
            {
                for (int y = 0; y < _rect.Height; y++)
                {
                    WriteChar(colors, 0, y, ' ', _rect.Width);
                }
            }
        }

        protected virtual void DispatchEvents()
        {
            DispatchKeyEvents();
        }

        protected virtual void DispatchKeyEvents()
        {
            var key = Console.ReadKey(true);
            if (!ProcessKey(key))
            {
                var isEscape = key.Key == ConsoleKey.Escape;
                if (isEscape)
                {
                    Close();
                }
                else
                {
                    // Beep();
                }
            }
        }

        #endregion

        #region private methods

        private void SetRect(Rect rect)
        {
            this._rect = new Rect(
                rect.X + (_parent == null ? 0 : _parent.ClientRect.X),
                rect.Y + (_parent == null ? 0 : _parent.ClientRect.Y),
                rect.Width,
                rect.Height);

            this._rectClient = new Rect(
                rect.X + (_parent == null ? 0 : _parent.ClientRect.X),
                rect.Y + (_parent == null ? 0 : _parent.ClientRect.Y),
                rect.Width,
                rect.Height);

            if (_border != null)
            {
                _rectClient.X = _rectClient.X + 1;
                _rectClient.Y = _rectClient.Y + 1;
                _rectClient.Width = _rectClient.Width - 2;
                _rectClient.Height = _rectClient.Height - 2;
            }
        }

        private void SaveScreen()
        {
            // screen.Save(...)
        }

        private void RestoreScreen()
        {
            var isRestoreSupported = false;
            if (isRestoreSupported)
            {
                // screen.Restore(...)
            }
            else
            {
                var colors = _parent != null
                    ? _parent.Colors
                    : Screen.Current.ColorsStart;
                PaintWindow(colors);
            }

            Screen.Current.ResetColors();
            Screen.Current.SetCursorPosition(_rect.X, _rect.Y);
        }

        #endregion

        #region private data
 
        private Window? _parent;
        private Rect _rect = new Rect(0, 0, 0, 0);
        private Rect _rectClient = new Rect(0, 0, 0, 0);
        private Colors _colors;
        private string? _border;
        private bool _focus = false;
        private bool _open = false;
        private bool _killFocusCursorVisible = true;
 
        #endregion
    }
}
