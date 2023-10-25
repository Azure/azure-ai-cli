import argparse
import json
import time
from datetime import datetime, timedelta
from azure.ai.resources .clent import AIClient
from azure.ai.resources.entities import Connection
from azure.ai.ml.entities._credentials import ApiKeyConfiguration
from azure.identity import DefaultAzureCredential

def get_api_key_connection(subscription_id, resource_group_name, project_name, connection_name):

    client = AIClient(
        credential=DefaultAzureCredential(),
        subscription_id=subscription_id,
        resource_group_name=resource_group_name,
        project_name=project_name,
        user_agent="ai-cli 0.0.1"
    )

    conn = client.connections.get(connection_name)

    result = {
        "name": conn.name,
        "type": conn.type,
        "target": conn.target,
        "credentials": conn.credentials.values()
    }

    print(result);
    return result

def main():
    """Parse command line arguments and get the api key connection."""
    parser = argparse.ArgumentParser(description="Get api key connection")
    parser.add_argument("--subscription", required=True, help="Azure subscription ID")
    parser.add_argument("--group", required=False, help="Azure resource group name")
    parser.add_argument("--project-name", required=True, help="Azure AI project project name.")
    parser.add_argument("--connection-name", required=True, help="Azure AI project connection name.")
    args = parser.parse_args()

    subscription_id = args.subscription
    resource_group_name = args.group
    project_name = args.project_name
    connection_name = args.connection_name

    timeout_seconds = 10

    start_time = datetime.now()
    timeout = timedelta(seconds=timeout_seconds)
    success = False

    while datetime.now() - start_time < timeout:
        try:
            connection = get_api_key_connection(subscription_id, resource_group_name, project_name, connection_name)
            if connection is not None:
                success = True
                break
        except Exception as e:
            print("An error occurred:", str(e))
        
        time.sleep(1)  # Wait for 1 second before the next attempt
    
    if success:
        formatted = json.dumps({"connection": connection}, indent=2)
        print("---")
        print(formatted)

if __name__ == "__main__":
    main()
