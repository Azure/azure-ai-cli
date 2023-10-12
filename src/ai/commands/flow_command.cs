//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                case "flow.invoke": await DoInvokeFlow(); break;
                // case "flow.serve": await DoServeFlow(); break;
                // case "flow.package": await DoPackageFlow(); break;
                // case "flow.deploy": await DoDeployFlow(); break;

                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
            }
        }

        private async Task DoNewFlow()
        {
            StartCommand();

            var flow = FlowNameToken.Data().Demand(_values, "Creating flow", "flow new");
            var prompt = SystemPromptTemplateToken.Data().GetOrDefault(_values);
            var function = FunctionToken.Data().GetOrDefault(_values, "");
            SplitFunctionReference(function, out var module, out function);

            await DoFlowInitConsoleGui(flow, prompt, function, module);

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private async Task DoInvokeFlow()
        {
            StartCommand();

            var flowName = FlowNameToken.Data().Demand(_values, "Invoking flow", "flow invoke");
            var nodeName = FlowNodeToken.Data().GetOrDefault(_values);
            var inputs = string.Join(" ", InputWildcardToken.GetNames(_values)
                .Select(name => $"{name}={InputWildcardToken.Data(name).GetOrDefault(_values)}"));

            await DoFlowTestConsoleGui(flowName, inputs, nodeName);
            
            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private async Task DoFlowInitConsoleGui(string flow, string prompt, string function, string module)
        {
            var response = await PfCli.FlowInit(flow, module, function, prompt, "chat", true);
            if (!string.IsNullOrEmpty(response.StdError))
            {
                _values.AddThrowError("ERROR:", response.StdError);
            }
        }

        private async Task DoFlowTestConsoleGui(string flowName, string inputs, string nodeName)
        {
            var response = await PfCli.FlowTest(flowName, inputs, nodeName);
            if (!string.IsNullOrEmpty(response.StdError))
            {
                _values.AddThrowError("ERROR:", response.StdError);
            }
        }

        private static void SplitFunctionReference(string functionData, out string entryFile, out string functionName)
        {
            var parts = functionData.Split(':');
            entryFile = parts != null && parts.Length > 0 ? parts[0] : "";
            functionName = parts != null && parts.Length > 1 ? parts[1] : "";
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
