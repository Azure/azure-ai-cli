<#@ template hostspecific="true" #>
<#@ output extension=".py" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_API_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_API_VERSION" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_EMBEDDING_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
<#@ parameter type="System.String" name="AZURE_AI_SEARCH_KEY" #>
<#@ parameter type="System.String" name="AZURE_AI_SEARCH_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_AI_SEARCH_INDEX_NAME" #>
from openai_chat_completions_with_data_streaming import <#= ClassName #>
import os
import sys

def main():
    openai_api_key = os.getenv('AZURE_OPENAI_API_KEY', '<#= AZURE_OPENAI_API_KEY #>')
    openai_api_version = os.getenv('AZURE_OPENAI_API_VERSION', '<#= AZURE_OPENAI_API_VERSION #>')
    openai_endpoint = os.getenv('AZURE_OPENAI_ENDPOINT', '<#= AZURE_OPENAI_ENDPOINT #>')
    openai_chat_deployment_name = os.getenv('AZURE_OPENAI_CHAT_DEPLOYMENT', '<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>')
    openai_embeddings_deployment_name = os.getenv('AZURE_OPENAI_EMBEDDING_DEPLOYMENT', '<#= AZURE_OPENAI_EMBEDDING_DEPLOYMENT #>')
    openai_embeddings_endpoint = f"{openai_endpoint.rstrip('/')}/openai/deployments/{openai_embeddings_deployment_name}/embeddings?api-version={openai_api_version}"
    openai_system_prompt = os.getenv('AZURE_OPENAI_SYSTEM_PROMPT', '<#= AZURE_OPENAI_SYSTEM_PROMPT #>')
    search_api_key = os.getenv('AZURE_AI_SEARCH_KEY', '<#= AZURE_AI_SEARCH_KEY #>')
    search_endpoint =os.getenv('AZURE_AI_SEARCH_ENDPOINT', '<#= AZURE_AI_SEARCH_ENDPOINT #>')
    search_index_name = os.getenv('AZURE_AI_SEARCH_INDEX_NAME', '<#= AZURE_AI_SEARCH_INDEX_NAME #>')

    chat = <#= ClassName #>(openai_api_version, openai_endpoint, openai_api_key, openai_chat_deployment_name, openai_system_prompt, search_endpoint, search_api_key, search_index_name, openai_embeddings_endpoint)

    while True:
        user_input = input('User: ')
        if user_input == 'exit' or user_input == '':
            break

        print("\nAssistant: ", end="")
        response = chat.get_chat_completions(user_input, lambda content: print(content, end=""))
        print("\n")

if __name__ == '__main__':
    try:
        main()
    except EOFError:
        pass
    except Exception as e:
        print(f"The sample encountered an error: {e}")
        sys.exit(1)