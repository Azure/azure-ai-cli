//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Text;

namespace Azure.AI.Details.Common.CLI.ConsoleGui
{
    public class EditBoxQuickEdit : EditBoxControl
    {
        public static string Edit(int width, int height, Colors normal, string text = "", int maxCch = 1024, string picture = null, string border = null)
        {
            var rect = Screen.Current.MakeSpaceAtCursor(width, height);
            var editBox = new EditBoxQuickEdit(null, rect, normal, text, maxCch, picture, border);
            editBox.Run();
            return editBox._finalText;
        }

        #region protected methods

        protected EditBoxQuickEdit(Window parent, Rect rect, Colors colorNormal, string text = "", int maxCch = 1024, string picture = null, string border = null) : base(parent, rect, colorNormal, text, maxCch, picture, border, true)
        {
        }

        public override bool ProcessKey(ConsoleKeyInfo key)
        {
            var processed = false;
            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    _finalText = null;
                    processed = true;
                    break;

                case ConsoleKey.Enter:
                    _finalText = GetText();
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

        private string _finalText;

        #endregion
    }
}
