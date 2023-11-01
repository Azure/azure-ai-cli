import argparse
import json
from azure.ai.resources.client import AIClient
from azure.ai.resources.entities import AIResource
from azure.ai.ml.entities import ManagedNetwork
from azure.identity import DefaultAzureCredential

def create_hub(subscription_id, resource_group_name, ai_resource_name, location, display_name, description):
    """Create Azure AI hub."""
    ai_client = AIClient(
        credential=DefaultAzureCredential(),
        subscription_id=subscription_id,
        resource_group_name=resource_group_name,
        user_agent="ai-cli 0.0.1"
    )

    resource = AIResource(
        name=ai_resource_name,
        location=location,
        display_name=display_name,
        description=description,
        public_network_access="Enabled",
        managed_network=ManagedNetwork(
            isolation_mode="Disabled" # Disabled, AllowOnlyApprovedOutbound, AllowInternetOutbound
        )
    )

    # TODO allow setting of optional bool update_dependent_resources?
    result = ai_client.ai_resources.begin_create(ai_resource=resource).result()
    return result._workspace_hub._to_dict()

def main():
    """Parse command line arguments and print created hub."""
    parser = argparse.ArgumentParser(description="Create Azure AI hub")
    parser.add_argument("--subscription", required=True, help="Azure subscription ID")
    parser.add_argument("--group", required=True, help="Azure resource group name")
    parser.add_argument("--name", required=True, help="Azure AI resource display name. This is non-unique within the resource group.")
    parser.add_argument("--location", required=True, help="The location in which to create the AI resource.")
    parser.add_argument("--display-name", required=False, help="Display name for the AI resource. This is non-unique within the resource group.")
    parser.add_argument("--description", required=False, help="Description of the AI resource.")
    args = parser.parse_args()

    subscription_id = args.subscription
    resource_group_name = args.group
    ai_resource_name = args.name
    location = args.location
    display_name = args.display_name
    description = args.description

    hub = create_hub(subscription_id, resource_group_name, ai_resource_name, location, display_name, description)
    formatted = json.dumps({"hub": hub}, indent=2)

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

