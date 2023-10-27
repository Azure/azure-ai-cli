//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Text;
using System.Collections.Generic;

namespace Azure.AI.Details.Common.CLI
{
    public class ConfigSetHelpers
    {
        public static void ConfigureProject(string subscriptionId, string groupName, string projectName)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`AZURE AI PROJECT CONFIG`");
            Console.WriteLine();

            int maxLabelWidth = 0;
            var actions = new List<Action<int>>(new Action<int>[] {
                ConfigSetLambda("@subscription", subscriptionId, "Subscription", subscriptionId, ref maxLabelWidth),
                ConfigSetLambda("@group", groupName, "Group", groupName, ref maxLabelWidth),
                ConfigSetLambda("@project", projectName, "Project", projectName, ref maxLabelWidth)
            });
            actions.ForEach(x => x?.Invoke(maxLabelWidth));
        }

        public static void ConfigOpenAiResource(string subscriptionId, string region, string endpoint, AzCli.CognitiveServicesDeploymentInfo chatDeployment, AzCli.CognitiveServicesDeploymentInfo embeddingsDeployment, AzCli.CognitiveServicesDeploymentInfo evaluationDeployment, string key)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CONFIG OPEN AI RESOURCE`");
            Console.WriteLine();

            int maxLabelWidth = 0;
            var actions = new List<Action<int>>(new Action<int>[] {
                ConfigSetLambda("@subscription", subscriptionId, "Subscription", subscriptionId, ref maxLabelWidth),

                ConfigSetLambda("@chat.key", key, "Key (chat)", key.Substring(0, 4) + "****************************", ref maxLabelWidth),
                ConfigSetLambda("@chat.endpoint", endpoint, "Endpoint (chat)", endpoint, ref maxLabelWidth),
                ConfigSetLambda("@chat.deployment", chatDeployment.Name, "Deployment (chat)", chatDeployment.Name, ref maxLabelWidth),
                ConfigSetLambda("@chat.model", chatDeployment.ModelName, "Model Name (chat)", chatDeployment.ModelName, ref maxLabelWidth),

                ConfigSetLambda("@search.embedding.key", key, "Key (embedding)", key.Substring(0, 4) + "****************************", ref maxLabelWidth),
                ConfigSetLambda("@search.embedding.endpoint", endpoint, "Endpoint (embedding)", endpoint, ref maxLabelWidth),
                ConfigSetLambda("@search.embedding.model.deployment.name", embeddingsDeployment.Name, "Deployment (embedding)", embeddingsDeployment.Name, ref maxLabelWidth),
                ConfigSetLambda("@search.embedding.model.name", embeddingsDeployment.ModelName, "Model Name (embedding)", embeddingsDeployment.ModelName, ref maxLabelWidth),

                ConfigSetLambda("@chat.evaluation.key", key, "Key (evaluation)", key.Substring(0, 4) + "****************************", ref maxLabelWidth),
                ConfigSetLambda("@chat.evaluation.endpoint", endpoint, "Endpoint (evaluation)", endpoint, ref maxLabelWidth),
                ConfigSetLambda("@chat.evaluation.model.deployment.name", evaluationDeployment.Name, "Deployment (evaluation)", evaluationDeployment.Name, ref maxLabelWidth),
                ConfigSetLambda("@chat.evaluation.model.name", evaluationDeployment.ModelName, "Model Name (evaluation)", evaluationDeployment.ModelName, ref maxLabelWidth),

                ConfigSetLambda("@chat.region", region, "Region", region, ref maxLabelWidth),
            });
            actions.ForEach(x => x?.Invoke(maxLabelWidth));
        }

        public static void ConfigSpeechResource(string subscriptionId, string region, string endpoint, string key)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CONFIG SPEECH RESOURCE`");
            Console.WriteLine();

            int maxLabelWidth = 0;
            var actions = new List<Action<int>>(new Action<int>[] {
                ConfigSetLambda("@subscription", subscriptionId, "Subscription", subscriptionId, ref maxLabelWidth),
                ConfigSetLambda("@speech.endpoint", endpoint, "Endpoint (speech)", endpoint, ref maxLabelWidth),
                ConfigSetLambda("@speech.key", key, "Key (speech)", key.Substring(0, 4) + "****************************", ref maxLabelWidth),
                ConfigSetLambda("@speech.region", region, "Region (speech)", region, ref maxLabelWidth),
            });
            actions.ForEach(x => x?.Invoke(maxLabelWidth));
        }

        public static void ConfigSearchResource(string endpoint, string key)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CONFIG AI SEARCH RESOURCE`");
            Console.WriteLine();

            int maxLabelWidth = 0;
            var actions = new List<Action<int>>(new Action<int>[]
            {
                ConfigSetLambda("@search.endpoint", endpoint, "Endpoint (search)", endpoint, ref maxLabelWidth),
                ConfigSetLambda("@search.key", key, "Key (search)", key.Substring(0, 4) + "****************************", ref maxLabelWidth),
            });
            actions.ForEach(x => x(maxLabelWidth));
        }

        private static Action<int> ConfigSetLambda(string atFile, string setValue, string displayLabel, string displayValue, ref int maxWidth)
        {
            maxWidth = Math.Max(maxWidth, displayLabel.Length);
            return (int labelWidth) =>
            {
                ConfigSet(atFile, setValue, labelWidth, displayLabel, displayValue);
            };
        }

        private static void ConfigSet(string atFile, string setValue, int labelWidth, string displayLabel, string displayValue)
        {
            displayLabel = displayLabel.PadLeft(labelWidth);
            Console.Write($"*** SETTING *** {displayLabel}");
            ConfigSet(atFile, setValue);
            // Thread.Sleep(50);
            Console.WriteLine($"\r  *** SET ***   {displayLabel}: {displayValue}");
        }

        public static string ConfigSet(string atFile, string setValue)
        {
            var setCommandValues = new CommandValues();
            setCommandValues.Add("x.command", "config");
            setCommandValues.Add("x.config.scope.hive", "local");
            setCommandValues.Add("x.config.command.at.file", atFile);
            setCommandValues.Add("x.config.command.set", setValue);
            var fileName = FileHelpers.GetOutputConfigFileName(atFile, setCommandValues);
            FileHelpers.WriteAllText(fileName, setValue, Encoding.UTF8);
            return fileName;
        }
    }
}
