import argparse
import json
from azure.ai.generative import AIClient
from azure.identity import DefaultAzureCredential
from azure.ai.ml.constants._common import Scope

def list_projects(subscription_id, resource_group_name):
    """List Azure ML projects."""
    ai_client = AIClient(
        credential=DefaultAzureCredential(),
        subscription_id=subscription_id,
        resource_group_name=resource_group_name,
        user_agent="ai-cli 0.0.1"
    )

    items = ai_client.projects.list(scope=Scope.SUBSCRIPTION)
    results = []

    for item in items:
        results.append(item._workspace._to_dict())

    return results

def main():
    """Parse command line arguments and print projects."""
    parser = argparse.ArgumentParser(description="List Azure ML projects")
    parser.add_argument("--subscription", required=True, help="Azure subscription ID")
    parser.add_argument("--group", required=False, help="Azure resource group name")
    args = parser.parse_args()

    subscription_id = args.subscription
    resource_group_name = args.group

    projects = list_projects(subscription_id, resource_group_name)
    formatted = json.dumps({"projects": projects}, indent=2)

    print("---")
    print(formatted)

if __name__ == "__main__":
    main()
