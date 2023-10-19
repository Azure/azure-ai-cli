//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Azure.AI.Details.Common.CLI.ConsoleGui;

namespace Azure.AI.Details.Common.CLI
{
    public partial class AzCliConsoleGui
    {
        public class AiResourceDeploymentPicker
        {
            public static async Task<AzCli.CognitiveServicesDeploymentInfo> PickOrCreateDeployment(bool interactive, string deploymentExtra, string subscriptionId, string groupName, string resourceRegionLocation, string resourceName, string deploymentFilter)
            {
               ConsoleHelpers.WriteLineWithHighlight($"\n`OPEN AI DEPLOYMENT ({deploymentExtra.ToUpper()})`");

               var createNewItem = !string.IsNullOrEmpty(deploymentFilter)
                   ? $"(Create `{deploymentFilter}`)"
                   : interactive ? "(Create new)" : null;

               var deployment = await FindDeployment(interactive, deploymentExtra, subscriptionId, groupName, resourceName, deploymentFilter, createNewItem);
               if (deployment != null && deployment.Value.Name == null)
               {
                   deployment = await TryCreateDeployment(interactive, deploymentExtra, subscriptionId, groupName, resourceRegionLocation, resourceName, deploymentFilter);
               }

               if (deployment == null)
               {
                   throw new ApplicationException($"CANCELED: No deployment selected");
               }

               return deployment.Value;
            }

            public static async Task<AzCli.CognitiveServicesDeploymentInfo?> FindDeployment(bool interactive, string deploymentExtra, string subscriptionId, string groupName, string resourceName, string deploymentFilter, string allowCreateDeploymentOption)
            {
                var allowCreateDeployment = !string.IsNullOrEmpty(allowCreateDeploymentOption);

                Console.Write($"Name: *** Loading choices ***");
                var response = await AzCli.ListCognitiveServicesDeployments(subscriptionId, groupName, resourceName, "OpenAI");

                Console.Write($"\rName: ");
                if (string.IsNullOrEmpty(response.Output.StdOutput) && !string.IsNullOrEmpty(response.Output.StdError))
                {
                    throw new ApplicationException($"ERROR: Loading deployments:\n{response.Output.StdError}");
                }

                var deployments = response.Payload
                    .Where(x => MatchDeploymentFilter(x, deploymentFilter))
                    .OrderBy(x => x.Name)
                    .ToList();

                var exactMatch = deploymentFilter != null && deployments.Count(x => ExactMatchDeployment(x, deploymentFilter)) == 1;
                if (exactMatch) deployments = deployments.Where(x => ExactMatchDeployment(x, deploymentFilter)).ToList();

                if (deployments.Count() == 0)
                {
                    if (!allowCreateDeployment)
                    {
                        ConsoleHelpers.WriteLineError($"*** No deployments found ***");
                        return null;
                    }
                    else if (!interactive)
                    {
                        Console.WriteLine(allowCreateDeploymentOption);
                        return new AzCli.CognitiveServicesDeploymentInfo();
                    }
                }
                else if (deployments.Count() == 1 && (!interactive || exactMatch))
                {
                    var deployment = deployments.First();
                    DisplayName(deployment);
                    return deployment;
                }
                else if (!interactive)
                {
                    ConsoleHelpers.WriteLineError("*** More than 1 deployment found ***");
                    Console.WriteLine();
                    DisplayDeployments(deployments, "  ");
                    return null;
                }

                var scanFor = deploymentExtra.ToLower() switch {
                    "chat" => "gpt",
                    "embeddings" => "embedding",
                    "evaluation" => "gpt",
                    _ => deploymentExtra.ToLower()
                };

                var choices = deployments.ToArray();
                var select = Array.FindIndex(choices, x => x.Name.Contains(scanFor));

                if (allowCreateDeployment && select >= 0) select++;
                select = Math.Max(0, select);

                return interactive
                    ? ListBoxPickDeployment(choices, allowCreateDeploymentOption, select)
                    : null;
            }

            public static async Task<AzCli.CognitiveServicesDeploymentInfo?> TryCreateDeployment(bool interactive, string deploymentExtra, string subscriptionId, string groupName, string resourceRegionLocation, string resourceName, string deploymentName)
            {
                ConsoleHelpers.WriteLineWithHighlight($"\n`CREATE DEPLOYMENT ({deploymentExtra.ToUpper()})`");

                Console.Write("\rModel: *** Loading choices ***");
                var models = await AzCli.ListCognitiveServicesModels(subscriptionId, resourceRegionLocation);

                Console.Write("\rModel: ");
                var choices = models.Payload.Select(x => x.Name + " (version " + x.Version + ")").ToArray();

                var scanFor = deploymentExtra.ToLower() switch {
                    "chat" => "gpt",
                    "embeddings" => "embedding",
                    _ => deploymentExtra.ToLower()
                };
                var select = Math.Max(0, Array.FindLastIndex(choices, x => x.Contains(scanFor)));

                var index = ListBoxPicker.PickIndexOf(choices, select);
                if (index < 0) return null;

                var modelName = models.Payload[index].Name;
                Console.WriteLine($"\rModel: {modelName}");

                var modelFormat = "OpenAI";
                var modelVersion = models.Payload[index].Version;
                var scaleCapacity = models.Payload[index].DefaultCapacity;

                // HACK: There's a bug in the RP or region specific or something that sometimes 120 doesn't work, and we need to use 119
                if (scaleCapacity == "120") scaleCapacity = "119";

                Console.Write("\rName: ");
                choices = new string[] {
                    $"{modelName}-{modelVersion}",
                    "(Enter custom name)"
                };
                var pick = ListBoxPicker.PickIndexOf(choices);

                deploymentName = pick switch{
                    0 => $"{modelName}-{modelVersion}",
                    1 => AskPromptHelper.AskPrompt("\rName: "),
                    _ => null
                };

                if (string.IsNullOrEmpty(deploymentName)) return null;
                if (pick != choices.Length - 1) Console.WriteLine($"\rName: {deploymentName}");

                Console.Write("*** CREATING ***");
                var response = await AzCli.CreateCognitiveServicesDeployment(subscriptionId, groupName, resourceName, deploymentName, modelName, modelVersion, modelFormat, scaleCapacity);

                Console.Write("\r");
                if (string.IsNullOrEmpty(response.Output.StdOutput) && !string.IsNullOrEmpty(response.Output.StdError))
                {
                    throw new ApplicationException($"ERROR: Creating deployment: {response.Output.StdError}");
                }

                Console.WriteLine("\r*** CREATED ***  ");
                return response.Payload;
            }

            private static AzCli.CognitiveServicesDeploymentInfo? ListBoxPickDeployment(AzCli.CognitiveServicesDeploymentInfo[] deployments, string p0, int select = 0)
            {
                var list = deployments.Select(x => $"{x.Name} ({x.ModelName})").ToList();

                var hasP0 = !string.IsNullOrEmpty(p0);
                if (hasP0) list.Insert(0, p0);

                var picked = ListBoxPicker.PickIndexOf(list.ToArray(), select);
                if (picked < 0)
                {
                    return null;
                }

                if (hasP0 && picked == 0)
                {
                    Console.WriteLine(p0);
                    return new AzCli.CognitiveServicesDeploymentInfo();
                }

                if (hasP0) picked--;
                Console.WriteLine($"{deployments[picked].Name}");
                return deployments[picked];
            }

            private static bool ExactMatchDeployment(AzCli.CognitiveServicesDeploymentInfo deployment, string deploymentFilter)
            {
                return !string.IsNullOrEmpty(deploymentFilter) && deployment.Name.ToLower() == deploymentFilter;
            }

            private static bool MatchDeploymentFilter(AzCli.CognitiveServicesDeploymentInfo deployment, string deploymentFilter)
            {
                if (deploymentFilter != null && ExactMatchDeployment(deployment, deploymentFilter))
                {
                    return true;
                }

                var name = deployment.Name.ToLower();
                return (string.IsNullOrEmpty(deploymentFilter) || name.Contains(deploymentFilter) || StringHelpers.ContainsAllCharsInOrder(name, deploymentFilter));
            }

            private static void DisplayDeployments(List<AzCli.CognitiveServicesDeploymentInfo> deployments, string prefix)
            {
                foreach (var deployment in deployments)
                {
                    Console.Write(prefix);
                    DisplayName(deployment);
                }
            }

            private static void DisplayName(AzCli.CognitiveServicesDeploymentInfo deployment)
            {
                Console.Write($"{deployment.Name}");
                Console.WriteLine(new string(' ', 20));
            }
        }
    }
}
