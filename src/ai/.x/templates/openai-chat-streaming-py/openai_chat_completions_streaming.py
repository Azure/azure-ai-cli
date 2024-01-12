from openai import AzureOpenAI

class OpenAIChatCompletionsStreaming:
    def __init__(self, system_prompt, endpoint, azure_api_key, azure_api_version, deployment_name):
        self.system_prompt = system_prompt
        self.endpoint = endpoint
        self.azure_api_key = azure_api_key
        self.azure_api_version = azure_api_version
        self.deployment_name = deployment_name
        self.client = AzureOpenAI(
            api_key=self.azure_api_key,
            api_version=self.azure_api_version,
            azure_endpoint = endpoint
            )
        self.clear_conversation()

    def clear_conversation(self):
        self.messages = [
            {'role': 'system', 'content': self.system_prompt}
        ]

    def get_chat_completions(self, user_input, callback):
        self.messages.append({'role': 'user', 'content': user_input})

        complete_content = ""
        response = self.client.chat.completions.create(
            model=self.deployment_name,
            messages=self.messages,
            stream=True)

        for chunk in response:

            choice0 = chunk.choices[0] if hasattr(chunk, 'choices') and chunk.choices else None
            delta = choice0.delta if choice0 and hasattr(choice0, 'delta') else None

            content = delta.content if delta and hasattr(delta, 'content') else ""
            if content is None: continue

            if content is not None:
                callback(content)
                complete_content += content

            finish_reason = choice0.finish_reason if choice0 and hasattr(choice0, 'finish_reason') else None
            if finish_reason == "length":
                content += f"{content}\nERROR: Exceeded max token length!"

        self.messages.append({"role": "assistant", "content": complete_content})
        return complete_content
