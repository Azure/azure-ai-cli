from azure.ai.ml import MLClient
from azure.ai.ml.entities import WorkspaceHub, ManagedNetwork
from azure.identity import DefaultAzureCredential

# get a handle to the subscription
ml_client = MLClient(
    credential=DefaultAzureCredential(),
    subscription_id="e72e5254-f265-4e95-9bd2-9ee8e7329051",
    resource_group_name="my_resource_group2"
)

# create a workspace hub with auto-provisioned associated resource (storage, key vault, ACR)
wshub = WorkspaceHub(
    name="robch-demo-hub-2-westus2",
    location="westus2",
    display_name="RobCh demo hub 2 (westus2)",
    description="This example shows how to create a workspace hub.",
    public_network_access="Enabled",
    managed_network=ManagedNetwork(
        isolation_mode="Disabled" # Disabled, AllowOnlyApprovedOutbound, AllowInternetOutbound
    ),
    storage_accounts=[],
    key_vaults=[],
    container_registries=[],
    tags={
        "costcenter": "123"
    }
)

result = ml_client.workspace_hubs.begin_create(wshub).result()
print("Created workspace hub:\n{}".format(result))