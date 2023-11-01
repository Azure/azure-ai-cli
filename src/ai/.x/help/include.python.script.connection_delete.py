import argparse
import json

def delete_connection(subscription_id, resource_group_name, project_name, connection_name):

    from azure.identity import DefaultAzureCredential
    from azure.ai.resources.client import AIClient

    client = AIClient(
        credential=DefaultAzureCredential(),
        subscription_id=subscription_id,
        resource_group_name=resource_group_name,
        project_name=project_name,
        user_agent="ai-cli 0.0.1"
    )

    client.connections.delete(connection_name)

    result = {
        "name": connection_name,
        "deleted": True
    }

    print(result)
    return result

def main():
    """Parse command line arguments and delete the connection."""
    parser = argparse.ArgumentParser(description="Delete api key connection")
    parser.add_argument("--subscription", required=True, help="Azure subscription ID")
    parser.add_argument("--group", required=False, help="Azure resource group name")
    parser.add_argument("--project-name", required=True, help="Azure AI project project name.")
    parser.add_argument("--connection-name", required=True, help="Azure AI project connection name.")
    args = parser.parse_args()

    subscription_id = args.subscription
    resource_group_name = args.group
    project_name = args.project_name
    connection_name = args.connection_name

    result = delete_connection(subscription_id, resource_group_name, project_name, connection_name)
    formatted = json.dumps({"result": result}, indent=2)
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

