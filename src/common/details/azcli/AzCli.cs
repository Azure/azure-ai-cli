//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Azure.AI.Details.Common.CLI;
using System.Collections.Generic;

namespace Azure.AI.Details.Common.CLI
{
    public class AzCli
    {
        public struct SubscriptionInfo
        {
            public string Id;
            public string Name;
            public string UserName;
            public bool IsDefault;
        }

        public struct AccountRegionLocationInfo
        {
            public string Name;
            public string DisplayName;
            public string RegionalDisplayName;
        }

        public struct ResourceGroupInfo
        {
            public string Id;
            public string Name;
            public string RegionLocation;
        }

        public struct CognitiveServicesResourceInfo
        {
            public string Id;
            public string Group;
            public string Name;
            public string Kind;
            public string RegionLocation;
            public string Endpoint;
        }

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

        public struct CognitiveServicesSpeechResourceInfo
        {
            public string Id;
            public string Group;
            public string Name;
            public string Kind;
            public string RegionLocation;
            public string Endpoint;

            public string Key;
        }


        public struct CognitiveServicesKeyInfo
        {
            public string Key1;
            public string Key2;
        }

        public struct CognitiveServicesDeploymentInfo
        {
            public string Name { get; set; }
            public string ModelFormat { get; set; }
            public string ModelName { get; set; }
            public bool ChatCompletionCapable { get; set; }
            public bool EmbeddingsCapable { get; set; }
        }

        public struct CognitiveServicesModelInfo
        {
            public string Name { get; set; }
            public string Format { get; set; }
            public string Version { get; set; }
            public string DefaultCapacity { get; set; }
            public bool ChatCompletionCapable { get; set; }
            public bool EmbeddingsCapable { get; set; }
        }

        public struct CognitiveServicesUsageInfo
        {
            public string Name { get; set; }
            public string Current { get; set; }
            public string Limit { get; set; }
        }

        public struct CognitiveSearchResourceInfo
        {
            public string Id;
            public string Group;
            public string Name;
            public string RegionLocation;
            public string Endpoint;
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

        public struct CognitiveSearchKeyInfo
        {
            public string Key1;
            public string Key2;
        }

        private static Dictionary<string, string> GetUserAgentEnv()
        {
            var dict = new Dictionary<string, string>();
            dict.Add("AZURE_HTTP_USER_AGENT", Program.TelemetryUserAgent);
            return dict;
        }
        
        public static async Task<ParsedJsonProcessOutput<SubscriptionInfo[]>> Login(bool useDeviceCode = false)
        {
            var showDeviceCodeMessage = (string message) => {
                if (message.Contains("device") && message.Contains("code"))
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(message);
                    Console.WriteLine();
                    Console.ResetColor();
                }
            };

            var stdErrHandler = useDeviceCode ? showDeviceCodeMessage : null;
            var deviceCodePart = useDeviceCode ? "--use-device-code" : "";
            var queryPart = $"--query \"[].{{Name:name,Id:id,IsDefault:isDefault,UserName:user.name}}\"";

            var parsed = await ProcessHelpers.ParseShellCommandJson<JArray>("az", $"login --output json {queryPart} {deviceCodePart}", GetUserAgentEnv(), null, stdErrHandler);
            var accounts = parsed.Payload;

            var x = new ParsedJsonProcessOutput<SubscriptionInfo[]>(parsed.Output);
            x.Payload = new SubscriptionInfo[accounts.Count];

            var i = 0;
            foreach (var account in accounts)
            {
                x.Payload[i].Id = account["Id"].Value<string>();
                x.Payload[i].Name = account["Name"].Value<string>();
                x.Payload[i].IsDefault = account["IsDefault"].Value<bool>();
                x.Payload[i].UserName = account["UserName"].Value<string>();
                i++;
            }

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<SubscriptionInfo[]>> ListAccounts()
        {
            var parsed = await ProcessHelpers.ParseShellCommandJson<JArray>("az", "account list --output json --query \"[].{Name:name,Id:id,IsDefault:isDefault,UserName:user.name}\"", GetUserAgentEnv());
            var accounts = parsed.Payload;

            var x = new ParsedJsonProcessOutput<SubscriptionInfo[]>(parsed.Output);
            x.Payload = new SubscriptionInfo[accounts.Count];

            var i = 0;
            foreach (var account in accounts)
            {
                x.Payload[i].Id = account["Id"].Value<string>();
                x.Payload[i].Name = account["Name"].Value<string>();
                x.Payload[i].IsDefault = account["IsDefault"].Value<bool>();
                x.Payload[i].UserName = account["UserName"].Value<string>();
                i++;
            }

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<string>> SetAccount(string subscriptionId)
        {
            var parsed = await ProcessHelpers.ParseShellCommandJson<JObject>("az", $"account set --output json --subscription {subscriptionId}", GetUserAgentEnv());

            var x = new ParsedJsonProcessOutput<string>(parsed.Output);
            x.Payload = subscriptionId;

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<AccountRegionLocationInfo[]>> ListAccountRegionLocations()
        {
            var supportedRegions = await ListSupportedResourceRegions();

            var parsed = await ProcessHelpers.ParseShellCommandJson<JArray>("az", "account list-locations --output json --query \"[].{Name:name,RegionalDisplayName:regionalDisplayName,DisplayName:displayName}\"", GetUserAgentEnv());
            var regionLocations = parsed.Payload;

            var list = new List<AccountRegionLocationInfo>();
            foreach (var regionLocation in regionLocations)
            {
                if (supportedRegions.Count == 0 || supportedRegions.Contains(regionLocation["Name"].Value<string>().ToLower()))
                {
                    list.Add(new AccountRegionLocationInfo()
                    {
                        Name = regionLocation["Name"].Value<string>(),
                        DisplayName = regionLocation["DisplayName"].Value<string>(),
                        RegionalDisplayName = regionLocation["RegionalDisplayName"].Value<string>()
                    });
                }
            }

            var x = new ParsedJsonProcessOutput<AccountRegionLocationInfo[]>(parsed.Output);
            x.Payload = list.ToArray();

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<ResourceGroupInfo[]>> ListResourceGroups(string subscriptionId = null, string regionLocation = null)
        {
            var cmdPart = "group list";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";
            var queryPart1 = $"--query \"[";
            var queryPart2 = regionLocation != null ? $"? location=='{regionLocation}'" : "";
            var queryPart3 = $"].{{Id:id,Name:name,Location:location}}\"";

            var parsed = await ProcessHelpers.ParseShellCommandJson<JArray>("az", $"{cmdPart} --output json {subPart} {queryPart1}{queryPart2}{queryPart3}", GetUserAgentEnv());
            var groups = parsed.Payload;

            var x = new ParsedJsonProcessOutput<ResourceGroupInfo[]>(parsed.Output);
            x.Payload = new ResourceGroupInfo[groups.Count];

            var i = 0;
            foreach (var resource in groups)
            {
                x.Payload[i].Id = resource["Id"].Value<string>();
                x.Payload[i].Name = resource["Name"].Value<string>();
                x.Payload[i].RegionLocation = resource["Location"].Value<string>();
                i++;
            }

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<CognitiveServicesResourceInfo[]>> ListCognitiveServicesResources(string subscriptionId = null, string kinds = null)
        {
            var cmdPart = "cognitiveservices account list";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";
            var groupPart = "--group \"\"";
            
            var lookForKind = kinds.Split(';').First();
            var condPart= lookForKind switch
            {
                "OpenAI" => "? kind == 'OpenAI' || kind == 'AIServices'",
                "ComputerVision" => "? kind == 'ComputerVision' || kind == 'CognitiveServices' || kind == 'AIServices'",
                "SpeechServices" => "? kind == 'SpeechServices' || kind == 'CognitiveServices' || kind == 'AIServices'",

                "AIServices" => $"? kind == '{lookForKind}'",
                "CognitiveServices" => $"? kind == '{lookForKind}'",

                _ => kinds != null ? $"? kind == '{lookForKind}' || kind == 'CognitiveServices' || kind == 'AIServices'" : null
            };

            var parsed = await ProcessHelpers.ParseShellCommandJson<JArray>("az", $"{cmdPart} --output json {subPart} {groupPart} --query \"[{condPart}].{{Id:id,Name:name,Location: location,Kind:kind,Group:resourceGroup,Endpoint:properties.endpoint}}\"", GetUserAgentEnv());
            var resources = parsed.Payload;

            var x = new ParsedJsonProcessOutput<CognitiveServicesResourceInfo[]>(parsed.Output);
            x.Payload = new CognitiveServicesResourceInfo[resources.Count];

            var i = 0;
            foreach (var resource in resources)
            {
                x.Payload[i].Id = resource["Id"]?.Value<string>();
                x.Payload[i].Group = resource["Group"].Value<string>();
                x.Payload[i].Name = resource["Name"].Value<string>();
                x.Payload[i].Kind = resource["Kind"].Value<string>();
                x.Payload[i].RegionLocation = resource["Location"].Value<string>();
                x.Payload[i].Endpoint = resource["Endpoint"].Value<string>();
                i++;
            }

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<CognitiveServicesDeploymentInfo[]>> ListCognitiveServicesDeployments(string subscriptionId = null, string group = null, string resourceName = null, string modelFormat = null)
        {
            var cmdPart = "cognitiveservices account deployment list";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";

            var parsed = await ProcessHelpers.ParseShellCommandJson<JArray>("az", $"{cmdPart} --output json {subPart}  -g {group} -n {resourceName} --query \"[].{{Name:name,Location: location,Group:resourceGroup,Endpoint:properties.endpoint,Model:properties.model.name,Format:properties.model.format,ChatCompletionCapable:properties.capabilities.chatCompletion,EmbeddingsCapable:properties.capabilities.embeddings}}\"", GetUserAgentEnv());
            var deployments = parsed.Payload;

            var x = new ParsedJsonProcessOutput<CognitiveServicesDeploymentInfo[]>(parsed.Output);
            x.Payload = new CognitiveServicesDeploymentInfo[deployments.Count];

            var i = 0;
            foreach (var deployment in deployments)
            {
                x.Payload[i].Name = deployment["Name"].Value<string>();
                x.Payload[i].ModelFormat = deployment["Format"].Value<string>();
                x.Payload[i].ModelName = deployment["Model"].Value<string>();
                x.Payload[i].ChatCompletionCapable = deployment["ChatCompletionCapable"].Value<string>() == "true";
                x.Payload[i].EmbeddingsCapable = deployment["EmbeddingsCapable"].Value<string>() == "true";
                i++;
            }

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<CognitiveServicesModelInfo[]>> ListCognitiveServicesModels(string subscriptionId = null, string regionLocation = null)
        {
            var cmdPart = "cognitiveservices model list";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";

            var parsed = await ProcessHelpers.ParseShellCommandJson<JArray>("az", $"{cmdPart} --output json {subPart} -l {regionLocation} --query \"[].{{Name:model.name,Format:model.format,Version:model.version,DefaultCapacity:model.skus[0].capacity.default,ChatCompletionCapable:model.capabilities.chatCompletion,EmbeddingsCapable:model.capabilities.embeddings}}\"", GetUserAgentEnv());
            var models = parsed.Payload;

            var x = new ParsedJsonProcessOutput<CognitiveServicesModelInfo[]>(parsed.Output);
            x.Payload = new CognitiveServicesModelInfo[models.Count];

            var i = 0;
            foreach (var model in models)
            {
                x.Payload[i].Name = model["Name"].Value<string>();
                x.Payload[i].Format = model["Format"].Value<string>();
                x.Payload[i].Version = model["Version"].Value<string>();
                x.Payload[i].DefaultCapacity = model["DefaultCapacity"].Value<string>();
                x.Payload[i].ChatCompletionCapable = model["ChatCompletionCapable"].Value<string>() == "true";
                x.Payload[i].EmbeddingsCapable = model["EmbeddingsCapable"].Value<string>() == "true";
                i++;
            }

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<CognitiveServicesUsageInfo[]>> ListCognitiveServicesUsage(string subscriptionId = null, string regionLocation = null)
        {
            var cmdPart = "cognitiveservices usage list";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";

            var parsed = await ProcessHelpers.ParseShellCommandJson<JArray>("az", $"{cmdPart} --output json {subPart} -l {regionLocation} --query \"[].{{Name:name.value,Current:currentValue,Limit:limit}}\"", GetUserAgentEnv());
            var models = parsed.Payload;

            var x = new ParsedJsonProcessOutput<CognitiveServicesUsageInfo[]>(parsed.Output);
            x.Payload = new CognitiveServicesUsageInfo[models.Count];

            var i = 0;
            foreach (var model in models)
            {
                x.Payload[i].Name = model["Name"].Value<string>();
                x.Payload[i].Current = model["Current"].Value<string>();
                x.Payload[i].Limit = model["Limit"].Value<string>();
                i++;
            }

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<CognitiveServicesResourceInfo?>> CreateCognitiveServicesResource(string subscriptionId, string group, string regionLocation, string name, string kinds = "AIServices", string sku = "F0")
        {
            var cmdPart = "cognitiveservices account create";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";

            var createKind = kinds.Split(';').Last();
            var parsed = await ProcessHelpers.ParseShellCommandJson<JObject>("az", $"{cmdPart} --output json {subPart} --kind {createKind} --location {regionLocation} --sku {sku} -g {group} -n {name} --custom-domain {name}", GetUserAgentEnv());
            var resource = parsed.Payload;

            var x = new ParsedJsonProcessOutput<CognitiveServicesResourceInfo?>(parsed.Output);
            x.Payload = new CognitiveServicesResourceInfo()
            {
                Id = resource?["id"]?.Value<string>(),
                Group = resource?["resourceGroup"]?.Value<string>(),
                Name = resource?["name"]?.Value<string>(),
                Kind = resource?["kind"]?.Value<string>(),
                RegionLocation = resource?["location"]?.Value<string>(),
                Endpoint = resource?["properties"]?["endpoint"].Value<string>()
            };

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<ResourceGroupInfo>> CreateResourceGroup(string subscriptionId, string regionLocation, string name)
        {
            var cmdPart = "group create";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";

            var parsed = await ProcessHelpers.ParseShellCommandJson<JObject>("az", $"{cmdPart} --output json {subPart} -l {regionLocation} -n {name}", GetUserAgentEnv());
            var resource = parsed.Payload;

            var x = new ParsedJsonProcessOutput<ResourceGroupInfo>(parsed.Output);
            x.Payload = new ResourceGroupInfo()
            {
                Id = resource?["id"]?.Value<string>(),
                Name = resource?["name"]?.Value<string>(),
                RegionLocation = resource?["location"]?.Value<string>()
            };

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<CognitiveServicesDeploymentInfo>> CreateCognitiveServicesDeployment(string subscriptionId, string group, string resourceName, string deploymentName, string modelName, string modelVersion, string modelFormat, string scaleCapacity)
        {
            var cmdPart = "cognitiveservices account deployment create";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";

            var parsed = await ProcessHelpers.ParseShellCommandJson<JObject>("az", $"{cmdPart} --output json {subPart} -g {group} -n {resourceName} --deployment-name {deploymentName} --model-name {modelName} --model-version {modelVersion} --model-format {modelFormat} --sku-capacity {scaleCapacity} --sku-name \"Standard\"", GetUserAgentEnv());
            var resource = parsed.Payload;

            var x = new ParsedJsonProcessOutput<CognitiveServicesDeploymentInfo>(parsed.Output);
            x.Payload = new CognitiveServicesDeploymentInfo()
            {
                Name = resource?["name"]?.Value<string>(),
                ModelFormat = resource?["kind"]?.Value<string>(),
                ModelName = modelName,
                ChatCompletionCapable = resource?["properties"]?["capabilities"]?["chatCompletion"]?.Value<bool>() ?? false,
                EmbeddingsCapable = resource?["properties"]?["capabilities"]?["embeddings"]?.Value<bool>() ?? false
            };

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<CognitiveServicesKeyInfo>> ListCognitiveServicesKeys(string subscriptionId, string group, string name)
        {
            var cmdPart = "cognitiveservices account keys list";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";

            var parsed = await ProcessHelpers.ParseShellCommandJson<JObject>("az", $"{cmdPart} --output json {subPart} -g {group} -n {name}", GetUserAgentEnv());
            var keys = parsed.Payload;

            var x = new ParsedJsonProcessOutput<CognitiveServicesKeyInfo>(parsed.Output);
            x.Payload = new CognitiveServicesKeyInfo()
            {
                Key1 = keys?["key1"]?.Value<string>(),
                Key2 = keys?["key2"]?.Value<string>()
            };

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<CognitiveSearchResourceInfo>> CreateSearchResource(string subscriptionId, string group, string regionLocation, string name, string sku = "Standard")
        {
            var cmdPart = "search service create";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";

            var parsed = await ProcessHelpers.ParseShellCommandJson<JObject>("az", $"{cmdPart} --output json {subPart} -g {group} -l {regionLocation} -n {name} --sku {sku}", GetUserAgentEnv());
            var resource = parsed.Payload;

            var x = new ParsedJsonProcessOutput<CognitiveSearchResourceInfo>(parsed.Output);
            x.Payload = new CognitiveSearchResourceInfo()
            {
                Id = resource?["id"]?.Value<string>(),
                Name = name,
                Group = resource?["resourceGroup"]?.Value<string>(),
                RegionLocation = resource?["location"]?.Value<string>(),
                Endpoint = $"https://{name}.search.windows.net"             // TODO: Need to find official way of getting this
            };

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<CognitiveSearchKeyInfo>> ListSearchAdminKeys(string subscriptionId, string group, string name)
        {
            var cmdPart = "search admin-key show";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";

            var parsed = await ProcessHelpers.ParseShellCommandJson<JObject>("az", $"{cmdPart} --output json {subPart} -g {group} --service-name {name}", GetUserAgentEnv());
            var keys = parsed.Payload;

            var x = new ParsedJsonProcessOutput<CognitiveSearchKeyInfo>(parsed.Output);
            x.Payload = new CognitiveSearchKeyInfo()
            {
                Key1 = keys?["primaryKey"]?.Value<string>(),
                Key2 = keys?["secondaryKey"]?.Value<string>()
            };

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<CognitiveSearchResourceInfo[]>> ListSearchResources(string subscriptionId, string regionLocation)
        {
            var cmdPart = "resource list";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";
            var groupPart = "--resource-group \"\"";

            var queryPart1 = string.IsNullOrEmpty(regionLocation) ? "" : $"--location {regionLocation}";
            var queryPart2 = "--query \"[].{Name:name,Id:id,Group:resourceGroup,Location:location}\"";

            var parsed = await ProcessHelpers.ParseShellCommandJson<JArray>("az", $"{cmdPart} --output json {subPart} {groupPart} {queryPart1} {queryPart2} --resource-type Microsoft.Search/searchServices", GetUserAgentEnv());
            var groups = parsed.Payload;

            var x = new ParsedJsonProcessOutput<CognitiveSearchResourceInfo[]>(parsed.Output);
            x.Payload = new CognitiveSearchResourceInfo[groups.Count];

            var i = 0;
            foreach (var resource in groups)
            {
                x.Payload[i].Id = resource["Id"].Value<string>();
                x.Payload[i].Name = resource["Name"].Value<string>();
                x.Payload[i].Group = resource["Group"].Value<string>();
                x.Payload[i].RegionLocation = resource["Location"].Value<string>();
                x.Payload[i].Endpoint = $"https://{x.Payload[i].Name}.search.windows.net"; // TODO: Need to find official way of getting this
                i++;
            }

            return x;
        }

        private static async Task<List<string>> ListSupportedResourceRegions()
        {
            // TODO: What kind should we use here?
            var process2 = await ProcessHelpers.ParseShellCommandJson<JArray>("az", $"cognitiveservices account list-skus --output json --kind {Program.CognitiveServiceResourceKind} --query \"[].{{Name:locations[0]}}\"", GetUserAgentEnv());
            var supportedRegions = new List<string>();
            foreach (var regionLocation in process2.Payload)
            {
                supportedRegions.Add(regionLocation["Name"].Value<string>().ToLower());
            }

            return supportedRegions;
        }


    }
}
