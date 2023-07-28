//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;

namespace Azure.AI.Details.Common.CLI.ConsoleGui
{
    public class ControlWindow : Window
    {
        public ControlWindow(Window parent, Rect rect, Colors colors, string border = null, bool enabled = true) : base(parent, rect, colors, border)
        {
            this._enabled = enabled;
        }

        public virtual bool IsHotKey(ConsoleKeyInfo key)
        {
            return false;
        }

        public bool IsEnabled()
        {
            return _enabled;
        }

        private bool _enabled;
    }
}
