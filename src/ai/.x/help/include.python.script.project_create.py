import argparse
import json

def create_project(subscription_id, resource_id, resource_group_name, project_name, location, display_name, description):
    """Create Azure AI project."""

    from azure.identity import DefaultAzureCredential
    from azure.ai.resources.client import AIClient
    from azure.ai.resources.entities import Project

    ai_client = AIClient(
        credential=DefaultAzureCredential(),
        subscription_id=subscription_id,
        resource_group_name=resource_group_name,
        user_agent="ai-cli 0.0.1"
    )

    project = Project(
        name=project_name,
        location=location,
        display_name=display_name,
        description=description,
        ai_resource=resource_id,
    )

    result = ai_client.projects.begin_create(project=project).result()
    return result._workspace._to_dict()

def main():
    """Parse command line arguments and print created project."""
    parser = argparse.ArgumentParser(description="Create Azure AI project")
    parser.add_argument("--subscription", required=True, help="Azure subscription ID")
    parser.add_argument("--resource-id", required=True, help="Azure AI hub ID")
    parser.add_argument("--group", required=True, help="Azure resource group name")
    parser.add_argument("--name", required=True, help="Azure AI project display name. This is non-unique within the resource group.")
    parser.add_argument("--location", required=True, help="The location in which to create the AI project.")
    parser.add_argument("--display-name", required=False, help="Display name for the AI project. This is non-unique within the resource group.")
    parser.add_argument("--description", required=False, help="Description of the AI project.")
    args = parser.parse_args()

    subscription_id = args.subscription
    resource_id = args.resource_id;
    resource_group_name = args.group
    project_name = args.name
    location = args.location
    display_name = args.display_name
    description = args.description

    project = create_project(subscription_id, resource_id, resource_group_name, project_name, location, display_name, description)
    formatted = json.dumps({"project": project}, indent=2)

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

