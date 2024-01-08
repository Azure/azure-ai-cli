<#@ template hostspecific="true" #>
<#@ output extension=".py" encoding="utf-8" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_API_VERSION" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
import os
from openai import AzureOpenAI

api_key = os.getenv("AZURE_OPENAI_KEY") or "<#= AZURE_OPENAI_KEY #>"
endpoint = os.getenv("AZURE_OPENAI_ENDPOINT") or "<#= AZURE_OPENAI_ENDPOINT #>"
api_version = os.getenv("AZURE_OPENAI_API_VERSION") or "<#= AZURE_OPENAI_API_VERSION #>"
deploymentName = os.getenv("AZURE_OPENAI_CHAT_DEPLOYMENT") or "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>"
systemPrompt = os.getenv("AZURE_OPENAI_SYSTEM_PROMPT") or "<#= AZURE_OPENAI_SYSTEM_PROMPT #>"

client = AzureOpenAI(
  api_key=api_key,
  api_version=api_version,
  azure_endpoint = endpoint
)

messages=[
    {"role": "system", "content": systemPrompt},
]

def getChatCompletions(user_input) -> str:
    messages.append({"role": "user", "content": user_input})

    response = client.chat.completions.create(
        model=deploymentName,
        messages=messages,
    )

    response_content = response.choices[0].message.content
    messages.append({"role": "assistant", "content": response_content})

    return response_content

while True:
    user_input = input("User: ")
    if user_input == "" or user_input == "exit":
        break

    response_content = getChatCompletions(user_input)
    print(f"\nAssistant: {response_content}\n")