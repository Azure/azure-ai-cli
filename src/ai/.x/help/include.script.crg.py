from azure.identity import DefaultAzureCredential
from azure.mgmt.resource import ResourceManagementClient

subscription_id = 'e72e5254-f265-4e95-9bd2-9ee8e7329051'
credentials = DefaultAzureCredential()

resource_client = ResourceManagementClient(credentials, subscription_id)

resource_group_name = 'my_resource_group2'
location = 'westus2'

resource_group_params = {'location': location}
resource_group = resource_client.resource_groups.create_or_update(resource_group_name, resource_group_params)

print(f"Resource group '{resource_group.name}' created in location '{resource_group.location}'.")
