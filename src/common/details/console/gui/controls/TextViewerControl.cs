//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Text;
using Azure.AI.Details.Common.CLI;

namespace Azure.AI.Details.Common.CLI.ConsoleGui
{
    public class TextViewerControl : SpeedSearchListBoxControl
    {
        protected static (int row, int col, int width) Display(string[] lines, int width, int height, Colors normal, Colors selected, int selectedRow = 0, int selectedCol = 0, int selectionWidth = 1)
        {
            if (height > lines.Length + 2) height = lines.Length + 2;
            if (width == int.MinValue)
            {
                foreach (var line in lines)
                {
                    if (line.Length > width)
                    {
                        width = line.Length;
                    }
                }
                width += 2;
            }

            var rect = Screen.Current.MakeSpaceAtCursor(width, height);
            var border = rect.Height > 2 ? Window.Borders.SingleLine : null;
            var viewer = new TextViewerControl(null, rect, normal, selected, border)
            {
                Items = lines,
            };

            viewer.SetSelectedRow(selectedRow);
            viewer.SetSelectedColumn(selectedCol, selectionWidth);
            viewer.Run();

            Screen.Current.SetCursorPosition(rect.X, rect.Y);
            Screen.Current.ResetColors();

            return (viewer._rowPicked, viewer._colPicked, viewer._widthPicked);
        }

        public override bool ProcessKey(ConsoleKeyInfo key)
        {
            var processed = false;

            var left = key.Key == ConsoleKey.LeftArrow;
            var right = key.Key == ConsoleKey.RightArrow;
            if (left || right)
            {
                var newCol = SelectedColumn + (left ? -1 : +1);
                var textLength = GetDisplayText(SelectedRow).Length;
                processed = SetSelectedColumn(Math.Min(newCol, textLength));
                if (processed)
                {
                    DisplayRow(SelectedRow);
                    return true;
                }
            }

            if (key.IsNavigation())
            {
                processed = SetSelectedColumn(SelectedColumn, 1);
                if (processed) DisplayRow(SelectedRow);
            }

            processed = ProcessSpeedSearchKey(key);
            if (processed) return processed;

            var escape = key.Key == ConsoleKey.Escape;
            var enter = key.Key == ConsoleKey.Enter;
            if (escape || enter)
            {
                Close();
                _rowPicked = escape ? -1 : SelectedRow;
                _colPicked = escape ? -1 : SelectedColumn;
                _widthPicked = escape ? 1 : SelectedColumnWidth;
                return true;
            }

            return base.ProcessKey(key);
        }

        #region overrides

        public override void DisplayRow(int row)
        {
            if (row == SelectedRow)
            {
                DisplaySelectedRow(row);
            }
            else
            {
                DisplayNonSelectedRow(row);
            }
        }

        #endregion

        #region protected methods

        protected TextViewerControl(Window parent, Rect rect, Colors colorNormal, Colors colorSelected, string border = null, bool fEnabled = true) : base(parent, rect, colorNormal, colorSelected, border, fEnabled)
        {
        }

        protected override string GetSpeedSearchText(int row)
        {
            var text = GetDisplayText(row);
            return text.Replace("`", "");
        }

        protected void DisplayNonSelectedRow(int row)
        {
            var text = GetDisplayText(row);
            WriteClientTextWithHighlight(Colors, SelectedColors, 0, row, text, ClientRect.Width);
        }

        protected void DisplaySelectedRow(int row)
        {
            var text = GetDisplayText(row);

            var selX1 = SelectedColumn;
            var selX2 = selX1 + SelectedColumnWidth - 1;

            var normal = Colors;
            var highlight = SelectedColors;
            var error = ColorHelpers.GetErrorColors(normal.Foreground, normal.Background);

            var highlightOn = false;

            var x = 0;
            var cchSkips = 0;
            var cchSelected = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '`')
                {
                    cchSkips++;
                    highlightOn = !highlightOn;
                    continue;
                }

                if (i < ColumnOffset) continue;

                var selectedOn = (i - cchSkips) >= selX1 && (i - cchSkips) <= selX2;
                if (selectedOn) cchSelected++;

                if (selectedOn && highlightOn)
                {
                    WriteClientChar(error, x++, row, text[i]);
                }
                else if (selectedOn || highlightOn)
                {
                    WriteClientChar(highlight, x++, row, text[i]);
                }
                else
                {
                    WriteClientChar(normal, x++, row, text[i]);
                }
            }

            if (x < ClientRect.Width)
            {
                if (cchSelected == 0 && SelectedColumnWidth > 0)
                {
                    WriteClientChar(SelectedColors, x++, row, ' ');
                }
                WriteClientChar(Colors, x, row, ' ', ClientRect.Width - x);
            }
        }

        #endregion

        #region protected data

        protected int _rowPicked;
        protected int _colPicked;
        protected int _widthPicked;

        #endregion
    }
}
