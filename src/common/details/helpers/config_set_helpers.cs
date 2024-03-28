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

        public static void ConfigCognitiveServicesAIServicesKindResource(string subscriptionId, string region, string endpoint, AzCli.CognitiveServicesDeploymentInfo? chatDeployment, AzCli.CognitiveServicesDeploymentInfo? embeddingsDeployment, AzCli.CognitiveServicesDeploymentInfo? evaluationDeployment, string key)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CONFIG AI SERVICES`");
            Console.WriteLine();

            int maxLabelWidth = 0;
            var actions = new List<Action<int>>();

            actions.Add(ConfigSetLambda("@services.endpoint", endpoint, "Endpoint (AIServices)", endpoint, ref maxLabelWidth));
            actions.Add(ConfigSetLambda("@services.key", key, "Key (AIServices)", key.Substring(0, 4) + "****************************", ref maxLabelWidth));
            actions.Add(ConfigSetLambda("@services.region", region, "Region (AIServices)", region, ref maxLabelWidth));

            actions.Add(ConfigSetLambda("@chat.key", key, "Key (chat)", key.Substring(0, 4) + "****************************", ref maxLabelWidth));
            actions.Add(ConfigSetLambda("@chat.region", region, "Region (chat)", region, ref maxLabelWidth));
            actions.Add(ConfigSetLambda("@chat.endpoint", endpoint, "Endpoint (chat)", endpoint, ref maxLabelWidth));
            if (chatDeployment != null)
            {
                actions.Add(ConfigSetLambda("@chat.deployment", chatDeployment.Value.Name, "Deployment (chat)", chatDeployment.Value.Name, ref maxLabelWidth));
                actions.Add(ConfigSetLambda("@chat.model", chatDeployment.Value.ModelName, "Model Name (chat)", chatDeployment.Value.ModelName, ref maxLabelWidth));
            }

            actions.Add(ConfigSetLambda("@search.embedding.key", key, "Key (embedding)", key.Substring(0, 4) + "****************************", ref maxLabelWidth));
            actions.Add(ConfigSetLambda("@search.embedding.endpoint", endpoint, "Endpoint (embedding)", endpoint, ref maxLabelWidth));
            if (embeddingsDeployment != null)
            {
                actions.Add(ConfigSetLambda("@search.embedding.model.deployment.name", embeddingsDeployment.Value.Name, "Deployment (embedding)", embeddingsDeployment.Value.Name, ref maxLabelWidth));
                actions.Add(ConfigSetLambda("@search.embedding.model.name", embeddingsDeployment.Value.ModelName, "Model Name (embedding)", embeddingsDeployment.Value.ModelName, ref maxLabelWidth));
            }

            actions.Add(ConfigSetLambda("@chat.evaluation.key", key, "Key (evaluation)", key.Substring(0, 4) + "****************************", ref maxLabelWidth));
            actions.Add(ConfigSetLambda("@chat.evaluation.endpoint", endpoint, "Endpoint (evaluation)", endpoint, ref maxLabelWidth));
            if (evaluationDeployment != null)
            {
                actions.Add(ConfigSetLambda("@chat.evaluation.model.deployment.name", evaluationDeployment.Value.Name, "Deployment (evaluation)", evaluationDeployment.Value.Name, ref maxLabelWidth));
                actions.Add(ConfigSetLambda("@chat.evaluation.model.name", evaluationDeployment.Value.ModelName, "Model Name (evaluation)", evaluationDeployment.Value.ModelName, ref maxLabelWidth));
            }

            actions.Add(ConfigSetLambda("@speech.endpoint", endpoint, "Endpoint (speech)", endpoint, ref maxLabelWidth));
            actions.Add(ConfigSetLambda("@speech.key", key, "Key (speech)", key.Substring(0, 4) + "****************************", ref maxLabelWidth));
            actions.Add(ConfigSetLambda("@speech.region", region, "Region (speech)", region, ref maxLabelWidth));

            actions.ForEach(x => x?.Invoke(maxLabelWidth));
        }

        public static void ConfigCognitiveServicesCognitiveServicesKindResource(string subscriptionId, string region, string endpoint, string key)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CONFIG AI SERVICES (v1)`");
            Console.WriteLine();

            int maxLabelWidth = 0;
            var actions = new List<Action<int>>(new Action<int>[] {
                ConfigSetLambda("@services.endpoint", endpoint, "Endpoint (AIServices)", endpoint, ref maxLabelWidth),
                ConfigSetLambda("@services.key", key, "Key (AIServices)", key.Substring(0, 4) + "****************************", ref maxLabelWidth),
                ConfigSetLambda("@services.region", region, "Region (AIServices)", region, ref maxLabelWidth),

                ConfigSetLambda("@speech.endpoint", endpoint, "Endpoint (speech)", endpoint, ref maxLabelWidth),
                ConfigSetLambda("@speech.key", key, "Key (speech)", key.Substring(0, 4) + "****************************", ref maxLabelWidth),
                ConfigSetLambda("@speech.region", region, "Region (speech)", region, ref maxLabelWidth),
            });
            actions.ForEach(x => x?.Invoke(maxLabelWidth));
        }

        public static void ConfigOpenAiResource(string subscriptionId, string region, string endpoint, AzCli.CognitiveServicesDeploymentInfo? chatDeployment, AzCli.CognitiveServicesDeploymentInfo? embeddingsDeployment, AzCli.CognitiveServicesDeploymentInfo? evaluationDeployment, string key)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CONFIG AZURE OPENAI RESOURCE`");
            Console.WriteLine();

            int maxLabelWidth = 0;
            var actions = new List<Action<int>>();

            actions.Add(ConfigSetLambda("@chat.key", key, "Key (chat)", key.Substring(0, 4) + "****************************", ref maxLabelWidth));
            actions.Add(ConfigSetLambda("@chat.endpoint", endpoint, "Endpoint (chat)", endpoint, ref maxLabelWidth));
            if (chatDeployment != null)
            {
                actions.Add(ConfigSetLambda("@chat.deployment", chatDeployment.Value.Name, "Deployment (chat)", chatDeployment.Value.Name, ref maxLabelWidth));
                actions.Add(ConfigSetLambda("@chat.model", chatDeployment.Value.ModelName, "Model Name (chat)", chatDeployment.Value.ModelName, ref maxLabelWidth));
            }

            actions.Add(ConfigSetLambda("@search.embedding.key", key, "Key (embedding)", key.Substring(0, 4) + "****************************", ref maxLabelWidth));
            actions.Add(ConfigSetLambda("@search.embedding.endpoint", endpoint, "Endpoint (embedding)", endpoint, ref maxLabelWidth));
            if (embeddingsDeployment != null)
            {
                actions.Add(ConfigSetLambda("@search.embedding.model.deployment.name", embeddingsDeployment.Value.Name, "Deployment (embedding)", embeddingsDeployment.Value.Name, ref maxLabelWidth));
                actions.Add(ConfigSetLambda("@search.embedding.model.name", embeddingsDeployment.Value.ModelName, "Model Name (embedding)", embeddingsDeployment.Value.ModelName, ref maxLabelWidth));
            }

            actions.Add(ConfigSetLambda("@chat.evaluation.key", key, "Key (evaluation)", key.Substring(0, 4) + "****************************", ref maxLabelWidth));
            actions.Add(ConfigSetLambda("@chat.evaluation.endpoint", endpoint, "Endpoint (evaluation)", endpoint, ref maxLabelWidth));
            if (evaluationDeployment != null)
            {
                actions.Add(ConfigSetLambda("@chat.evaluation.model.deployment.name", evaluationDeployment.Value.Name, "Deployment (evaluation)", evaluationDeployment.Value.Name, ref maxLabelWidth));
                actions.Add(ConfigSetLambda("@chat.evaluation.model.name", evaluationDeployment.Value.ModelName, "Model Name (evaluation)", evaluationDeployment.Value.ModelName, ref maxLabelWidth));
            }

            actions.Add(ConfigSetLambda("@chat.region", region, "Region", region, ref maxLabelWidth));

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

        public static void ConfigVisionResource(string subscriptionId, string region, string endpoint, string key)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CONFIG VISION RESOURCE`");
            Console.WriteLine();

            int maxLabelWidth = 0;
            var actions = new List<Action<int>>(new Action<int>[] {
                ConfigSetLambda("@subscription", subscriptionId, "Subscription", subscriptionId, ref maxLabelWidth),
                ConfigSetLambda("@vision.endpoint", endpoint, "Endpoint (vision)", endpoint, ref maxLabelWidth),
                ConfigSetLambda("@vision.key", key, "Key (vision)", key.Substring(0, 4) + "****************************", ref maxLabelWidth),
                ConfigSetLambda("@vision.region", region, "Region (vision)", region, ref maxLabelWidth),
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
