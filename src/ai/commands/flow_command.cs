//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Azure.AI.Details.Common.CLI
{
    public class FlowCommand : Command
    {
        internal FlowCommand(ICommandValues values)
        {
            _values = values.ReplaceValues();
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", true);
        }

        internal bool RunCommand()
        {
            try
            {
                RunFlowCommand().Wait();
            }
            catch (WebException ex)
            {
                ConsoleHelpers.WriteLineError($"\n  ERROR: {ex.Message}");
                JsonHelpers.PrintJson(HttpHelpers.ReadWriteJson(ex.Response, _values, "flow"));
            }

            return _values.GetOrDefault("passed", true);
        }

        private async Task<bool> RunFlowCommand()
        {
            await DoCommand(_values.GetCommand());
            return _values.GetOrDefault("passed", true);
        }

        private async Task DoCommand(string command)
        {
            CheckPath();

            switch (command)
            {
                case "flow.new": await DoNewFlow(); break;

                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
            }
        }

        private async Task DoNewFlow()
        {
            StartCommand();

            string flowPath = FlowNameToken.Data().Demand(_values, "Creating flow", "flow new");
            string promptTemplate = SystemPromptTemplateToken.Data().GetOrDefault(_values);

            string functionData = FunctionToken.Data().GetOrDefault(_values, "");
            var parts = functionData.Split(':');
            var entryFile = parts != null && parts.Length > 0 ? parts[0] : "";
            var functionName = parts != null && parts.Length > 1 ? parts[1] : "";
            
            string type = "chat";
            bool yes = true;

            var response = await PfCli.FlowInit(flowPath, entryFile, functionName, promptTemplate, type, yes);
            if (!string.IsNullOrEmpty(response.StdError))
            {
                _values.AddThrowError("ERROR:", response.StdError);
            }

            Console.WriteLine(response.StdOutput);

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private void DisplayBanner(string which)
        {
            if (_quiet) return;

            var logo = FileHelpers.FindFileInHelpPath($"help/include.{Program.Name}.{which}.ascii.logo");
            if (!string.IsNullOrEmpty(logo))
            {
                var text = FileHelpers.ReadAllHelpText(logo, Encoding.UTF8);
                ConsoleHelpers.WriteLineWithHighlight(text);
            }
        }

        private void StartCommand()
        {
            CheckPath();
            LogHelpers.EnsureStartLogFile(_values);

            // _display = new DisplayHelper(_values);

            // _output = new OutputHelper(_values);
            // _output.StartOutput();

            _lock = new SpinLock();
            _lock.StartLock();
        }

        private void StopCommand()
        {
            _lock.StopLock(5000);

            // LogHelpers.EnsureStopLogFile(_values);
            // _output.CheckOutput();
            // _output.StopOutput();

            _stopEvent.Set();
        }

        private SpinLock _lock = null;
        private readonly bool _quiet = false;
        private readonly bool _verbose = false;
    }
}
