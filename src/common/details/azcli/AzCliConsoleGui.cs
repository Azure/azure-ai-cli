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
    public class AzCliConsoleGui
    {
        public static async Task<string> PickSubscriptionIdAsync(bool interactive, string subscriptionFilter = null)
        {
            if (Guid.TryParse(subscriptionFilter, out var subscriptionId))
            {
                return subscriptionId.ToString();
            }
            
            var subscription = await PickSubscriptionAsync(interactive, subscriptionFilter);
            return subscription.Id;
        }

        public static async Task<AzCli.SubscriptionInfo> PickSubscriptionAsync(bool interactive, string subscriptionFilter = null)
        {
            (var subscription, var error) = await FindSubscriptionAsync(interactive, subscriptionFilter);
            if (subscription == null && error != null)
            {
                throw new ApplicationException($"ERROR: Loading subscriptions:\n{error}");
            }
            else if (subscription == null)
            {
                throw new ApplicationException($"CANCELED: No subscription selected.");
            }
            return subscription.Value;
        }

        public static async Task<AzCli.AccountRegionLocationInfo> PickRegionLocationAsync(bool interactive, string regionFilter = null)
        {
            (var regionLocation, var error) = await FindRegionAsync(interactive, regionFilter, true);
            if (regionLocation == null && error != null)
            {
                throw new ApplicationException($"ERROR: Loading resource regions/locations:\n{error}");
            }
            else if (regionLocation == null)
            {
                throw new ApplicationException($"CANCELED: No resource region/location selected.");
            }
            return regionLocation.Value;
        }

        public static async Task<AzCli.ResourceGroupInfo> PickOrCreateResourceGroup(bool interactive, string subscriptionId = null, string regionFilter = null, string groupFilter = null)
        {
            var createNewItem = !string.IsNullOrEmpty(groupFilter)
                ? $"(Create `{groupFilter}`)"
                : interactive ? "(Create new)" : null;

            (var group, var error) = await FindGroupAsync(interactive, subscriptionId, regionFilter, groupFilter, createNewItem);
            if ((group != null && group.Value.Name == null) || (group == null && groupFilter == null))
            {
                (group, error) = await TryCreateResourceGroup(interactive, subscriptionId, regionFilter, groupFilter);
            }

            if (group == null && error != null)
            {
                throw new ApplicationException($"ERROR: Loading or creating resource group:\n{error}");
            }
            else if (group == null)
            {
                throw new ApplicationException($"CANCELED: No resource selected");
            }

            return group.Value;
        }

        public static async Task<AzCli.CognitiveServicesResourceInfo> PickOrCreateCognitiveResource(bool interactive, string subscriptionId = null, string regionFilter = null, string groupFilter = null, string resourceFilter = null, string kind = null, string sku = "F0", bool agreeTerms = false)
        {
            var createNewItem = !string.IsNullOrEmpty(resourceFilter)
                ? $"(Create `{resourceFilter}`)"
                : interactive ? "(Create new)" : null;

            (var resource, var error) = await FindCognitiveServicesResource(interactive, subscriptionId, regionFilter, groupFilter, resourceFilter, kind, createNewItem);
            if (resource != null && resource.Value.Name == null)
            {
                (resource, error) = await TryCreateCognitiveServicesResource(interactive, subscriptionId, regionFilter, groupFilter, resourceFilter, kind, sku, agreeTerms);
            }

            if (resource == null && error != null)
            {
                throw new ApplicationException($"ERROR: Loading or creating resource:\n{error}");
            }
            else if (resource == null)
            {
                throw new ApplicationException($"CANCELED: No resource selected");
            }

            return resource.Value;
        }

        // public static async Task<AzCli.CognitiveServicesDeploymentInfo> PickOrCreateCognitiveResourceDeployment(bool interactive, string subscriptionId, string regionLocation, string group, string resourceName, string deploymentFilter)
        // {
        //     var createNewItem = !string.IsNullOrEmpty(deploymentFilter)
        //         ? $"(Create `{deploymentFilter}`)"
        //         : interactive ? "(Create new)" : null;

        //     (var deployment, var error) = await FindCognitiveServicesResourceDeployment(interactive, subscriptionId, regionLocation, group, resourceName, deploymentFilter, createNewItem);
        //     if (deployment != null && deployment.Value.Name == null)
        //     {
        //         (deployment, error) = await TryCreateCognitiveServicesResourceDeployment(interactive, subscriptionId, regionLocation, group, resourceName, deploymentFilter);
        //     }

        //     if (deployment == null && error != null)
        //     {
        //         throw new ApplicationException($"ERROR: Loading or creating resource deployment:\n{error}");
        //     }
        //     else if (deployment == null)
        //     {
        //         throw new ApplicationException($"CANCELED: No resource deployment selected");
        //     }

        //     return deployment.Value;
        // }

        public static async Task<AzCli.CognitiveServicesKeyInfo> LoadCognitiveServicesResourceKeys(string subscriptionId, AzCli.CognitiveServicesResourceInfo resource)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`{Program.SERVICE_RESOURCE_DISPLAY_NAME_ALL_CAPS} KEYS`");

            Console.Write("Keys: *** Loading ***");
            var keys = await AzCli.ListCognitiveServicesKeys(subscriptionId, resource.Group, resource.Name);

            Console.Write("\r");
            Console.WriteLine($"Key1: {keys.Payload.Key1.Substring(0, 4)}****************************");
            Console.WriteLine($"Key2: {keys.Payload.Key2.Substring(0, 4)}****************************");
            return keys.Payload;
        }

        private static string AskPrompt(string prompt, string value = null, bool useEditBox = false)
        {
            Console.Write(prompt);

            if (useEditBox)
            {
                var normal = new Colors(ConsoleColor.White, ConsoleColor.Blue);
                var text = EditBoxQuickEdit.Edit(40, 1, normal, value, 128);
                Console.WriteLine(text);
                return text;
            }

            if (!string.IsNullOrEmpty(value))
            {
                Console.WriteLine(value);
                return value;
            }

            return Console.ReadLine();
        }

        private static async Task<(AzCli.SubscriptionInfo? Subscription, string Error)> FindSubscriptionAsync(bool interactive, string subscriptionFilter = null)
        {
            Console.Write("\rSubscription: *** Loading choices ***");
            var response = await AzCli.ListAccounts();

            var noOutput = string.IsNullOrEmpty(response.StdOutput);
            var hasError = !string.IsNullOrEmpty(response.StdError);
            var hasErrorNotFound = hasError && (response.StdError.Contains(" not ") || response.StdError.Contains("No such file"));

            Console.Write("\rSubscription: ");
            if (noOutput && hasError && hasErrorNotFound)
            {
                ConsoleHelpers.WriteLineError("*** Please install the Azure CLI - https://aka.ms/azcli ***");
                Console.Write("\nNOTE: If it's already installed ensure it's in the system PATH and working (try: `az account list`)");
                return (null, response.StdError);
            }
            else if (noOutput && hasError)
            {
                ConsoleHelpers.WriteLineError("*** ERROR: Loading subscriptions ***");
                return (null, response.StdError);
            }

            var needLogin = response.StdError != null && response.StdError.Contains("az login");
            if (response.Payload.Count() == 0 && needLogin)
            {
                bool cancelLogin = !interactive;
                if (interactive)
                {
                    ConsoleHelpers.WriteError("*** WARNING: `az login` required ***");
                    Console.Write(" ");

                    var choices = "LAUNCH: `az login` (interactive browser);CANCEL: `az login ...` (non-interactive)".Split(';').ToArray();
                    var normal = new Colors(ConsoleColor.White, ConsoleColor.Blue);
                    var selected = new Colors(ConsoleColor.White, ConsoleColor.Red);

                    var picked = ListBoxPicker.PickIndexOf(choices, int.MinValue, 30, normal, selected);
                    cancelLogin = (picked < 0 || picked == 1);
                }

                if (cancelLogin)
                {
                    Console.Write("\rSubscription: ");
                    ConsoleHelpers.WriteLineError("*** Please run `az login` and try again ***");
                    return (null, null);
                }

                Console.Write("\rSubscription: *** Launching `az login` (interactive) ***");
                response = await AzCli.Login();
                Console.Write("\rSubscription: ");
            }

            var subscriptions = response.Payload
                .Where(x => MatchSubscriptionFilter(x, subscriptionFilter))
                .OrderBy(x => (x.IsDefault ? "0" : "1") + x.Name)
                .ToArray();

            if (subscriptions.Count() == 0)
            {
                ConsoleHelpers.WriteLineError(response.Payload.Count() > 0
                    ? "*** No matching subscriptions found ***"
                    : "*** No subscriptions found ***");
                return (null, null);
            }
            else if (subscriptions.Count() == 1)
            {
                var subscription = subscriptions[0];
                DisplayNameAndId(subscription);
                return (subscription, null);
            }
            else if (!interactive)
            {
                ConsoleHelpers.WriteLineError("*** More than 1 subscription found ***");
                Console.WriteLine();
                DisplaySubscriptions(subscriptions, "  ");
                return (null, null);
            }

            return ListBoxPickSubscription(subscriptions);
        }

        public static async Task<(AzCli.AccountRegionLocationInfo? RegionLocation, string Error)> FindRegionAsync(bool interactive, string regionFilter = null, bool allowAnyRegionOption = false)
        {
            var p0 = allowAnyRegionOption ? "(Any region/location)" : null;
            var hasP0 = !string.IsNullOrEmpty(p0);

            Console.Write("\rRegion: *** Loading choices ***");
            var response = await AzCli.ListAccountRegionLocations();

            Console.Write("\rRegion: ");
            if (string.IsNullOrEmpty(response.StdOutput) && !string.IsNullOrEmpty(response.StdError))
            {
                ConsoleHelpers.WriteLineError($"ERROR: Loading resource region/locations\n{response.StdError}");
                return (null, response.StdError);
            }

            var regions = response.Payload
                .Where(x => MatchRegionLocationFilter(x, regionFilter))
                .OrderBy(x => x.RegionalDisplayName)
                .ToList();

            var exactMatch = regionFilter != null && regions.Count(x => ExactMatchRegionLocation(x, regionFilter)) == 1;
            if (exactMatch) regions = regions.Where(x => ExactMatchRegionLocation(x, regionFilter)).ToList();

            if (regions.Count() == 0)
            {
                ConsoleHelpers.WriteLineError(response.Payload.Count() > 0
                    ? "*** No matching resource region/locations found ***"
                    : "*** No resource region/locations found ***");
                return (null, null);
            }
            else if (regions.Count() == 1 && (!interactive || exactMatch))
            {
                var region = regions.First();
                DisplayNameAndDisplayName(region);
                return (region, null);
            }
            else if (!interactive)
            {
                ConsoleHelpers.WriteLineError("*** More than 1 region/location found ***");
                Console.WriteLine();
                DisplayRegionLocations(regions, "  ");
                return (null, null);
            }

            return interactive
                ? ListBoxPickAccountRegionLocation(regions.ToArray(), p0)
                : (null, null);
        }

        public static async Task<(AzCli.ResourceGroupInfo? Group, string Error)> FindGroupAsync(bool interactive, string subscription = null, string regionLocation = null, string groupFilter = null, string allowCreateGroupOption = null)
        {
            var allowCreateGroup = !string.IsNullOrEmpty(allowCreateGroupOption);

            Console.Write("\rGroup: *** Loading choices ***");
            var response = await AzCli.ListResourceGroups(subscription, regionLocation);

            Console.Write("\rGroup: ");
            if (string.IsNullOrEmpty(response.StdOutput) && !string.IsNullOrEmpty(response.StdError))
            {
                ConsoleHelpers.WriteLineError($"ERROR: Loading resource groups: {response.StdError}");
                return (null, response.StdError);
            }

            var groups = response.Payload
                .Where(x => MatchGroupFilter(x, groupFilter))
                .OrderBy(x => x.Name)
                .ToList();

            var exactMatch = groupFilter != null && groups.Count(x => ExactMatchGroup(x, groupFilter)) == 1;
            if (exactMatch) groups = groups.Where(x => ExactMatchGroup(x, groupFilter)).ToList();

            if (groups.Count() == 0)
            {
                if  (!allowCreateGroup)
                {
                    ConsoleHelpers.WriteLineError(response.Payload.Count() > 0
                        ? $"*** No matching resource groups found ***"
                        : $"*** No resource groups found ***");
                    return (null, null);
                }
                else if (!interactive)
                {
                    Console.WriteLine(allowCreateGroupOption);
                    return (new AzCli.ResourceGroupInfo(), null);
                }
            }
            else if (groups.Count() == 1 && (!interactive || exactMatch))
            {
                var group = groups.First();
                DisplayNameAndRegionLocation(group);
                return (group, null);
            }
            else if (!interactive)
            {
                ConsoleHelpers.WriteLineError("*** More than 1 resource group found ***");
                Console.WriteLine();
                DisplayGroups(groups, "  ");
                return (null, null);
            }

            return ListBoxPickResourceGroup(groups.ToArray(), allowCreateGroupOption);
        }

        public static async Task<(AzCli.CognitiveServicesResourceInfo? Resource, string Error)> FindCognitiveServicesResource(bool interactive, string subscriptionId = null, string regionLocationFilter = null, string groupFilter = null, string resourceFilter = null, string kind = null, string allowCreateResourceOption = null)
        {
            var allowCreateResource = !string.IsNullOrEmpty(allowCreateResourceOption);

            Console.Write("\rName: *** Loading choices ***");
            var response = await AzCli.ListCognitiveServicesResources(subscriptionId, kind);

            Console.Write("\rName: ");
            if (string.IsNullOrEmpty(response.StdOutput) && !string.IsNullOrEmpty(response.StdError))
            {
                ConsoleHelpers.WriteLineError($"ERROR: Loading Cognitive Services resources: {response.StdError}");
                return (null, response.StdError);
            }

            var resources = response.Payload
                .Where(x => MatchResourceFilter(x, regionLocationFilter, groupFilter, resourceFilter))
                .OrderBy(x => x.Name + x.RegionLocation)
                .ToList();

            var exactMatch = resourceFilter != null && resources.Count(x => ExactMatchResource(x, regionLocationFilter, groupFilter, resourceFilter)) == 1;
            if (exactMatch) resources = resources.Where(x => ExactMatchResource(x, regionLocationFilter, groupFilter, resourceFilter)).ToList();

            if (resources.Count() == 0)
            {
                if (!allowCreateResource)
                {
                    ConsoleHelpers.WriteLineError($"*** No Cognitive Services resources found ***");
                    return (null, null);
                }
                else if (!interactive)
                {
                    Console.WriteLine(allowCreateResourceOption);
                    return (new AzCli.CognitiveServicesResourceInfo(), null);
                }
            }
            else if (resources.Count() == 1 && (!interactive || exactMatch))
            {
                var resource = resources.First();
                DisplayName(resource);
                return (resource, null);
            }
            else if (!interactive)
            {
                ConsoleHelpers.WriteLineError("*** More than 1 resource found ***");
                Console.WriteLine();
                DisplayResources(resources, "  ");
                return (null, null);
            }

            return interactive
                ? ListBoxPickCognitiveServicesResource(resources.ToArray(), allowCreateResourceOption)
                : (null, null);
        }

        // private static async Task<(AzCli.CognitiveServicesDeploymentInfo? Deployment, string Error)> FindCognitiveServicesResourceDeployment(bool interactive, string subscriptionId, string regionLocation, string group, string resourceName, string deploymentFilter, string createNewItem)
        // {
        //     var allowCreateDeployment = !string.IsNullOrEmpty(createNewItem);

        //     Console.Write("\rDeployment: *** Loading choices ***");
        //     var response = await AzCli.ListCognitiveServicesResourceDeployments(subscriptionId, group, resourceName, deploymentFilter);

        //     Console.Write("\rDeployment: ");
        //     if (string.IsNullOrEmpty(response.StdOutput) && !string.IsNullOrEmpty(response.StdError))
        //     {
        //         ConsoleHelpers.WriteLineError($"ERROR: Loading Cognitive Services resource deployments: {response.StdError}");
        //         return (null, response.StdError);
        //     }

        //     var deployments = response.Payload
        //         .Where(x => MatchDeploymentFilter(x, deploymentFilter))
        //         .OrderBy(x => x.Name)
        //         .ToList();

        //     var exactMatch = deploymentFilter != null && deployments.Count(x => ExactMatchDeployment(x, deploymentFilter)) == 1;
        //     if (exactMatch) deployments = deployments.Where(x => ExactMatchDeployment(x, deploymentFilter)).ToList();

        //     if (deployments.Count() == 0)
        //     {
        //         if (!createNew)
        //         {
        //             ConsoleHelpers.WriteLineError($"*** No Cognitive Services resource deployments found ***");
        //             return (null, null);
        //         }
        //         else if (!interactive)
        //         {
        //             Console.WriteLine(createNewItem);
        //             return (new AzCli.CognitiveServicesDeploymentInfo(), null);
        //         }
        //     }
        //     else if (deployments.Count() == 1 && (!interactive || exactMatch))
        //     {
        //         var deployment = deployments.First();
        //         DisplayName(deployment);
        //         return (deployment, null);
        //     }
        //     else if (!interactive)
        //     {
        //         ConsoleHelpers.WriteLineError("*** More than 1 deployment found ***");
        //         Console.WriteLine();
        //         DisplayDeployments(deployments, "  ");
        //         return (null, null);
        //     }

        //     return interactive
        //         ? ListBoxPickCognitiveServicesResourceDeployment(deployments.ToArray(), createNewItem)
        //         : (null, null);
        // }

        // private static Task<(AzCli.CognitiveServicesDeploymentInfo? deployment, string error)> TryCreateCognitiveServicesResourceDeployment(bool interactive, string subscriptionId, string regionLocation, string group, string resourceName, string deploymentFilter)
        // {
        // }

        public static async Task<(AzCli.CognitiveServicesResourceInfo? Resource, string Error)> TryCreateCognitiveServicesResource(bool interactive, string subscriptionId = null, string regionLocationFilter = null, string groupFilter = null, string resourceFilter = null, string kind = null, string sku = "F0", bool agreeTerms = false)
        {
            ConsoleHelpers.WriteLineWithHighlight("\n`RESOURCE GROUP`");

            (var regionLocation, var errorLoc) = await FindRegionAsync(interactive, regionLocationFilter, true);
            if (regionLocation == null) return (null, errorLoc);

            var group = await PickOrCreateResourceGroup(interactive, subscriptionId, regionLocationFilter, groupFilter);

            ConsoleHelpers.WriteLineWithHighlight($"\n`CREATE {Program.SERVICE_RESOURCE_DISPLAY_NAME_ALL_CAPS}`");
            Console.WriteLine($"Region: {group.RegionLocation}");
            Console.WriteLine($"Group: {group.Name}");

            var name = AskPrompt("Name: ", resourceFilter);
            if (string.IsNullOrEmpty(name)) return (null, null);
            if (!agreeTerms && !CheckAgreeTerms(kind)) return (null, null);

            Console.Write("*** CREATING ***");
            var response = await AzCli.CreateCognitiveServicesResource(subscriptionId, group.Name, group.RegionLocation, name, kind, sku);

            Console.Write("\r");
            if (string.IsNullOrEmpty(response.StdOutput) && !string.IsNullOrEmpty(response.StdError))
            {
                ConsoleHelpers.WriteLineError($"ERROR: Creating Cognitive Services resources: {response.StdError}");
                return (null, response.StdError);
            }

            Console.WriteLine("\r*** CREATED ***  ");
            return (response.Payload, null);
        }

        private static async Task<(AzCli.ResourceGroupInfo? Group, string Error)> TryCreateResourceGroup(bool interactive, string subscriptionId, string regionLocationFilter, string groupName)
        {
            ConsoleHelpers.WriteLineWithHighlight("\n`CREATE RESOURCE GROUP`");

            (var regionLocation, var errorLoc) = await FindRegionAsync(interactive, regionLocationFilter, false);
            if (regionLocation == null) return (null, errorLoc);

            var name = AskPrompt("Name: ", groupName);
            if (string.IsNullOrEmpty(name)) return (null, null);

            Console.Write("*** CREATING ***");
            var response = await AzCli.CreateResourceGroup(subscriptionId, regionLocation.Value.Name, name);

            Console.Write("\r");
            if (string.IsNullOrEmpty(response.StdOutput) && !string.IsNullOrEmpty(response.StdError))
            {
                ConsoleHelpers.WriteLineError($"ERROR: Creating resource group.\n{response.StdError}");
                return (null, response.StdError);
            }

            Console.WriteLine("\r*** CREATED ***  ");
            return (response.Payload, null);
        }

        private static bool CheckAgreeTerms(string kind)
        {
            var checkAttestation = kind.ToLower() switch
            {
                "face" => true,
                "cognitiveservices" => true,
                _ => false
            };

            if (checkAttestation)
            {
                Console.Write("\nI certify that use of this service is not by or for a police department in the United States: ");
                var yesOrNo = ListBoxPickYesNo();
                Console.WriteLine();
                return yesOrNo;
            }

            return true;
        }

        private static bool ListBoxPickYesNo()
        {
            var choices = "Yes;No".Split(';').ToArray();
            var normal = new Colors(ConsoleColor.White, ConsoleColor.Blue);
            var selected = new Colors(ConsoleColor.White, ConsoleColor.Red);

            var picked = ListBoxPicker.PickIndexOf(choices, int.MinValue, 4, normal, selected);
            Console.WriteLine(picked switch {
                0 => "Yes",
                1 => "No",
                _ => "Canceled"
            });
            return (picked == 0);
        }

        private static (AzCli.SubscriptionInfo? Subscription, string Error) ListBoxPickSubscription(AzCli.SubscriptionInfo[] subscriptions)
        {
            var list = subscriptions.Select(x => x.Name).ToArray();
            var normal = new Colors(ConsoleColor.White, ConsoleColor.Blue);
            var selected = new Colors(ConsoleColor.White, ConsoleColor.Red);

            var picked = ListBoxPicker.PickIndexOf(list, 60, 30, normal, selected);
            if (picked < 0)
            {
                return (null, null);
            }

            var subscription = subscriptions[picked];
            DisplayNameAndId(subscription);
            return (subscription, null);
        }

        private static (AzCli.AccountRegionLocationInfo? RegionLocation, string Error) ListBoxPickAccountRegionLocation(AzCli.AccountRegionLocationInfo[] items, string p0)
        {
            var list = items.Select(x => x.RegionalDisplayName).ToList();

            var hasP0 = !string.IsNullOrEmpty(p0);
            if (hasP0) list.Insert(0, p0);

            var normal = new Colors(ConsoleColor.White, ConsoleColor.Blue);
            var selected = new Colors(ConsoleColor.White, ConsoleColor.Red);

            var picked = ListBoxPicker.PickIndexOf(list.ToArray(), 60, 30, normal, selected);
            if (picked < 0)
            {
                return (null, null);
            }

            if (hasP0 && picked == 0)
            {
                Console.WriteLine(p0);
                return (new AzCli.AccountRegionLocationInfo(), null);
            }

            if (hasP0) picked--;
            var regionLocation = items[picked];

            DisplayNameAndDisplayName(regionLocation);
            return (regionLocation, null);
        }

        private static (AzCli.ResourceGroupInfo? Group, string Error) ListBoxPickResourceGroup(AzCli.ResourceGroupInfo[] groups, string p0)
        {
            var list = groups.Select(x => x.Name).ToList();

            var hasP0 = !string.IsNullOrEmpty(p0);
            if (hasP0) list.Insert(0, p0);

            var normal = new Colors(ConsoleColor.White, ConsoleColor.Blue);
            var selected = new Colors(ConsoleColor.White, ConsoleColor.Red);

            var picked = ListBoxPicker.PickIndexOf(list.ToArray(), 60, 30, normal, selected);
            if (picked < 0)
            {
                return (null, null);
            }

            if (hasP0 && picked == 0)
            {
                Console.WriteLine(p0);
                return (new AzCli.ResourceGroupInfo(), null);
            }

            if (hasP0) picked--;
            Console.WriteLine(groups[picked].Name);
            return (groups[picked], null);
        }

        private static (AzCli.CognitiveServicesResourceInfo? Resource, string Error) ListBoxPickCognitiveServicesResource(AzCli.CognitiveServicesResourceInfo[] resources, string p0)
        {
            var list = resources.Select(x => $"{x.Name} ({x.RegionLocation}, {x.Kind})").ToList();

            var hasP0 = !string.IsNullOrEmpty(p0);
            if (hasP0) list.Insert(0, p0);

            var normal = new Colors(ConsoleColor.White, ConsoleColor.Blue);
            var selected = new Colors(ConsoleColor.White, ConsoleColor.Red);

            var picked = ListBoxPicker.PickIndexOf(list.ToArray(), 60, 30, normal, selected);
            if (picked < 0)
            {
                return (null, null);
            }

            if (hasP0 && picked == 0)
            {
                Console.WriteLine(p0);
                return (new AzCli.CognitiveServicesResourceInfo(), null);
            }

            if (hasP0) picked--;
            Console.WriteLine($"{resources[picked].Name}");
            return (resources[picked], null);
        }

        static bool ExactMatchRegionLocation(AzCli.AccountRegionLocationInfo regionLocation, string regionLocationFilter)
        {
            return regionLocation.Name.ToLower() == regionLocationFilter || regionLocation.DisplayName == regionLocationFilter || regionLocation.RegionalDisplayName == regionLocationFilter;
        }

        static bool ExactMatchGroup(AzCli.ResourceGroupInfo group, string groupFilter)
        {
            return group.Id == groupFilter || group.Name.ToLower() == groupFilter;
        }

        private static bool ExactMatchResource(AzCli.CognitiveServicesResourceInfo resource, string regionLocationFilter, string groupFilter, string resourceFilter)
        {
            return !string.IsNullOrEmpty(resourceFilter) && resource.Name.ToLower() == resourceFilter &&
                   (string.IsNullOrEmpty(regionLocationFilter) || resource.RegionLocation.ToLower() == regionLocationFilter) &&
                   (string.IsNullOrEmpty(groupFilter) || resource.Group.ToLower() == groupFilter);
        }

        private static bool MatchSubscriptionFilter(AzCli.SubscriptionInfo subscription, string subscriptionFilter)
        {
            if (subscriptionFilter == null || subscription.Id == subscriptionFilter)
            {
                return true;
            }

            var name = subscription.Name.ToLower();
            return name.Contains(subscriptionFilter) || StringHelpers.ContainsAllCharsInOrder(name, subscriptionFilter);
        }

        private static bool MatchRegionLocationFilter(AzCli.AccountRegionLocationInfo regionLocation, string regionLocationFilter)
        {
            if (regionLocationFilter == null || ExactMatchRegionLocation(regionLocation, regionLocationFilter))
            {
                return true;
            }

            var displayName = regionLocation.DisplayName.ToLower();
            var regionalName = regionLocation.RegionalDisplayName.ToLower();

            return displayName.Contains(regionLocationFilter) || StringHelpers.ContainsAllCharsInOrder(displayName, regionLocationFilter) ||
                   regionalName.Contains(regionLocationFilter) || StringHelpers.ContainsAllCharsInOrder(regionalName, regionLocationFilter);
        }

        private static bool MatchGroupFilter(AzCli.ResourceGroupInfo group, string groupFilter)
        {
            if (groupFilter == null || ExactMatchGroup(group, groupFilter))
            {
                return true;
            }

            var name = group.Name.ToLower();
            return name.Contains(groupFilter) || StringHelpers.ContainsAllCharsInOrder(name, groupFilter);
        }

        private static bool MatchResourceFilter(AzCli.CognitiveServicesResourceInfo resource, string regionLocationFilter, string groupFilter, string resourceFilter)
        {
            if (resourceFilter != null && ExactMatchResource(resource, regionLocationFilter, groupFilter, resourceFilter))
            {
                return true;
            }

            var name = resource.Name.ToLower();
            var group = resource.Group.ToLower();
            var regionLocation = resource.RegionLocation.ToLower();

            return (string.IsNullOrEmpty(resourceFilter) || name.Contains(resourceFilter) || StringHelpers.ContainsAllCharsInOrder(name, resourceFilter)) &&
                   (string.IsNullOrEmpty(regionLocationFilter) || name.Contains(regionLocationFilter) || StringHelpers.ContainsAllCharsInOrder(regionLocation, regionLocationFilter)) &&
                   (string.IsNullOrEmpty(groupFilter) || name.Contains(groupFilter) || StringHelpers.ContainsAllCharsInOrder(group, groupFilter));
        }

        private static void DisplaySubscriptions(AzCli.SubscriptionInfo[] subscriptions, string prefix = "")
        {
            foreach (var subscription in subscriptions)
            {
                Console.Write(prefix);
                DisplayNameAndId(subscription);
            }
        }

        private static void DisplayRegionLocations(List<AzCli.AccountRegionLocationInfo> regionLocations, string prefix)
        {
            foreach (var regionLocation in regionLocations)
            {
                Console.Write(prefix);
                DisplayNameAndDisplayName(regionLocation);
            }
        }

        private static void DisplayGroups(List<AzCli.ResourceGroupInfo> groups, string prefix)
        {
            foreach (var group in groups)
            {
                Console.Write(prefix);
                DisplayNameAndRegionLocation(group);
            }
        }

        private static void DisplayResources(List<AzCli.CognitiveServicesResourceInfo> resources, string prefix)
        {
            foreach (var resource in resources)
            {
                Console.Write(prefix);
                DisplayName(resource);
            }
        }

        private static void DisplayNameAndId(AzCli.SubscriptionInfo subscription)
        {
            Console.Write($"{subscription.Name} ({subscription.Id})");
            Console.WriteLine(new string(' ', 20));
        }

        private static void DisplayNameAndDisplayName(AzCli.AccountRegionLocationInfo regionLocation)
        {
            Console.Write($"{regionLocation.DisplayName} ({regionLocation.Name})                  ");
            Console.WriteLine(new string(' ', 20));
        }

        private static void DisplayNameAndRegionLocation(AzCli.ResourceGroupInfo group)
        {
            Console.Write($"{group.Name} ({group.RegionLocation})");
            Console.WriteLine(new string(' ', 20));
        }

        private static void DisplayName(AzCli.CognitiveServicesResourceInfo resource)
        {
            Console.Write($"{resource.Name} ({resource.RegionLocation})");
            Console.WriteLine(new string(' ', 20));
        }
    }
}
