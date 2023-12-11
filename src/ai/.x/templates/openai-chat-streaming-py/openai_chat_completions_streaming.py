<#@ template hostspecific="true" #>
<#@ output extension=".py" encoding="utf-8" #>
<#@ parameter type="System.String" name="OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="OPENAI_API_KEY" #>
<#@ parameter type="System.String" name="OPENAI_API_VERSION" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
import os
import openai

openai.api_type = "azure"
openai.api_base = os.getenv("OPENAI_ENDPOINT") or "<#= OPENAI_ENDPOINT #>"
openai.api_key = os.getenv("OPENAI_API_KEY") or "<#= OPENAI_API_KEY #>"
openai.api_version = os.getenv("OPENAI_API_VERSION") or "<#= 2023-12-01 #>"

deploymentName = os.getenv("AZURE_OPENAI_CHAT_DEPLOYMENT") or "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>"
systemPrompt = os.getenv("AZURE_OPENAI_SYSTEM_PROMPT") or "<#= AZURE_OPENAI_SYSTEM_PROMPT #>"

messages=[
    {"role": "system", "content": systemPrompt},
]

def getChatStreamingCompletions() -> str:
    messages.append({"role": "user", "content": userPrompt})

    response_content = ""
    response = openai.ChatCompletion.create(
        engine=deploymentName,
        messages=messages,
        stream=True)

    for update in response:

        choices = update["choices"] if "choices" in update else []
        choice0 = choices[0] if len(choices) > 0 else {}
        delta = choice0["delta"] if "delta" in choice0 else {}

        content = delta["content"] if "content" in delta else ""
        response_content += content
        print(content, end="")

        finish_reason = choice0["finish_reason"] if "finish_reason" in choice0 else ""
        if finish_reason == "length":
            content += f"{content}\nERROR: Exceeded max token length!"

    messages.append({"role": "assistant", "content": response_content})
    return response_content

while True:
    userPrompt = input("User: ")
    if userPrompt == "" or userPrompt == "exit":
        break

    print("\nAssistant: ", end="")
    response_content = getChatStreamingCompletions()
    print("\n")
