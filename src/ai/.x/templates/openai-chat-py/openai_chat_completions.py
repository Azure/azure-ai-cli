<#@ template hostspecific="true" #>
<#@ output extension=".py" encoding="utf-8" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_KEY" #>
<#@ parameter type="System.String" name="OPENAI_API_VERSION" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
import os
import openai

openai.api_type = "azure"
openai.api_base = os.getenv("AZURE_OPENAI_ENDPOINT") or "<#= AZURE_OPENAI_ENDPOINT #>"
openai.api_key = os.getenv("AZURE_OPENAI_KEY") or "<#= AZURE_OPENAI_KEY #>"
openai.api_version = os.getenv("OPENAI_API_VERSION") or "<#= OPENAI_API_VERSION #>"

deploymentName = os.getenv("AZURE_OPENAI_CHAT_DEPLOYMENT") or "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>"
systemPrompt = os.getenv("AZURE_OPENAI_SYSTEM_PROMPT") or "<#= AZURE_OPENAI_SYSTEM_PROMPT #>"

messages=[
    {"role": "system", "content": systemPrompt},
]

def getChatCompletions(user_input) -> str:
    messages.append({"role": "user", "content": user_input})

    response = openai.ChatCompletion.create(
        engine=deploymentName,
        messages=messages,
    )

    response_content = response["choices"][0]["message"]["content"]
    messages.append({"role": "assistant", "content": response_content})

    return response_content

while True:
    user_input = input("User: ")
    if user_input == "" or user_input == "exit":
        break

    response_content = getChatCompletions(user_input)
    print(f"\nAssistant: {response_content}\n")