//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;

namespace Azure.AI.Details.Common.CLI.ConsoleGui
{
    class Cursor
    {
        public Cursor()
        {
            _saved = false;
        }

        public Cursor(int x, int y) : this()
        {
            Save();
            _prevX = x;
            _prevY = y;
        }

        public static Cursor Current
        {
            get
            {
                var saved = new Cursor();
                saved.Save();
                return saved;
            }
        }

        public void Save()
        {
            _saved = true;
            _prevSize = GetSize();
            _visible = IsVisible();
            GetPosition(ref _prevX, ref _prevY);
        }

        public void Restore()
        {
            if (_saved)
            {
                SetPosition(_prevX, _prevY);
                SetSize(_prevSize);

                if (_visible)
                {
                    Show();
                }
                else
                {
                    Hide();
                }
            }
        }

        public bool SetPosition(int x, int y)
        {
            return Screen.Current.SetCursorPosition(x, y);
        }

        public void GetPosition(ref int x, ref int y)
        {
            x = Console.CursorLeft;
            y = Console.CursorTop;
        }

        public void SetBoxShape()
        {
            SetSize(_cursorBoxSize);
        }

        public void SetLineShape()
        {
            SetSize(_curosrLineSize);
        }

        public void Hide()
        {
            Screen.Current.SetCursorVisible(false);
        }

        public void Show()
        {
            Screen.Current.SetCursorVisible(true);
        }

        public void SetSize(int size)
        {
            Screen.Current.SetCursorSize(size);
        }

        public int GetSize()
        {
            return Screen.Current.GetCursorSize();
        }

        public bool IsVisible()
        {
            return Screen.GetCursorVisible();
        }

        private int _prevX, _prevY;
        private int _prevSize;
        private bool _visible;

        private bool _saved;

        const int _cursorBoxSize = 100;
        const int _curosrLineSize = 15;
    }
}
