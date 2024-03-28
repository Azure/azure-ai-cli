//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;

namespace Azure.AI.Details.Common.CLI.ConsoleGui
{
    abstract public class VirtualListBoxControl : ScrollingControl
    {
        protected VirtualListBoxControl(Window? parent, Rect rect, Colors colorNormal, Colors colorSelected, string? border = null, bool fEnabled = true) : base(parent, rect, colorNormal, border, fEnabled)
        {
            this.colorsSelected = colorSelected;
        }

        #region abstract methods

        public abstract override int GetNumRows();
        public abstract override int GetNumColumns();
        public abstract void DisplayRow(int row);

        #endregion

        #region colors

        public Colors SelectedColors
        {
            get { return colorsSelected; }
        }

        protected Colors ColorsFromRow(int row)
        {
            return row == SelectedRow && IsFocus()
                ? SelectedColors
                : Colors;
        }

        #endregion

        #region open/close/focus

        public override bool Open()
        {
            if (base.Open())
            {
                DisplayRows();
                return true;
            }

            return false;
        }

        public override bool SetFocus()
        {
            if (base.SetFocus())
            {
                DisplayRow(SelectedRow);
                return true;
            }

            return false;
        }

        public override void KillFocus()
        {
            base.KillFocus();
            DisplayRow(SelectedRow);
        }

        #endregion

        #region selection, offset, and display

        protected override bool SetSelectedRow(int row)
        {
            int prevSelected = SelectedRow;

            if (base.SetSelectedRow(row))
            {
                if (prevSelected >= RowOffset && prevSelected < RowOffset + ClientRect.Height)
                {
                    DisplayRow(prevSelected);
                }

                DisplayRow(SelectedRow);
                
                return true;
            }

            return false;
        }

        protected override bool SetRowOffset(int offset)
        {
            if (base.SetRowOffset(offset))
            {
                DisplayRows();
                return true;
            }

            return false;
        }

        protected override bool SetColumnOffset(int offset)
        {
            if (base.SetColumnOffset(offset))
            {
                DisplayRows();
                return true;
            }

            return false;
        }

        protected void DisplayRows()
        {
            var rows = GetNumRows();
            for (int row = RowOffset; row < RowOffset + ClientRect.Height && row < GetNumRows(); row++)
            {
                DisplayRow(row);
            }
        }

        #endregion

        #region process keys

        public override bool ProcessKey(ConsoleKeyInfo key)
        {
            switch (key.Key)
                {
                case ConsoleKey.UpArrow:
                    Up();
                    return true;

                case ConsoleKey.DownArrow:
                    Down();
                    return true;

                case ConsoleKey.PageUp:
                    PageUp();
                    return true;

                case ConsoleKey.PageDown:
                    PageDown();
                    return true;

                case ConsoleKey.Home:
                    Home();
                    return true;

                case ConsoleKey.End:
                    End();
                    return true;
                }

            return false;

        }

        public bool Up()
        {
            if (SelectedRow > 0)
            {
                return SetSelectedRow(SelectedRow - 1);
            }

            return false;
        }

        public bool Down()
        {
            if (SelectedRow < GetNumRows() - 1)
            {
                return SetSelectedRow(SelectedRow + 1);
            }

            return false;
        }

        public bool PageUp()
        {
            if (SelectedRow != RowOffset)
            {
                return SetSelectedRow(RowOffset);
            }

            if (SelectedRow < ClientRect.Height)
            {
                return SetSelectedRow(0);
            }

            return SetSelectedRow(SelectedRow - ClientRect.Height + 1);
        }

        public bool PageDown()
        {
            if (SelectedRow != RowOffset + ClientRect.Height - 1)
                return SetSelectedRow(RowOffset + ClientRect.Height - 1);

            return SetSelectedRow(SelectedRow + ClientRect.Height - 1);
        }

        public bool Home()
        {
            return SetSelectedRow(0);
        }

        public bool End()
        {
            if (GetNumRows() > 0)
            {
                return SetSelectedRow(GetNumRows() - 1);
            }
            else
            {
                return SetSelectedRow(0);
            }
        }

        #endregion

        #region write text/char

        public new void WriteChar(int x, int y, char ch, int count = 1)
        {
            base.WriteChar(x - ColumnOffset, y - RowOffset, ch, count);
        }

        public new void WriteText(int x, int y, string text, int cch = int.MaxValue)
        {
            if (IsOpen())
            {
                base.WriteText(x - ColumnOffset, y - RowOffset, text, cch);
            }
        }

        public new void WriteChar(Colors colors, int x, int y, char ch, int count = 1)
        {
            if (IsOpen())
            {
                base.WriteChar(colors, x - ColumnOffset, y - RowOffset, ch, count);
            }
        }

        public new void WriteText(Colors colors, int x, int y, string text, int cch = int.MaxValue)
        {
            if (IsOpen())
            {
                base.WriteText(colors, x - ColumnOffset, y - RowOffset, text, cch);
            }
        }

        #endregion

        #region write client text/char
        
        public new void WriteClientChar(int x, int y, char ch, int count = 1)
        {
            if (IsOpen())
            {
                base.WriteClientChar(x - ColumnOffset, y - RowOffset, ch, count);
            }
        }

        public new void WriteClientText(int x, int y, string text, int cch = int.MaxValue)
        {
            if (IsOpen())
            {
                base.WriteClientText(x - ColumnOffset, y - RowOffset, text, cch);
            }
        }

        public new void WriteClientChar(Colors colors, int x, int y, char ch, int count = 1)
        {
            if (IsOpen())
            {
                base.WriteClientChar(colors, x - ColumnOffset, y - RowOffset, ch, count);
            }
        }

        public new void WriteClientText(Colors colors, int x, int y, string text, int cch = int.MaxValue)
        {
            if (IsOpen())
            {
                base.WriteClientText(colors, x - ColumnOffset, y - RowOffset, text, cch);
            }
        }

        public new void WriteClientTextWithHighlight(Colors colors, Colors highlight, int x, int y, string text, int cch = int.MaxValue)
        {
            if (IsOpen())
            {
                base.WriteClientTextWithHighlight(colors, highlight, x - ColumnOffset, y - RowOffset, text, cch);
            }
        }

        #endregion


        #region private data

        private Colors colorsSelected;

        #endregion
    }
}
