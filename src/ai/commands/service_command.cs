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

namespace Azure.AI.Details.Common.CLI
{
    public class ServiceCommand : Command
    {
        internal ServiceCommand(ICommandValues values) : base(values)
        {
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
            StartCommand();

            switch (command)
            {
                case "service.resource.create": DoCreateResource(); break;
                case "service.resource.list": DoListResources(); break;
                case "service.resource.delete": DoDeleteResource(); break;
                case "service.project.create": DoCreateProject(); break;
                case "service.project.list": DoListProjects(); break;
                case "service.project.delete": DoDeleteProject(); break;
                case "service.connection.create": DoCreateConnection(); break;
                case "service.connection.list": DoListConnections(); break;
                case "service.connection.delete": DoDeleteConnection(); break;

                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
            }

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private void DoCreateResource()
        {
            var action = "Creating AI resource";
            var command = "service resource create";
            var subscription = SubscriptionToken.Data().Demand(_values, action, command, checkConfig: "subscription");
            var location = RegionLocationToken.Data().Demand(_values, action, command);

            var name = ResourceNameToken.Data().Demand(_values, action, command);
            var group = ResourceGroupNameToken.Data().GetOrDefault(_values) ?? $"{name}-rg";
            var displayName = ResourceDisplayNameToken.Data().GetOrDefault(_values, name);
            var description = ResourceDescriptionToken.Data().GetOrDefault(_values, name);

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
            var subscription = SubscriptionToken.Data().Demand(_values, action, command, checkConfig: "subscription");
            var location = RegionLocationToken.Data().Demand(_values, action, command);
            var resource = ResourceNameToken.Data().Demand(_values, action, command);

            var name = ProjectNameToken.Data().Demand(_values, action, command);
            var group = ResourceGroupNameToken.Data().GetOrDefault(_values) ?? $"{name}-rg";
            var displayName = ProjectDisplayNameToken.Data().GetOrDefault(_values, name);
            var description = ProjectDescriptionToken.Data().GetOrDefault(_values, name);

            var message = $"{action} '{name}'";

            if (!_quiet) Console.WriteLine(message);
            var output = PythonSDKWrapper.CreateProject(_values, subscription, group, resource, name, location, displayName, description);
            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            if (!_quiet) Console.WriteLine(output);
            CheckWriteOutputValueFromJson("service.output", "json", output);
            CheckWriteOutputValueFromJson("service.output", "project.id", output, "id");
        }

        private void DoCreateConnection()
        {
            var action = "Creating AI connection";
            var command = "service connection create";
            var subscription = SubscriptionToken.Data().Demand(_values, action, command, checkConfig: "subscription");
            var project = ProjectNameToken.Data().Demand(_values, action, command, checkConfig: "project");
            var group = ResourceGroupNameToken.Data().Demand(_values, action, command, checkConfig: "group");

            var connectionName = ProjectConnectionNameToken.Data().Demand(_values, action, command);
            var connectionType = ProjectConnectionTypeToken.Data().Demand(_values, action, command);
            var connectionEndpoint = ProjectConnectionEndpointToken.Data().Demand(_values, action, command);
            var connectionKey = ProjectConnectionKeyToken.Data().Demand(_values, action, command);
            var cogServicesResourceKind = connectionType.Replace('-', '_') == "cognitive_services"
                ? CognitiveServicesResourceKindToken.Data().Demand(_values, action, command)
                : CognitiveServicesResourceKindToken.Data().GetOrDefault(_values);

            var message = $"{action} '{connectionName}'";

            if (!_quiet) Console.WriteLine(message);
            var output = PythonSDKWrapper.CreateConnection(_values, subscription, group, project, connectionName, connectionType, cogServicesResourceKind, connectionEndpoint, connectionKey);
            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            if (!_quiet) Console.WriteLine(output);
            CheckWriteOutputValueFromJson("service.output", "json", output);
        }

        private void DoListResources()
        {
            var action = "Listing AI resources";
            var command = "service resource list";
            var subscription = SubscriptionToken.Data().Demand(_values, action, command, checkConfig: "subscription");

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
            var subscription = SubscriptionToken.Data().Demand(_values, action, command, checkConfig: "subscription");

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
            var subscription = SubscriptionToken.Data().Demand(_values, action, command, checkConfig: "subscription");
            var group = ResourceGroupNameToken.Data().Demand(_values, action, command, checkConfig: "group");
            var project = ProjectNameToken.Data().Demand(_values, action, command, checkConfig: "project");

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

            var subscription = SubscriptionToken.Data().Demand(_values, action, command, checkConfig: "subscription");
            var resourceName = ResourceNameToken.Data().Demand(_values, action, command);
            var group = ResourceGroupNameToken.Data().Demand(_values, action, command, checkConfig: "group");

            var deleteDependentResources = DeleteDependentResourcesToken.Data().GetOrDefault(_values, false);

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

            var subscription = SubscriptionToken.Data().Demand(_values, action, command, checkConfig: "subscription");
            var projectName = ProjectNameToken.Data().Demand(_values, action, command);
            var group = ResourceGroupNameToken.Data().Demand(_values, action, command, checkConfig: "group");

            var deleteDependentResources = DeleteDependentResourcesToken.Data().GetOrDefault(_values, false);

            var message = $"{action} for '{projectName}'";

            if (!_quiet) Console.WriteLine(message);
            var output = PythonSDKWrapper.DeleteProject(_values, subscription, group, projectName, deleteDependentResources);
            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            if (!_quiet) Console.WriteLine(output);
            CheckWriteOutputValueFromJson("service.output", "json", output);
        }

        private void DoDeleteConnection()
        {
            var action = "Deleting AI connection";
            var command = "service connection delete";

            var subscription = SubscriptionToken.Data().Demand(_values, action, command, checkConfig: "subscription");
            var group = ResourceGroupNameToken.Data().Demand(_values, action, command, checkConfig: "group");
            var projectName = ProjectNameToken.Data().Demand(_values, action, command, checkConfig: "project");
            var connectionName = ProjectConnectionNameToken.Data().Demand(_values, action, command);

            var message = $"{action} for '{connectionName}'";

            if (!_quiet) Console.WriteLine(message);
            var output = PythonSDKWrapper.DeleteConnection(_values, subscription, group, projectName, connectionName);
            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            if (!_quiet) Console.WriteLine(output);
            CheckWriteOutputValueFromJson("service.output", "json", output);
        }

        private void CheckWriteOutputValueFromJson(string part1, string part2, string json, string valueKey = null)
        {
            var parsed = !string.IsNullOrEmpty(json) ? JsonDocument.Parse(json) : null;
            var value = !string.IsNullOrEmpty(valueKey) ? parsed?.GetPropertyStringOrNull(valueKey) : json;
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
        private bool _quiet = false;
        private bool _verbose = false;
    }

}


