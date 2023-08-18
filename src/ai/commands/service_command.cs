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
using Newtonsoft.Json.Linq;

namespace Azure.AI.Details.Common.CLI
{
    public class ServiceCommand : Command
    {
        internal ServiceCommand(ICommandValues values)
        {
            _values = values.ReplaceValues();
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", true);
        }

        internal bool RunCommand()
        {
            try
            {
                RunServiceCommand();
            }
            catch (WebException ex)
            {
                ConsoleHelpers.WriteLineError($"\n  ERROR: {ex.Message}");
                JsonHelpers.PrintJson(HttpHelpers.ReadWriteJson(ex.Response, _values, "service"));
            }

            return _values.GetOrDefault("passed", true);
        }

        private bool RunServiceCommand()
        {
            DoCommand(_values.GetCommand());
            return _values.GetOrDefault("passed", true);
        }

        private void DoCommand(string command)
        {
            CheckPath();

            switch (command)
            {
                case "service.resource.list": DoListResources(); break;
                case "service.project.list": DoListProjects(); break;

                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
            }
        }

        private void DoListResources()
        {
            var subscription = DemandSubscription();

            var message = $"Listing resources for '{subscription}'";
            if (!_quiet) Console.WriteLine(message);

            DoListResourcesViaPython(subscription);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");
        }

        private void DoListProjects()
        {
            var subscription = DemandSubscription();

            var message = $"Listing projects for '{subscription}'";
            if (!_quiet) Console.WriteLine(message);

            DoListProjectsViaPython(subscription);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");
        }


        private void DoListResourcesViaPython(string subscription)
        {
            var path = FileHelpers.FindFileInHelpPath($"help/include.python.script.hub_list.py");
            var script = FileHelpers.ReadAllHelpText(path, Encoding.UTF8);

            (var exit, var output)= PythonRunner.RunScriptAsync(script, $"--subscription {subscription}").Result;
            if (exit == 0)
            {
                Console.Write(output);
            }
            else
            {
                ConsoleHelpers.WriteLineError("\nERROR: Python script failed!\n");
                Console.WriteLine("  " + output.Trim().Replace("\n", "\n  "));
            }
        }

        private void DoListProjectsViaPython(string subscription)
        {
            var path = FileHelpers.FindFileInHelpPath($"help/include.python.script.project_list.py");
            var script = FileHelpers.ReadAllHelpText(path, Encoding.UTF8);

            (var exit, var output)= PythonRunner.RunScriptAsync(script, $"--subscription {subscription}").Result;
            if (exit == 0)
            {
                Console.Write(output);
            }
            else
            {
                ConsoleHelpers.WriteLineError("\nERROR: Python script failed!\n");
                Console.WriteLine("  " + output.Trim().Replace("\n", "\n  "));
            }
        }

        private string DemandSubscription()
        {
            var subscription = _values.Get("service.subscription", true);

            if (string.IsNullOrEmpty(subscription) || subscription.Contains("rror"))
            {
                _values.AddThrowError(
                    "ERROR:", $"Listing AI resoures; requires subscription.",
                            "",
                      "TRY:", $"{Program.Name} init",
                              $"{Program.Name} config --set subscription SUBSCRIPTION",
                              $"{Program.Name} service resource list --subscription SUBSCRIPTION",
                            "",
                      "SEE:", $"{Program.Name} help service resource list");
            }

            return subscription;
        }

        private bool _quiet = false;
        private bool _verbose = false;
    }
}
