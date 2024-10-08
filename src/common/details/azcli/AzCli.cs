//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Azure.AI.Details.Common.CLI;
using System.Collections.Generic;
using System.Text.Json;

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
            public string RealTimeDeployment;
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

        public struct CognitiveServicesVisionResourceInfo
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
            public string SkuName { get; set; }
            public string UsageName { get; set; }
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
            var queryPart = $"--query \"[?state=='Enabled'].{{Name:name,Id:id,IsDefault:isDefault,UserName:user.name}}\"";

            var parsed = await ProcessHelpers.ParseShellCommandJsonArray("az", $"login --output json {queryPart} {deviceCodePart}", GetUserAgentEnv(), null, stdErrHandler);
            var accounts = parsed.Payload;

            var x = new ParsedJsonProcessOutput<SubscriptionInfo[]>(parsed.Output);
            x.Payload = new SubscriptionInfo[accounts.GetArrayLength()];

            var i = 0;
            foreach (var account in accounts.EnumerateArray())
            {
                x.Payload[i].Id = account.GetPropertyStringOrEmpty("Id");
                x.Payload[i].Name = account.GetPropertyStringOrEmpty("Name");
                x.Payload[i].IsDefault = account.GetPropertyBool("IsDefault", false);
                x.Payload[i].UserName = account.GetPropertyStringOrEmpty("UserName");
                i++;
            }

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<SubscriptionInfo[]>> ListAccounts()
        {
            var parsed = await ProcessHelpers.ParseShellCommandJsonArray("az", "account list --refresh --output json --query \"[?state=='Enabled'].{Name:name,Id:id,IsDefault:isDefault,UserName:user.name}\"", GetUserAgentEnv());
            var accounts = parsed.Payload;

            var x = new ParsedJsonProcessOutput<SubscriptionInfo[]>(parsed.Output);
            x.Payload = new SubscriptionInfo[accounts.GetArrayLength()];

            var i = 0;
            foreach (var account in accounts.EnumerateArray())
            {
                x.Payload[i].Id = account.GetPropertyStringOrEmpty("Id");
                x.Payload[i].Name = account.GetPropertyStringOrEmpty("Name");
                x.Payload[i].IsDefault = account.GetPropertyBool("IsDefault", false);
                x.Payload[i].UserName = account.GetPropertyStringOrEmpty("UserName");
                i++;
            }

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<string>> SetAccount(string subscriptionId)
        {
            var parsed = await ProcessHelpers.ParseShellCommandJsonString("az", $"account set --output json --subscription {subscriptionId}", GetUserAgentEnv());

            var x = new ParsedJsonProcessOutput<string>(parsed.Output);
            x.Payload = subscriptionId;

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<AccountRegionLocationInfo[]>> ListAccountRegionLocations()
        {
            var supportedRegions = await ListSupportedResourceRegions();

            var parsed = await ProcessHelpers.ParseShellCommandJsonArray("az", "account list-locations --output json --query \"[].{Name:name,RegionalDisplayName:regionalDisplayName,DisplayName:displayName}\"", GetUserAgentEnv());
            var regionLocations = parsed.Payload;

            var list = new List<AccountRegionLocationInfo>();
            foreach (var regionLocation in regionLocations.EnumerateArray())
            {
                if (supportedRegions.Count == 0 || supportedRegions.Contains(regionLocation.GetPropertyStringOrEmpty("Name").ToLower()))
                {
                    list.Add(new AccountRegionLocationInfo()
                    {
                        Name = regionLocation.GetPropertyStringOrEmpty("Name"),
                        DisplayName = regionLocation.GetPropertyStringOrEmpty("DisplayName"),
                        RegionalDisplayName = regionLocation.GetPropertyStringOrEmpty("RegionalDisplayName")
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

            var parsed = await ProcessHelpers.ParseShellCommandJsonArray("az", $"{cmdPart} --output json {subPart} {queryPart1}{queryPart2}{queryPart3}", GetUserAgentEnv());
            var groups = parsed.Payload;

            var x = new ParsedJsonProcessOutput<ResourceGroupInfo[]>(parsed.Output);
            x.Payload = new ResourceGroupInfo[groups.GetArrayLength()];

            var i = 0;
            foreach (var resource in groups.EnumerateArray())
            {
                x.Payload[i].Id = resource.GetPropertyStringOrEmpty("Id");
                x.Payload[i].Name = resource.GetPropertyStringOrEmpty("Name");
                x.Payload[i].RegionLocation = resource.GetPropertyStringOrEmpty("Location");
                i++;
            }

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<CognitiveServicesResourceInfo[]>> ListCognitiveServicesResources(string subscriptionId = null, string kinds = null)
        {
            var cmdPart = "cognitiveservices account list";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";
            var groupPart = "--resource-group \"\"";
            
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

            var parsed = await ProcessHelpers.ParseShellCommandJsonArray("az", $"{cmdPart} --output json {subPart} {groupPart} --query \"[{condPart}].{{Id:id,Name:name,Location: location,Kind:kind,Group:resourceGroup,Endpoint:properties.endpoint}}\"", GetUserAgentEnv());
            var resources = parsed.Payload;

            var x = new ParsedJsonProcessOutput<CognitiveServicesResourceInfo[]>(parsed.Output);
            x.Payload = new CognitiveServicesResourceInfo[resources.GetArrayLength()];

            var i = 0;
            foreach (var resource in resources.EnumerateArray())
            {
                x.Payload[i].Id = resource.GetPropertyStringOrEmpty("Id");
                x.Payload[i].Group = resource.GetPropertyStringOrEmpty("Group");
                x.Payload[i].Name = resource.GetPropertyStringOrEmpty("Name");
                x.Payload[i].Kind = resource.GetPropertyStringOrEmpty("Kind");
                x.Payload[i].RegionLocation = resource.GetPropertyStringOrEmpty("Location");
                x.Payload[i].Endpoint = resource.GetPropertyStringOrEmpty("Endpoint");
                i++;
            }

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<CognitiveServicesDeploymentInfo[]>> ListCognitiveServicesDeployments(string subscriptionId = null, string group = null, string resourceName = null, string modelFormat = null)
        {
            var cmdPart = "cognitiveservices account deployment list";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";

            var parsed = await ProcessHelpers.ParseShellCommandJsonArray("az", $"{cmdPart} --output json {subPart}  -g {group} -n {resourceName} --query \"[].{{Name:name,Location: location,Group:resourceGroup,Endpoint:properties.endpoint,Model:properties.model.name,Format:properties.model.format,ChatCompletionCapable:properties.capabilities.chatCompletion,EmbeddingsCapable:properties.capabilities.embeddings}}\"", GetUserAgentEnv());
            var deployments = parsed.Payload;

            var x = new ParsedJsonProcessOutput<CognitiveServicesDeploymentInfo[]>(parsed.Output);
            x.Payload = new CognitiveServicesDeploymentInfo[deployments.GetArrayLength()];

            var i = 0;
            foreach (var deployment in deployments.EnumerateArray())
            {
                x.Payload[i].Name = deployment.GetPropertyStringOrEmpty("Name");
                x.Payload[i].ModelFormat = deployment.GetPropertyStringOrEmpty("Format");
                x.Payload[i].ModelName = deployment.GetPropertyStringOrEmpty("Model");
                x.Payload[i].ChatCompletionCapable = deployment.GetPropertyStringOrEmpty("ChatCompletionCapable") == "true";
                x.Payload[i].EmbeddingsCapable = deployment.GetPropertyStringOrEmpty("EmbeddingsCapable") == "true";
                i++;
            }

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<CognitiveServicesModelInfo[]>> ListCognitiveServicesModels(string subscriptionId = null, string regionLocation = null)
        {
            var cmdPart = "cognitiveservices model list";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";

            var parsed = await ProcessHelpers.ParseShellCommandJsonArray("az", $"{cmdPart} --output json {subPart} -l {regionLocation} --query \"[].{{Name:model.name,Format:model.format,Version:model.version,ChatCompletionCapable:model.capabilities.chatCompletion,EmbeddingsCapable:model.capabilities.embeddings,Skus:model.skus}}\"", GetUserAgentEnv());
            var models = parsed.Payload;

            var x = new ParsedJsonProcessOutput<CognitiveServicesModelInfo[]>(parsed.Output);
            var payload = new List<CognitiveServicesModelInfo>();

            foreach (var model in models.EnumerateArray())
            {
                var name = model.GetPropertyStringOrEmpty("Name");
                var format = model.GetPropertyStringOrEmpty("Format");
                var version = model.GetPropertyStringOrEmpty("Version");
                var chatCompletionsCapable = model.GetPropertyStringOrEmpty("ChatCompletionCapable") == "true";
                var embeddingsCapable = model.GetPropertyStringOrEmpty("EmbeddingsCapable") == "true";

                var skus = model.GetProperty("Skus").EnumerateArray();
                foreach (var sku in skus)
                {
                    payload.Add(new CognitiveServicesModelInfo()
                    {
                        Name = name,
                        Format = format,
                        Version = version,
                        ChatCompletionCapable = chatCompletionsCapable,
                        EmbeddingsCapable = embeddingsCapable,
                        SkuName = sku.GetPropertyStringOrEmpty("name"),
                        UsageName = sku.GetPropertyStringOrEmpty("usageName"),
                        DefaultCapacity = sku.GetPropertyElementOrNull("capacity")?.GetPropertyStringOrEmpty("default") ?? "50"
                    });
                }
            }

            x.Payload = payload.ToArray();
            return x;
        }

        public static async Task<ParsedJsonProcessOutput<CognitiveServicesUsageInfo[]>> ListCognitiveServicesUsage(string subscriptionId = null, string regionLocation = null)
        {
            var cmdPart = "cognitiveservices usage list";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";

            var parsed = await ProcessHelpers.ParseShellCommandJsonArray("az", $"{cmdPart} --output json {subPart} -l {regionLocation} --query \"[].{{Name:name.value,Current:currentValue,Limit:limit}}\"", GetUserAgentEnv());
            var models = parsed.Payload;

            var x = new ParsedJsonProcessOutput<CognitiveServicesUsageInfo[]>(parsed.Output);
            x.Payload = new CognitiveServicesUsageInfo[models.GetArrayLength()];

            var i = 0;
            foreach (var model in models.EnumerateArray())
            {
                x.Payload[i].Name = model.GetPropertyStringOrEmpty("Name");
                x.Payload[i].Current = model.GetPropertyStringOrEmpty("Current");
                x.Payload[i].Limit = model.GetPropertyStringOrEmpty("Limit");
                i++;
            }

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<CognitiveServicesResourceInfo?>> CreateCognitiveServicesResource(string subscriptionId, string group, string regionLocation, string name, string kinds = "AIServices", string sku = "F0")
        {
            var cmdPart = "cognitiveservices account create";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";

            var createKind = kinds.Split(';').Last();
            var parsed = await ProcessHelpers.ParseShellCommandJsonObject("az", $"{cmdPart} --output json {subPart} --kind {createKind} --location {regionLocation} --sku {sku} -g {group} -n {name} --custom-domain {name}", GetUserAgentEnv());
            var resource = parsed.Payload;

            var x = new ParsedJsonProcessOutput<CognitiveServicesResourceInfo?>(parsed.Output);
            x.Payload = new CognitiveServicesResourceInfo()
            {
                Id = resource.GetPropertyStringOrEmpty("id"),
                Group = resource.GetPropertyStringOrEmpty("resourceGroup"),
                Name = resource.GetPropertyStringOrEmpty("name"),
                Kind = resource.GetPropertyStringOrEmpty("kind"),
                RegionLocation = resource.GetPropertyStringOrEmpty("location"),
                Endpoint = resource.GetPropertyElementOrNull("properties")?.GetPropertyStringOrEmpty("endpoint")
            };

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<ResourceGroupInfo>> CreateResourceGroup(string subscriptionId, string regionLocation, string name)
        {
            var cmdPart = "group create";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";

            var parsed = await ProcessHelpers.ParseShellCommandJsonObject("az", $"{cmdPart} --output json {subPart} -l {regionLocation} -n {name}", GetUserAgentEnv());
            var resource = parsed.Payload;

            var x = new ParsedJsonProcessOutput<ResourceGroupInfo>(parsed.Output);
            x.Payload = new ResourceGroupInfo()
            {
                Id = resource.GetPropertyStringOrEmpty("id"),
                Name = resource.GetPropertyStringOrEmpty("name"),
                RegionLocation = resource.GetPropertyStringOrEmpty("location")
            };

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<CognitiveServicesDeploymentInfo>> CreateCognitiveServicesDeployment(string subscriptionId, string group, string resourceName, string deploymentName, string modelName, string modelVersion, string modelFormat, string scaleCapacity, string skuName)
        {
            var cmdPart = "cognitiveservices account deployment create";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";

            var parsed = await ProcessHelpers.ParseShellCommandJsonObject("az", $"{cmdPart} --output json {subPart} -g {group} -n {resourceName} --deployment-name {deploymentName} --model-name {modelName} --model-version {modelVersion} --model-format {modelFormat} --sku-capacity {scaleCapacity} --sku-name \"{skuName}\"", GetUserAgentEnv());
            var resource = parsed.Payload;

            var x = new ParsedJsonProcessOutput<CognitiveServicesDeploymentInfo>(parsed.Output);
            x.Payload = new CognitiveServicesDeploymentInfo()
            {
                Name = resource.GetPropertyStringOrEmpty("name"),
                ModelFormat = resource.GetPropertyStringOrEmpty("kind"),
                ModelName = modelName,
                ChatCompletionCapable = resource.GetPropertyElementOrNull("properties")?.GetPropertyElementOrNull("capabilities")?.GetPropertyBool("chatCompletion", false) ?? false,
                EmbeddingsCapable = resource.GetPropertyElementOrNull("properties")?.GetPropertyElementOrNull("capabilities")?.GetPropertyBool("embeddings", false) ?? false
            };

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<CognitiveServicesKeyInfo>> ListCognitiveServicesKeys(string subscriptionId, string group, string name)
        {
            var cmdPart = "cognitiveservices account keys list";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";

            var parsed = await ProcessHelpers.ParseShellCommandJsonObject("az", $"{cmdPart} --output json {subPart} -g {group} -n {name}", GetUserAgentEnv());
            var keys = parsed.Payload;

            var x = new ParsedJsonProcessOutput<CognitiveServicesKeyInfo>(parsed.Output);
            x.Payload = new CognitiveServicesKeyInfo()
            {
                Key1 = keys.GetPropertyStringOrEmpty("key1"),
                Key2 = keys.GetPropertyStringOrEmpty("key2")
            };

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<CognitiveSearchResourceInfo>> CreateSearchResource(string subscriptionId, string group, string regionLocation, string name, string sku = "Standard")
        {
            var cmdPart = "search service create";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";

            var parsed = await ProcessHelpers.ParseShellCommandJsonObject("az", $"{cmdPart} --output json {subPart} -g {group} -l {regionLocation} -n {name} --sku {sku}", GetUserAgentEnv());
            var resource = parsed.Payload;

            var x = new ParsedJsonProcessOutput<CognitiveSearchResourceInfo>(parsed.Output);
            x.Payload = new CognitiveSearchResourceInfo()
            {
                Id = resource.GetPropertyStringOrEmpty("id"),
                Name = name,
                Group = resource.GetPropertyStringOrEmpty("resourceGroup"),
                RegionLocation = resource.GetPropertyStringOrEmpty("location"),
                Endpoint = $"https://{name}.search.windows.net"             // TODO: Need to find official way of getting this
            };

            return x;
        }

        public static async Task<ParsedJsonProcessOutput<CognitiveSearchKeyInfo>> ListSearchAdminKeys(string subscriptionId, string group, string name)
        {
            var cmdPart = "search admin-key show";
            var subPart = subscriptionId != null ? $"--subscription {subscriptionId}" : "";

            var parsed = await ProcessHelpers.ParseShellCommandJsonObject("az", $"{cmdPart} --output json {subPart} -g {group} --service-name {name}", GetUserAgentEnv());
            var keys = parsed.Payload;

            var x = new ParsedJsonProcessOutput<CognitiveSearchKeyInfo>(parsed.Output);
            x.Payload = new CognitiveSearchKeyInfo()
            {
                Key1 = keys.GetPropertyStringOrEmpty("primaryKey"),
                Key2 = keys.GetPropertyStringOrEmpty("secondaryKey")
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

            var parsed = await ProcessHelpers.ParseShellCommandJsonArray("az", $"{cmdPart} --output json {subPart} {groupPart} {queryPart1} {queryPart2} --resource-type Microsoft.Search/searchServices", GetUserAgentEnv());
            var groups = parsed.Payload;

            var x = new ParsedJsonProcessOutput<CognitiveSearchResourceInfo[]>(parsed.Output);
            x.Payload = new CognitiveSearchResourceInfo[groups.GetArrayLength()];

            var i = 0;
            foreach (var resource in groups.EnumerateArray())
            {
                x.Payload[i].Id = resource.GetPropertyStringOrEmpty("Id");
                x.Payload[i].Name = resource.GetPropertyStringOrEmpty("Name");
                x.Payload[i].Group = resource.GetPropertyStringOrEmpty("Group");
                x.Payload[i].RegionLocation = resource.GetPropertyStringOrEmpty("Location");
                x.Payload[i].Endpoint = $"https://{x.Payload[i].Name}.search.windows.net"; // TODO: Need to find official way of getting this
                i++;
            }

            return x;
        }

        private static async Task<List<string>> ListSupportedResourceRegions()
        {
            // TODO: What kind should we use here?
            var process2 = await ProcessHelpers.ParseShellCommandJsonArray("az", $"cognitiveservices account list-skus --output json --kind {Program.CognitiveServiceResourceKind} --query \"[].{{Name:locations[0]}}\"", GetUserAgentEnv());
            var supportedRegions = new List<string>();
            foreach (var regionLocation in process2.Payload.EnumerateArray())
            {
                supportedRegions.Add(regionLocation.GetPropertyStringOrEmpty("Name").ToLower());
            }

            return supportedRegions;
        }


    }
}
