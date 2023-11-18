//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;

namespace Azure.AI.Details.Common.CLI.ConsoleGui
{
    public class EditBoxControl : ScrollingControl
    {
        public EditBoxControl(Window parent, Rect rect, Colors color, string text, int uiMaxTextLengthParam, string picture, string pszBorder, bool fEnabled = true) :
            base(parent, rect, color, pszBorder, fEnabled)
        {
            uiMaxTextLength = uiMaxTextLengthParam;

            _text = text;
            _picture = picture;

            cursor.Save();
        }

        #region text, rows, and columns

        public string GetText() { return _text; }

        public override int GetNumRows()
        {
            return 0;
        }

        public override int GetNumColumns()
        {
            return GetText().Length + 1;
        }

        #endregion

        #region open, close, focus

        public override bool Open()
        {
            if (base.Open())
            {
                DisplayText();
                DisplayCursor();
                return true;
            }

            return false;
        }

        public override bool Close()
        {
            cursor.Restore();
            return base.Close();
        }

        public override bool SetFocus()
        {
            if (base.SetFocus())
            {
                DisplayText();
                DisplayCursor();
                return true;
            }

            return false;
        }

        public override void KillFocus()
        {
            base.KillFocus();

            Home();

            DisplayText();
            DisplayCursor();
        }

        #endregion

        #region selection and offset

        protected override bool SetRowOffset(int ui)
        {
            return false;
        }

        protected override bool SetColumnOffset(int ui)
        {
            if (base.SetColumnOffset(ui))
                {
                DisplayText();
                DisplayCursor();
                return true;
                }

            return false;
        }

        protected override bool SetSelectedColumn(int ui, int width = 1)
        {
            if (IsPosValid(ui) && base.SetSelectedColumn(ui, width))
                {
                DisplayText();
                DisplayCursor();
                return true;
                }

            return false;
        }

        #endregion

        #region processing keys

        public override bool ProcessKey(ConsoleKeyInfo key)
        {
            bool fProcessedKey = false;

            cursor.Hide();
            switch (key.Key)
            {
                case ConsoleKey.Home:
                    if (!key.IsShift() && !key.IsCtrl() && !key.IsAlt())
                    {
                        Home();
                        fProcessedKey = true;
                    }
                    break;

                case ConsoleKey.End:
                    if (!key.IsShift() && !key.IsCtrl() && !key.IsAlt())
                    {
                        End();
                        fProcessedKey = true;
                    }
                    break;

                case ConsoleKey.UpArrow:
                case ConsoleKey.LeftArrow:
                    if (!key.IsShift() && !key.IsCtrl() && !key.IsAlt())
                    {
                        Left();
                        fProcessedKey = true;
                    }
                    break;

                case ConsoleKey.DownArrow:
                case ConsoleKey.RightArrow:
                    if (!key.IsShift() && !key.IsCtrl() && !key.IsAlt())
                    {
                        Right();
                        fProcessedKey = true;
                    }
                    break;

                case ConsoleKey.Backspace:
                    if (!key.IsShift() && !key.IsCtrl() && !key.IsAlt())
                    {
                        BackSpace();
                        fProcessedKey = true;
                    }
                    break;

                case ConsoleKey.Insert:
                    if (!key.IsShift() && !key.IsCtrl() && !key.IsAlt())
                    {
                        Insert();
                        fProcessedKey = true;
                    }
                    break;

                case ConsoleKey.Delete:
                    if (!key.IsShift() && !key.IsCtrl() && !key.IsAlt())
                    {
                        Delete();
                        fProcessedKey = true;
                    }
                    break;

                case ConsoleKey.Escape:
                case ConsoleKey.Enter:
                case ConsoleKey.PageDown:
                case ConsoleKey.PageUp:
                case ConsoleKey.F1:
                case ConsoleKey.F2:
                case ConsoleKey.F3:
                case ConsoleKey.F4:
                case ConsoleKey.F5:
                case ConsoleKey.F6:
                case ConsoleKey.F7:
                case ConsoleKey.F8:
                case ConsoleKey.F9:
                case ConsoleKey.F10:
                case ConsoleKey.F11:
                case ConsoleKey.F12:
                case ConsoleKey.Tab:
                    break;

                default:
                    if (key.IsAscii())
                    {
                        TypeChar(key.KeyChar);
                        fProcessedKey = true;
                    }
                    break;
            }

            DisplayCursor();
            return fProcessedKey;
        }

        public bool Home()
        {
            bool fSuccess = true;
            int ui = 0;

            int uiMaxPos = GetText().Length;

            for (; !IsPosValid(ui); ui++)
                if (ui >= uiMaxPos)
                {
                    fSuccess = false;
                    break;
                }

            if (fSuccess)
                SetSelectedColumn(ui);

            return fSuccess;
        }

        public bool End()
        {
            bool fSuccess = true;
            int ui = GetText().Length;

            if (ui >= uiMaxTextLength)
                ui = uiMaxTextLength - 1;

            for (; !IsPosValid(ui); ui--)
                if (ui == 0)
                {
                    fSuccess = false;
                    break;
                }

            if (fSuccess)
                SetSelectedColumn(ui);

            return fSuccess;
        }

        public bool Left()
        {
            bool fSuccess = true;
            int ui = SelectedColumn;

            do
            {
                if (ui == 0)
                {
                    fSuccess = false;
                    break;
                }
            }
            while (!IsPosValid(--ui));

            if (fSuccess)
                SetSelectedColumn(ui);

            return fSuccess;
        }

        public bool Right()
        {
            bool fSuccess = true;
            int ui = SelectedColumn;

            int uiMaxPos = GetText().Length;
            if (uiMaxPos >= uiMaxTextLength)
                uiMaxPos = uiMaxTextLength - 1;

            do
            {
                if (ui >= uiMaxPos)
                {
                    fSuccess = false;
                    break;
                }
            }
            while (!IsPosValid(++ui));

            if (fSuccess)
                SetSelectedColumn(ui);

            return fSuccess;
        }

        public bool BackSpace()
        {
            bool fSuccess;

            if ((fSuccess = Left()) != false)
                Delete();

            return fSuccess;
        }

        public bool Delete()
        {
            bool fSuccess;

            if (SelectedColumn < GetText().Length)
            {
                fSuccess = true;

                if (_picture == null)
                {
                    _text = _text.Remove(SelectedColumn, 1);
                }
                else
                {
                    int ui;
                    for (ui = SelectedColumn + 1; ui < _text.Length && IsPosValid(ui) && IsCharValid(_text[ui], ui - 1); ui++)
                    {
                        _text = _text.Insert(ui, _text[ui].ToString()).Remove(ui - 1, 1);
                    }

                    if (ui == _text.Length)
                    {
                        _text = _text.Remove(ui - 1);
                    }
                    else
                    {
                        _text = _text.Insert(ui - 1, " ").Remove(ui, 1);
                    }
                }

                DisplayText();
            }
            else
                fSuccess = false;

            return fSuccess;
        }

        public bool Insert()
        {
            fInsertMode = !fInsertMode;
            DisplayCursor();
            return true;
        }

        public bool TypeChar(char ch)
        {
            return fInsertMode ? InsertChar(ch) : TypeOverChar(ch);
        }

        #endregion

        #region cursor

        public void DisplayCursor()
        {
            if (IsFocus())
            {
                cursor.SetPosition(ClientRect.X + SelectedColumn - ColumnOffset, ClientRect.Y);
                cursor.Show();

                if (fInsertMode)
                    cursor.SetBoxShape();
                else
                    cursor.SetLineShape();
            }
            else
            {
                HideCursor();
            }
        }

        public void HideCursor()
        {
            cursor.Hide();
        }

        #endregion

        #region private methods

        private void DisplayText()
        {
            Screen.Current.SetColors(Colors);
            WriteClientText(0, 0, _text.Substring(ColumnOffset), ClientRect.Width);
        }

        private bool IsPosValid(int uiPos)
        {
            return uiPos < uiMaxTextLength &&
                   uiPos <= GetText().Length &&
                   (_picture == null ||
                    _picture == "@#" ||
                    _picture == "@A" ||
                    (uiPos < _picture.Length &&
                     (_picture[uiPos] == '#' ||
                      _picture[uiPos] == 'A')));
        }

        private bool IsCharValid(char ch, int uiPos)
        {
            return IsPosValid(uiPos) &&
                   (_picture == null ||
                    ((char.IsLetter(ch) && (_picture == "@A" || _picture[uiPos] == 'A')) ||
                     (char.IsDigit(ch) && (_picture == "@#" || _picture[uiPos] == '#'))));
        }

        private bool InsertChar(char ch)
        {
            bool fSuccess;

            if ((fSuccess = IsCharValid(ch, SelectedColumn)) != false)
            {
                // insert the char
                _text = _text.Insert(SelectedColumn, ch.ToString());

                // check to make sure the rest of the characters are in valid spots
                for (int i = SelectedColumn + 1; i < _text.Length; i++)
                {
                    if (!IsCharValid(_text[i], i))
                    {
                        // truncate at the first one that's not valid
                        _text = _text.Remove(i);
                        break;
                    }
                }

                // dipsplay the text
                DisplayText();

                // move to the right since we're done
                Right();
            }

            return fSuccess;
        }

        private bool TypeOverChar(char ch)
        {
            bool fSuccess;

            if ((fSuccess = IsCharValid(ch, SelectedColumn)) != false)
            {
                // make space if needed
                if (SelectedColumn == _text.Length)
                {
                    _text = _text.Insert(SelectedColumn, " ");
                }

                // insert the new character
                _text = _text.Remove(SelectedColumn, 1).Insert(SelectedColumn, ch.ToString());

                // display the text
                DisplayText();

                // move to the right
                Right();
            }

            return fSuccess;
        }

        #endregion

        #region private data

        string _text;
        int uiMaxTextLength;
        string _picture;

        Cursor cursor = new Cursor();

        static bool fInsertMode = false;

        #endregion
    }
}
