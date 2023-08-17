import argparse
import json
from azure.ai.ml import MLClient
from azure.ai.ml.entities import WorkspaceHub, ManagedNetwork, WorkspaceHubConfig
from azure.identity import DefaultAzureCredential

def create_hub(subscription_id, resource_group_name, resource_name, location, display_name, description):
    """Create Azure ML hub."""
    ml_client = MLClient(
        credential=DefaultAzureCredential(),
        subscription_id=subscription_id,
        resource_group_name=resource_group_name
    )

    wshub = WorkspaceHub(
        name=resource_name,
        location=location,
        display_name=display_name,
        description=description,
        public_network_access="Enabled",
        managed_network=ManagedNetwork(
            isolation_mode="Disabled" # Disabled, AllowOnlyApprovedOutbound, AllowInternetOutbound
        ),
        # storage_account="/subscriptions/640a8878-8722-42d8-8527-bd4cabbe0d4d/resourceGroups/test/providers/Microsoft.Storage/storageAccounts/wsintestcluste7585074041",
        # key_vault="/subscriptions/640a8878-8722-42d8-8527-bd4cabbe0d4d/resourceGroups/test/providers/Microsoft.Keyvault/vaults/wsintestcluste0798560852",
        # container_registry="/subscriptions/4bf6b28a-452b-4af4-8080-8a196ee0ca4b/resourceGroups/azureml-rg-df3c88f9-907c-4b17-89a1_34ea597f-d69c-4d70-92da-87400e100a2a/providers/Microsoft.ContainerRegistry/registries/000db7fc81f",
        # tags={
        #     "costcenter": "123"
        # }
        # workspace_hub_config(additional_workspace_storage_accounts=[])
    )

    result = ml_client.workspace_hubs.begin_create(wshub).result()
    return { "id": result.id }

def main():
    """Parse command line arguments and print created hub."""
    parser = argparse.ArgumentParser(description="Create Azure ML hub")
    parser.add_argument("--subscription", required=True, help="Azure subscription ID")
    parser.add_argument("--group", required=True, help="Azure resource group name")
    parser.add_argument("--name", required=True, help="Azure AI resource display name. This is non-unique within the resource group.")
    parser.add_argument("--location", required=True, help="The location in which to create the AI resource.")
    parser.add_argument("--display-name", required=False, help="Display name for the AI resource. This is non-unique within the resource group.")
    parser.add_argument("--description", required=False, help="Description of the AI resource.")
    args = parser.parse_args()

    subscription_id = args.subscription
    resource_group_name = args.group
    resource_name = args.name
    location = args.location
    display_name = args.display_name
    description = args.description

    hub = create_hub(subscription_id, resource_group_name, resource_name, location, display_name, description)
    formatted = json.dumps({"hub": hub}, indent=2)

    print("---")
    print(formatted)

if __name__ == "__main__":
    main()
