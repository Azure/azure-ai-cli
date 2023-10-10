//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Net;

namespace Azure.AI.Details.Common.CLI
{
    public class DevCommand : Command
    {
        internal DevCommand(ICommandValues values)
        {
            _values = values.ReplaceValues();
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", true);
        }

        internal bool RunCommand()
        {
            try
            {
                RunDevCommand();
            }
            catch (WebException ex)
            {
                ConsoleHelpers.WriteLineError($"\n  ERROR: {ex.Message}");
                JsonHelpers.PrintJson(HttpHelpers.ReadWriteJson(ex.Response, _values, "dev"));
            }

            return _values.GetOrDefault("passed", true);
        }

        private bool RunDevCommand()
        {
            DoCommand(_values.GetCommand());
            return _values.GetOrDefault("passed", true);
        }

        private void DoCommand(string command)
        {
            CheckPath();

            switch (command)
            {
                case "dev.new": DoNew(); break;
                case "dev.shell": DoShell(); break;

                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
            }
        }

        private void DoNew()
        {
            _values.AddThrowError("WARNING:", $"''ai dev new' NOT YET IMPLEMENTED!!");
        }

        private void DoShell()
        {
            _values.AddThrowError("WARNING:", $"'ai dev shell' NOT YET IMPLEMENTED!!");
        }

        private readonly bool _quiet;
        private readonly bool _verbose;
    }
}
