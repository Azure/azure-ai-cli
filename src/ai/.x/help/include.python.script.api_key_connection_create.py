import argparse
import json
import time
from datetime import datetime, timedelta
from azure.ai.resources.client import AIClient
from azure.ai.resources.entities import BaseConnection, AzureOpenAIConnection, AzureAISearchConnection, AzureAIServiceConnection
from azure.ai.ml.entities._credentials import ApiKeyConfiguration
from azure.identity import DefaultAzureCredential

def create_api_key_connection(subscription_id, resource_group_name, project_name, connection_name, connection_type, endpoint, key, api_version, kind):

    client = AIClient(
        credential=DefaultAzureCredential(),
        subscription_id=subscription_id,
        resource_group_name=resource_group_name,
        project_name=project_name,
        user_agent="ai-cli 0.0.1"
    )

    conn_class = BaseConnection._get_ai_connection_class_from_type(connection_type)
    if conn_class == BaseConnection:
        # TODO print warning, users shouldn't run into this unless dealing with odd legacy stuff.
        conn = conn_class(
            name=connection_name,
            type=connection_type,
            credentials=ApiKeyConfiguration(key=key),
            target=endpoint,
        )
    elif conn_class == AzureOpenAIConnection:
        conn = conn_class(
            name=connection_name,
            credentials=ApiKeyConfiguration(key=key),
            target=endpoint,
            api_version = api_version,
        )
    elif conn_class == AzureAISearchConnection:
        conn = conn_class(
            name=connection_name,
            credentials=ApiKeyConfiguration(key=key),
            target=endpoint,
            api_version=api_version,
        )
    elif conn_class == AzureAIServiceConnection:
        if kind is None:
            print("Error: --kind argument is required for Cognitive Service connection.")
            return {}
        conn = conn_class(
            name=connection_name,
            credentials=ApiKeyConfiguration(key=key),
            target=endpoint,
            api_version=api_version,
            kind=kind,
        )

    conn = client.connections.create_or_update(conn)
    conn2 = client.connections.get(conn.name)

    result = {
        "name": conn2.name,
        "type": conn2.type,
        "target": conn2.target,
        "credentials": conn2.credentials.values()
    }
    print(result);
    return result

def main():
    """Parse command line arguments and create api key connection."""
    parser = argparse.ArgumentParser(description="Create api key connection")
    parser.add_argument("--subscription", required=True, help="Azure subscription ID")
    parser.add_argument("--group", required=False, help="Azure resource group name")
    parser.add_argument("--project-name", required=True, help="Azure AI project project name.")
    parser.add_argument("--connection-name", required=True, help="Azure AI project connection name.")
    parser.add_argument("--connection-type", required=True, help="Azure AI project connection type. Accepted types are 'azure-open-ai', 'cognitive-search', and 'cognitive-service'.")
    parser.add_argument("--endpoint", required=True, help="Azure AI Project connection endpoint.")
    parser.add_argument("--key", required=True, help="Azure AI Project connection key.")
    parser.add_argument("--api-version", required=False, help="The expected api version of the service this connection will link to.", default="unset")
    parser.add_argument("--kind", required=False, help="Kind of AI Service being connected to. Required for Cognitive Service connections.", default=None)
    args = parser.parse_args()

    subscription_id = args.subscription
    resource_group_name = args.group
    project_name = args.project_name
    connection_name = args.connection_name
    connection_type = args.connection_type
    endpoint = args.endpoint
    key = args.key
    api_version = args.api_version
    kind = args.kind

    timeout_seconds = 10

    start_time = datetime.now()
    timeout = timedelta(seconds=timeout_seconds)
    success = False

    while datetime.now() - start_time < timeout:
        try:
            connection = create_api_key_connection(subscription_id, resource_group_name, project_name, connection_name, connection_type, endpoint, key, api_version, kind)
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
