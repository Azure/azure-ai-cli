<#@ template hostspecific="true" #>
<#@ output extension=".py" encoding="utf-8" #>
<#@ parameter type="System.String" name="OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="OPENAI_API_KEY" #>
<#@ parameter type="System.String" name="OPENAI_API_VERSION" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
import openai
from function_factory import FunctionFactory
from function_call_context import FunctionCallContext

class ChatCompletionsFunctionsStreaming:
    def __init__(self, system_prompt, endpoint, azure_api_key, deployment_name, function_factory=None):
        self.system_prompt = system_prompt
        self.endpoint = endpoint
        self.azure_api_key = azure_api_key
        self.deployment_name = deployment_name
        self.function_factory = function_factory or FunctionFactory()
        self.clear_conversation()

    def clear_conversation(self):
        self.messages = [
            {'role': 'system', 'content': self.system_prompt}
        ]
        self.function_call_context = FunctionCallContext(self.function_factory, self.messages)

    def get_chat_completions(self, user_input, callback):
        self.messages.append({'role': 'user', 'content': user_input})

        response_content = ""
        functions = self.function_factory.get_function_schemas()

        while True:
            response = openai.ChatCompletion.create(
                engine=self.deployment_name,
                messages=self.messages,
                stream=True,
                functions=functions,
                function_call="auto")

            for update in response:

                choices = update["choices"] if "choices" in update else []
                choice0 = choices[0] if len(choices) > 0 else {}
                self.function_call_context.check_for_update(choice0)

                delta = choice0["delta"] if "delta" in choice0 else {}
                content = delta["content"] if "content" in delta else ""

                if content is not None:
                    callback(content)
                    response_content += content

                finish_reason = choice0["finish_reason"] if "finish_reason" in choice0 else ""
                if finish_reason == "length":
                    content += f"{content}\nERROR: Exceeded max token length!"

            if self.function_call_context.try_call_function() is not None:
                self.function_call_context.clear()
                continue

            self.messages.append({"role": "assistant", "content": response_content})
            return response_content
