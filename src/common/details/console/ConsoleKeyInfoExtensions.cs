//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;

namespace Azure.AI.Details.Common.CLI
{
    public static class ConsoleKeyInfoExtensions
    {
        public static bool IsAlt(this ConsoleKeyInfo key)
        {
            return (key.Modifiers & ConsoleModifiers.Alt) == ConsoleModifiers.Alt;
        }

        public static bool IsShift(this ConsoleKeyInfo key)
        {
            return (key.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift;
        }

        public static bool IsCtrl(this ConsoleKeyInfo key)
        {
            return (key.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control;
        }

        public static bool IsAscii(this ConsoleKeyInfo key)
        {
            return key.KeyChar > 0 && key.KeyChar < 128;
        }

        public static bool IsNavigation(this ConsoleKeyInfo key)
        {
            return key.Key switch
            {
                ConsoleKey.PageUp => true,
                ConsoleKey.PageDown => true,
                ConsoleKey.UpArrow => true,
                ConsoleKey.DownArrow => true,
                ConsoleKey.Home => true,
                ConsoleKey.End => true,
                ConsoleKey.LeftArrow => true,
                ConsoleKey.RightArrow => true,
                _ => false
            };
        }
    }
}
