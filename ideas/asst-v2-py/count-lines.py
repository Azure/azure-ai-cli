import sys
from typing_extensions import override
from openai import AssistantEventHandler
from openai import OpenAI

if len(sys.argv) != 2:
  print("Usage: python3 count-lines.py <file_name>")
  sys.exit(1)

file_name = sys.argv[1]
message = "Count the number of lines in the file."

client = OpenAI()

file = client.files.create(
  file=open(file_name, "rb"),
  purpose='assistants'
)

assistant = client.beta.assistants.create(
  instructions="You are a file assistant. You can do things with the file specified.",
  model="gpt-4-turbo",
  tools=[{"type": "code_interpreter"}],
  tool_resources={
    "code_interpreter": {
      "file_ids": [file.id]
    }
  }
)
assistant_id = assistant.id

thread = client.beta.threads.create()
thread_id = thread.id

message = client.beta.threads.messages.create(
  thread_id=thread_id,
  role="user",
  content=message
)

class EventHandler(AssistantEventHandler):    
  @override
  def on_text_created(self, text) -> None:
    print(f"\nassistant > ", end="", flush=True)
      
  @override
  def on_text_delta(self, delta, snapshot):
    print(delta.value, end="", flush=True)
      
  def on_tool_call_created(self, tool_call):
    print(f"\nassistant > {tool_call.type}\n", flush=True)
  
  def on_tool_call_delta(self, delta, snapshot):
    if delta.type == 'code_interpreter':
      if delta.code_interpreter.input:
        print(delta.code_interpreter.input, end="", flush=True)
      if delta.code_interpreter.outputs:
        print(f"\n\noutput >", flush=True)
        for output in delta.code_interpreter.outputs:
          if output.type == "logs":
            print(f"\n{output.logs}", flush=True)
 
with client.beta.threads.runs.stream(
  thread_id=thread_id,
  assistant_id=assistant_id,
  event_handler=EventHandler()
) as stream:
  stream.until_done()
