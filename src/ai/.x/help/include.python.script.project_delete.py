import argparse
import json
from azure.ai.generative import AIClient
from azure.identity import DefaultAzureCredential

def delete_project(subscription_id, resource_group_name, project_name, delete_dependent_resources):
    """Delete Azure ML projects."""
    ai_client = AIClient(
        credential=DefaultAzureCredential(),
        subscription_id=subscription_id,
        resource_group_name=resource_group_name,
        user_agent="ai-cli 0.0.1"
    )
    # TODO should we allow assigning optional permanently_delete bool? 
    result = ai_client.projects.begin_delete(name=project_name, delete_dependent_resources=delete_dependent_resources).result()
    return result

def main():
    """Parse command line arguments and delete's the project."""
    parser = argparse.ArgumentParser(description="Delete Azure ML project")
    parser.add_argument("--subscription", required=True, help="Azure subscription ID")
    parser.add_argument("--group", required=True, help="Azure resource group name")
    parser.add_argument("--name", required=True, help="Azure resource project name")
    parser.add_argument("--delete-dependent-resources", required=True, help="Delete resources associated with the project")
    args = parser.parse_args()

    subscription_id = args.subscription
    resource_group_name = args.group
    project_name = args.name;
    delete_dependent_resources = args.delete_dependent_resources

    result = delete_project(subscription_id, resource_group_name, project_name, delete_dependent_resources)
    formatted = json.dumps(result, indent=2)

    print("---")
    print(formatted)

if __name__ == "__main__":
    main()
