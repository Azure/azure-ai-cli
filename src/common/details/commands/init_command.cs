//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;
using Azure.AI.Details.Common.CLI.ConsoleGui;

namespace Azure.AI.Details.Common.CLI
{
    public class InitCommand : Command
    {
        internal InitCommand(ICommandValues values)
        {
            _values = values.ReplaceValues();
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", true);
        }

        internal bool RunCommand()
        {
            RunInitCommand().Wait();
            return _values.GetOrDefault("passed", true);
        }

        private async Task<bool> RunInitCommand()
        {
            try
            {
                await DoCommand(_values.GetCommand());
                return _values.GetOrDefault("passed", true);
            }
            catch (ApplicationException)
            {
                Console.WriteLine();
                _values.Reset("passed", "false");
                return false;
            }
        }

        private async Task DoCommand(string command)
        {
            DisplayInitServiceBanner();

            CheckPath();

            var interactive = _values.GetOrDefault("init.service.interactive", true);
            switch (command)
            {
                case "init.openai": await DoInitOpenAi(interactive); break;
                case "init": await DoInitService(interactive); break;
            }
        }

        private async Task DoInitService(bool interactive)
        {
            Console.Write("Initialize: ");

            var choices = new string[] { "Azure OpenAI", "    + Cognitive Search", "    + AI Resource", "    + AI Project" };
            var choices2 = new string[] { "openai", "search", "resource", "project" };

            var normal = new Colors(ConsoleColor.White, ConsoleColor.Blue);
            var selected = new Colors(ConsoleColor.White, ConsoleColor.Red);

            var width = Math.Max(choices.Max(x => x.Length) + 4, 29);
            var picked = ListBoxPicker.PickIndexOf(choices.ToArray(), width, 30, normal, selected);
            if (picked < 0)
            {
                Console.WriteLine("\rInitialize: (canceled)");
                return;
            }
    
            var choice = string.Join(' ', choices.Take(picked + 1).Select(x => x.Trim()));
            Console.WriteLine($"\rInitialize: {choice}");

            // if (picked > 0)
            // {
            //     Console.WriteLine();
            //     _values.AddThrowError("WARNING:", $"'ai init {choices2[picked].ToLower()}' NOT YET IMPLEMENTED!!");
            //     return;
            // }

            await DoInitOpenAi(interactive);
        }

        private async Task DoInitOpenAi(bool interactive)
        {
            var subscriptionFilter = _values.GetOrDefault("init.service.subscription", "");
            var regionFilter = _values.GetOrDefault("init.service.resource.region.name", "");
            var groupFilter = _values.GetOrDefault("init.service.resource.group.name", "");
            var resourceFilter = _values.GetOrDefault("init.service.cognitiveservices.resource.name", "");
            var kind = _values.GetOrDefault("init.service.cognitiveservices.resource.kind", Program.CognitiveServiceResourceKind);
            var sku = _values.GetOrDefault("init.service.cognitiveservices.resource.sku", Program.CognitiveServiceResourceSku);
            var yes = _values.GetOrDefault("init.service.cognitiveservices.terms.agree", false);

            var (subscriptionId, region, endpoint, deployment, key) = await GetRegionAndKey(interactive, subscriptionFilter, regionFilter, groupFilter, resourceFilter, kind, sku, yes);
            ConfigServiceResource(subscriptionId, region, endpoint, deployment, key);
        }

        private void DisplayInitServiceBanner()
        {
            if (_quiet) return;

            var logo = FileHelpers.FindFileInHelpPath($"help/include.{Program.Name}.init.ascii.logo");
            if (!string.IsNullOrEmpty(logo))
            {
                var text = FileHelpers.ReadAllHelpText(logo, Encoding.UTF8);
                ConsoleHelpers.WriteLineWithHighlight(text + "\n");
            }
            else
            {
                ConsoleHelpers.WriteLineWithHighlight($"`{Program.Name.ToUpper()} INIT`");
            }
        }

        private async Task<(string, string, string, string, string)> GetRegionAndKey(bool interactive, string subscriptionFilter, string regionFilter, string groupFilter, string resourceFilter, string kind, string sku, bool agreeTerms)
        {
            var subscriptionId = await AzCliConsoleGui.PickSubscriptionIdAsync(interactive, subscriptionFilter);

            ConsoleHelpers.WriteLineWithHighlight($"\n`{Program.SERVICE_RESOURCE_DISPLAY_NAME_ALL_CAPS}`");
            var regionLocation = new AzCli.AccountRegionLocationInfo(); // await AzCliConsoleGui.PickRegionLocationAsync(interactive, regionFilter);
            var resource = await AzCliConsoleGui.PickOrCreateCognitiveResource(interactive, subscriptionId, regionLocation.Name, groupFilter, resourceFilter, kind, sku, agreeTerms);

            var deployment = await AzCliConsoleGui.AiResourceDeploymentPicker.PickOrCreateDeployment(interactive, subscriptionId, resource, null);

            var keys = await AzCliConsoleGui.LoadCognitiveServicesResourceKeys(subscriptionId, resource);
            return (subscriptionId, resource.RegionLocation, resource.Endpoint, deployment.Name, keys.Key1);
        }

        private static void ConfigServiceResource(string subscriptionId, string region, string endpoint, string deployment, string key)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CONFIG {Program.SERVICE_RESOURCE_DISPLAY_NAME_ALL_CAPS}`");
            Console.WriteLine();

            if (Program.InitConfigsSubscription)
            {
                ConfigSet("@subscription", subscriptionId, $"Subscription: {subscriptionId}");
            }

            if (Program.InitConfigsEndpoint)
            {
                ConfigSet("@endpoint", endpoint, $"    Endpoint: {endpoint}");
            }

            ConfigSet("@deployment", deployment, $"  Deployment: {deployment}");
            ConfigSet("@region", region, $"      Region: {region}");
            ConfigSet("@key", key, $"         Key: {key.Substring(0, 4)}****************************");
        }

        private static void ConfigSet(string atFile, string setValue, string message)
        {
            Console.Write($"*** SETTING *** {message}");
            ConfigSet(atFile, setValue);
            Console.WriteLine($"\r  *** SET ***   {message}");
        }

        private static void ConfigSet(string atFile, string setValue)
        {
            var setCommandValues = new CommandValues();
            setCommandValues.Add("x.command", "config");
            setCommandValues.Add("x.config.scope.hive", "local");
            setCommandValues.Add("x.config.command.at.file", atFile);
            setCommandValues.Add("x.config.command.set", setValue);
            var fileName = FileHelpers.GetOutputConfigFileName(atFile, setCommandValues);
            FileHelpers.WriteAllText(fileName, setValue, Encoding.UTF8);
        }

        private readonly bool _quiet = false;
        private readonly bool _verbose = false;
    }
}
