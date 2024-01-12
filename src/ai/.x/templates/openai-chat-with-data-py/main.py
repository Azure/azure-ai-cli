<#@ template hostspecific="true" #>
<#@ output extension=".py" encoding="utf-8" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_API_VERSION" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
from openai_chat_completions_with_data_streaming import <#= ClassName #>
import os

def main():
    azure_api_key = os.getenv('AZURE_OPENAI_KEY', '<#= AZURE_OPENAI_KEY #>')
    endpoint = os.getenv('AZURE_OPENAI_ENDPOINT', '<#= AZURE_OPENAI_ENDPOINT #>')
    api_version = os.getenv("AZURE_OPENAI_API_VERSION") or "<#= AZURE_OPENAI_API_VERSION #>"
    deployment_name = os.getenv('AZURE_OPENAI_CHAT_DEPLOYMENT', '<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>')
    system_prompt = os.getenv('AZURE_OPENAI_SYSTEM_PROMPT', '<#= AZURE_OPENAI_SYSTEM_PROMPT #>')

    chat = <#= ClassName #>(system_prompt, endpoint, azure_api_key, api_version, deployment_name)

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