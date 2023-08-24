import argparse
import json
from azure.ai.ml import MLClient
from azure.identity import DefaultAzureCredential

def delete_hub(subscription_id, resource_group_name, resource_name, delete_dependent_resources):
    """Delete Azure ML hubs."""
    ml_client = MLClient(
        credential=DefaultAzureCredential(),
        subscription_id=subscription_id,
        resource_group_name=resource_group_name,
        user_agent="ai-cli 0.0.1"
    )

    result = ml_client.workspace_hubs.begin_delete(resource_name, delete_dependent_resources=delete_dependent_resources).result()
    return result

def main():
    """Parse command line arguments and delete's the hub."""
    parser = argparse.ArgumentParser(description="Delete Azure ML hub")
    parser.add_argument("--subscription", required=True, help="Azure subscription ID")
    parser.add_argument("--group", required=True, help="Azure resource group name")
    parser.add_argument("--name", required=True, help="Azure resource hub name")
    parser.add_argument("--delete-dependent-resources", required=True, help="Delete resources associated with the hub")
    args = parser.parse_args()

    subscription_id = args.subscription
    resource_group_name = args.group
    resource_name = args.name;
    delete_dependent_resources = args.delete_dependent_resources

    result = delete_hub(subscription_id, resource_group_name, resource_name, delete_dependent_resources)
    formatted = json.dumps(result, indent=2)

    print("---")
    print(formatted)

if __name__ == "__main__":
    main()
