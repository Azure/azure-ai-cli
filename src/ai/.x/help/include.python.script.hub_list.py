import argparse
import json
from azure.ai.ml import MLClient
from azure.identity import DefaultAzureCredential

def list_workspace_hubs(subscription_id, resource_group_name):
    """List Azure ML workspace hubs."""
    ml_client = MLClient(
        credential=DefaultAzureCredential(),
        subscription_id=subscription_id,
        resource_group_name=resource_group_name
    )

    hub_list = ml_client.workspace_hubs.list()
    items = []

    for hub in hub_list:
        fields = {
            "id": hub.id,
            "group": hub.resource_group,
            "location": hub.location,
            "display_name": hub.display_name,
            "name": hub.name
        }
        items.append(fields)

    return items

def main():
    """Parse command line arguments and print workspace hubs."""
    parser = argparse.ArgumentParser(description="List Azure ML workspace hubs")
    parser.add_argument("--subscription", required=True, help="Azure subscription ID")
    parser.add_argument("--group", required=True, help="Azure resource group name")
    args = parser.parse_args()

    subscription_id = args.subscription
    resource_group_name = args.group

    workspace_hubs = list_workspace_hubs(subscription_id, resource_group_name)

    # Convert the list of hubs to JSON format
    hubs_json = json.dumps({"workspace_hubs": workspace_hubs}, indent=2)

    print("---")
    print(hubs_json)

if __name__ == "__main__":
    main()
