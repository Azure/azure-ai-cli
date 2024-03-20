//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Microsoft.CognitiveServices.Speech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Azure.ResourceManager;
using Azure.Identity;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.MachineLearning;
using Azure.Core;
using Azure.ResourceManager.MachineLearning.Models;
using System.ComponentModel;
using Azure.ResourceManager.Models;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.ResourceManager.CognitiveServices;
using Azure.ResourceManager.CognitiveServices.Models;
using System.Data;

namespace Azure.AI.Details.Common.CLI
{
    public class AIClient
    {
        private static string s_currentSubscriptionId;
        private static ArmClient s_armClient;
        private static SubscriptionResource s_currentSubscription;
        private static Object s_lock = new Object();

        public static string CreateAIProject(string subscriptionId, string groupName, string resourceHubName, string projectName, string location, string displayName, string description)
        {
            SubscriptionResource subscription = null;
            ArmClient armClient = null;
            lock (s_lock)
            {
                subscription = GetSubscription(subscriptionId);
                armClient = s_armClient;
            }
            if (subscription == null)
            {
                throw new AIResourceException("Subscription not found");
            }
            ResourceGroupResource group = subscription.GetResourceGroup(groupName);
            if (group == null)
            {
                throw new AIResourceException("Resource group not found");
            }
            Azure.ResourceManager.MachineLearning.MachineLearningWorkspaceResource workspace = group.GetMachineLearningWorkspace(resourceHubName);
            if (workspace == null)
            {
                throw new AIResourceException("AI resource hub not found");
            }
            MachineLearningWorkspaceData projectData = new MachineLearningWorkspaceData(location);
            projectData.Kind = "Project";
            projectData.HubResourceId = workspace.Id;
            ManagedServiceIdentity managedServiceIdentity2 = new ManagedServiceIdentity(ManagedServiceIdentityType.SystemAssigned);
            projectData.Identity = managedServiceIdentity2;
            ArmOperation<MachineLearningWorkspaceResource> armOperation = group.GetMachineLearningWorkspaces().CreateOrUpdate(WaitUntil.Completed, projectName, projectData);
            MachineLearningWorkspaceResource project = armOperation.Value;
            AIProject aiProject = new()
            {
                name = project.Data.Name,
                location = project.Data.Location,
                id = project.Id,
                resrouce_group = group.Data.Name,
                display_name = project.Data.FriendlyName,
                workspace_hub = workspace.Id
            };
            string projectJson = JsonSerializer.Serialize(aiProject);
            return "{\"project\": " + projectJson + "}";
        }

        public static string ListAIProjects(string subscriptionId)
        {
            SubscriptionResource subscription = null;
            subscription = GetSubscription(subscriptionId);
            if (subscription == null)
            {
                throw new AIResourceException("Subscription not found");
            }
            ResourceGroupCollection groups = subscription.GetResourceGroups();
            List<AIProject> projects = new List<AIProject>();
            List<AIProject> projectsWithoutHub = new List<AIProject>();
            List<AIResourceHub> hubs = new List<AIResourceHub>();
            foreach (var group in groups)
            {
                Azure.ResourceManager.MachineLearning.MachineLearningWorkspaceCollection workspaces = group.GetMachineLearningWorkspaces();
                foreach (MachineLearningWorkspaceResource? workspace in workspaces)
                {
                    if (!string.IsNullOrEmpty(workspace.Data.Kind))
                    {
                        if (workspace.Data.Kind.ToLower() == "project")
                        {
                            AIProject project = new AIProject();
                            project.name = workspace.Data.Name;
                            project.location = workspace.Data.Location;
                            project.id = workspace.Id;
                            project.resrouce_group = group.Data.Name;
                            project.display_name = workspace.Data.FriendlyName;
                            project.workspace_hub = "";
                            if (!string.IsNullOrEmpty(project.id))
                            {
                                if (hubs.Count > 0)
                                {
                                    // If we have a previoulsy processed hub, check if it is the hub
                                    // for this project since there is a good chance for that.
                                    AIResourceHub previousHub = hubs.ElementAt(hubs.Count - 1);
                                    foreach (string projectIdInHub in previousHub.projects)
                                    {
                                        if (!string.IsNullOrEmpty(projectIdInHub))
                                        {
                                            if (projectIdInHub == project.id)
                                            {
                                                project.workspace_hub = previousHub.id;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            if (string.IsNullOrEmpty(project.workspace_hub))
                            {
                                projectsWithoutHub.Add(project);
                            }
                            else
                            {
                                projects.Add(project);
                            }
                        }
                        else if (workspace.Data.Kind.ToLower() == "hub")
                        {
                            AIResourceHub hub = new AIResourceHub();
                            hub.name = workspace.Data.Name;
                            hub.location = workspace.Data.Location;
                            hub.id = workspace.Id;
                            hub.resource_group = group.Data.Name;
                            hub.display_name = workspace.Data.FriendlyName;
                            IList<string> projectsOfHub = workspace.Data.AssociatedWorkspaces;
                            AIProject previousProject = null;
                            if (projectsWithoutHub.Count > 0)
                            {
                                previousProject = projectsWithoutHub.ElementAt(projectsWithoutHub.Count - 1);
                            }
                            foreach (string projectId in projectsOfHub)
                            {
                                hub.AddAssociatedAIProject(projectId);
                                if (previousProject != null)
                                {
                                    // Check if the previously processed project belongs to this hub.
                                    if (previousProject.id == projectId)
                                    {
                                        previousProject.workspace_hub = hub.id;
                                        projectsWithoutHub.Remove(previousProject);
                                        projects.Add(previousProject);
                                        previousProject = null;
                                    }
                                }    
                            }
                            hubs.Add(hub);
                        }
                    }
                }
            }
            // If we have projects left without assigned hub, try to find the hub for them.
            for (int i = 0; i < projectsWithoutHub.Count; i++)
            {
                 foreach (AIResourceHub hubCandidate in hubs)
                 {
                    if (hubCandidate.projects.Contains(projectsWithoutHub[i].id))
                    {
                        projectsWithoutHub[i].workspace_hub = hubCandidate.id;
                        projects.Add(projectsWithoutHub[i]);
                        projectsWithoutHub.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }
            // If we still have projects left without hub, just add them without hub.
            foreach (AIProject projectWithoutHub in projectsWithoutHub)
            {
                projects.Add(projectWithoutHub);
            }
            string projectsJson = JsonSerializer.Serialize(projects);
            return "{\"projects\": " + projectsJson + "}";
        }

        public static string DeleteAIProject(string subscriptionId, string groupName, string projectName, bool deleteDependentResources)
        {
            SubscriptionResource subscription = null;
            ArmClient armClient = null;
            lock (s_lock)
            {
                subscription = GetSubscription(subscriptionId);
                armClient = s_armClient;
            }
            if (subscription == null)
            {
                throw new AIResourceException("Subscription not found");
            }
            ResourceGroupResource group = subscription.GetResourceGroup(groupName);
            if (group == null)
            {
                throw new AIResourceException("Resource group not found");
            }
            Azure.ResourceManager.MachineLearning.MachineLearningWorkspaceResource project = group.GetMachineLearningWorkspace(projectName);
            if (project == null)
            {
                throw new AIResourceException("AI project not found");
            }
            if (string.IsNullOrEmpty(project.Data.Kind) || (project.Data.Kind.ToLower() != "project"))
            {
                throw new AIResourceException("Not an AI project");
            }
            ArmOperation deleteOperation = project.Delete(WaitUntil.Completed);
            return null;
        }

        public static string CreateAIResourceHub(string subscriptionId, string groupName, string resourceHubName, string location, string displayName, string description, string openAIResourceId = null, string openAIResourceKind = null)
        {
            SubscriptionResource subscription = null;
            ArmClient armClient = null;
            lock (s_lock)
            {
                subscription = GetSubscription(subscriptionId);
                armClient = s_armClient;
            }
            if (subscription == null)
            {
                throw new AIResourceException("Subscription not found");
            }
            ResourceGroupResource group = subscription.GetResourceGroup(groupName);
            if (group == null)
            {
                throw new AIResourceException("Resource group not found");
            }
            try
            {
                // If workspace does not exist, the next line throws an exception.
                group.GetMachineLearningWorkspace(resourceHubName);
                throw new AIResourceException("AI resource hub already exists");
            }
            catch(Exception)
            {
            }
            // Create KeyVault
            string keyVaultName = GetNameForDependentResource(resourceHubName, "keyvault");
            AzureLocation azureLocation = new AzureLocation(location);
            var tenants = armClient.GetTenants();
            if (tenants.Count() < 1)
            {
                throw new AIResourceException("No tenant available");
            }
            Guid? tenantId = tenants.ElementAt(0).Data.TenantId;
            if (tenantId == null)
            {
                throw new AIResourceException("No tenantId found");
            }
            Azure.ResourceManager.KeyVault.Models.KeyVaultSku keyVaultSku = new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard);
            Azure.ResourceManager.KeyVault.Models.KeyVaultProperties keyVaultProperties = new Azure.ResourceManager.KeyVault.Models.KeyVaultProperties((Guid)tenantId, keyVaultSku);
            KeyVaultCreateOrUpdateContent content = new KeyVaultCreateOrUpdateContent(location, keyVaultProperties);
            ArmOperation<KeyVaultResource> armOperation1 = group.GetKeyVaults().CreateOrUpdate(WaitUntil.Completed, keyVaultName, content);
            KeyVaultResource keyVault = armOperation1.Value;
            // Create StorageAccount
            string storageAccountName = GetNameForDependentResource(resourceHubName, "storage");
            StorageSku storageSku = new StorageSku(StorageSkuName.StandardLrs);
            StorageKind kind = StorageKind.Storage;
            StorageAccountCreateOrUpdateContent storageAccountCreateOrUpdateContent = new StorageAccountCreateOrUpdateContent(storageSku, StorageKind.StorageV2, location);
            ArmOperation<StorageAccountResource> armOperation2 = group.GetStorageAccounts().CreateOrUpdate(WaitUntil.Completed, storageAccountName, storageAccountCreateOrUpdateContent);
            StorageAccountResource storageAccount = armOperation2.Value;
            // Create AI resource hub
            MachineLearningWorkspaceData data = new MachineLearningWorkspaceData(azureLocation);
            data.StorageAccount = storageAccount.Id;
            data.KeyVault = keyVault.Id;
            data.Kind = "Hub";
            data.FriendlyName = displayName;
            data.Description = description;
            ManagedServiceIdentity managedServiceIdentity = new ManagedServiceIdentity(ManagedServiceIdentityType.SystemAssigned);
            data.Identity = managedServiceIdentity;
            ArmOperation<MachineLearningWorkspaceResource> armOperation3 = group.GetMachineLearningWorkspaces().CreateOrUpdate(WaitUntil.Completed, resourceHubName, data);
            Azure.ResourceManager.MachineLearning.MachineLearningWorkspaceResource workspace = armOperation3.Value;
            AIResourceHub hub = new()
            {
                name = workspace.Data.Name,
                location = workspace.Data.Location,
                id = workspace.Id,
                resource_group = group.Data.Name,
                display_name = workspace.Data.FriendlyName
            };
            if (!string.IsNullOrEmpty(openAIResourceKind))
            {
                if (openAIResourceKind == "AIServices")
                {
                    string openAIResourceGroupName = ParseResourceGroupName(openAIResourceId);
                    if (string.IsNullOrEmpty(openAIResourceGroupName))
                    {
                        throw new AIResourceException("No resource group in openAIResourceId");
                    }
                    ResourceGroupResource openAIServiceResourceGroup = subscription.GetResourceGroup(groupName);
                    if (openAIServiceResourceGroup == null)
                    {
                        throw new AIResourceException("Open AI resource group not found");
                    }
                    string openAIResourceName = ParseAccountName(openAIResourceId);
                    CognitiveServicesAccountResource csa = openAIServiceResourceGroup.GetCognitiveServicesAccount(openAIResourceName);
                    ServiceAccountApiKeys keys = csa.GetKeys();
                    // Connection creation does not currently work
                    //CreateConnection(subscriptionId, groupName, resourceHubName, "Default_AzureOpenAI", "azure_open_ai", null, openAIResourceId, keys.Key1);
                }
                else
                {
                    throw new AIResourceException("Unknown openAIResourceKind");
                }
            }
            string hubJson = JsonSerializer.Serialize(hub);
            return "{\"resource\": " + hubJson + "}";
        }

        public static string ListAIResourceHubs(string subscriptionId)
        {
            SubscriptionResource subscription = null;
            subscription = GetSubscription(subscriptionId);
            if (subscription == null)
            {
                throw new AIResourceException("Subscription not found");
            }
            ResourceGroupCollection groups = subscription.GetResourceGroups();
            List<AIResourceHub> hubs = new List<AIResourceHub>();
            foreach (var group in groups)
            {
                Azure.ResourceManager.MachineLearning.MachineLearningWorkspaceCollection workspaces = group.GetMachineLearningWorkspaces();
                foreach (MachineLearningWorkspaceResource? workspace in workspaces)
                {
                    if (!string.IsNullOrEmpty(workspace.Data.Kind))
                    {
                        if (workspace.Data.Kind.ToLower() == "hub")
                        {
                            AIResourceHub hub = new AIResourceHub();
                            hub.name = workspace.Data.Name;
                            hub.location = workspace.Data.Location;
                            hub.id = workspace.Id;
                            hub.resource_group = group.Data.Name;
                            hub.display_name = workspace.Data.FriendlyName;
                            IList<string> projectsOfHub = workspace.Data.AssociatedWorkspaces;
                            foreach (string projectId in projectsOfHub)
                            {
                                hub.AddAssociatedAIProject(projectId);
                            }
                            hubs.Add(hub);
                        }
                    }
                }
            }
            string resourceHubsJson = JsonSerializer.Serialize(hubs);
            return "{\"resources\": " + resourceHubsJson + "}";
        }

        public static string DeleteAIResourceHub(string subscriptionId, string groupName, string resourceHubName, bool deleteDependentResources)
        {
            SubscriptionResource subscription = null;
            ArmClient armClient = null;
            lock (s_lock)
            {
                subscription = GetSubscription(subscriptionId);
                armClient = s_armClient;
            }
            if (subscription == null)
            {
                throw new AIResourceException("Subscription not found");
            }
            ResourceGroupResource group = subscription.GetResourceGroup(groupName);
            if (group == null)
            {
                throw new AIResourceException("Resource group not found");
            }
            Azure.ResourceManager.MachineLearning.MachineLearningWorkspaceResource resourceHub = group.GetMachineLearningWorkspace(resourceHubName);
            if (resourceHub == null)
            {
                throw new AIResourceException("AI resource hub not found");
            }
            if (string.IsNullOrEmpty(resourceHub.Data.Kind) || (resourceHub.Data.Kind.ToLower() != "hub"))
            {
                throw new AIResourceException("Not an AI resource hub");
            }
            ArmOperation deleteOperation = resourceHub.Delete(WaitUntil.Completed);
            return null;
        }

        public static string CreateConnection(string subscriptionId, string resourceGroupName, string projectName, string connectionName, string connectionType, string cogServicesResourceKind, string resourceId, string key)
        {
            SubscriptionResource subscription = null;
            subscription = GetSubscription(subscriptionId);
            if (subscription == null)
            {
                throw new AIResourceException("Subscription not found");
            }
            ResourceGroupResource group = subscription.GetResourceGroup(resourceGroupName);
            if (group == null)
            {
                throw new AIResourceException("Resource group not found");
            }
            Azure.ResourceManager.MachineLearning.MachineLearningWorkspaceResource workspace = group.GetMachineLearningWorkspace(projectName);
            if (workspace == null)
            {
                throw new AIResourceException("AI project not found");
            }
            ApiKeyAuthWorkspaceConnectionProperties props = new ApiKeyAuthWorkspaceConnectionProperties();
            props.CredentialsKey = key;
            props.Category = GetAIConnectionCategoryFromType(connectionType);
            props.Target = resourceId;
            MachineLearningWorkspaceConnectionData data = new MachineLearningWorkspaceConnectionData(props);
            ArmOperation<MachineLearningWorkspaceConnectionResource> armOperation = workspace.GetMachineLearningWorkspaceConnections().CreateOrUpdate(WaitUntil.Completed, connectionName, data);
            MachineLearningWorkspaceConnectionResource connection = armOperation.Value;
            AIProjectConnection projectConnection = new AIProjectConnection();
            projectConnection.name = connection.Data.Name;
            string categoryValueString = "";
            MachineLearningConnectionCategory? category = connection.Data.Properties.Category;
            if (category != null)
            {
                categoryValueString = ConvertToSnakeCase(connection.Data.Properties.Category.Value.ToString());
            }
            projectConnection.type = categoryValueString;
            projectConnection.target = connection.Data.Properties.Target;
            string connectionJson = JsonSerializer.Serialize(connection);
            return "{\"connection\": " + connectionJson + "}";
        }

        public static string ListConnections(string subscriptionId, string resourceGroup, string aiProjectName)
        {
            SubscriptionResource subscription = null;
            subscription = GetSubscription(subscriptionId);
            if (subscription == null)
            {
                throw new AIResourceException("Subscription not found");
            }
            ResourceGroupResource group = subscription.GetResourceGroup(resourceGroup);
            if (group == null)
            {
                throw new AIResourceException("Resource group not found");
            }
            Azure.ResourceManager.MachineLearning.MachineLearningWorkspaceResource workspace = group.GetMachineLearningWorkspace(aiProjectName);
            if (workspace == null)
            {
                throw new AIResourceException("AI project not found");
            }
            Azure.ResourceManager.MachineLearning.MachineLearningWorkspaceConnectionCollection connections = workspace.GetMachineLearningWorkspaceConnections();
            List<AIProjectConnection> connectionList = new List<AIProjectConnection>();
            foreach (var connection in connections)
            {
                AIProjectConnection projectConnection = new AIProjectConnection();
                projectConnection.name = connection.Data.Name;
                string categoryValueString = "";
                MachineLearningConnectionCategory? category = connection.Data.Properties.Category;
                if (category != null)
                {
                    categoryValueString = ConvertToSnakeCase(connection.Data.Properties.Category.Value.ToString());
                }
                projectConnection.type = categoryValueString;
                projectConnection.target = connection.Data.Properties.Target;
                connectionList.Add(projectConnection);
            }
            string connectionsJson = JsonSerializer.Serialize(connectionList);
            return "{\"connections\": " + connectionsJson + "}";
        }

        public static string GetConnection(string subscriptionId, string resourceGroupName, string projectName, string connectionName)
        {
            SubscriptionResource subscription = null;
            subscription = GetSubscription(subscriptionId);
            if (subscription == null)
            {
                throw new AIResourceException("Subscription not found");
            }
            ResourceGroupResource group = subscription.GetResourceGroup(resourceGroupName);
            if (group == null)
            {
                throw new AIResourceException("Resource group not found");
            }
            Azure.ResourceManager.MachineLearning.MachineLearningWorkspaceResource workspace = group.GetMachineLearningWorkspace(projectName);
            if (workspace == null)
            {
                throw new AIResourceException("AI project not found");
            }
            Azure.ResourceManager.MachineLearning.MachineLearningWorkspaceConnectionResource connection = workspace.GetMachineLearningWorkspaceConnection(connectionName);
            AIProjectConnection projectConnection = new AIProjectConnection();
            projectConnection.name = connection.Data.Name;
            string categoryValueString = "";
            MachineLearningConnectionCategory? category = connection.Data.Properties.Category;
            if (category != null)
            {
                categoryValueString = ConvertToSnakeCase(connection.Data.Properties.Category.Value.ToString());
            }
            projectConnection.type = categoryValueString;
            projectConnection.target = connection.Data.Properties.Target;
            string connectionJson = JsonSerializer.Serialize(connection);
            return "{\"connection\": " + connectionJson + "}";
        }

        public static string DeleteConnection(string subscriptionId, string resourceGroupName, string projectName, string connectionName)
        {
            SubscriptionResource subscription = null;
            subscription = GetSubscription(subscriptionId);
            if (subscription == null)
            {
                throw new AIResourceException("Subscription not found");
            }
            ResourceGroupResource group = subscription.GetResourceGroup(resourceGroupName);
            if (group == null)
            {
                throw new AIResourceException("Resource group not found");
            }
            Azure.ResourceManager.MachineLearning.MachineLearningWorkspaceResource workspace = group.GetMachineLearningWorkspace(projectName);
            if (workspace == null)
            {
                throw new AIResourceException("AI project not found");
            }
            Azure.ResourceManager.MachineLearning.MachineLearningWorkspaceConnectionResource connection = workspace.GetMachineLearningWorkspaceConnection(connectionName);
            connection.Delete(WaitUntil.Completed);
            return null;
        }

        private static SubscriptionResource GetSubscription(string subscriptionId)
        {
            lock (s_lock)
            {
                bool useCurrentSubscription = false;
                if (!string.IsNullOrEmpty(s_currentSubscriptionId))
                {
                    if (s_currentSubscriptionId == subscriptionId)
                    {
                        useCurrentSubscription = true;
                    }
                }

                if (!useCurrentSubscription)
                {
                    s_currentSubscription = null;
                    s_currentSubscriptionId = subscriptionId;
                    s_armClient = new ArmClient(new DefaultAzureCredential(), subscriptionId);
                    s_currentSubscription = s_armClient.GetDefaultSubscription();
                }

                return s_currentSubscription;
            }
        }

        private static MachineLearningConnectionCategory GetAIConnectionCategoryFromType(string type)
        {
            if (type == ConvertToSnakeCase(MachineLearningConnectionCategory.CognitiveSearch.ToString()))
            {
                return MachineLearningConnectionCategory.CognitiveSearch;
            }
            else if (type == ConvertToSnakeCase(MachineLearningConnectionCategory.CognitiveService.ToString()))
            {
                return MachineLearningConnectionCategory.CognitiveService;
            }
            else if (type == ConvertToSnakeCase(MachineLearningConnectionCategory.AzureOpenAI.ToString()))
            {
                return MachineLearningConnectionCategory.AzureOpenAI;
            }
            return null;
        }

        private static string ConvertToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            string snakeCase = string.Empty;
            bool wasPreviousCharUppercase = false;

            for (int i = 0; i < input.Length; i++)
            {
                char currentChar = input[i];

                if (char.IsUpper(currentChar))
                {
                    if (!wasPreviousCharUppercase && i > 0)
                    {
                        snakeCase += "_";
                    }

                    snakeCase += char.ToLower(currentChar);
                    wasPreviousCharUppercase = true;
                }
                else
                {
                    snakeCase += currentChar;
                    wasPreviousCharUppercase = false;
                }
            }

            return snakeCase;
        }

        private static string GetNameForDependentResource(string workspaceName, string resourceType)
        {
            string alphabetsStr = string.Empty;
            foreach (char c in workspaceName.ToLower())
            {
                if (char.IsLetterOrDigit(c))
                {
                    alphabetsStr += c;
                }
            }
            string randStr = Guid.NewGuid().ToString().Replace("-", "");
            ReadOnlySpan<char> workspaceSpan = alphabetsStr.AsSpan(0, Math.Min(8, alphabetsStr.Length));
            ReadOnlySpan<char> resourceTypeSpan = resourceType.AsSpan(0, Math.Min(8, resourceType.Length));
            ReadOnlySpan<char> randStrSpan = randStr.AsSpan();
            string resourceName = string.Concat(workspaceSpan, resourceTypeSpan, randStrSpan);
#pragma warning disable IDE0057
            return resourceName.Substring(0, Math.Min(24, resourceName.Length));
#pragma warning restore IDE0057
        }

        private static string ParseResourceGroupName(string inputString)
        {
            string[] segments = inputString.Split('/');

            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == "resourceGroups" && i + 1 < segments.Length)
                {
                    return segments[i + 1];
                }
            }

            return string.Empty; // Resource group name not found  
        }

        private static string ParseAccountName(string inputString)
        {
            string[] segments = inputString.Split('/');

            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == "accounts" && i + 1 < segments.Length)
                {
                    return segments[i + 1];
                }
            }

            return string.Empty; // Account name not found  
        }
}
