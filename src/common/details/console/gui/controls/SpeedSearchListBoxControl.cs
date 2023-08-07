//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Azure.AI.Details.Common.CLI;

namespace Azure.AI.Details.Common.CLI.ConsoleGui
{
    public class SpeedSearchListBoxControl : ListBoxControl
    {
        #region protected methods

        protected SpeedSearchListBoxControl(Window parent, Rect rect, Colors colorNormal, Colors colorSelected, string border = null, bool fEnabled = true) : base(parent, rect, colorNormal, colorSelected, border, fEnabled)
        {
        }

        public override bool ProcessKey(ConsoleKeyInfo key)
        {
            bool processed = base.ProcessKey(key);
            if (IsSpeedSearchOpen()) 
            {
                _speedSearchBox.DisplayCursor();
            }
            return processed;
        }

        protected override void PaintWindow(Colors colors, string border = null)
        {
            base.PaintWindow(colors, border);
            if (border != null)
            {
                DisplaySpeedSearchToolTip();
            }
        }

        protected bool IsSpeedSearchOpen()
        {
            return _speedSearchBox != null && _speedSearchBox.IsOpen();
        }

        protected bool OpenSpeedSearch(string text = null)
        {
            if (_speedSearchBox == null)
            {
                _speedSearchBox = new EditBoxControl(this, new Rect(0, -1, Width - 2, 1), ColorHelpers.GetHighlightColors(Colors.Foreground, Colors.Background), "", Width, null, null);
                _speedSearchBox.Open();

                DisplaySpeedSearchToolTip();

                if (text != null)
                {
                    foreach (var ch in text)
                    {
                        if (!char.IsLetterOrDigit(ch) && ch != ' ') continue;
                        _speedSearchBox.ProcessKey(new ConsoleKeyInfo(ch, (ConsoleKey)char.ToUpper(ch), false, false, false));
                    }
                }

                EnsureSetSpeedSearchFocus();
                return true;
            }

            return false;
        }

        protected bool EnsureSetSpeedSearchFocus()
        {
            if (_speedSearchBox != null && _speedSearchBox.SetFocus())
            {
                _speedSearchBox.End();
                return true;
            }
            return false;
        }

        protected void EnsureKillSpeedSearchFocus()
        {
            if (_speedSearchBox != null) _speedSearchBox.KillFocus();
        }

        protected void DisplaySpeedSearchToolTip()
        {
            var tooltip = GetSpeedSearchTooltip();
            if (tooltip.Length > ClientRect.Width) return;

            WriteText(ColorHelpers.GetHighlightColors(), ColumnOffset + ClientRect.Width - tooltip.Length + 1, RowOffset + ClientRect.Height + 1, tooltip);
        }

        protected virtual string GetSpeedSearchText(int row)
        {
            return GetDisplayText(row);
        }

        protected virtual string GetSpeedSearchTooltip()
        {
            return IsSpeedSearchOpen()
                ? " \u2191\u2193  <TAB> Next <ESC> Clear  <ENTER> Select "
                : " \u2191\u2193  <?> Find  <ESC> Close ";
        }

        protected virtual string GetSpeedSearchText()
        {
            return _speedSearchBox?.GetText();
        }

        protected bool ProcessSpeedSearchKey(ConsoleKeyInfo key)
        {
            if (_speedSearchBox != null && key.Key == ConsoleKey.Escape)
            {
                _speedSearchBox.Close();
                _speedSearchBox = null;
                PaintWindow(Colors, $"{Border.Substring(0, 4)}{(char)0}{Border.Substring(5)}");
                return true;
            }

            var tab = key.Key == ConsoleKey.Tab;
            var f3 = key.Key == ConsoleKey.F3;
            if (_speedSearchBox != null && (tab || f3))
            {
                return key.IsShift()
                    ? SelectRowContaining(GetSpeedSearchText(), SelectedRow - 1, false, true, true, true, -1) || true
                    : SelectRowContaining(GetSpeedSearchText(), SelectedRow + 1, false, true, true, true, +1) || true;
            }

            var questionOpen = !IsSpeedSearchOpen() && key.KeyChar == '?';
            if (questionOpen) return OpenSpeedSearch();

            var ctrlF = !IsSpeedSearchOpen() && key.Key == ConsoleKey.F && key.IsCtrl();
            if (ctrlF) return OpenSpeedSearch();

            var processed = base.ProcessKey(key);
            if (processed) return processed;

            if (key.IsAscii())
            {
                OpenSpeedSearch();
                _speedSearchBox.End();

                if (_speedSearchBox.ProcessKey(key))
                {
                    return SelectRowContaining(GetSpeedSearchText(), SelectedRow, true, true, true, true, +1);
                }
            }

            return false;
        }

        protected bool SelectRowContaining(string searchFor, int startWithRow, bool startsWith = true, bool containsExact = true, bool containsRegex = true, bool containsChars = true, int direction = +1)
        {
            if (GetNumRows() == 0)
            {
                return false;
            }

            searchFor = searchFor.ToLower();
            startWithRow = MinMaxRow(startWithRow);

            // try to find exact match of what's been typed at the very beginning of the string
            if (startsWith)
            {
                var row = startWithRow;
                do
                {
                    if (RowStartsWith(row, searchFor, out int col, out int width))
                    {
                        return SetSelectedSpeedSearchRow(row, col, width);
                    }

                    row = MinMaxRow(row + direction);
                }
                while (row != startWithRow);
            }

            // try to find exact match of what's been typed, anywhere in the items
            if (containsExact)
            {
                var row = startWithRow;
                do
                {
                    if (RowContainsExactMatch(row, searchFor, out int col, out int width))
                    {
                        return SetSelectedSpeedSearchRow(row, col, width);
                    }

                    row = MinMaxRow(row + direction);
                }
                while (row != startWithRow);
            }

            // try to find a regexp match of what's been typed, anywhere in the items
            if (containsRegex)
            {
                var row = startWithRow;
                do
                {
                    if (RowContainsRegex(row, searchFor, out int col, out int width))
                    {
                        return SetSelectedSpeedSearchRow(row, col, width);
                    }

                    row = MinMaxRow(row + direction);
                }
                while (row != startWithRow);
            }

            // if not found, try finding just based on character matches
            if (containsChars)
            {
                var row = startWithRow;
                do
                {
                    if (RowContainsAllCharsInOrder(row, searchFor, out int col, out int width))
                    {
                        return SetSelectedSpeedSearchRow(row, col, width);
                    }

                    row = MinMaxRow(row + direction);
                }
                while (row != startWithRow);
            }

            return false;
        }

        protected bool RowStartsWith(int row, string searchFor, out int col, out int width)
        {
            var text = GetSpeedSearchText(row).ToLower();
            var startsWith = text.StartsWith(searchFor);

            col = startsWith ? 0 : -1;
            width = startsWith ? searchFor.Length : -1;

            return startsWith;
        }

        protected bool RowContainsExactMatch(int row, string searchFor, out int col, out int width)
        {
            var text = GetSpeedSearchText(row).ToLower();

            col = text.IndexOf(searchFor);
            width = col >= 0 ? searchFor.Length : -1;

            return col >= 0;
        }

        protected bool RowContainsRegex(int row, string searchFor, out int col, out int width)
        {
            var text = GetSpeedSearchText(row).ToLower();
            var matches = TryCatchHelpers.TryCatchNoThrow<MatchCollection>(() => Regex.Matches(text, searchFor), null, out _);

            var maxIndex = matches?.Count() > 0 ? matches?.Max(match => match.Index) : null;
            var match = maxIndex.HasValue ? matches.Where(match => match.Index == maxIndex).Last() : null;
           
            col = match != null && match.Success ? match.Index : -1;
            width = match != null && match.Success ? match.Length : -1;

            return col >= 0;
        }

        private bool RowContainsAllCharsInOrder(int row, string searchFor, out int col, out int width)
        {
            return StringHelpers.ContainsAllCharsInOrder(GetSpeedSearchText(row).ToLower(), searchFor, out col, out width);
        }

        protected int MinMaxRow(int row)
        {
            return GetNumRows() == 0 ? 0
                 : row < 0 ? GetNumRows() - 1
                 : row >= GetNumRows() ? 0
                 : row;
        }

        protected bool SetSelectedSpeedSearchRow(int row, int col, int width)
        {
            var sel1 = SetSelectedColumn(col, width);
            var sel2 = SetSelectedRow(row);

            var selected = sel1 || sel2;
            if (selected) DisplayRow(row);

            if (IsSpeedSearchOpen())
            {
                _speedSearchBox.DisplayCursor();
            }

            return selected;
        }

        #endregion

        #region private data

        private EditBoxControl _speedSearchBox;

        #endregion
    }
}
