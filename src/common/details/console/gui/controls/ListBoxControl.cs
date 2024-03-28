//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Text;

namespace Azure.AI.Details.Common.CLI.ConsoleGui
{
    public class ListBoxControl : VirtualListBoxControl
    {
        public ListBoxControl(Window? parent, Rect rect, Colors colorNormal, Colors colorSelected, string? border = null, bool fEnabled = true) : base(parent, rect, colorNormal, colorSelected, border, fEnabled)
        {
        }

        #region items

        public string[] Items
        {
            get { return _items; }
            set { _items = value; }
        }

        public virtual string GetDisplayText(int row)
        {
            return row >= 0 && row < Items.Length ? Items[row].TrimEnd('\r') : "";
        }

        #endregion

        #region overrides

        public override int GetNumRows()
        {
            return _rows < 0
                ? _rows = Items.Length
                : _rows;
        }

        public override int GetNumColumns()
        {
            if (_columns >= 0) return _columns;

            foreach (var item in Items)
            {
                if (_columns < item.Length)
                {
                    _columns = item.Length;
                }
            }

            return _columns;
        }

        public override void DisplayRow(int row)
        {
            var text = GetDisplayText(row);
            WriteClientText(ColorsFromRow(row), 0, row, text, ClientRect.Width);
        }

        #endregion

        #region private data

        private int _rows = -1;
        private int _columns = -1;
        private string[] _items = Array.Empty<string>();

        #endregion
    }
}

