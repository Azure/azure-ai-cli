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
            //public static async Task<AzCli.CognitiveServicesDeploymentInfo> PickOrCreateCognitiveResourceDeployment(bool interactive, string subscriptionId, string regionLocation, string group, string resourceName, string deploymentFilter)
            //{
            //    var createNewItem = !string.IsNullOrEmpty(deploymentFilter)
            //        ? $"(Create `{deploymentFilter}`)"
            //        : interactive ? "(Create new)" : null;

            //    (var deployment, var error) = await FindCognitiveServicesResourceDeployment(interactive, subscriptionId, regionLocation, group, resourceName, deploymentFilter, createNewItem);
            //    if (deployment != null && deployment.Value.Name == null)
            //    {
            //        (deployment, error) = await TryCreateCognitiveServicesResourceDeployment(interactive, subscriptionId, regionLocation, group, resourceName, deploymentFilter);
            //    }

            //    if (deployment == null && error != null)
            //    {
            //        throw new ApplicationException($"ERROR: Loading or creating resource deployment:\n{error}");
            //    }
            //    else if (deployment == null)
            //    {
            //        throw new ApplicationException($"CANCELED: No resource deployment selected");
            //    }

            //    return deployment.Value;
            //}

            //public static async Task<(AzCli.CognitiveServicesDeploymentInfo, string Error)> FindCognitiveServicesResourceDeployment(bool interactive, string subscriptionId, string group, string resourceName, string deploymentFilter, string createNewItem)
            //{
            //    ConsoleHelpers.WriteLineWithHighlight($"\n`{Program.SERVICE_RESOURCE_DISPLAY_NAME_ALL_CAPS} DEPLOYMENTS`");

            //    Console.Write("Deployment: *** Loading ***");
            //    var response = await AzCli.ListCognitiveServicesDeployments(subscriptionId, group, resourceName, "OpenAI");

            //    Console.WriteLine($"\rDeployment: ");
            //    if (string.IsNullOrEmpty(response.StdOutput) && !string.IsNullOrEmpty(response.StdError))
            //    {
            //        ConsoleHelpers.WriteLineError($"ERROR: Loading Cognitive Services resources: {response.StdError}");
            //        return (null, response.StdError);
            //    }

            //    var deployments = response.Payload
            //        .Where(x => MatchDeploymentFilter(x, deploymentFilter))
            //        .OrderBy(x => x.Name)
            //        .ToList();


            //    //if (deployments.Count == 0)
            //    //{
            //    //    Console.WriteLine($"No deployments found for resource `{resourceName}`");
            //    return (null, null);
            //    //}

            //    //var deployment = deployments.FirstOrDefault(x => x.Value.Name == deploymentFilter);
            //    //if (deployment != null)
            //    //{
            //    //    Console.WriteLine($"Found deployment `{deploymentFilter}`");
            //    //    return (deployment, null);
            //    //}

            //    //if (!interactive)
            //    //{
            //    //    Console.WriteLine($"No deployment found for resource `{resourceName}`");
            //    //    return (null, null);
            //    //}

            //    //var deploymentNames = deployments.Select(x => x.Value.Name).ToList();
            //    //deploymentNames.Sort();

            //    //var deploymentName = ConsoleHelpers.PickItemFromList("Pick a deployment", deploymentNames, createNewItem);
            //    //if (deploymentName == createNewItem)
            //    //{
            //    //    return (new AzCli.CognitiveServicesDeploymentInfo(), null);
            //    //}

            //    //deployment = deployments.FirstOrDefault(x => x.Value.Name == deploymentName);
            //    //if (deployment == null)
            //    //{
            //    //    throw new ApplicationException($"ERROR: Deployment `{deploymentName}` not found");
            //    //}

            //    //return (deployment, null);
            //}

            //private static bool MatchDeploymentFilter(AzCli.CognitiveServicesDeploymentInfo x, string deploymentFilter)
            //{
            //    throw new NotImplementedException();
            //}
        }
    }
}
