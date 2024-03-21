//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;

namespace Azure.AI.Details.Common.CLI.ConsoleGui
{
    public abstract class ScrollingControl : ControlWindow
    {
        public abstract int GetNumRows();
        public abstract int GetNumColumns();

        #region selection and offset

        public int SelectedRow
        {
            get { return _selectedRow; }
        }

        public int SelectedColumn
        {
            get { return _selectedColumn; }
        }

        public int SelectedColumnWidth
        {
            get { return _selectedColumnWidth; }
        }

        public int RowOffset
        {
            get { return _rowOffset; }
        }

        public int ColumnOffset
        {
            get { return _columnOffset; }
        }

        #endregion

        #region protected methods

        protected ScrollingControl(Window? parent, Rect rect, Colors colors, string? border, bool fEnabled) : base(parent, rect, colors, border, fEnabled)
        {
            _rowOffset = 0;
            _columnOffset = 0;
            _selectedRow = 0;
            _selectedColumn = 0;
            _selectedColumnWidth = 1;
        }

        protected virtual bool SetSelectedRow(int row)
        {
            if (GetNumRows() == 0)
            {
                row = 0;
            }
            else if (row > GetNumRows() - 1)
            {
                row = GetNumRows() - 1;
            }

            if (row != _selectedRow)
            {
                _selectedRow = row;

                if (_selectedRow < RowOffset)
                {
                    SetRowOffset(_selectedRow);
                }

                if (_selectedRow > RowOffset + ClientRect.Height - 1)
                {
                    SetRowOffset(_selectedRow - ClientRect.Height + 1);
                }

                return true;
            }

            return false;
        }

        protected virtual bool SetSelectedColumn(int col, int width = 1)
        {
            if (GetNumColumns() == 0 || col < 0)
            {
                col = 0;
            }
            else if (col > GetNumColumns() - 1)
            {
                col = GetNumColumns() - 1;
            }

            if (col != _selectedColumn || width != _selectedColumnWidth)
            {
                _selectedColumn = col;
                _selectedColumnWidth = width;

                if (_selectedColumn < ColumnOffset)
                    SetColumnOffset(_selectedColumn);

                if (_selectedColumn > ColumnOffset + ClientRect.Width - 1)
                    SetColumnOffset(_selectedColumn - ClientRect.Width + 1);

                return true;
            }

            return false;
        }

        protected virtual bool SetRowOffset(int offset)
        {
            if (GetNumRows() <= ClientRect.Height)
            {
                offset = 0;
            }
            else if (offset > GetNumRows() - ClientRect.Height)
            {
                offset = GetNumRows() - ClientRect.Height;
            }

            if (offset != _rowOffset)
            {
                _rowOffset = offset;
                return true;
            }

            return false;
        }

        protected virtual bool SetColumnOffset(int offset)
        {
            if (GetNumColumns() <= ClientRect.Width)
            {
                offset = 0;
            }
            else if (offset > GetNumColumns() - ClientRect.Width)
            {
                offset = GetNumColumns() - ClientRect.Width;
            }

            if (offset != _columnOffset)
            {
                _columnOffset = offset;
                return true;
            }

            return false;
        }

        #endregion
        
        #region private data

        private int _rowOffset;
        private int _columnOffset;

        private int _selectedRow;
        private int _selectedColumn;
        private int _selectedColumnWidth;

        #endregion
    }
}
