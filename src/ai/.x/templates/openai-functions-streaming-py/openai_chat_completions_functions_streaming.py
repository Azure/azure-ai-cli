from openai import AzureOpenAI
from function_call_context import FunctionCallContext

class OpenAIChatCompletionsFunctionsStreaming:
    def __init__(self, system_prompt, endpoint, azure_api_key, azure_api_version, deployment_name, function_factory):
        self.system_prompt = system_prompt
        self.endpoint = endpoint
        self.azure_api_key = azure_api_key
        self.azure_api_version = azure_api_version
        self.deployment_name = deployment_name
        self.function_factory = function_factory
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
        self.function_call_context = FunctionCallContext(self.function_factory, self.messages)

    def get_chat_completions(self, user_input, callback):
        self.messages.append({'role': 'user', 'content': user_input})

        complete_content = ""
        functions = self.function_factory.get_function_schemas()

        while True:
            response = self.client.chat.completions.create(
                model=self.deployment_name,
                messages=self.messages,
                stream=True,
                functions=functions,
                function_call="auto")

            for chunk in response:

                choice0 = chunk.choices[0] if hasattr(chunk, 'choices') and chunk.choices else None
                self.function_call_context.check_for_update(choice0)

                delta = choice0.delta if choice0 and hasattr(choice0, 'delta') else None
                content = delta.content if delta and hasattr(delta, 'content') else ""
                if content is None: continue

                if content is not None:
                    callback(content)
                    complete_content += content

                finish_reason = choice0.finish_reason if choice0 and hasattr(choice0, 'finish_reason') else None
                if finish_reason == "length":
                    content += f"{content}\nERROR: Exceeded max token length!"

            if self.function_call_context.try_call_function() is not None:
                self.function_call_context.clear()
                continue

            self.messages.append({"role": "assistant", "content": complete_content})
            return complete_content
