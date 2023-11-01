import argparse
import json

def delete_hub(subscription_id, resource_group_name, ai_resource_name, delete_dependent_resources):
    """Delete Azure AI hubs."""

    from azure.identity import DefaultAzureCredential
    from azure.ai.resources.client import AIClient

    ai_client = AIClient(
        credential=DefaultAzureCredential(),
        subscription_id=subscription_id,
        resource_group_name=resource_group_name,
        user_agent="ai-cli 0.0.1"
    )

    result = ai_client.ai_resources.begin_delete(name=ai_resource_name, delete_dependent_resources=delete_dependent_resources).result()
    return result

def main():
    """Parse command line arguments and delete's the hub."""
    parser = argparse.ArgumentParser(description="Delete Azure AI hub")
    parser.add_argument("--subscription", required=True, help="Azure subscription ID")
    parser.add_argument("--group", required=True, help="Azure resource group name")
    parser.add_argument("--name", required=True, help="Azure resource hub name")
    parser.add_argument("--delete-dependent-resources", required=True, help="Delete resources associated with the hub")
    args = parser.parse_args()

    subscription_id = args.subscription
    resource_group_name = args.group
    ai_resource_name = args.name;
    delete_dependent_resources = args.delete_dependent_resources

    result = delete_hub(subscription_id, resource_group_name, ai_resource_name, delete_dependent_resources)
    formatted = json.dumps(result, indent=2)

    print("---")
    print(formatted)

if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        import sys
        import traceback
        print("MESSAGE: " + str(sys.exc_info()[1]), file=sys.stderr)
        print("EXCEPTION: " + str(sys.exc_info()[0]), file=sys.stderr)
        print("TRACEBACK: " + "".join(traceback.format_tb(sys.exc_info()[2])), file=sys.stderr)
        sys.exit(1)

