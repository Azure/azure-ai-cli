import argparse
import json
from azure.ai.ml import MLClient
from azure.identity import DefaultAzureCredential

def list_hubs(subscription_id, resource_group_name):
    """List Azure ML hubs."""
    ml_client = MLClient(
        credential=DefaultAzureCredential(),
        subscription_id=subscription_id,
        resource_group_name=resource_group_name,
        user_agent="ai-cli 0.0.1"
    )

    items = ml_client.workspace_hubs.list(scope="subscription")
    results = []

    for item in items:
        results.append(item._to_dict())

    return results

def main():
    """Parse command line arguments and print hubs."""
    parser = argparse.ArgumentParser(description="List Azure ML hubs")
    parser.add_argument("--subscription", required=True, help="Azure subscription ID")
    parser.add_argument("--group", required=False, help="Azure resource group name")
    args = parser.parse_args()

    subscription_id = args.subscription
    resource_group_name = args.group

    hubs = list_hubs(subscription_id, resource_group_name)
    formatted = json.dumps({"resources": hubs}, indent=2)

    print("---")
    print(formatted)

if __name__ == "__main__":
    main()
