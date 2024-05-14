import sys
import glob
from typing_extensions import override
from openai import AssistantEventHandler
from openai import OpenAI

vector_store_name = "my_vector_store"
message = "How do I do speech recognition with a custom speech recognition model?"

if len(sys.argv) < 2:
  print("Usage: python3 search-files.py FILE1 [FILE2 ...]")
  print("   OR: python3 search-files.py \"**/*.txt\"")
  sys.exit(1)

files = []
for i, arg in enumerate(sys.argv):
  if i > 0:
    pattern = sys.argv[i]
    files += glob.glob(pattern, recursive=True) if "**" in pattern \
      else glob.glob(pattern)

if not files:
  print("No files found.")
  sys.exit(0)
else:
  print(f'Found {len(files)} file(s):\n')
  for file in files:
    print(f'  {file}')

# --- Create Assistant ---
client = OpenAI()

assistant = client.beta.assistants.create(
  instructions="You are search assistant. You search documents that have been previously uploaded.",
  model="gpt-4-turbo",
  tools=[{"type": "file_search"}]
)
assistant_id = assistant.id

print(f'\n------------------')
print(f'Assistant: {assistant.id}')
print(f'Name: {assistant.name}')
print(f'Model: {assistant.model}')
if assistant.tools:
    print(f'Tools: {assistant.tools}')
print(f'\nInstructions:\n{assistant.instructions}')
print(f'------------------\n')

# --- Upload files and attach to vector store
vector_store = client.beta.vector_stores.create(name=vector_store_name)
file_streams = [open(path, "rb") for path in files]
file_batch = client.beta.vector_stores.file_batches.upload_and_poll(
  vector_store_id=vector_store.id, files=file_streams
)
print(file_batch.status)
print(file_batch.file_counts)

# --- Update the assistant to use the vector store
assistant = client.beta.assistants.update(
  assistant_id=assistant.id,
  tool_resources={"file_search": {"vector_store_ids": [vector_store.id]}},
)

# --- Create a thread and send a message
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
  def on_tool_call_created(self, tool_call):
    print(f"\nassistant > {tool_call.type}\n", flush=True)

  @override
  def on_message_done(self, message) -> None:
    # print a citation to the file searched
    message_content = message.content[0].text
    annotations = message_content.annotations
    citations = []
    for index, annotation in enumerate(annotations):
      message_content.value = message_content.value.replace(
        annotation.text, f"[{index}]"
      )
      if file_citation := getattr(annotation, "file_citation", None):
        cited_file = client.files.retrieve(file_citation.file_id)
        citations.append(f"[{index}] {cited_file.filename}")

    print(message_content.value)
    print("\n".join(citations))
 
with client.beta.threads.runs.stream(
  thread_id=thread_id,
  assistant_id=assistant_id,
  event_handler=EventHandler()
) as stream:
  stream.until_done()
