import argparse
import json
from azure.ai.resources.client import AIClient
from azure.identity import DefaultAzureCredential

def list_connections(subscription_id, resource_group_name, project_name):

    client = AIClient(
        credential=DefaultAzureCredential(),
        subscription_id=subscription_id,
        resource_group_name=resource_group_name,
        project_name=project_name,
        user_agent="ai-cli 0.0.1"
    )

    items = client.connections.list()
    connections = []

    for item in items:
        connection = {
            "name": item.name,
            "type": item.type,
            "target": item.target,
            "credentials": item.credentials.values()
        }
        connections.append(connection)

    return connections

def main():
    """Parse command line arguments and list connections."""
    parser = argparse.ArgumentParser(description="List connection")
    parser.add_argument("--subscription", required=True, help="Azure subscription ID")
    parser.add_argument("--group", required=False, help="Azure resource group name")
    parser.add_argument("--project-name", required=True, help="Azure AI project project name.")
    args = parser.parse_args()

    subscription_id = args.subscription
    resource_group_name = args.group
    project_name = args.project_name

    connections = list_connections(subscription_id, resource_group_name, project_name)
    formatted = json.dumps({"connections": connections}, indent=2)

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

