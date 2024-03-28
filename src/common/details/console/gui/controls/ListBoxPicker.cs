//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Linq;
using Azure.AI.Details.Common.CLI;

namespace Azure.AI.Details.Common.CLI.ConsoleGui
{
    public class ListBoxPicker : SpeedSearchListBoxControl
    {
        public static int PickIndexOf(string[] choices, int select = 0)
        {
            var width = Math.Max(choices.Max(x => x.Length) + 4, 29);
            return ListBoxPicker.PickIndexOf(choices, width, 30, new Colors(ConsoleColor.White, ConsoleColor.Blue), new Colors(ConsoleColor.White, ConsoleColor.Red), select);
        }

        public static int PickIndexOf(string[] lines, int width, int height, Colors normal, Colors selected, int select = 0)
        {
            if (StdInOutServerHelpers.IsStdInOutServer)
            {
                return StdInOutServerHelpers.QuickPick(lines, select);
            }

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
            var picker = new ListBoxPicker(null, rect, normal, selected, border)
            {
                Items = lines,
            };

            picker.SetSelectedRow(select);
            picker.Run();

            return picker._picked;
        }

        public static string? PickString(string[] choices, int select = 0)
        {
            var width = Math.Max(choices.Max(x => x.Length) + 4, 29);
            return ListBoxPicker.PickString(choices, width, 30, new Colors(ConsoleColor.White, ConsoleColor.Blue), new Colors(ConsoleColor.White, ConsoleColor.Red), select);
        }

        public static string? PickString(string[] lines, int width, int height, Colors normal, Colors selected, int select = 0)
        {
            var picked = PickIndexOf(lines, width, height, normal, selected, select);
            return picked >= 0 && picked < lines.Length
                ? lines[picked]
                : null;
        }

        public override bool ProcessKey(ConsoleKeyInfo key)
        {
            var processed = ProcessSpeedSearchKey(key);
            if (processed) return processed;

            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    _picked = -1;
                    processed = true;
                    break;

                case ConsoleKey.Enter:
                    _picked = SelectedRow;
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

        #region protected methods

        protected ListBoxPicker(Window? parent, Rect rect, Colors colorNormal, Colors colorSelected, string? border = null, bool fEnabled = true) : base(parent, rect, colorNormal, colorSelected, border, fEnabled)
        {
        }

        #endregion

        #region private data

        private int _picked;

        #endregion
    }
}
