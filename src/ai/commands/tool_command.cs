//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.Details.Common.CLI.Extensions.Otel;
namespace Azure.AI.Details.Common.CLI
{
    public class ToolCommand : Command
    {
        internal ToolCommand(ICommandValues values) : base(values)
        {
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", true);
        }

        internal bool RunCommand()
        {
            try
            {
                RunToolCommand();
            }
            catch (WebException ex)
            {
                FileHelpers.LogException(_values, ex);
                ConsoleHelpers.WriteLineError($"\n  ERROR: {ex.Message}");
                JsonHelpers.PrintJson(HttpHelpers.ReadWriteJson(ex.Response, _values, "tool"));
            }

            return _values.GetOrDefault("passed", true);
        }
        private void StartDashboard() 
        {
            Dashboard.StartDashboard();
        }

        private void StopDashboard() 
        {
            Dashboard.StopDashboard();
        }

        private bool RunToolCommand()
        {
            DoCommand(_values.GetCommand());
            return _values.GetOrDefault("passed", true);
        }

        private void DoCommand(string command)
        {
            CheckPath();

            switch (command)
            {
                case "tool.dashboard.start": StartDashboard(); break;
                case "tool.dashboard.stop": StopDashboard(); break;
                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
            }
        }

        private bool _quiet = false;
        private bool _verbose = false;
    }
}
