<#@ template hostspecific="true" #>
<#@ output extension=".py" encoding="utf-8" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_API_VERSION" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
import os
from openai import AzureOpenAI

openai_api_version = os.getenv('AZURE_OPENAI_API_VERSION', '<#= AZURE_OPENAI_API_VERSION #>')
openai_endpoint = os.getenv('AZURE_OPENAI_ENDPOINT', '<#= AZURE_OPENAI_ENDPOINT #>')
openai_key = os.getenv('AZURE_OPENAI_KEY', '<#= AZURE_OPENAI_KEY #>')
openai_chat_deployment_name = os.getenv('AZURE_OPENAI_CHAT_DEPLOYMENT', '<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>')
openai_system_prompt = os.getenv('AZURE_OPENAI_SYSTEM_PROMPT', '<#= AZURE_OPENAI_SYSTEM_PROMPT #>')

client = AzureOpenAI(
  api_key=openai_key,
  api_version=openai_api_version,
  azure_endpoint = openai_endpoint
)

messages=[
    {'role': 'system', 'content': openai_system_prompt},
]

def get_chat_completions(user_input) -> str:
    messages.append({'role': 'user', 'content': user_input})

    response = client.chat.completions.create(
        model=openai_chat_deployment_name,
        messages=messages,
    )

    response_content = response.choices[0].message.content
    messages.append({'role': 'assistant', 'content': response_content})

    return response_content

while True:
    user_input = input('User: ')
    if user_input == 'exit' or user_input == '':
        break

    response_content = get_chat_completions(user_input)
    print(f"\nAssistant: {response_content}\n")