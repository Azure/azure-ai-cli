import argparse
import json

def list_hubs(subscription_id, resource_group_name):
    """List Azure AI hubs."""

    from azure.identity import DefaultAzureCredential
    from azure.ai.resources.client import AIClient
    from azure.ai.ml.constants._common import Scope

    ai_client = AIClient(
        credential=DefaultAzureCredential(),
        subscription_id=subscription_id,
        resource_group_name=resource_group_name,
        user_agent="ai-cli 0.0.1"
    )

    items = ai_client.ai_resources.list(scope=Scope.SUBSCRIPTION)
    results = []

    for item in items:
        results.append(item._workspace_hub._to_dict())

    return results

def main():
    """Parse command line arguments and print hubs."""
    parser = argparse.ArgumentParser(description="List Azure AI hubs")
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
    try:
        main()
    except Exception as e:
        import sys
        import traceback
        print("MESSAGE: " + str(sys.exc_info()[1]), file=sys.stderr)
        print("EXCEPTION: " + str(sys.exc_info()[0]), file=sys.stderr)
        print("TRACEBACK: " + "".join(traceback.format_tb(sys.exc_info()[2])), file=sys.stderr)
        sys.exit(1)

