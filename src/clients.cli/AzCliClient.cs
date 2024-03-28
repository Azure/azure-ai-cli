using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.CLI.Common.Clients;
using Azure.AI.CLI.Common.Clients.Models;
using Azure.AI.CLI.Common.Clients.Models.Utils;
using Azure.AI.Details.Common.CLI;
using Azure.AI.Details.Common.CLI.AzCli;


namespace Azure.AI.CLI.Clients.AzPython
{
    /// <summary>
    /// A client for Azure subscriptions that uses the AZ CLI
    /// </summary>
    public class AzCliClient : LoginHelpers, ISubscriptionsClient, ICognitiveServicesClient, ISearchClient
    {
        private static readonly System.Text.Json.JsonSerializerOptions JSON_OPTIONS = new System.Text.Json.JsonSerializerOptions()
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                // for support JPath expressions for property deserialization
                new JsonPathConverterFactory(),
                // for deserializing strings into an Enum
                new JsonStringEnumConverter(),
                // for deserializing strings into a bool or bool?
                new StringToBoolJsonConverter(),
                new StringToNullableBoolJsonConverter(),
            }
        };

        private IDictionary<string, string> _cliEnv;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="loginManager">The instance to use for logging in to Azure</param>
        /// <param name="getIsInteractive">Used to retrieve whether or not we are running interactive</param>
        /// <param name="userAgent">The user agent string to add to all HTTP/HTTPS requests</param>
        public AzCliClient(ILoginManager loginManager, Func<bool> getIsInteractive, string? userAgent)
            : base(loginManager, getIsInteractive)
        {
            _cliEnv = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(userAgent))
            {
                _cliEnv["AZURE_HTTP_USER_AGENT"] = userAgent;
            }
        }

        /// <inheritdoc />
        public Task<ClientResult<SubscriptionInfo[]>> GetAllSubscriptionsAsync(CancellationToken token)
        {
            return RunOrLoginArrayAsync<SubscriptionInfo>(
                () => ProcessHelpers.RunShellCommandAsync(
                    "az",
                    "account list"
                    + " --refresh"
                    + " --output json"
                    + " --query \"[?state=='Enabled']\"",
                    _cliEnv),
                token);
        }

        /// <inheritdoc />
        public async Task<ClientResult<SubscriptionInfo?>> GetSubscriptionAsync(string subscriptionId, CancellationToken token)
        {
            try
            {
                ValidateString(subscriptionId, nameof(subscriptionId));

                var cmdOut = await GetResponseOnLogin(
                    () => ProcessHelpers.RunShellCommandAsync(
                        "az",
                        $"account show"
                        + $" --subscription {subscriptionId}"
                        + $" --output json",
                        _cliEnv),
                    token);
                if (cmdOut.HasError)
                {
                    // special case: check if the error indicates the subscription does not exist
                    string err = cmdOut.StdError ?? string.Empty;
                    if (err.Contains(subscriptionId) && err.Contains(" not found."))
                    {
                        return ClientResult.From<SubscriptionInfo?>(null);
                    }

                    return ClientResult.From<SubscriptionInfo?>(cmdOut, null);
                }
                else
                {
                    return ClientResult.From<SubscriptionInfo?>(cmdOut, DeserializeJson<SubscriptionInfo>(cmdOut.StdOutput));
                }
            }
            catch (Exception ex)
            {
                return ClientResult.FromException<SubscriptionInfo?>(ex);
            }
        }

        /// <inheritdoc />
        public async Task<ClientResult<AccountRegionLocationInfo[]>> GetAllRegionsAsync(string subscriptionId, CancellationToken token)
        {
            try
            {
                ValidateString(subscriptionId, nameof(subscriptionId));

                // 2 step process, first see the regions where we can have OpenAI resources, and query for all possible regions
                // and filter that list. The second call is needed to get more details about the regions

                var csRegionsOutput = await RunOrLoginArrayAsync<string>(
                    () => ProcessHelpers.RunShellCommandAsync(
                        "az",
                        $"cognitiveservices account list-skus" +
                        $" --output json" +
                        $" --kind {Program.CognitiveServiceResourceKind ?? "CognitiveServices"}" +
                        $" --query \"[].locations[0]\"",
                        _cliEnv),
                    token);

                if (!csRegionsOutput.IsSuccess)
                {
                    return new ClientResult<AccountRegionLocationInfo[]>()
                    {
                        ErrorDetails = csRegionsOutput.ErrorDetails,
                        Exception = csRegionsOutput.Exception,
                        Outcome = csRegionsOutput.Outcome,
                        Value = Array.Empty<AccountRegionLocationInfo>()
                    };
                }

                // For now just ignore errors
                HashSet<string> openAiRegions = new HashSet<string>(
                    csRegionsOutput.Value ?? Array.Empty<string>(),
                    StringComparer.OrdinalIgnoreCase);

                var allRegionsOutput = await RunOrLoginArrayAsync<AccountRegionLocationInfo>(
                    () => ProcessHelpers.RunShellCommandAsync(
                        "az",
                        "account list-locations" +
                        " --output json",
                        _cliEnv),
                    token);

                if (!allRegionsOutput.IsSuccess)
                {
                    return allRegionsOutput;
                }

                return ClientResult.From(allRegionsOutput.Value
                    .Where(r => openAiRegions.Contains(r.Name))
                    .ToArray());
            }
            catch (Exception ex)
            {
                return ClientResult.FromException(ex, Array.Empty<AccountRegionLocationInfo>());
            }
        }

        /// <inheritdoc />
        public Task<ClientResult<ResourceGroupInfo[]>> GetAllResourceGroupsAsync(string subscriptionId, CancellationToken token)
        {
            return RunOrLoginArrayAsync<ResourceGroupInfo>(
                () =>
                {
                    ValidateString(subscriptionId, nameof(subscriptionId));

                    return ProcessHelpers.RunShellCommandAsync(
                        "az",
                        $"group list" +
                        $" --output json" +
                        $" --subscription {subscriptionId}",
                        _cliEnv);
                },
                token);
        }

        /// <inheritdoc />
        public Task<ClientResult<ResourceGroupInfo>> CreateResourceGroupAsync(
            string subscriptionId, string regionName, string name, CancellationToken token)
        {
            return RunOrLoginAsync<ResourceGroupInfo>(
                () =>
                {
                    ValidateString(subscriptionId, nameof(subscriptionId));
                    ValidateString(regionName, nameof(regionName));
                    ValidateString(name, nameof(name));

                    return ProcessHelpers.RunShellCommandAsync(
                        "az",
                        $"group create" +
                        $" --output json" +
                        $" --subscription {subscriptionId}" +
                        $" -l {regionName}" +
                        $" -n {name}",
                        _cliEnv);
                },
                token);
        }

        /// <inheritdoc />
        public  Task<ClientResult> DeleteResourceGroupAsync(string subscriptionId, string resourceGroup, CancellationToken token)
        {
            return RunOrLoginAsync(
                () =>
                {
                    ValidateString(subscriptionId, nameof(subscriptionId));
                    ValidateString(resourceGroup, nameof(resourceGroup));

                    return ProcessHelpers.RunShellCommandAsync(
                        "az",
                        $"group delete" +
                        $" --output json" +
                        $" --subscription {subscriptionId}" +
                        $" --name {resourceGroup}" +
                        $" --yes",
                        _cliEnv);
                },
                token);
        }

        /// <inheritdoc />
        public Task<ClientResult<CognitiveServicesResourceInfo[]>> GetAllResourcesAsync(
            string subscriptionId, CancellationToken token, ResourceKind? filter = null)
        {
            return RunOrLoginArrayAsync<CognitiveServicesResourceInfo>(
                () =>
                {
                    ValidateString(subscriptionId, nameof(subscriptionId));

                    // TODO FIXME: The Azure rest endpoint doesn't seem to have an obvious way to filter by a specific kind of
                    // Cognitive services resource. All this filtering is done locally after we get the resources from the service

                    // TODO FIXME: If we ask for a specific resource, I don't think we should be returning some other kind of resource
                    // but for compatibility with existing code, keeping this as is for now
                    var condPart = filter switch
                    {
                        ResourceKind.OpenAI => "? kind == 'OpenAI' || kind == 'AIServices'",
                        ResourceKind.Vision => "? kind == 'ComputerVision' || kind == 'CognitiveServices' || kind == 'AIServices'",
                        ResourceKind.Speech => "? kind == 'SpeechServices' || kind == 'CognitiveServices' || kind == 'AIServices'",
                        ResourceKind.AIServices => "? kind == 'AIServices'",
                        ResourceKind.CognitiveServices => "? kind == 'CognitiveServices'",

                        null => null,
                        _ => $"? kind == '{filter}' || kind == 'CognitiveServices' || kind == 'AIServices'",
                    };

                    return ProcessHelpers.RunShellCommandAsync(
                        "az",
                        $"cognitiveservices account list" +
                        $" --output json" +
                        $" --subscription {subscriptionId}" +
                        $" --query \"[{condPart}]\"", _cliEnv);
                },
                token);
        }

        /// <inheritdoc />
        public async Task<ClientResult<CognitiveServicesResourceInfo?>> GetResourceFromNameAsync(
            string subscriptionId, string resourceGroup, string name, CancellationToken token)
        {
            try
            {
                ValidateString(subscriptionId, nameof(subscriptionId));
                ValidateString(resourceGroup, nameof(resourceGroup));
                ValidateString(name, nameof(name));

                var cmdOut = await GetResponseOnLogin(
                    () => ProcessHelpers.RunShellCommandAsync(
                        "az",
                        $"cognitiveservices account show" +
                        $" --output json" +
                        $" --subscription {subscriptionId}" +
                        $" -g {resourceGroup}" +
                        $" -n {name}",
                        _cliEnv),
                    token);
                if (cmdOut.HasError)
                {
                    // special case: check if the error indicates the subscription does not exist
                    string err = cmdOut.StdError ?? string.Empty;
                    if (err.Contains(name) && err.Contains("ResourceNotFound"))
                    {
                        return ClientResult.From<CognitiveServicesResourceInfo?>(null);
                    }

                    return ClientResult.From<CognitiveServicesResourceInfo?>(cmdOut, null);
                }
                else
                {
                    return ClientResult.From(cmdOut, DeserializeJson<CognitiveServicesResourceInfo>(cmdOut.StdOutput));
                }
            }
            catch (Exception ex)
            {
                return ClientResult.FromException(ex, (CognitiveServicesResourceInfo?)null);
            }
        }

        /// <inheritdoc />
        public Task<ClientResult<CognitiveServicesResourceInfo>> CreateResourceAsync(
            ResourceKind kind, string subscriptionId, string resourceGroup, string region, string name, string sku, CancellationToken token)
        {
            return RunOrLoginAsync<CognitiveServicesResourceInfo>(
                () =>
                {
                    if (!IsCognitiveServicesResourceKind(kind))
                    {
                        throw new ArgumentException($"'{kind}' is not a supported Cognitive Services resource kind", nameof(kind));
                    }

                    ValidateString(subscriptionId, nameof(subscriptionId));
                    ValidateString(resourceGroup, nameof(resourceGroup));
                    ValidateString(region, nameof(region));
                    ValidateString(name, nameof(name));
                    ValidateString(sku, nameof(sku));

                    return ProcessHelpers.RunShellCommandAsync(
                        "az",
                        $"cognitiveservices account create" +
                        $" --output json" +
                        $" --kind {kind.AsJsonString()}" +
                        $" --subscription {subscriptionId}" +
                        $" -g {resourceGroup}" +
                        $" -l {region}" +
                        $" -n {name}" +
                        $" --custom-domain {name}" +
                        $" --sku {sku}",
                        _cliEnv);
                },
                token);
        }

        /// <inheritdoc />
        public Task<ClientResult> DeleteResourceAsync(string subscriptionId, string resourceGroup, string resourceName, CancellationToken token)
        {
            return RunOrLoginAsync(
                () =>
                {
                    ValidateString(subscriptionId, nameof(subscriptionId));
                    ValidateString(resourceGroup, nameof(resourceGroup));
                    ValidateString(resourceName, nameof(resourceName));

                    return ProcessHelpers.RunShellCommandAsync(
                        "az",
                        $"cognitiveservices account delete" +
                        $" --output json" +
                        $" --subscription {subscriptionId}" +
                        $" -g {resourceGroup}" +
                        $" -n {resourceName}",
                        _cliEnv);
                },
                token);
        }

        /// <inheritdoc />
        public Task<ClientResult<CognitiveServicesDeploymentInfo[]>> GetAllDeploymentsAsync(
            string subscriptionId, string resourceGroup, string resourceName, CancellationToken token)
        {
            return RunOrLoginArrayAsync<CognitiveServicesDeploymentInfo>(
                () =>
                {
                    ValidateString(subscriptionId, nameof(subscriptionId));
                    ValidateString(resourceGroup, nameof(resourceGroup));
                    ValidateString(resourceName, nameof(resourceName));

                    return ProcessHelpers.RunShellCommandAsync(
                        "az", 
                        $"cognitiveservices account deployment list" +
                        $" --output json" +
                        $" --subscription {subscriptionId}" +
                        $" -g {resourceGroup}" +
                        $" -n {resourceName}",
                        _cliEnv);
                },
                token);
        }

        /// <inheritdoc />
        public Task<ClientResult<CognitiveServicesModelInfo[]>> GetAllModelsAsync(
            string subscriptionId, string regionName, CancellationToken token)
        {
            return RunOrLoginArrayAsync<CognitiveServicesModelInfo>(
                () =>
                {
                    ValidateString(subscriptionId, nameof(subscriptionId));
                    ValidateString(regionName, nameof(regionName));

                    return ProcessHelpers.RunShellCommandAsync(
                        "az",
                        $"cognitiveservices model list" +
                        $" --output json" +
                        $" --subscription {subscriptionId}" +
                        $" -l {regionName}",
                        _cliEnv);
                },
                token);
        }

        /// <inheritdoc />
        public Task<ClientResult<CognitiveServicesUsageInfo[]>> GetAllModelUsageAsync(
            string subscriptionId, string regionName, CancellationToken token)
        {
            return RunOrLoginArrayAsync<CognitiveServicesUsageInfo>(
                () =>
                {
                    ValidateString(subscriptionId, nameof(subscriptionId));
                    ValidateString(regionName, nameof(regionName));

                    return ProcessHelpers.RunShellCommandAsync(
                        "az",
                        $"cognitiveservices usage list" +
                        $" --output json" +
                        $" --subscription {subscriptionId}" +
                        $" -l {regionName}",
                        _cliEnv);
                },
                token);
        }

        /// <inheritdoc />
        public Task<ClientResult<CognitiveServicesDeploymentInfo>> CreateDeploymentAsync(
            string subscriptionId, string resourceGroup, string resourceName,
            string deploymentName, string modelName, string modelVersion,
            string modelFormat, string scaleCapacity, CancellationToken token)
        {
            return RunOrLoginAsync<CognitiveServicesDeploymentInfo>(
                () =>
                {
                    ValidateString(subscriptionId, nameof(subscriptionId));
                    ValidateString(resourceGroup, nameof(resourceGroup));
                    ValidateString(resourceName, nameof(resourceName));
                    ValidateString(deploymentName, nameof(deploymentName));
                    ValidateString(modelName, nameof(modelName));
                    ValidateString(modelVersion, nameof(modelVersion));
                    ValidateString(modelFormat, nameof(modelFormat));
                    ValidateString(scaleCapacity, nameof(scaleCapacity));

                    return ProcessHelpers.RunShellCommandAsync(
                        "az",
                        $"cognitiveservices account deployment create" +
                        $" --output json" +
                        $" --subscription {subscriptionId}" +
                        $" -g {resourceGroup}" +
                        $" -n {resourceName}" +
                        $" --deployment-name {deploymentName}" +
                        $" --model-name {modelName}" +
                        $" --model-version {modelVersion}" +
                        $" --model-format {modelFormat}" +
                        $" --sku-capacity {scaleCapacity}" +
                        $" --sku-name \"Standard\"",
                        _cliEnv);
                },
                token);
        }

        public Task<ClientResult> DeleteDeploymentAsync(string subscriptionId, string resourceGroup, string resourceName, string deploymentName, CancellationToken token)
        {
            return RunOrLoginAsync(
                () =>
                {
                    ValidateString(subscriptionId, nameof(subscriptionId));
                    ValidateString(resourceGroup, nameof(resourceGroup));
                    ValidateString(resourceName, nameof(resourceName));
                    ValidateString(deploymentName, nameof(deploymentName));

                    return ProcessHelpers.RunShellCommandAsync(
                        "az",
                        $"cognitiveservices account deployment delete" +
                        $" --output json" +
                        $" --subscription {subscriptionId}" +
                        $" -g {resourceGroup}" +
                        $" -n {resourceName}" +
                        $" --deployment-name {deploymentName}",
                        _cliEnv);
                },
                token);
        }

        /// <inheritdoc />
        public Task<ClientResult<(string, string?)>> GetResourceKeysFromNameAsync(
            string subscriptionId, string resourceGroup, string resourceName, CancellationToken token)
        {
            return RunOrLoginAsync(
                () =>
                {
                    ValidateString(subscriptionId, nameof(subscriptionId));
                    ValidateString(resourceGroup, nameof(resourceGroup));
                    ValidateString(resourceName, nameof(resourceName));

                    return ProcessHelpers.RunShellCommandAsync(
                        "az",
                        $"cognitiveservices account keys list" +
                        $" --output json" +
                        $" --subscription {subscriptionId}" +
                        $" -g {resourceGroup}" +
                        $" -n {resourceName}",
                        _cliEnv);
                },
                json =>
                {
                    using var doc = JsonDocument.Parse(json ?? "{}");
                    return (
                        doc.RootElement.GetPropertyStringOrEmpty("key1"),
                        doc.RootElement.GetPropertyStringOrNull("key2")
                    );
                },
                () => (string.Empty, null),
                token);
        }

        Task<ClientResult<CognitiveSearchResourceInfo[]>> ISearchClient.GetAllAsync(
            string subscriptionId, string? regionName, CancellationToken token)
        {
            return RunOrLoginArrayAsync<CognitiveSearchResourceInfo>(
                () =>
                {
                    ValidateString(subscriptionId, nameof(subscriptionId));

                    return ProcessHelpers.RunShellCommandAsync(
                        "az",
                        $"resource list" +
                        $" --output json" +
                        $" --subscription {subscriptionId}" +
                        (!string.IsNullOrWhiteSpace(regionName) ? $" -l {regionName}" : string.Empty) +
                        $" --resource-type Microsoft.Search/searchServices",
                        _cliEnv);
                },
                token);
        }

        async Task<ClientResult<CognitiveSearchResourceInfo?>> ISearchClient.GetFromNameAsync(
            string subscriptionId, string resourceGroup, string name, CancellationToken token)
        {
            try
            {
                ValidateString(subscriptionId, nameof(subscriptionId));
                ValidateString(resourceGroup, nameof(resourceGroup));
                ValidateString(name, nameof(name));

                var cmdOut = await GetResponseOnLogin(
                    () => ProcessHelpers.RunShellCommandAsync(
                        "az",
                        $"search service show" +
                        $" --output json" +
                        $" --subscription {subscriptionId}" +
                        $" -g {resourceGroup}" +
                        $" -n {name}",
                        _cliEnv),
                    token);
                if (cmdOut.HasError)
                {
                    // special case: check if the error indicates the subscription does not exist
                    string err = cmdOut.StdError ?? string.Empty;
                    if (err.Contains(name) && err.Contains("ResourceNotFound"))
                    {
                        return ClientResult.From<CognitiveSearchResourceInfo?>(null);
                    }

                    return ClientResult.From<CognitiveSearchResourceInfo?>(cmdOut, null);
                }
                else
                {
                    return ClientResult.From(cmdOut, DeserializeJson<CognitiveSearchResourceInfo>(cmdOut.StdOutput));
                }
            }
            catch (Exception ex)
            {
                return ClientResult.FromException<CognitiveSearchResourceInfo?>(ex);
            }
        }

        Task<ClientResult<CognitiveSearchResourceInfo>> ISearchClient.CreateAsync(
            string subscriptionId, string resourceGroup, string regionName, string name, CancellationToken token)
        {
            return RunOrLoginAsync<CognitiveSearchResourceInfo>(
                () =>
                {
                    ValidateString(subscriptionId, nameof(subscriptionId));
                    ValidateString(resourceGroup, nameof(resourceGroup));
                    ValidateString(regionName, nameof(regionName));
                    ValidateString(name, nameof(name));

                    return ProcessHelpers.RunShellCommandAsync(
                        "az",
                        $"search service create" +
                        $" --output json" +
                        $" --subscription {subscriptionId}" +
                        $" -g {resourceGroup}" +
                        $" -l {regionName}" +
                        $" -n {name}" +
                        $" --sku \"Standard\"",
                        _cliEnv);
                },
                token);
        }

        Task<ClientResult> ISearchClient.DeleteAsync(string subscriptionId, string resourceGroup, string resourceName, CancellationToken token)
        {
            return RunOrLoginAsync(
            () =>
            {
                ValidateString(subscriptionId, nameof(subscriptionId));
                ValidateString(resourceGroup, nameof(resourceGroup));
                ValidateString(resourceName, nameof(resourceName));

                return ProcessHelpers.RunShellCommandAsync(
                    "az",
                    $"search service delete" +
                    $" --output json" +
                    $" --subscription {subscriptionId}" +
                    $" -g {resourceGroup}" +
                    $" -n {resourceName}" +
                    $" --yes",
                    _cliEnv);
                },
                token);
        }

        Task<ClientResult<(string, string?)>> ISearchClient.GetKeysAsync(
            string subscriptionId, string resourceGroup, string name, CancellationToken token)
        {
            return RunOrLoginAsync(
                () =>
                {
                    ValidateString(subscriptionId, nameof(subscriptionId));
                    ValidateString(resourceGroup, nameof(resourceGroup));
                    ValidateString(name, nameof(name));

                    return ProcessHelpers.RunShellCommandAsync(
                        "az",
                        $"search admin-key show" +
                        $" --output json" +
                        $" --subscription {subscriptionId}" +
                        $" -g {resourceGroup}" +
                        $" --service-name {name}",
                        _cliEnv);
                },
                json =>
                {
                    using var doc = JsonDocument.Parse(json ?? "{}");
                    return (
                        doc.GetPropertyStringOrEmpty("primaryKey"),
                        doc.GetPropertyStringOrNull("secondaryKey")
                    );
                },
                () => (string.Empty, null),
                token);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // TODO implement?
        }

        private static void ValidateString(string str, string name)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                throw new ArgumentException($"{name} is null, empty, or only whitespace");
            }
        }

        private static bool IsCognitiveServicesResourceKind(ResourceKind resourceKind) =>
            resourceKind switch
            {
                ResourceKind.AIServices => true,
                ResourceKind.CognitiveServices => true,
                ResourceKind.OpenAI => true,
                ResourceKind.Speech => true,
                ResourceKind.Vision => true,
                _ => false
            };

        private static TValue? DeserializeJson<TValue>(string? json)
            => DeserializeJson<TValue>(json, JSON_OPTIONS);

        private static TValue? DeserializeJson<TValue>(string? json, JsonSerializerOptions options)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            return JsonSerializer.Deserialize<TValue>(json, options);
        }

        private Task<ClientResult<TValue[]>> RunOrLoginArrayAsync<TValue>(
            Func<Task<ProcessOutput>> func,
            CancellationToken token,
            Func<TValue[]>? @default = null)
        {
            @default ??= Array.Empty<TValue>;

            return RunOrLoginAsync(
                func,
                DeserializeJson<TValue[]>,
                @default,
                token);
        }

        private Task<ClientResult<TValue>> RunOrLoginAsync<TValue>(
            Func<Task<ProcessOutput>> func,
            CancellationToken token)
        {
            return RunOrLoginAsync(
                func,
                DeserializeJson<TValue>,
                () => default!,
                token);
        }

        private async Task<ClientResult<TValue>> RunOrLoginAsync<TValue>(
            Func<Task<ProcessOutput>> func,
            Func<string?, TValue?> conv,
            Func<TValue> @default,
            CancellationToken token)
        {
            try
            {
                ProcessOutput cmdOut = await GetResponseOnLogin(func, token);
                if (cmdOut.HasError)
                {
                    return ClientResult.From(cmdOut, @default());
                }
                else
                {
                    return ClientResult.From(cmdOut, conv(cmdOut.StdOutput) ?? @default());
                }
            }
            catch (Exception ex)
            {
                return ClientResult.FromException(ex, @default());
            }
        }

        private async Task<ClientResult> RunOrLoginAsync(
            Func<Task<ProcessOutput>> func,
            CancellationToken token)
        {
            try
            {
                ProcessOutput cmdOut = await GetResponseOnLogin(func, token);
                return ClientResult.From(cmdOut);
            }
            catch (Exception ex)
            {
                return ClientResult.FromException(ex);
            }
        }
    }
}
