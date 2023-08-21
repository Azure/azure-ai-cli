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
        )
    )

    result = ml_client.workspace_hubs.begin_create(wshub).result()
    return result._to_dict()

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
