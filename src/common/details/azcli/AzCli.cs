//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    namespace AzCli
    {
        public struct CognitiveServicesResourceInfoEx
        {
            public string Id;
            public string Group;
            public string Name;
            public string Kind;
            public string RegionLocation;
            public string Endpoint;

            public string Key;
            public string ChatDeployment;
            public string EmbeddingsDeployment;
            public string EvaluationDeployment;
        }

        public struct ResourceKeyInfo
        {
            public string Key1;
            public string Key2;
        }

        public struct CognitiveSearchResourceInfoEx
        {
            public string Id;
            public string Group;
            public string Name;
            public string RegionLocation;
            public string Endpoint;
            public string Key;
        }
    }

    public class LegacyAzCli
    {
        private static Dictionary<string, string> GetUserAgentEnv()
        {
            var dict = new Dictionary<string, string>();
            dict.Add("AZURE_HTTP_USER_AGENT", Program.TelemetryUserAgent);
            return dict;
        }

        public static async Task SetAccount(string subscriptionId)
        {
            await ProcessHelpers.RunShellCommandAsync("az", $"account set --output json --subscription {subscriptionId}", GetUserAgentEnv());
        }
    }
}
