import sys
from typing_extensions import override
from openai import OpenAI
from openai import AssistantEventHandler

assistant_id = ""
thread_id = ""

for i, arg in enumerate(sys.argv):
    if arg == "--thread":
        thread_id = sys.argv[i + 1]
    elif arg == "--assistant":
        assistant_id = sys.argv[i + 1]

if not thread_id or not assistant_id:
    print('Usage: python run-create.py --thread <thread_id> --assistant <assistant_id>')
    sys.exit(1)
 
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
 
client = OpenAI()
with client.beta.threads.runs.stream(
  thread_id=thread_id,
  assistant_id=assistant_id,
  event_handler=EventHandler()
) as stream:
  stream.until_done()
