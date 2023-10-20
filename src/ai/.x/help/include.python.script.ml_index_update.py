import argparse
import json
from azure.identity import DefaultAzureCredential
from azure.ai.generative import AIClient
from azure.ai.generative.operations._index_data_source import LocalSource, ACSOutputConfig
from azure.ai.generative.functions.build_mlindex import build_mlindex
from azure.ai.generative.entities.mlindex import MLIndex
import os

class MLIndexEncoder(json.JSONEncoder):
    def default(self, obj):
        if isinstance(obj, MLIndex):
            return {
                "name": obj.name,
                "path": obj.path,
                "version": obj.version,
                "description": obj.description,
                "tags": obj.tags,
                "properties": obj.properties
            }
        return super().default(obj)

def search_index_update(
    subscription_id : str,
    resource_group_name : str,
    project_name : str,
    index_name : str,
    embedding_model_deployment : str,
    embedding_model_name : str, 
    data_files : str, 
    external_source_url : str):

    client = AIClient(
        credential=DefaultAzureCredential(),
        subscription_id=subscription_id,
        resource_group_name=resource_group_name,
        project_name=project_name,
        user_agent="ai-cli 0.0.1"
    )

    openaiConnection = client.get_default_aoai_connection()
    openaiConnection.set_current_environment()
    searchConnection = client.connections.get("Default_CognitiveSearch")
    searchConnection.set_current_environment()

    # data_files is a string that specifies one of the following three things:
    # 1. A path to a directory containing data files (no glob, e.g. "path/to/data")
    # 2. A glob pattern that matches data files (no directory path, e.g. "*.json")
    # 3. A path and glob pattern that matches data files (e.g. "path/to/data/*.json")
    #
    # split the string into a path and glob pattern
    # if there is no path, the path will be empty
    # if there is no glob pattern, the glob pattern will "**/*"
    # if there is a glob pattern, the glob will start with "**/"
    # if there is a path and a glob pattern, the path will end with a slash
    data_files_path, data_files_glob_pattern = os.path.split(data_files)
    if data_files_path == "":
        data_files_path = "."
    if data_files_glob_pattern == "":
        data_files_glob_pattern = "**/*"
    elif not data_files_glob_pattern.startswith("**/"):
        data_files_glob_pattern = "**/" + data_files_glob_pattern
    if not data_files_path.endswith("/"):
        data_files_path = data_files_path + "/"
    print(f"Data files path: {data_files_path}")
    print(f"Data files glob pattern: {data_files_glob_pattern}")

    index = build_mlindex(
        output_index_name=index_name,
        vector_store="azure_cognitive_search",
        embeddings_model = f"azure_open_ai://deployment/{embedding_model_deployment}/model/{embedding_model_name}",
        data_source_url=external_source_url,
        index_input_config=LocalSource(input_data=f"{data_files_path}"),
        input_glob=f"{data_files_glob_pattern}",
        acs_config=ACSOutputConfig(
            acs_index_name=index_name,
        ),
    )

    return client.mlindexes.create_or_update(index)

def main():
    """Parse command line arguments and build MLIndex."""
    parser = argparse.ArgumentParser(description="Build MLIndex")
    parser.add_argument("--subscription", required=True, help="Azure subscription ID")
    parser.add_argument("--group", required=False, help="Azure resource group name")
    parser.add_argument("--project-name", required=True, help="Azure AI project project name.")
    parser.add_argument("--index-name", required=True, help="Name of the index to create.")
    parser.add_argument("--embedding-model-deployment", required=True, help="Name of the embedding model deployment.")
    parser.add_argument("--embedding-model-name", required=True, help="Name of the embedding model.")
    parser.add_argument("--data-files", required=True, help="Path to the data files.")
    parser.add_argument("--external-source-url", required=False, help="URL to the external data source.")

    args = parser.parse_args()

    subscription_id = args.subscription
    resource_group_name = args.group
    project_name = args.project_name
    index_name = args.index_name
    embedding_model_deployment = args.embedding_model_deployment
    embedding_model_name = args.embedding_model_name
    data_files = args.data_files
    external_source_url = args.external_source_url
    
    index = search_index_update(subscription_id, resource_group_name, project_name, index_name, embedding_model_deployment, embedding_model_name, data_files, external_source_url)
    formatted = json.dumps({"index": index}, indent=2, cls=MLIndexEncoder)

    print("---")
    print(formatted)

if __name__ == "__main__":
    main()
