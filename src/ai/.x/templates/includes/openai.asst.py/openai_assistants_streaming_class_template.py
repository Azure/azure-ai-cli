{{if {_IS_OPENAI_ASST_STREAMING_TEMPLATE}}}
from typing_extensions import override
from openai import AssistantEventHandler

class EventHandler(AssistantEventHandler):

    {{if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}
    def __init__(self, function_factory, openai, callback):
        super().__init__()
        self.function_factory = function_factory
        self.openai = openai
        self.callback = callback
    {{else}}
    def __init__(self, openai, callback):
        super().__init__()
        self.openai = openai
        self.callback = callback
    {{endif}}

    @override
    def on_text_delta(self, delta, snapshot):
        {{if {_IS_OPENAI_ASST_FILE_SEARCH_TEMPLATE}}}
        content = delta.value
        if delta.annotations:
            for annotation in delta.annotations:
                content = content.replace(annotation.text, f"[{annotation.index}]")
        self.callback(content)
        {{else}}
        self.callback(delta.value)
        {{endif}}

    {{if {_IS_OPENAI_ASST_FILE_SEARCH_TEMPLATE}}}
    @override
    def on_message_done(self, message) -> None:
        message_content = message.content[0].text
        annotations = message_content.annotations
        citations = []
        for index, annotation in enumerate(annotations):
            if file_citation := getattr(annotation, "file_citation", None):
                cited_file = self.openai.files.retrieve(file_citation.file_id)
                citations.append(f"[{index}] {cited_file.filename}")
        if citations:
            print("\n\n" + "\n".join(citations), end="", flush=True)
    {{endif}}

    {{if {_IS_OPENAI_ASST_CODE_INTERPRETER_TEMPLATE}}}
    def on_tool_call_created(self, tool_call):
        if tool_call.type == 'code_interpreter':
            print('\n\nassistant-code:\n', end='', flush=True) 
    
    def on_tool_call_delta(self, delta, snapshot):
        if delta.type == 'code_interpreter':
            if delta.code_interpreter.input:
                print(delta.code_interpreter.input, end='', flush=True)
            if delta.code_interpreter.outputs:
                print(f'\n\nassistant-output:', end='', flush=True)
                for output in delta.code_interpreter.outputs:
                    if output.type == 'logs':
                        print(f'\n{output.logs}', flush=True)

    {{endif}}
    @override
    def on_event(self, event):
        {{if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}
        if event.event == 'thread.run.requires_action':
            run_id = event.data.id
            self.handle_requires_action(event.data, run_id)
        elif event.event == 'thread.run.failed':
        {{else}}
        if event.event == 'thread.run.failed':
        {{endif}}
            print(event)
            raise Exception('Run failed')
        super().on_event(event)

    {{if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}
    def handle_requires_action(self, data, run_id):
        tool_outputs = []

        tool_calls = data.required_action.submit_tool_outputs.tool_calls
        if tool_calls != None:
            tool_outputs = self.get_tool_outputs(tool_calls)

        self.submit_tool_outputs(tool_outputs, run_id)

    def get_tool_outputs(self, tool_calls):
        tool_outputs = []
        for tool_call in tool_calls:
            if tool_call.type == 'function':
                result = self.function_factory.try_call_function(tool_call.function.name, tool_call.function.arguments)
                tool_outputs.append({ 'output': result, 'tool_call_id': tool_call.id })
        return tool_outputs

    def submit_tool_outputs(self, tool_outputs, run_id):
        with self.openai.beta.threads.runs.submit_tool_outputs_stream(
            thread_id=self.current_run.thread_id,
            run_id=self.current_run.id,
            tool_outputs=tool_outputs,
            event_handler=EventHandler(self.function_factory, self.openai, self.callback),
        ) as stream:
            stream.until_done()

    {{endif}}
{{endif}}
class {ClassName}:

    {{if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}
    def __init__(self, assistant_id, function_factory, openai):
    {{else}}
    def __init__(self, assistant_id, openai):
    {{endif}}
        self.assistant_id = assistant_id
        {{if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}
        self.function_factory = function_factory
        {{endif}}
        self.thread = None
        self.openai = openai

    def create_thread(self):
        self.thread = self.openai.beta.threads.create()
        return self.thread
    
    def retrieve_thread(self, thread_id):
        self.thread = self.openai.beta.threads.retrieve(thread_id)
        return self.thread
    
    def get_thread_messages(self, callback):
        messages = self.openai.beta.threads.messages.list(self.thread.id)
        messages.data.reverse()

        for message in messages.data:
            content = ''.join([item.text.value for item in message.content]) + '\n\n'
            callback(message.role, content)

    {{if {_IS_OPENAI_ASST_STREAMING_TEMPLATE}}}
    def get_response(self, user_input, callback) -> None:
    {{else}}
    def get_response(self, user_input) -> str:
    {{endif}}
        if self.thread == None:
            self.create_thread()

        message = self.openai.beta.threads.messages.create(
            thread_id=self.thread.id,
            role="user",
            content=user_input,
        )

        {{if {_IS_OPENAI_ASST_STREAMING_TEMPLATE}}}
        with self.openai.beta.threads.runs.stream(
            thread_id=self.thread.id,
            assistant_id=self.assistant_id,
            {{if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}
            tools=self.function_factory.get_tools(),
            event_handler=EventHandler(self.function_factory, self.openai, callback)
            {{else}}
            event_handler=EventHandler(self.openai, callback)
            {{endif}}
        ) as stream:
            stream.until_done()
        {{else}}
        run = self.openai.beta.threads.runs.create_and_poll(
            thread_id=self.thread.id,
            assistant_id=self.assistant_id
        )

        if run.status == 'completed': 
            messages = self.openai.beta.threads.messages.list(thread_id=self.thread.id)
            return ''.join([item.text.value for item in messages.data[0].content])

        return str(run.status)
        {{endif}}
