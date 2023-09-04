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
        public static void ConfigServiceResource(string subscriptionId, string region, string endpoint, string chatDeployment, string embeddingsDeployment, string key)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CONFIG {Program.SERVICE_RESOURCE_DISPLAY_NAME_ALL_CAPS}`");
            Console.WriteLine();

            int maxLabelWidth = 0;
            var actions = new List<Action<int>>(new Action<int>[] {
                Program.InitConfigsSubscription ?
                ConfigSetLambda("@subscription", subscriptionId, "Subscription", subscriptionId, ref maxLabelWidth) : null,
                Program.InitConfigsEndpoint ?
                ConfigSetLambda("@chat.endpoint", endpoint, "Endpoint (chat)", endpoint, ref maxLabelWidth) : null,
                ConfigSetLambda("@chat.deployment", chatDeployment, "Deployment (chat)", chatDeployment, ref maxLabelWidth),
                ConfigSetLambda("@chat.key", key, "Key (chat)", key.Substring(0, 4) + "****************************", ref maxLabelWidth),
                Program.InitConfigsEndpoint ?
                ConfigSetLambda("@search.embeddings.endpoint", endpoint, "Endpoint (embeddings)", endpoint, ref maxLabelWidth) : null,
                ConfigSetLambda("@search.embeddings.deployment", embeddingsDeployment, "Deployment (embeddings)", embeddingsDeployment, ref maxLabelWidth),
                ConfigSetLambda("@search.embeddings.key", key, "Key (embeddings)", key.Substring(0, 4) + "****************************", ref maxLabelWidth),
                ConfigSetLambda("@chat.region", region, "Region", region, ref maxLabelWidth),
            });
            actions.ForEach(x => x?.Invoke(maxLabelWidth));
        }

        public static void ConfigSearchResource(string endpoint, string key)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CONFIG COGNITIVE SEARCH RESOURCE`");
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
    }
}
