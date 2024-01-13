<#@ template hostspecific="true" #>
<#@ output extension=".py" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
<#@ parameter type="System.String" name="AZURE_AI_SEARCH_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_AI_SEARCH_KEY" #>
<#@ parameter type="System.String" name="AZURE_AI_SEARCH_INDEX_NAME" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_EMBEDDING_DEPLOYMENT" #>
<#@ parameter type="System.String" name="OPENAI_API_VERSION" #>
from openai_chat_completions_streaming_with_data_ai_search import <#= ClassName #>
import os

def main():
    azure_openai_key = os.getenv('AZURE_OPENAI_KEY', '<insert your OpenAI API key here>')
    azure_openai_api_version = os.getenv("OPENAI_API_VERSION", "<insert your open api version here>")
    azure_openai_deployment_name = os.getenv('AZURE_OPENAI_CHAT_DEPLOYMENT', '<insert your chat deployment here>')
    azure_openai_endpoint = os.getenv('AZURE_OPENAI_ENDPOINT', '<insert your OpenAI endpoint here>')
    system_prompt = os.getenv('AZURE_OPENAI_SYSTEM_PROMPT', '<insert your OpenAI system prompt here>')
    search_endpoint =os.getenv("AZURE_AI_SEARCH_ENDPOINT", "<insert your search endpoint here>")
    search_api_key = os.getenv("AZURE_AI_SEARCH_KEY", "<insert your search api key here>")
    search_index_name = os.getenv("AZURE_AI_SEARCH_INDEX_NAME", "<insert your search index name here>")
    embeddings_deployment = os.getenv("AZURE_OPENAI_EMBEDDING_DEPLOYMENT", "<insert your embeddings deployment here>")
    embeddings_endpoint = f"{azure_openai_endpoint.rstrip('/')}/openai/deployments/{embeddings_deployment}/embeddings?api-version={azure_openai_api_version}";

    chat = <#= ClassName #>(system_prompt, azure_openai_endpoint, azure_openai_key, azure_openai_api_version, azure_openai_deployment_name, search_endpoint, search_api_key, search_index_name, embeddings_endpoint)

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
    except Exception as e:
        print(f'The sample encountered an error: {e}')