<#@ template hostspecific="true" #>
<#@ output extension=".py" encoding="utf-8" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_API_VERSION" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
from chat_completions_custom_functions import get_current_weather_schema, get_current_weather, get_current_date_schema, get_current_date, factory
from chat_completions_functions_streaming import ChatCompletionsFunctionsStreaming
import os

def main():
    azure_api_key = os.getenv('AZURE_OPENAI_KEY', '<#= AZURE_OPENAI_KEY #>')
    endpoint = os.getenv('AZURE_OPENAI_ENDPOINT', '<#= AZURE_OPENAI_ENDPOINT #>')
    api_version = os.getenv("AZURE_OPENAI_API_VERSION") or "<#= AZURE_OPENAI_API_VERSION #>"
    deployment_name = os.getenv('AZURE_OPENAI_CHAT_DEPLOYMENT', '<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>')
    system_prompt = os.getenv('AZURE_OPENAI_SYSTEM_PROMPT', '<#= AZURE_OPENAI_SYSTEM_PROMPT #>')

    streaming_chat_completions = ChatCompletionsFunctionsStreaming(system_prompt, endpoint, azure_api_key, api_version, deployment_name, factory)

    while True:
        user_input = input('User: ')
        if user_input == 'exit' or user_input == '':
            break

        print("\nAssistant: ", end="")
        response = streaming_chat_completions.get_chat_completions(user_input, lambda content: print(content, end=""))
        print("\n")

if __name__ == '__main__':
    try:
        main()
    except Exception as e:
        print(f'The sample encountered an error: {e}')