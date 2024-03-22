using Azure.AI.CLI.Clients.AzPython;
using Azure.AI.CLI.Common.Clients;
using Azure.AI.CLI.Common.Clients.Models;
using Azure.AI.Details.Common.CLI;
using Azure.AI.Details.Common.CLI.AzCli;

namespace Azure.AI.CLI.Test.UnitTests
{
    [TestClass]
    [Ignore("Until the recording test proxy has been set up, this test is not ready to run in CI/CD. You can remove this to run these tests locally")]
    public class AzCliClientIntegrationTests
    {
        const string TEST_DATA_FILE = "test_config.json";
        static TestConfig _config;
        CancellationTokenSource? _cts;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var configFile = FileHelpers.FindFileInConfigPath(TEST_DATA_FILE, null);
            if (string.IsNullOrWhiteSpace(configFile))
            {
                throw new ApplicationException($"Could not find {TEST_DATA_FILE} in any config folder. Please create that file before proceeding");
            }

            _config = Newtonsoft.Json.JsonConvert.DeserializeObject<TestConfig>(File.ReadAllText(configFile));
        }

        [TestInitialize]
        public void Setup()
        {
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _cts.CancelAfter(System.Diagnostics.Debugger.IsAttached
                ? TimeSpan.FromMinutes(15)
                : TimeSpan.FromSeconds(75));
        }

        public CancellationToken Token => _cts?.Token ?? CancellationToken.None;
        protected TestConfig Config => _config;

        [TestMethod]
        public async Task ListSubscriptionAsync()
        {
            using ISubscriptionsClient client = CreateClient();
            var result = await client.GetAllSubscriptionsAsync(Token);

            Assert.AreEqual(ClientOutcome.Success, result.Outcome, "Failed with '{0}'", result.ErrorDetails);
            Assert.IsTrue(result.Value?.Length > 0, "Null or empty subscriptions");

            var subs = result.Value.FirstOrDefault();
            Assert.IsTrue(!string.IsNullOrWhiteSpace(subs.Id), "Id was null or empty '{0}'", subs.Id);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(subs.Name), "Name was null or empty '{0}'", subs.Name);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(subs.UserName), "Username was null or empty '{0}'", subs.UserName);

            var anyDefault = result.Value.Any(s => s.IsDefault);
            Assert.IsTrue(anyDefault, "No subscriptions were marked as default");

            subs = result.Value.FirstOrDefault(s => string.Equals(s.Id, Config.SubscriptionId, StringComparison.OrdinalIgnoreCase));
            Assert.IsNotNull(subs, "Could not find the expected '{0}' subscription", Config.SubscriptionId);
            Assert.AreEqual(Config.SubscriptionName, subs.Name, "Names did not match");
        }

        [TestMethod]
        public async Task GetSubscriptionAsync()
        {
            using ISubscriptionsClient client = CreateClient();

            // Null argument
            var result = await client.GetSubscriptionAsync(null!, Token);
            HasException<ArgumentException>(result);

            // Valid subscription id
            result = await client.GetSubscriptionAsync(Config.SubscriptionId, Token);
            Assert.AreEqual(ClientOutcome.Success, result.Outcome, "Failed with '{0}'", result.ErrorDetails);
            Assert.IsNotNull(result.Value, "Null subscription info");
            Assert.AreEqual(Config.SubscriptionName, result.Value?.Name, "Name did not match");

            // Subscription that doesn't exist
            result = await client.GetSubscriptionAsync("this_does_not_exit", Token);
            Assert.AreEqual(ClientOutcome.Success, result.Outcome, "Failed with '{0}'", result.ErrorDetails);
            Assert.IsNull(result.Value, "Null subscription info");
        }

        [TestMethod]
        public async Task ListRegionsAsync()
        {
            using ISubscriptionsClient client = CreateClient();

            var result = await client.GetAllRegionsAsync(Config.SubscriptionId, Token);
            Assert.AreEqual(ClientOutcome.Success, result.Outcome, "Failed with '{0}'", result.ErrorDetails);
            Assert.IsTrue(result.Value?.Length > 0, "Null or empty regions");

            var region = result.Value.FirstOrDefault(r => r.Name == "eastus");
            Assert.IsNotNull(region, "Could not find eastus region");
            Assert.AreEqual("eastus", region.Name, "Wrong region name");
            Assert.IsTrue(!string.IsNullOrWhiteSpace(region.DisplayName), "Empty or invalid display name");
            Assert.IsTrue(!string.IsNullOrWhiteSpace(region.Id), "Empty or invalid ID");
            Assert.IsTrue(!string.IsNullOrWhiteSpace(region.RegionalDisplayName), "Empty or invalid regional display name");
        }

        [TestMethod]
        public async Task ListRegionsInvalidArgAsync()
        {
            using ISubscriptionsClient client = CreateClient();
            var result = await client.GetAllRegionsAsync(null!, Token);
            HasException<ArgumentException>(result);
        }

        [TestMethod]
        public async Task ListResourceGroupsAsync()
        {
            using ISubscriptionsClient client = CreateClient();

            var result = await client.GetAllResourceGroupsAsync(Config.SubscriptionId, Token);
            Assert.AreEqual(ClientOutcome.Success, result.Outcome, "Failed with '{0}'", result.ErrorDetails);
            Assert.IsTrue(result.Value?.Length > 0, "Null or empty regions");

            var group = result.Value.FirstOrDefault(r => string.Equals(Config.ResourceGroup, r.Name));
            Assert.IsNotNull(group, "Could not find expected '{0}' resource group", Config.ResourceGroup);
            Assert.AreEqual(Config.ResourceGroup, group.Name, "Wrong region name");
            Assert.IsTrue(!string.IsNullOrWhiteSpace(group.Id), "Empty or invalid ID");
            Assert.IsTrue(!string.IsNullOrWhiteSpace(group.Region), "Empty or invalid region");
        }

        [TestMethod]
        public async Task ListResourceGroupsInvalidArgAsync()
        {
            using ISubscriptionsClient client = CreateClient();
            var result = await client.GetAllResourceGroupsAsync(null!, Token);
            HasException<ArgumentException>(result);
        }

        [TestMethod]
        public async Task CreateAndDeleteResourceGroupAsync()
        {
            using ISubscriptionsClient client = CreateClient();
            string name = GenerateName("rg");

            try
            {
                var result = await client.CreateResourceGroupAsync(Config.SubscriptionId, Config.Region, name, Token);
                Assert.AreEqual(ClientOutcome.Success, result.Outcome, "Failed with '{0}'", result.ErrorDetails);
                Assert.IsTrue(!string.IsNullOrWhiteSpace(result.Value.Id), "Empty or invalid ID");
                Assert.AreEqual(Config.Region, result.Value.Region, "Wrong region");
                Assert.AreEqual(name, result.Value.Name, "Wrong name");
            }
            finally
            {
                var delResult = await client.DeleteResourceGroupAsync(Config.SubscriptionId, name, Token);
                Assert.AreEqual(ClientOutcome.Success, delResult.Outcome, "Wrong outcome. Details: {0}", delResult.ErrorDetails);
            }
        }

        [TestMethod]
        public async Task CreateResourceGroupInvalidAsync()
        {
            using ISubscriptionsClient client = CreateClient();
            string id = Config.SubscriptionId;
            string region = Config.Region;
            string name = "this-is-a-test";

            var args = new ValueTuple<string, string, string>[]
            {
                ("", region, name),
                (id, null!, name),
                (id, region, "  "),
            };

            foreach (var arg in args)
            {
                bool created = false;
                try
                {
                    var result = await client.CreateResourceGroupAsync(arg.Item1, arg.Item2, arg.Item3, Token);
                    created = result.IsSuccess;
                    HasException<ArgumentException>(result);
                }
                finally
                {
                    if (created)
                    {
                        await client.DeleteResourceGroupAsync(id, name, Token);
                    }
                }
            }
        }

        [TestMethod]
        public async Task DeleteResourceGroupNotExistAsync()
        {
            using ISubscriptionsClient client = CreateClient();
            var result = await client.DeleteResourceGroupAsync(Config.SubscriptionId, "this_does_not_exist" + Guid.NewGuid(), Token);
            Assert.AreEqual(ClientOutcome.Failed, result.Outcome, "Wrong outcome. Details: '{0}'", result.ErrorDetails);
        }

        [TestMethod]
        public async Task DeleteResourceGroupInvalidArgAsync()
        {
            using ISubscriptionsClient client = CreateClient();
            HasException<ArgumentException>(await client.DeleteResourceGroupAsync(null!, "name", Token));
            HasException<ArgumentException>(await client.DeleteResourceGroupAsync(Config.SubscriptionId, "  ", Token));
        }

        [TestMethod]
        public async Task ListCognitiveServicesResourcesAsync()
        {
            using ICognitiveServicesClient client = CreateClient();

            var result = await client.GetAllResourcesAsync(Config.SubscriptionId, Token);
            result.ThrowOnFail("getting all resources");
            Assert.IsTrue(result.Value?.Length > 0, "Null or empty resources");

            // All resources
            var resource = result.Value.FirstOrDefault(r => string.Equals(Config.CognitiveServicesResource, r.Name));
            Assert.IsNotNull(resource, "Could not find expected '{0}' resource", Config.CognitiveServicesResource);
            Assert.AreEqual(ResourceKind.CognitiveServices, resource.Kind, "Wrong resource kind");
            Assert.That.IsPopulatedString(resource.Id, "ID");
            Assert.AreEqual(Config.CognitiveServicesResource, resource.Name, "Wrong resource name");
            Assert.AreEqual(Config.ResourceGroup, resource.Group, "Wrong resource group");
            Assert.AreEqual(Config.Region, resource.RegionLocation, "Wrong region");
            Assert.That.IsPopulatedString(resource.Endpoint, "Endpoint");
            Assert.IsTrue(resource.Endpoints?.Count() > 0, "Empty or null endpoints");
            Assert.IsTrue(
                resource.Endpoints.Any(kvp => !string.IsNullOrWhiteSpace(kvp.Key + kvp.Value)),
                "No populated endpoints in dictionary");

            // Filtered resources
            var expectedKind = ResourceKind.AIServices;
            result = await client.GetAllResourcesAsync(Config.SubscriptionId, Token, expectedKind);
            result.ThrowOnFail("getting all resources");
            Assert.IsTrue(result.Value?.Length > 0, "Null or empty resources");
            Assert.IsFalse(
                result.Value.Any(r => r.Kind != expectedKind),
                "Wrong type of resource found in filtered list:\n  {0}",
                string.Join("\n  ", result.Value.Select(r => $"{r.Kind} - {r.Name}")));
        }

        [TestMethod]
        public async Task ListCognitiveServicesResourcesInvalidArgAsync()
        {
            using ICognitiveServicesClient client = CreateClient();
            HasException<ArgumentException>(await client.GetAllResourcesAsync(null!, Token));
        }

        [TestMethod]
        public async Task GetCognitiveServicesResourceAsync()
        {
            using ICognitiveServicesClient client = CreateClient();

            var result = await client.GetResourceFromNameAsync(Config.SubscriptionId, Config.ResourceGroup, Config.CognitiveServicesResource, Token);
            result.ThrowOnFail("getting resource");
            Assert.IsNotNull(result.Value, "Null resource info");
            Assert.AreEqual(ResourceKind.CognitiveServices, result.Value?.Kind, "Wrong resource kind");
            Assert.That.IsPopulatedString(result.Value?.Id, "ID");
            Assert.AreEqual(Config.CognitiveServicesResource, result.Value?.Name, "Wrong resource name");
            Assert.AreEqual(Config.ResourceGroup, result.Value?.Group, "Wrong resource group");
            Assert.AreEqual(Config.Region, result.Value?.RegionLocation, "Wrong region");
            Assert.That.IsPopulatedString(result.Value?.Endpoint, "Endpoint");
            Assert.IsTrue(result.Value?.Endpoints?.Count() > 0, "Empty or null endpoints");
            Assert.IsTrue(
                result.Value.Endpoints.Any(kvp => !string.IsNullOrWhiteSpace(kvp.Key + kvp.Value)),
                "No populated endpoints in dictionary");
        }

        [TestMethod]
        public async Task GetCognitiveServicesResourceInvalidArgAsync()
        {
            using ICognitiveServicesClient client = CreateClient();

            string id = Config.SubscriptionId;
            string rg = Config.ResourceGroup;
            string name = "name";

            HasException<ArgumentException>(await client.GetResourceFromNameAsync(null!, rg, name, Token));
            HasException<ArgumentException>(await client.GetResourceFromNameAsync(id, "", name, Token));
            HasException<ArgumentException>(await client.GetResourceFromNameAsync(id, rg, "   ", Token));
        }

        [TestMethod]
        public async Task GetCognitiveServicesResourceNotExistAsync()
        {
            using ICognitiveServicesClient client = CreateClient();
            var result = await client.GetResourceFromNameAsync(Config.SubscriptionId, Config.ResourceGroup, "this_does_not_exist" + Guid.NewGuid(), Token);
            Assert.AreEqual(ClientOutcome.Success, result.Outcome, "Wrong outcome. Details: '{0}'", result.ErrorDetails);
            Assert.IsNull(result.Value, "Null resource info");
        }

        [TestMethod]
        [DataRow(ResourceKind.AIServices, "S0")]
        [DataRow(ResourceKind.CognitiveServices, "S0")]
        [DataRow(ResourceKind.OpenAI, "S0")]
        [DataRow(ResourceKind.Speech, "S0")]
        [DataRow(ResourceKind.Vision, "S1")]
        public async Task CreateAndDeleteCognitiveServicesResource(ResourceKind kind, string sku)
        {
            using ICognitiveServicesClient client = CreateClient();
            string name = GenerateName("r" + Acronym(kind));

            try
            {
                var result = await client.CreateResourceAsync(kind, Config.SubscriptionId, Config.ResourceGroup, Config.Region, name, sku, Token);
                result.ThrowOnFail("creating resource");
                Assert.AreEqual(kind, result.Value.Kind, "Wrong resource kind");
                Assert.That.ContainsString(name, result.Value.Id, "ID");
                Assert.AreEqual(name, result.Value.Name, "Wrong resource name");
                Assert.AreEqual(Config.ResourceGroup, result.Value.Group, "Wrong resource group");
                Assert.AreEqual(Config.Region, result.Value.RegionLocation, "Wrong region");
                Assert.That.IsPopulatedString(result.Value.Endpoint, "Endpoint");
                Assert.IsTrue(result.Value.Endpoints?.Count() > 0, "Empty or null endpoints");
                Assert.IsTrue(
                    result.Value.Endpoints.Any(kvp => !string.IsNullOrWhiteSpace(kvp.Key + kvp.Value)),
                    "No populated endpoints in dictionary");
            }
            finally
            {
                var delResult = await client.DeleteResourceAsync(Config.SubscriptionId, Config.ResourceGroup, name, Token);
                delResult.ThrowOnFail("deleting resource");
            }
        }

        [TestMethod]
        public async Task CreateCognitiveServicesInvalidArgResource()
        {
            using ICognitiveServicesClient client = CreateClient();
            var kind = ResourceKind.AIServices;
            string id = Config.SubscriptionId;
            string rg = Config.ResourceGroup;
            string region = Config.Region;
            string name = GenerateName("r-dummy");
            string sku = "S0";

            var args = new ValueTuple<ResourceKind, string, string, string, string, string>[]
            {
                // not supported kind
                (ResourceKind.Unknown, id, rg, region, name, sku),

                // invalud string
                (kind, "", rg, region, name, sku),
                (kind, id, null!, region, name, sku),
                (kind, id, rg, "   ", name, sku),
                (kind, id, rg, region, null!, sku),
                (kind, id, rg, region, name, "")
            };

            foreach (var arg in args)
            {
                bool created = false;
                try
                {
                    var result = await client.CreateResourceAsync(arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6, Token);
                    created = result.IsSuccess;
                    HasException<ArgumentException>(result);
                }
                finally
                {
                    if (created)
                    {
                        await client.DeleteResourceAsync(id, rg, name, Token);
                    }
                }
            }
        }

        [TestMethod]
        public async Task DeleteCognitiveServicesResourceInvalidArgsAsync()
        {
            using ICognitiveServicesClient client = CreateClient();

            string id = Config.SubscriptionId;
            string rg = Config.ResourceGroup;
            string name = "name";

            HasException<ArgumentException>(await client.DeleteResourceAsync(null!, rg, name, Token));
            HasException<ArgumentException>(await client.DeleteResourceAsync(id, "", name, Token));
            HasException<ArgumentException>(await client.DeleteResourceAsync(id, rg, "  ", Token));
        }

        [TestMethod]
        public async Task DeleteCognitiveServicesResourceNotExistAsync()
        {
            using ICognitiveServicesClient client = CreateClient();
            var result = await client.DeleteResourceAsync(Config.SubscriptionId, Config.ResourceGroup, "this_does_not_exist", Token);

            // TODO FIXME: the AZ CLI (and the corresponding REST APIs) do not return a 404 when deleting a resource that doesn't exist
            // instead returning HTTP 204. This results in a "success" outcome which is inconsistent with e.g. resource groups
            Assert.AreEqual(ClientOutcome.Success, result.Outcome, "Wrong outcome. Details: '{0}'", result.ErrorDetails);
        }

        [TestMethod]
        public async Task ListDeploymentsAsync()
        {
            using ICognitiveServicesClient client = CreateClient();

            var result = await client.GetAllDeploymentsAsync(Config.SubscriptionId, Config.ResourceGroup, Config.AIServicesResource, Token);
            result.ThrowOnFail("getting all deployments");
            Assert.IsTrue(result.Value?.Length > 0, "Null or empty deployments");

            var deployment = result.Value.FirstOrDefault(r => string.Equals(Config.ChatDeployment, r.Name));
            Assert.IsNotNull(deployment, "Could not find expected '{0}' deployment", Config.ChatDeployment);
            Assert.That.ContainsString(Config.ChatDeployment, deployment.Id, "ID");
            Assert.AreEqual(Config.ChatDeployment, deployment.Name, "Wrong deployment name");
            Assert.AreEqual(Config.ResourceGroup, deployment.ResourceGroup, "Wrong deployment group");
            Assert.That.IsPopulatedString(deployment.ModelName, "Model name");
            Assert.That.IsPopulatedString(deployment.ModelFormat, "Model format");
            Assert.AreEqual(true, deployment.ChatCompletionCapable, "Chat completion capability");
            Assert.AreEqual(false, deployment.EmbeddingsCapable, "Embeddings capability");

            deployment = result.Value.FirstOrDefault(r => string.Equals(Config.EmbeddingsDeployment, r.Name));
            Assert.IsNotNull(deployment, "Could not find expected '{0}' deployment", Config.EmbeddingsDeployment);
            Assert.That.ContainsString(Config.EmbeddingsDeployment, deployment.Id, "ID");
            Assert.AreEqual(Config.EmbeddingsDeployment, deployment.Name, "Wrong deployment name");
            Assert.AreEqual(Config.ResourceGroup, deployment.ResourceGroup, "Wrong deployment group");
            Assert.That.IsPopulatedString(deployment.ModelName, "Model name");
            Assert.That.IsPopulatedString(deployment.ModelFormat, "Model format");
            Assert.AreEqual(false, deployment.ChatCompletionCapable, "Chat completion capability");
            Assert.AreEqual(true, deployment.EmbeddingsCapable, "Embeddings capability");
        }

        [TestMethod]
        public async Task ListDeploymentsInvalidArgAsync()
        {
            using ICognitiveServicesClient client = CreateClient();
            string id = Config.SubscriptionId;
            string rg = Config.ResourceGroup;
            string name = "name";

            HasException<ArgumentException>(await client.GetAllDeploymentsAsync(null!, rg, name, Token));
            HasException<ArgumentException>(await client.GetAllDeploymentsAsync(id,    "", name, Token));
            HasException<ArgumentException>(await client.GetAllDeploymentsAsync(id,    rg, "  ", Token));
        }


        [TestMethod]
        public async Task ListModelsAsync()
        {
            using ICognitiveServicesClient client = CreateClient();

            var result = await client.GetAllModelsAsync(Config.SubscriptionId, Config.Region, Token);
            result.ThrowOnFail("getting all models");
            Assert.IsTrue(result.Value?.Length > 0, "Null or empty models");
            var models = result.Value;

            var model = models.FirstOrDefault(m => m.IsChatCapable);
            Assert.That.IsNotDefault(model, "Could not find any chat capable model");
            Assert.That.IsPopulatedString(model.Name, "Name");
            Assert.That.IsPopulatedString(model.Kind, "Name");
            Assert.That.IsPopulatedString(model.Version, "Version");
            Assert.That.IsPopulatedString(model.Format, "Format");
            Assert.That.IsPopulatedString(model.SkuName, "Sku name");
            Assert.AreEqual(true, model.IsChatCapable, "Chat capability");
            Assert.AreEqual(false, model.IsEmbeddingsCapable, "Embeddings capability");
            Assert.AreEqual(false, model.IsImageCapable, "Image capability");

            model = models.FirstOrDefault(m => m.IsEmbeddingsCapable);
            Assert.That.IsNotDefault(model, "Could not find any embedding capable model");
            Assert.That.IsPopulatedString(model.Name, "Name");
            Assert.That.IsPopulatedString(model.Kind, "Name");
            Assert.That.IsPopulatedString(model.Version, "Version");
            Assert.That.IsPopulatedString(model.Format, "Format");
            Assert.That.IsPopulatedString(model.SkuName, "Sku name");
            Assert.AreEqual(false, model.IsChatCapable, "Chat capability");
            Assert.AreEqual(true, model.IsEmbeddingsCapable, "Embeddings capability");
            Assert.AreEqual(false, model.IsImageCapable, "Image capability");

            model = models.FirstOrDefault(m => m.IsImageCapable);
            Assert.That.IsNotDefault(model, "Could not find any image capable model");
            Assert.That.IsPopulatedString(model.Name, "Name");
            Assert.That.IsPopulatedString(model.Kind, "Name");
            Assert.That.IsPopulatedString(model.Version, "Version");
            Assert.That.IsPopulatedString(model.Format, "Format");
            Assert.That.IsPopulatedString(model.SkuName, "Sku name");
            Assert.AreEqual(false, model.IsChatCapable, "Chat capability");
            Assert.AreEqual(false, model.IsEmbeddingsCapable, "Embeddings capability");
            Assert.AreEqual(true, model.IsImageCapable, "Image capability");

            model = models.FirstOrDefault(m => m.DefaultCapacity > 0);
            Assert.That.IsNotDefault(model, "Could not find any model with a default capacity greater than 0");

            model = models.FirstOrDefault(m => m.IsDeprecated);
            Assert.That.IsNotDefault(model, "Could not find any model that is NOT deprecated");

            model = models.FirstOrDefault(m => m.IsDeprecated);
            Assert.That.IsNotDefault(model, "Could not find any model that is deprecated");
        }

        [TestMethod]
        public async Task ListModelsInvalidArgAsync()
        {
            using ICognitiveServicesClient client = CreateClient();
            HasException<ArgumentException>(await client.GetAllModelsAsync(null!, Config.Region, Token));
            HasException<ArgumentException>(await client.GetAllModelsAsync(Config.SubscriptionId, "", Token));
        }

        [TestMethod]
        public async Task ListModelUsageAsync()
        {
            using ICognitiveServicesClient client = CreateClient();

            var result = await client.GetAllModelUsageAsync(Config.SubscriptionId, Config.Region, Token);
            result.ThrowOnFail("getting all model usages");
            Assert.IsTrue(result.Value?.Length > 0, "Null or empty model usages");

            // Do general deserialization welfare checks 
            Assert.IsTrue(result.Value.Any(u => Math.Abs(u.CurrentValue) > 0.001), "Nothing found with valid current value");
            Assert.IsTrue(result.Value.Any(u => Math.Abs(u.Limit) > 0.001), "Nothing found with valid limit");
            Assert.IsTrue(result.Value.Any(u => !string.IsNullOrWhiteSpace(u.Unit)), "Nothing found with valid unit");
            Assert.IsTrue(result.Value.Any(u => !string.IsNullOrWhiteSpace(u.Name.LocalizedValue)), "Nothing found with valid localized name");
            Assert.IsTrue(result.Value.Any(u => !string.IsNullOrWhiteSpace(u.Name.Value)), "Nothing found with valid name value");

            // Let's see if we can find the gpt-4 tokens model usage
            var usage = result.Value.FirstOrDefault(u => u.Name.Value?.EndsWith($".gpt-4") == true && u.Name.LocalizedValue?.Contains("Token") == true);
            Assert.That.IsNotDefault(usage, "Could not find GPT4 model tokens usage");

            Assert.IsTrue(usage.CurrentValue > 0, "Current value was not greater than 0: {0}", usage.CurrentValue);
            Assert.IsTrue(usage.Limit > 0, "Limit was not greater than 0: {0}", usage.Limit);
            Assert.AreEqual("Count", usage.Unit, "Unit");
            Assert.That.IsPopulatedString(usage.Name.LocalizedValue, "Localized name");
            Assert.That.IsPopulatedString(usage.Name.Value, "Name Value");
        }

        [TestMethod]
        public async Task ListModelUsageInvalidArgAsync()
        {
            using ICognitiveServicesClient client = CreateClient();
            HasException<ArgumentException>(await client.GetAllModelUsageAsync(null!, Config.Region, Token));
            HasException<ArgumentException>(await client.GetAllModelUsageAsync(Config.SubscriptionId, " ", Token));
        }

        [TestMethod]
        [DataRow(true, DisplayName = "chat deployment")]
        [DataRow(false, DisplayName = "embeddings deployment")]
        public async Task CreateAndDeleteDeploymentAsync(bool isChat)
        {
            using ICognitiveServicesClient client = CreateClient();
            string name = GenerateName((isChat ? "chat" : "embed") + "dep");

            (string modelName, string modelVersion) = isChat
                ? (Config.ChatModelName, Config.ChatModelVersion)
                : (Config.EmbeddingsModelName, Config.EmbeddingsModelVersion);

            try
            {
                var result = await client.CreateDeploymentAsync(
                    Config.SubscriptionId,
                    Config.ResourceGroup,
                    Config.AIServicesResource,
                    name,
                    modelName,
                    modelVersion,
                    "OpenAI",
                    "1",
                    Token);
                result.ThrowOnFail("creating deployment");

                Assert.That.IsNotDefault(result.Value, "Invalid deployment info");
                Assert.That.ContainsString(name, result.Value.Id, "ID");
                Assert.AreEqual(name, result.Value.Name, "Wrong deployment name");
                Assert.AreEqual(Config.ResourceGroup, result.Value.ResourceGroup, "Wrong deployment resource group");
                Assert.That.IsPopulatedString(result.Value.ModelName, "Model name");
                Assert.That.IsPopulatedString(result.Value.ModelFormat, "Model format");
                Assert.AreEqual(isChat, result.Value.ChatCompletionCapable, "Chat completion capability");
                Assert.AreEqual(!isChat, result.Value.EmbeddingsCapable, "Embeddings capability");
            }
            finally
            {
                var delResult = await client.DeleteDeploymentAsync(Config.SubscriptionId, Config.ResourceGroup, Config.AIServicesResource, name, Token);
                delResult.ThrowOnFail("deleting deployment");
            }
        }

        [TestMethod]
        public async Task CreateDeploymentInvalidArgAsync()
        {
            using ICognitiveServicesClient client = CreateClient();
            string id = Config.SubscriptionId;
            string rg = Config.ResourceGroup;
            string res = Config.AIServicesResource;
            string name = "name";
            string model = "model";
            string version = "version";
            string format = "OpenAI";
            string cap = "1";

            var args = new ValueTuple<string, string, string, string, string, string, string, ValueTuple<string>>[]
            {
                ("", rg, res, name, model, version, format, cap),
                (id, null!, res, name, model, version, format, cap),
                (id, rg, "  ", name, model, version, format, cap),
                (id, rg, res, "", model, version, format, cap),
                (id, rg, res, name, null!, version, format, cap),
                (id, rg, res, name, model, "   ", format, cap),
                (id, rg, res, name, model, version, null!, cap),
                (id, rg, res, name, model, version, format, ""),
            };

            foreach (var arg in args)
            {
                bool created = false;
                try
                {
                    var result = await client.CreateDeploymentAsync(
                        arg.Item1, arg.Item2, arg.Item3, arg.Item4, arg.Item5, arg.Item6, arg.Item7, arg.Item8, Token);
                    created = result.IsSuccess;
                    HasException<ArgumentException>(result);
                }
                finally
                {
                    if (created)
                    {
                        await client.DeleteDeploymentAsync(id, rg, res, name, Token);
                    }
                }
            }
        }

        [TestMethod]
        public async Task DeleteDeploymentNotExistAsync()
        {
            using ICognitiveServicesClient client = CreateClient();
            var result = await client.DeleteDeploymentAsync(
                Config.SubscriptionId,
                Config.ResourceGroup,
                Config.AIServicesResource,
                "this_does_not_exist" + Guid.NewGuid(),
                Token);
            Assert.AreEqual(ClientOutcome.Success, result.Outcome, "Wrong outcome. Details: '{0}'", result.ErrorDetails);
        }

        [TestMethod]
        public async Task DeleteDeploymentInvalidArgAsync()
        {
            using ICognitiveServicesClient client = CreateClient();
            HasException<ArgumentException>(await client.DeleteDeploymentAsync(null!, Config.ResourceGroup, Config.AIServicesResource, "name", Token));
            HasException<ArgumentException>(await client.DeleteDeploymentAsync(Config.SubscriptionId, "", Config.AIServicesResource, "name", Token));
            HasException<ArgumentException>(await client.DeleteDeploymentAsync(Config.SubscriptionId, Config.ResourceGroup, "  ", "name", Token));
            HasException<ArgumentException>(await client.DeleteDeploymentAsync(Config.SubscriptionId, Config.ResourceGroup, Config.AIServicesResource, null!, Token));
        }

        [TestMethod]
        public async Task GetCognitiveServicesResourceKeyAsync()
        {
            using ICognitiveServicesClient client = CreateClient();
            var result = await client.GetResourceKeysFromNameAsync(Config.SubscriptionId, Config.ResourceGroup, Config.CognitiveServicesResource, Token);
            result.ThrowOnFail("getting resource key");
            Assert.That.IsPopulatedString(result.Value.Item1, "Key1");
        }

        [TestMethod]
        public async Task GetCognitiveServicesResourceKeyInvalidArgAsync()
        {
            using ICognitiveServicesClient client = CreateClient();
            HasException<ArgumentException>(await client.GetResourceKeysFromNameAsync(null!, Config.ResourceGroup, Config.CognitiveServicesResource, Token));
            HasException<ArgumentException>(await client.GetResourceKeysFromNameAsync(Config.SubscriptionId, "", Config.CognitiveServicesResource, Token));
            HasException<ArgumentException>(await client.GetResourceKeysFromNameAsync(Config.SubscriptionId, Config.ResourceGroup, "  ", Token));
        }

        [TestMethod]
        public async Task ListSearchResourcesAsync()
        {
            using ISearchClient client = CreateClient();

            // all
            var result = await client.GetAllAsync(Config.SubscriptionId, null, Token);
            result.ThrowOnFail("getting all search resources");
            Assert.IsTrue(result.Value?.Length > 0, "Null or empty resources");

            var resource = result.Value.FirstOrDefault(r => string.Equals(Config.SearchResource, r.Name));
            Assert.IsNotNull(resource, "Could not find expected '{0}' resource", Config.SearchResource);
            Assert.AreEqual(ResourceKind.Search, resource.Kind, "Wrong resource kind");
            Assert.That.ContainsString(Config.SearchResource, resource.Id, "ID");
            Assert.AreEqual(Config.SearchResource, resource.Name, "Wrong resource name");
            Assert.AreEqual(Config.ResourceGroup, resource.Group, "Wrong resource group");
            Assert.That.IsPopulatedString(resource.RegionLocation, "Region");
            Assert.That.IsPopulatedString(resource.Endpoint, "Endpoint");

            // only in specific region
            result = await client.GetAllAsync(Config.SubscriptionId, Config.Region, Token);
            result.ThrowOnFail($"getting '{Config.Region}' search resources");
            Assert.IsTrue(result.Value?.Length > 0, "Null or empty resources");
            Assert.IsFalse(
                result.Value.Any(r => !string.Equals(Config.Region, r.RegionLocation, StringComparison.OrdinalIgnoreCase)),
                "Found resources from other regions");
        }

        [TestMethod]
        public async Task ListSearchResourcesInvalidArgAsync()
        {
            using ISearchClient client = CreateClient();
            HasException<ArgumentException>(await client.GetAllAsync(null!, Config.Region, Token));
        }

        [TestMethod]
        public async Task GetSearchResourceAsync()
        {
            using ISearchClient client = CreateClient();

            var result = await client.GetFromNameAsync(Config.SubscriptionId, Config.ResourceGroup, Config.SearchResource, Token);
            result.ThrowOnFail("getting search resource");
            Assert.IsNotNull(result.Value, "Null resource info");
            Assert.AreEqual(ResourceKind.Search, result.Value.Kind, "Wrong resource kind");
            Assert.That.ContainsString(Config.SearchResource, result.Value.Id, "ID");
            Assert.AreEqual(Config.SearchResource, result.Value.Name, "Wrong resource name");
            Assert.AreEqual(Config.ResourceGroup, result.Value.Group, "Wrong resource group");
            Assert.That.IsPopulatedString(result.Value.Endpoint, "Endpoint");

            // TODO FIXME: Right now we are using "az resource list" with a resource type filter to get all search resources
            // which returns the short region name (e.g. eastus). Unfortunately, creating a search resource, or getting a single
            // one using "az search service show" returns the 'display' region name (e.g. East US).
            // Apply the ostrich approach liberally here and just check that the region has something set
            Assert.That.IsPopulatedString(result.Value.RegionLocation, "Region");
        }

        [TestMethod]
        public async Task GetSearchResourceInvalidArgAsync()
        {
            using ISearchClient client = CreateClient();

            string id = Config.SubscriptionId;
            string rg = Config.ResourceGroup;
            string name = "name";

            HasException<ArgumentException>(await client.GetFromNameAsync(null!, rg, name, Token));
            HasException<ArgumentException>(await client.GetFromNameAsync(id, "", name, Token));
            HasException<ArgumentException>(await client.GetFromNameAsync(id, rg, "  ", Token));
        }

        [TestMethod]
        public async Task GetSearchResourceNotExistAsync()
        {
            using ISearchClient client = CreateClient();
            var result = await client.GetFromNameAsync(
                Config.SubscriptionId,
                Config.ResourceGroup,
                "this_does_not_exist" + Guid.NewGuid(),
                Token);
            Assert.AreEqual(ClientOutcome.Success, result.Outcome, "Wrong outcome. Details: '{0}'", result.ErrorDetails);
            Assert.IsNull(result.Value, "Null resource info");
        }

        [TestMethod]
        public async Task CreateAndDeleteSearchResourceAsync()
        {
            using ISearchClient client = CreateClient();
            string name = GenerateName("sr");

            try
            {
                var result = await client.CreateAsync(Config.SubscriptionId, Config.ResourceGroup, Config.Region, name, Token);
                result.ThrowOnFail("creating search resource");
                Assert.AreEqual(ResourceKind.Search, result.Value.Kind, "Wrong resource kind");
                Assert.That.ContainsString(name, result.Value.Id, "ID");
                Assert.AreEqual(name, result.Value.Name, "Wrong resource name");
                Assert.AreEqual(Config.ResourceGroup, result.Value.Group, "Wrong resource group");
                Assert.That.IsPopulatedString(result.Value.Endpoint, "Endpoint");

                // TODO FIXME: Right now we are using "az resource list" with a resource type filter to get all search resources
                // which returns the short region name (e.g. eastus). Unfortunately, creating a search resource, or getting a single
                // one using "az search service show" returns the 'display' region name (e.g. East US).
                // Apply the ostrich approach liberally here and just check that the region has something set
                Assert.That.IsPopulatedString(result.Value.RegionLocation, "Region");
            }
            finally
            {
                var delResult = await client.DeleteAsync(Config.SubscriptionId, Config.ResourceGroup, name, Token);
                delResult.ThrowOnFail("deleting search resource");
            }
        }

        [TestMethod]
        public async Task CreateSearchResourceInvalidArg()
        {
            using ISearchClient client = CreateClient();
            string id = Config.SubscriptionId;
            string rg = Config.ResourceGroup;
            string region = Config.Region;
            string name = "this-is-a-test";

            var args = new ValueTuple<string, string, string, string>[]
            {
                ("", rg, region, name),
                (id, null!, region, name),
                (id, rg, "  ", name),
                (id, rg, region, null!),
            };

            foreach (var arg in args)
            {
                bool created = false;
                try
                {
                    var result = await client.CreateAsync(arg.Item1, arg.Item2, arg.Item3, arg.Item4, Token);
                    created = result.IsSuccess;
                    HasException<ArgumentException>(result);
                }
                finally
                {
                    if (created)
                    {
                        await client.DeleteAsync(id, rg, name, Token);
                    }
                }
            }
        }

        [TestMethod]
        public async Task DeleteSearchResourceNotExistAsync()
        {
            using ISearchClient client = CreateClient();
            var result = await client.DeleteAsync(
                Config.SubscriptionId,
                Config.ResourceGroup,
                "this_does_not_exist" + Guid.NewGuid(),
                Token);
            Assert.AreEqual(ClientOutcome.Success, result.Outcome, "Wrong outcome. Details: '{0}'", result.ErrorDetails);
        }

        [TestMethod]
        public async Task DeleteSearchResourceInvalidArgAsync()
        {
            using ISearchClient client = CreateClient();
            HasException<ArgumentException>(await client.DeleteAsync(null!, Config.ResourceGroup, "name", Token));
            HasException<ArgumentException>(await client.DeleteAsync(Config.SubscriptionId, "", "name", Token));
            HasException<ArgumentException>(await client.DeleteAsync(Config.SubscriptionId, Config.ResourceGroup, "  ", Token));
        }

        [TestMethod]
        public async Task GetSearchResourceKeyAsync()
        {
            using ISearchClient client = CreateClient();
            var result = await client.GetKeysAsync(Config.SubscriptionId, Config.ResourceGroup, Config.SearchResource, Token);
            result.ThrowOnFail("getting search resource key");
            Assert.That.IsPopulatedString(result.Value.Item1, "Key1");
        }

        #region helper methods and classes

            private AzCliClient CreateClient()
        {
            string userAgent = "ai-integration-tests";
            var login = new AzConsoleLoginManager(userAgent);
            return new AzCliClient(login, () => false, userAgent);
        }

        private static void HasException<TException>(IClientResult result, ClientOutcome expectedOutcome = ClientOutcome.Failed)
        {
            Assert.AreEqual(expectedOutcome, result.Outcome, "Wrong outcome. Details: '{0}'", result.ErrorDetails);
            Assert.IsTrue(result.Exception is TException, "Wrong type of exception. Actual: {0}", result.Exception?.GetType().FullName);
        }

        private static string GenerateName(string type)
        {
            string prefix = "azcli-integrationtest";
            string datePart = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            return $"{prefix}-{type}-{datePart}";
        }

        private static string Acronym(ResourceKind kind)
        {
            return string.Join(
                string.Empty,
                kind.ToString().Where(char.IsUpper).Select(char.ToLowerInvariant));
        }

        protected readonly struct TestConfig
        {
            public string SubscriptionId { get; init; }
            public string SubscriptionName { get; init; }
            public string ResourceGroup { get; init; }
            public string Region { get; init; }
            public string AIServicesResource { get; init; }
            public string AIHub { get; init; }
            public string AIProject { get; init; }
            public string OpenAiResource { get; init; }
            public string CognitiveServicesResource { get; init; }
            public string SpeechResource { get; init; }
            public string SearchResource { get; init; }
            public string VisionResource { get; init; }
            public string ChatDeployment { get; init; }
            public string EmbeddingsDeployment { get; init; }
            public string ChatModelName { get; init; }
            public string ChatModelVersion { get; init; }
            public string EmbeddingsModelName { get; init; }
            public string EmbeddingsModelVersion { get; init; }
        }

        #endregion
    }
}