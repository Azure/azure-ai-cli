import argparse
import json
from azure.ai.ml import MLClient
from azure.ai.ml.entities import Workspace
from azure.identity import DefaultAzureCredential

def create_project(subscription_id, resource_id, resource_group_name, project_name, location, display_name, description, openai_resource_id):
    """Create Azure ML project."""
    ml_client = MLClient(
        credential=DefaultAzureCredential(),
        subscription_id=subscription_id,
        resource_group_name=resource_group_name,
        user_agent="ai-cli 0.0.1"
    )

    project = Workspace(
        name=project_name,
        location=location,
        display_name=display_name,
        description=description,
        workspace_hub=resource_id
    )

    result = ml_client.workspaces.begin_create(project, byo_open_ai_resource_id=openai_resource_id).result()
    return result._to_dict()

def main():
    """Parse command line arguments and print created project."""
    parser = argparse.ArgumentParser(description="Create Azure ML project")
    parser.add_argument("--subscription", required=True, help="Azure subscription ID")
    parser.add_argument("--resource-id", required=True, help="Azure AI resource ID")
    parser.add_argument("--group", required=True, help="Azure resource group name")
    parser.add_argument("--name", required=True, help="Azure AI project display name. This is non-unique within the resource group.")
    parser.add_argument("--location", required=True, help="The location in which to create the AI project.")
    parser.add_argument("--display-name", required=False, help="Display name for the AI project. This is non-unique within the resource group.")
    parser.add_argument("--description", required=False, help="Description of the AI project.")
    parser.add_argument("--openai-resource-id", required=False, help="OpenAI resource id to use.")
    args = parser.parse_args()

    subscription_id = args.subscription
    resource_id = args.resource_id;
    resource_group_name = args.group
    project_name = args.name
    location = args.location
    display_name = args.display_name
    description = args.description
    openai_resource_id = args.openai_resource_id

    project = create_project(subscription_id, resource_id, resource_group_name, project_name, location, display_name, description, openai_resource_id)
    formatted = json.dumps({"project": project}, indent=2)

    print("---")
    print(formatted)

if __name__ == "__main__":
    main()
