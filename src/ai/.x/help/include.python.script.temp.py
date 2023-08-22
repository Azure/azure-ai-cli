from azure.ai.generative import AIClient
from azure.ai.generative.entities import Connection
from azure.ai.ml.entities._credentials import ApiKeyConfiguration
from azure.identity import DefaultAzureCredential
from azure.identity import AzureCliCredential

aad_credential = AzureCliCredential()

client = AIClient(
    credential=aad_credential, # DefaultAzureCredential(),
    subscription_id="e72e5254-f265-4e95-9bd2-9ee8e7329051",
    resource_group_name="robch-hub-rg-eastus",
    project_name="robch-hub-project-eastus",
)

print("--before--")
connections = client.connections.list()
for item in connections:
    print(item);

conn = Connection(
    name="my-acs-connection",
    type="cognitive_search",
    credentials=ApiKeyConfiguration(key="1234567"),
    target="my-acs-endpoint",
    metadata={"Kind": "dummy", "ApiVersion": "dummy", "ApiType": "dummy"}
)

print("--creating--")
result = client.connections.create_or_update(conn)

print("--created--")
print(result)

print("--after--")
connections = client.connections.list()
for item in connections:
    print(item);
