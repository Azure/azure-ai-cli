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
                case "service.resource.create": DoCreateResource(); break;
                case "service.resource.list": DoListResources(); break;
                case "service.resource.delete": DoDeleteResource(); break;
                case "service.project.create": DoCreateProject(); break;
                case "service.project.list": DoListProjects(); break;
                case "service.project.delete": DoDeleteProject(); break;
                case "service.connection.list": DoListConnections(); break;

                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
            }
        }

        private void DoCreateResource()
        {
            var action = "Creating AI resource";
            var command = "service resource create";
            var subscription = DemandSubscription(action, command);
            var location = DemandRegionLocation(action, command);

            var name = DemandName("service.resource.name", action, command);
            var group = GetGroupName() ?? $"{name}-rg";
            var displayName = _values.Get("service.resource.display.name", true) ?? name;
            var description = _values.Get("service.resource.description", true) ?? name;

            var message = $"{action} '{name}'";

            if (!_quiet) Console.WriteLine(message);
            var output = PythonSDKWrapper.CreateResource(_values, subscription, group, name, location, displayName, description);
            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            if (!_quiet) Console.WriteLine(output);
            CheckWriteOutputValueFromJson("service.output", "json", output);
            CheckWriteOutputValueFromJson("service.output", "resource.id", output, "id");
        }

        private void DoCreateProject()
        {
            var action = "Creating AI project";
            var command = "service project create";
            var subscription = DemandSubscription(action, command);
            var location = DemandRegionLocation(action, command);
            var resource = DemandResource(action, command);

            var name = DemandName("service.project.name", action, command);
            var group = GetGroupName() ?? $"{name}-rg";
            var displayName = _values.Get("service.project.display.name", true) ?? name;
            var description = _values.Get("service.project.description", true) ?? name;

            var message = $"{action} '{name}'";

            if (!_quiet) Console.WriteLine(message);
            var output = PythonSDKWrapper.CreateProject(_values, subscription, group, resource, name, location, displayName, description);
            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            if (!_quiet) Console.WriteLine(output);
            CheckWriteOutputValueFromJson("service.output", "json", output);
            CheckWriteOutputValueFromJson("service.output", "project.id", output, "id");
        }

        private void DoListResources()
        {
            var action = "Listing AI resources";
            var command = "service resource list";
            var subscription = DemandSubscription(action, command);

            var message = $"{action} for '{subscription}'";

            if (!_quiet) Console.WriteLine(message);
            var output = PythonSDKWrapper.ListResources(_values, subscription);
            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            if (!_quiet) Console.WriteLine(output);
            CheckWriteOutputValueFromJson("service.output", "json", output);
        }

        private void DoListProjects()
        {
            var action = "Listing AI projects";
            var command = "service project list";
            var subscription = DemandSubscription(action, command);

            var message = $"{action} for '{subscription}'";

            if (!_quiet) Console.WriteLine(message);
            var output = PythonSDKWrapper.ListProjects(_values, subscription);
            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            if (!_quiet) Console.WriteLine(output);
            CheckWriteOutputValueFromJson("service.output", "json", output);
        }

        private void DoListConnections()
        {
            var action = "Listing Project connections";
            var command = "service connection list";
            var subscription = DemandSubscription(action, command);
            var group = DemandGroup(action, command);
            var project = DemandName("service.project.name", action, command);

            var message = $"{action} for '{project}'";

            if (!_quiet) Console.WriteLine(message);
            var output = PythonSDKWrapper.ListConnections(_values, subscription, group, project);
            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            if (!_quiet) Console.WriteLine(output);
            CheckWriteOutputValueFromJson("service.output", "json", output);
        }

        private void DoDeleteResource()
        {
            var action = "Deleting AI resource";
            var command = "service resource delete";

            var subscription = DemandSubscription(action, command);
            var resourceName = DemandName("service.resource.name", action, command);
            var group = DemandGroup(action, command);

            var deleteDependentResources = _values.GetOrDefault("service.resource.delete.dependent.resources", false);

            var message = $"{action} for '{resourceName}'";

            if (!_quiet) Console.WriteLine(message);
            var output = PythonSDKWrapper.DeleteResource(_values, subscription, group, resourceName, deleteDependentResources);
            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            if (!_quiet) Console.WriteLine(output);
            CheckWriteOutputValueFromJson("service.output", "json", output);
        }

        private void DoDeleteProject()
        {
            var action = "Deleting AI project";
            var command = "service project delete";

            var subscription = DemandSubscription(action, command);
            var projectName = DemandName("service.project.name", action, command);
            var group = DemandGroup(action, command);

            var deleteDependentResources = _values.GetOrDefault("service.project.delete.dependent.resources", false);

            var message = $"{action} for '{projectName}'";

            if (!_quiet) Console.WriteLine(message);
            var output = PythonSDKWrapper.DeleteProject(_values, subscription, group, projectName, deleteDependentResources);
            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            if (!_quiet) Console.WriteLine(output);
            CheckWriteOutputValueFromJson("service.output", "json", output);
        }

        private void CheckWriteOutputValueFromJson(string part1, string part2, string json, string valueKey = null)
        {
            var parsed = !string.IsNullOrEmpty(json) ? JToken.Parse(json) : null;
            var value = !string.IsNullOrEmpty(valueKey) ? parsed?[valueKey]?.ToString() : json;
            CheckWriteOutputValue(part1, part2, value);
        }

        private void CheckWriteOutputValue(string part1, string part2, string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            var atValue = _values.Get($"{part1}.{part2}", true);
            if (!string.IsNullOrEmpty(atValue))
            {
                var atValueFile = FileHelpers.GetOutputDataFileName(atValue, _values);
                FileHelpers.WriteAllText(atValueFile, value, Encoding.UTF8);

                var fi = new FileInfo(atValueFile);
                if (!_quiet) Console.WriteLine($"{fi.Name} (saved at {fi.DirectoryName})\n\n  {value}");
            }

            var addValue = _values.Get($"{part1}.add.{part2}", true);
            if (!string.IsNullOrEmpty(addValue))
            {
                var addValueFile = FileHelpers.GetOutputDataFileName(addValue, _values);
                FileHelpers.AppendAllText(addValueFile, "\n" + value, Encoding.UTF8);

                var fi = new FileInfo(addValueFile);
                if (!_quiet) Console.WriteLine($"{fi.Name} (saved/added at {fi.DirectoryName})\n\n  {value}");
            }
        }

        private string DemandSubscription(string action, string command)
        {
            var subscription = _values.Get("service.subscription", true);
            if (string.IsNullOrEmpty(subscription) || subscription.Contains("rror"))
            {
                _values.AddThrowError(
                    "ERROR:", $"{action}; requires subscription.",
                            "",
                      "TRY:", $"{Program.Name} init",
                              $"{Program.Name} config --set subscription SUBSCRIPTION",
                              $"{Program.Name} {command} --subscription SUBSCRIPTION",
                            "",
                      "SEE:", $"{Program.Name} help {command}");
            }
            return subscription;
        }

        private string DemandName(string valuesName, string action, string command)
        {
            var name = _values.Get(valuesName, true);
            if (string.IsNullOrEmpty(name))
            {
                _values.AddThrowError(
                    "ERROR:", $"{action}; requires name.",
                      "TRY:", $"{Program.Name} {command} --name NAME",
                      "SEE:", $"{Program.Name} help {command}");
            }
            return name;
        }

        private string DemandGroup(string action, string command)
        {
            var group = _values.Get("service.resource.group.name", true);
            if (string.IsNullOrEmpty(group))
            {
                _values.AddThrowError(
                    "ERROR:", $"{action}; requires group.",
                      "TRY:", $"{Program.Name} {command} --group GROUP",
                      "SEE:", $"{Program.Name} help {command}");
            }
            return group;
        }

        private string DemandResource(string action, string command)
        {
            var resource = _values.Get("service.resource.name", true);
            if (string.IsNullOrEmpty(resource))
            {
                _values.AddThrowError(
                    "ERROR:", $"{action}; requires resource.",
                      "TRY:", $"{Program.Name} {command} --resource RESOURCE",
                      "SEE:", $"{Program.Name} help {command}");
            }
            return resource;
        }

        private string DemandRegionLocation(string action, string command)
        {
            var location = _values.Get("service.region.location", true);
            if (string.IsNullOrEmpty(location))
            {
                _values.AddThrowError(
                    "ERROR:", $"{action}; requires location.",
                      "TRY:", $"{Program.Name} {command} --location LOCATION",
                      "SEE:", $"{Program.Name} help {command}");
            }
            return location;
        }

        private string GetGroupName()
        {
            return _values.Get("service.resource.group.name", true);
        }

        private bool _quiet = false;
        private bool _verbose = false;
    }

}


