import os
import sys
import glob
from openai.types.beta.threads import Text, TextDelta
from typing_extensions import override
from openai import AssistantEventHandler
from openai import OpenAI

message = 'using only the uploaded documents, how do you create an assistant using the OpenAI API?'
default_vector_store_name='my_vector_store'

ASSISTANT_ID = os.getenv('ASSISTANT_ID', None)
VECTOR_STORE_ID = os.getenv('VECTOR_STORE_ID', None)
VECTOR_STORE_NAME = os.getenv('VECTOR_STORE_NAME', default_vector_store_name)
DEBUG = os.getenv('DEBUG', None)

if len(sys.argv) < 2:
  print("Usage: python3 search-files.py FILE1 [FILE2 ...]")
  print("   OR: python3 search-files.py \"**/*.txt\"")
  sys.exit(1)

import logging
if DEBUG:
  if DEBUG.startswith('file:'):
    logging.basicConfig(filename=DEBUG[5:], level=logging.DEBUG, format='%(asctime)s - %(levelname)s - %(message)s')
  else:
    logging.basicConfig(level=logging.DEBUG, format='%(asctime)s - %(levelname)s - %(message)s')

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

#-----------------------
# NOTE: Never deploy your API Key in client-side environments like browsers or mobile apps
# SEE: https://help.openai.com/en/articles/5112595-best-practices-for-api-key-safety

# Get the required environment variables, and form the base URL for Azure OpenAI Assistants API
AZURE_OPENAI_API_KEY = os.getenv('AZURE_OPENAI_API_KEY', '<insert your Azure OpenAI API key here>')
AZURE_OPENAI_API_VERSION = os.getenv('AZURE_OPENAI_API_VERSION', '<insert your Azure OpenAI API version here>')
AZURE_OPENAI_ENDPOINT = os.getenv('AZURE_OPENAI_ENDPOINT', '<insert your Azure OpenAI endpoint here>')
AZURE_OPENAI_CHAT_DEPLOYMENT = os.getenv('AZURE_OPENAI_CHAT_DEPLOYMENT', '<insert your Azure OpenAI chat deployment here>')
AZURE_OPENAI_BASE_URL = f'{AZURE_OPENAI_ENDPOINT.rstrip("/")}/openai'

# Get the required environment variables, and form the base URL for OpenAI Platform API
OPENAI_API_KEY = os.getenv('OPENAI_API_KEY', '<insert your OpenAI API key here>')
OPENAI_MODEL_NAME = os.getenv('OPENAI_MODEL_NAME', '<insert your OpenAI model name here>')
OPENAI_ORG_ID = os.getenv('OPENAI_ORG_ID', None)

# Check if the required environment variables are set
azureOk = \
  AZURE_OPENAI_API_KEY != None and not AZURE_OPENAI_API_KEY.startswith('<insert') and \
  AZURE_OPENAI_API_VERSION != None and not AZURE_OPENAI_API_VERSION.startswith('<insert') and \
  AZURE_OPENAI_CHAT_DEPLOYMENT != None and not AZURE_OPENAI_CHAT_DEPLOYMENT.startswith('<insert') and \
  AZURE_OPENAI_ENDPOINT != None and not AZURE_OPENAI_ENDPOINT.startswith('<insert')
oaiOk = \
  OPENAI_API_KEY != None and not OPENAI_API_KEY.startswith('<insert') and \
  OPENAI_MODEL_NAME != None and not OPENAI_MODEL_NAME.startswith('<insert')
ok = azureOk or oaiOk

if not ok:
    print('To use OpenAI, set the following environment variables:\n' +
        '\n  ASSISTANT_ID' +
        '\n  OPENAI_API_KEY' +
        '\n  OPENAI_MODEL_NAME' +
        '\n  OPENAI_ORG_ID (optional)' +
        '\n  VECTOR_STORE_ID (optional)')
    print('\nYou can easily obtain some of these values by visiting these links:\n' +
        '\n  https://platform.openai.com/api-keys' +
        '\n  https://platform.openai.com/settings/organization/general' +
        '\n  https://platform.openai.com/playground/assistants' +
        '\n' +
        '\n Then, do one of the following:\n' +
        '\n  ai dev shell' +
        '\n  python main.py' +
        '\n' +
        '\n  or' +
        '\n' +
        '\n  ai dev shell --run "python main.py"');
    os._exit(1)

if not ok:
    print('To use Azure OpenAI, set the following environment variables:\n' +
        '\n  ASSISTANT_ID' +
        '\n  AZURE_OPENAI_API_KEY' +
        '\n  AZURE_OPENAI_API_VERSION' +
        '\n  AZURE_OPENAI_CHAT_DEPLOYMENT' +
        '\n  AZURE_OPENAI_ENDPOINT')
    print('\nYou can easily do that using the Azure AI CLI by doing one of the following:\n' +
      '\n  ai init' +
      '\n  ai dev shell' +
      '\n  python main.py' +
      '\n' +
      '\n  or' +
      '\n' +
      '\n  ai init' +
      '\n  ai dev shell --run "python main.py"')
    os._exit(1)

# Create the OpenAI client
if azureOk:
  print('\nUsing Azure OpenAI (w/ API Key)...')
  client = OpenAI(
      api_key = AZURE_OPENAI_API_KEY,
      base_url = AZURE_OPENAI_BASE_URL,
      default_query= { 'api-version': AZURE_OPENAI_API_VERSION },
      default_headers = { 'api-key': AZURE_OPENAI_API_KEY }
  )
else:
  print('\nUsing OpenAI...')
  client = OpenAI(
      api_key = OPENAI_API_KEY,
      organization = OPENAI_ORG_ID
  )

# --- Get or create Assistant ---
if ASSISTANT_ID is None:
  print(f'\nCreating assistant...')
  assistant = client.beta.assistants.create(
    instructions="You are search assistant. You search documents that have been previously uploaded.",
    model=AZURE_OPENAI_CHAT_DEPLOYMENT if azureOk else OPENAI_MODEL_NAME,
    tools=[{"type": "file_search"}]
  )
else:
  print(f'\nRetrieving assistant...')
  assistant = client.beta.assistants.retrieve(ASSISTANT_ID)

assistant_id = assistant.id
print(f'------------------')
print(f'Assistant: {assistant.id}')
print(f'Name: {assistant.name}')
print(f'Model: {assistant.model}')
if assistant.tools:
    print(f'Tools: {assistant.tools}')
print(f'\nInstructions:\n{assistant.instructions}')
print(f'------------------\n')

# --- Get or create Vector Store ---
if VECTOR_STORE_ID is None:
  has_vector_store = assistant.tool_resources and \
    "file_search" in assistant.tool_resources and \
    "vector_store_ids" in assistant.tool_resources["file_search"]
  if has_vector_store:
    print(f'\nRetrieving vector store...')
    vector_store = client.beta.vector_stores.retrieve(assistant.tool_resources["file_search"]["vector_store_ids"][0])
  else:
    print(f'\nCreating vector store...')
    vector_store = client.beta.vector_stores.create(name=VECTOR_STORE_NAME)
else:
  has_vector_store = True
  print(f'\nRetrieving vector store...')
  vector_store = client.beta.vector_stores.retrieve(VECTOR_STORE_ID)

vector_store_id = vector_store.id
print(f'------------------')
print(f'Vector Store: {vector_store.id}')
print(f'Name: {vector_store.name}')
print(f'------------------\n')

# --- Upload files to Vector Store ---
file_streams = [open(path, "rb") for path in files]
if file_streams:
  print(f'\nUploading files to vector store...')
  file_batch = client.beta.vector_stores.file_batches.upload_and_poll(
    vector_store_id=vector_store.id, files=file_streams
  )
  print(f'------------------')
  print(f'File Batch: {file_batch.id}')
  print(f'Status: {file_batch.status}')
  print(f'File(s): {file_batch.file_counts}')

# --- Update the assistant to use the vector store
needs_update = not has_vector_store
if needs_update:
  print(f'\nUpdating assistant...')
  assistant = client.beta.assistants.update(
    assistant_id=assistant.id,
    tool_resources={"file_search": {"vector_store_ids": [vector_store.id]}},
  )
  print(f'------------------')
  print(f'Assistant: {assistant.id}')
  print(f'Name: {assistant.name}')
  print(f'Model: {assistant.model}')
  if assistant.tools:
    print(f'Tools: {assistant.tools}')
  print(f'\nInstructions:\n{assistant.instructions}')
  print(f'------------------\n')

# --- Create a thread and send a message
print(f'\nCreating thread...')
thread = client.beta.threads.create()
thread_id = thread.id

print(f'\nSending message...\n\nuser: {message}\n')
message = client.beta.threads.messages.create(
  thread_id=thread_id,
  role="user",
  content=message
)

class EventHandler(AssistantEventHandler):

  @override
  def on_event(self, event) -> None:
      logging.debug(f"\n-----\nEVENT > {event}\n-----")
      super().on_event(event)

  @override
  def on_text_created(self, text) -> None:
    print(f"\nassistant > ", end="", flush=True)

  @override
  def on_text_delta(self, delta, snapshot):
    content = delta.value
    if delta.annotations:
      for annotation in delta.annotations:
        content = content.replace(annotation.text, f"[{annotation.index}]")
    print(content, end="", flush=True)

  @override
  def on_tool_call_created(self, tool_call):
    print(f"\nassistant > {tool_call.type}\n", flush=True)

  @override
  def on_message_done(self, message) -> None:
    message_content = message.content[0].text
    annotations = message_content.annotations
    citations = []
    for index, annotation in enumerate(annotations):
      if file_citation := getattr(annotation, "file_citation", None):
        cited_file = client.files.retrieve(file_citation.file_id)
        citations.append(f"[{index}] {cited_file.filename}")
    print("\n\n" + "\n".join(citations))

# --- Stream the assistant's responses ---
print('\nAssistant: ', end='', flush=True) 
with client.beta.threads.runs.stream(
  thread_id=thread_id,
  assistant_id=assistant_id,
  event_handler=EventHandler()
) as stream:
  stream.until_done()
