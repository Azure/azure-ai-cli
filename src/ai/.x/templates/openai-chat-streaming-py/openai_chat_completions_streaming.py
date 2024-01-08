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

def getChatStreamingCompletions(user_input) -> str:
    messages.append({"role": "user", "content": user_input})

    complete_content = ""
    stream = client.chat.completions.create(
        model=deploymentName,
        messages=messages,
        stream=True,
    )

    for chunk in stream:

        choice0 = chunk.choices[0] if hasattr(chunk, 'choices') and chunk.choices else None
        delta = choice0.delta if choice0 and hasattr(choice0, 'delta') else None

        content = delta.content if delta and hasattr(delta, 'content') else ""
        if content is None: continue

        complete_content += content
        print(content, end="")

        finish_reason = choice0.finish_reason if choice0 and hasattr(choice0, 'finish_reason') else None
        if finish_reason == "length":
            content += f"{content}\nERROR: Exceeded max token length!"

    messages.append({"role": "assistant", "content": complete_content})
    return complete_content

while True:
    user_input = input("User: ")
    if user_input == "" or user_input == "exit":
        break

    print("\nAssistant: ", end="")
    response_content = getChatStreamingCompletions(user_input)
    print("\n")
