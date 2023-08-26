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
            public static async Task<AzCli.CognitiveServicesDeploymentInfo> PickOrCreateDeployment(bool interactive, string deploymentExtra, string subscriptionId, AzCli.CognitiveServicesResourceInfo resource, string deploymentFilter)
            {
               var createNewItem = !string.IsNullOrEmpty(deploymentFilter)
                   ? $"(Create `{deploymentFilter}`)"
                   : interactive ? "(Create new)" : null;

               (var deployment, var error) = await FindDeployment(interactive, deploymentExtra, subscriptionId, resource, deploymentFilter, createNewItem);
               if (deployment != null && deployment.Value.Name == null)
               {
                   (deployment, error) = await TryCreateDeployment(interactive, deploymentExtra, subscriptionId, resource, deploymentFilter);
               }

               if (deployment == null && error != null)
               {
                   throw new ApplicationException($"ERROR: Loading or creating deployment:\n{error}");
               }
               else if (deployment == null)
               {
                   throw new ApplicationException($"CANCELED: No deployment selected");
               }

               return deployment.Value;
            }

            public static async Task<(AzCli.CognitiveServicesDeploymentInfo? Deployment, string Error)> FindDeployment(bool interactive, string deploymentExtra, string subscriptionId, AzCli.CognitiveServicesResourceInfo resource, string deploymentFilter, string allowCreateDeploymentOption)
            {
                var allowCreateDeployment = !string.IsNullOrEmpty(allowCreateDeploymentOption);

                Console.Write($"Deployment ({deploymentExtra}): *** Loading choices ***");
                var response = await AzCli.ListCognitiveServicesDeployments(subscriptionId, resource.Group, resource.Name, "OpenAI");

                Console.Write($"\rDeployment ({deploymentExtra}): ");
                if (string.IsNullOrEmpty(response.StdOutput) && !string.IsNullOrEmpty(response.StdError))
                {
                    ConsoleHelpers.WriteLineError($"ERROR: Loading Cognitive Services resources: {response.StdError}");
                    return (null, response.StdError);
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
                        return (null, null);
                    }
                    else if (!interactive)
                    {
                        Console.WriteLine(allowCreateDeploymentOption);
                        return (new AzCli.CognitiveServicesDeploymentInfo(), null);
                    }
                }
                else if (deployments.Count() == 1 && (!interactive || exactMatch))
                {
                    var deployment = deployments.First();
                    DisplayName(deployment);
                    return (deployment, null);
                }
                else if (!interactive)
                {
                    ConsoleHelpers.WriteLineError("*** More than 1 deployment found ***");
                    Console.WriteLine();
                    DisplayDeployments(deployments, "  ");
                    return (null, null);
                }

                var scanFor = deploymentExtra.ToLower() switch {
                    "chat" => "gpt",
                    "embeddings" => "embedding",
                    _ => deploymentExtra.ToLower()
                };

                var choices = deployments.ToArray();
                var select = Math.Max(0, Array.FindIndex(choices, x => x.Name.Contains(scanFor)));
                if (allowCreateDeployment) select++;

                return interactive
                    ? ListBoxPickDeployment(choices, allowCreateDeploymentOption, select)
                    : (null, null);
            }

            public static async Task<(AzCli.CognitiveServicesDeploymentInfo? Deployment, string Error)> TryCreateDeployment(bool interactive, string deploymentExtra, string subscriptionId, AzCli.CognitiveServicesResourceInfo resource, string deploymentName)
            {
                ConsoleHelpers.WriteLineWithHighlight($"\n`CREATE DEPLOYMENT ({deploymentExtra.ToUpper()})`");

                var normal = new Colors(ConsoleColor.White, ConsoleColor.Blue);
                var selected = new Colors(ConsoleColor.White, ConsoleColor.Red);
                
                Console.Write("\rModel: *** Loading choices ***");
                var models = await AzCli.ListCognitiveServicesModels(subscriptionId, resource.RegionLocation);

                Console.Write("\rModel: ");
                var choices = models.Payload.Select(x => x.Name + " (version " + x.Version + ")").ToArray();

                var scanFor = deploymentExtra.ToLower() switch {
                    "chat" => "gpt",
                    "embeddings" => "embedding",
                    _ => deploymentExtra.ToLower()
                };
                var select = Math.Max(0, Array.FindLastIndex(choices, x => x.Contains(scanFor)));

                var index = ListBoxPicker.PickIndexOf(choices, 60, 30, normal, selected, select);
                if (index < 0) return (null, null);

                var modelName = models.Payload[index].Name;
                Console.WriteLine($"\rModel: {modelName}");

                var modelFormat = "OpenAI";
                var modelVersion = models.Payload[index].Version;
                var scaleCapacity = models.Payload[index].DefaultCapacity;

                Console.Write("\rName: ");
                choices = new string[] {
                    $"{modelName}-{modelVersion}",
                    "(Enter custom name)"
                };
                var width = Math.Max(choices.Max(x => x.Length) + 5, 30);
                var pick = ListBoxPicker.PickIndexOf(choices, width, 30, normal, selected);

                deploymentName = pick switch{
                    0 => $"{modelName}-{modelVersion}",
                    1 => AskPrompt("\rName: "),
                    _ => null
                };

                if (string.IsNullOrEmpty(deploymentName)) return (null, null);
                if (pick != choices.Length - 1) Console.WriteLine($"\rName: {deploymentName}");

                Console.Write("*** CREATING ***");
                var response = await AzCli.CreateCognitiveServicesDeployment(subscriptionId, resource.Group, resource.Name, deploymentName, modelName, modelVersion, modelFormat, scaleCapacity);

                Console.Write("\r");
                if (string.IsNullOrEmpty(response.StdOutput) && !string.IsNullOrEmpty(response.StdError))
                {
                    ConsoleHelpers.WriteLineError($"ERROR: Creating deployment: {response.StdError}");
                    return (null, response.StdError);
                }

                Console.WriteLine("\r*** CREATED ***  ");
                return (response.Payload, null);
            }

            private static (AzCli.CognitiveServicesDeploymentInfo? Deployment, string Error) ListBoxPickDeployment(AzCli.CognitiveServicesDeploymentInfo[] deployments, string p0, int select = 0)
            {
                var list = deployments.Select(x => $"{x.Name} ({x.ModelFormat})").ToList();

                var hasP0 = !string.IsNullOrEmpty(p0);
                if (hasP0) list.Insert(0, p0);

                var normal = new Colors(ConsoleColor.White, ConsoleColor.Blue);
                var selected = new Colors(ConsoleColor.White, ConsoleColor.Red);

                var picked = ListBoxPicker.PickIndexOf(list.ToArray(), 60, 30, normal, selected, select);
                if (picked < 0)
                {
                    return (null, null);
                }

                if (hasP0 && picked == 0)
                {
                    Console.WriteLine(p0);
                    return (new AzCli.CognitiveServicesDeploymentInfo(), null);
                }

                if (hasP0) picked--;
                Console.WriteLine($"{deployments[picked].Name}");
                return (deployments[picked], null);
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
