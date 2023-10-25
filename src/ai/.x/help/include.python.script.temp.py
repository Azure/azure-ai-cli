from azure.ai.resources.clent import AIClient
from azure.ai.resources.entities import Connection
from azure.ai.ml.entities._credentials import ApiKeyConfiguration
from azure.identity import DefaultAzureCredential
from azure.identity import AzureCliCredential

aad_credential = AzureCliCredential()

client = AIClient(
    credential=aad_credential, # DefaultAzureCredential(),
    subscription_id="e72e5254-f265-4e95-9bd2-9ee8e7329051",
    resource_group_name="robch-hub-0823-2-rg",
    project_name="robch-hub-0823-2-project", 
)

print("--before--")
connections = client.connections.list()
for conn in connections:
    print(conn.target)
    print(conn.credentials.values())

conn = Connection(
    name="Default_CognitiveSearch",
    type="cognitive_search",
    credentials=ApiKeyConfiguration(key="12345"),
    target="https://robch-cogsearch-westus2.search.windows.net",
    metadata={"Kind": "dummy", "ApiVersion": "dummy", "ApiType": "dummy"}
)

print("--creating--")
conn = client.connections.create_or_update(conn)

print("--created--")
print(conn.target)
print(conn.credentials.values())

print("--after--")
connections = client.connections.list()
for conn in connections:
    print(conn.target)
    print(conn.credentials.values())
