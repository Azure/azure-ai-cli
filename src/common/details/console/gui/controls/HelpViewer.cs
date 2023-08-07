//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Azure.AI.Details.Common.CLI;

namespace Azure.AI.Details.Common.CLI.ConsoleGui
{
    public class HelpViewer : TextViewerControl
    {
        public static void DisplayHelpText(string[] lines, int width, int height, Colors normal, Colors selected, int selectedRow = 0, int selectedCol = 0, int selectionWidth = 1)
        {
            lines = lines.Prepend("").ToArray();

            while (true)
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
                var viewer = new HelpViewer(null, rect, normal, selected, border)
                {
                    Items = lines,
                };

                viewer.SetSelectedRow(selectedRow);
                viewer.SetSelectedColumn(selectedCol, selectionWidth);
                viewer.Run();

                Screen.Current.SetCursorPosition(rect.X, rect.Y);
                Screen.Current.ResetColors();

                (selectedRow, selectedCol, selectionWidth) = (viewer._rowPicked, viewer._colPicked, viewer._widthPicked);

                if (selectedRow < 0 || selectedCol < 0) break;
                if (selectedRow >= lines.Length) break;

                // check to see if there's a help command present on that row
                var helpCommand = GetProgramHelpCommand();
                var text = lines[selectedRow];
                if (text.Contains(helpCommand))
                {
                    var at = text.LastIndexOf(helpCommand);
                    helpCommand = text.Substring(at);

                    // remove potential trailing parenthesis
                    var i = at - "(see: ".Length;
                    if (i > 0 && text.Substring(i).StartsWith("(see: ") && text.EndsWith(")"))
                    {
                        helpCommand = helpCommand.Substring(0, helpCommand.Length - 1);
                    }

                    var start = new ProcessStartInfo(Program.Exe, $"quiet {helpCommand}");
                    start.UseShellExecute = false;
                    
                    var process = Process.Start(start);
                    process.WaitForExit();
                    
                    if (process.ExitCode == -1)
                    {
                        Environment.Exit(process.ExitCode);
                    }
                }
                else if (text.ToLower().Contains("see: https://") || text.ToLower().Contains("see https://"))
                {
                    var at = text.LastIndexOf("https://");
                    var url = text.Substring(at);
                    ProcessHelpers.StartBrowser(url);
                }
                else
                {
                    var tryItPrefix = $"try: {Program.Name} ";
                    var at = text.ToLower().LastIndexOf(tryItPrefix);
                    if (at > 0)
                    {
                        var tryCommand = text.Substring(at + tryItPrefix.Length);
                        tryCommand = tryCommand.TrimEnd(')');

                        var start = new ProcessStartInfo(Program.Exe, $"cls {tryCommand}");
                        start.UseShellExecute = false;
                        Process.Start(start).WaitForExit();
                        Environment.Exit(-1);
                    }
                }
            }
        }

        public static void DisplayHelpTopics(string[] topics, int width, int height, Colors normal, Colors selected, int selectedRow = 0)
        {
            while (true)
            {
                selectedRow = ConsoleGui.ListBoxPicker.PickIndexOf(topics, int.MinValue, 30, normal, selected, selectedRow);
                if (selectedRow < 0) break;

                var helpCommand = topics[selectedRow];
                var start = new ProcessStartInfo(Program.Exe, $"quiet {helpCommand}");

                start.UseShellExecute = false;
                Process.Start(start).WaitForExit();
            }
        }

        public override bool ProcessKey(ConsoleKeyInfo key)
        {
            EnsureKillSpeedSearchFocus();

            var ctrlHOpen = !IsSpeedSearchOpen() && key.Key == ConsoleKey.H && key.IsCtrl();
            if (ctrlHOpen) key = new ConsoleKeyInfo((char)ConsoleKey.Tab, ConsoleKey.Tab, false, false, true);

            var tabOpen = !IsSpeedSearchOpen() && key.Key == ConsoleKey.Tab;
            var f3Open = !IsSpeedSearchOpen() && key.Key == ConsoleKey.F3;
            if (tabOpen || f3Open) OpenSpeedSearch(GetProgramHelpCommand());

            var processed = base.ProcessKey(key);
            EnsureSetSpeedSearchFocus();

            return processed;
        }

        #region protected methods

        protected HelpViewer(Window parent, Rect rect, Colors colorNormal, Colors colorSelected, string border = null, bool fEnabled = true) : base(parent, rect, colorNormal, colorSelected, border, fEnabled)
        {
        }

        protected override void PaintWindow(Colors colors, string border = null)
        {
            base.PaintWindow(colors, border);
            if (border != null)
            {
                var banner = Program.GetDisplayBannerText();
                WriteText(SelectedColors, ColumnOffset + 1, RowOffset + 0, banner);
            }
        }

        protected override string GetSpeedSearchTooltip()
        {
            return IsSpeedSearchOpen()
                ? " \u2191\u2193  <TAB> Next  <ESC> Cancel  <ENTER> Select "
                : " \u2191\u2193  <TAB> Next Link  <?> Find  <ESC> Close ";
        }

        protected override string GetSpeedSearchText()
        {
            var text = base.GetSpeedSearchText();
            var helpCommand = GetProgramHelpCommand();
            return string.IsNullOrEmpty(text) || text == helpCommand
                ? $"(\\(see: {helpCommand}.*\\))|({helpCommand}[^()]*)|(https://[^ ]+)|(try: {Program.Name} .*)|(TRY: {Program.Name} .*)"
                : text;
        }

        private static string GetProgramHelpCommand()
        {
            return $"{Program.Name} help ";
        }

        #endregion
    }
}
