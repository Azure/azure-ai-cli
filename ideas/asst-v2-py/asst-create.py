import sys
from openai import OpenAI

name = "My Assistant"
model = "gpt-4-turbo"
interpreter = "FALSE"
instructions = "This is a test assistant."

for i, arg in enumerate(sys.argv):
    if arg == "--name":
        name = sys.argv[i + 1]
    elif arg == "--model":
        model = sys.argv[i + 1]
    elif arg == "--interpreter":
        interpreter = sys.argv[i + 1]
    elif arg == "--instructions":
        instructions = sys.argv[i + 1]
  
client = OpenAI()
assistant = client.beta.assistants.create(
  name=name,
  instructions=instructions,
  tools=[{"type": "code_interpreter"}] if interpreter == "TRUE" else [],
  model=model
)

print(f'Assistant: {assistant.id}')
print(f'Name: {assistant.name}')
print(f'Model: {assistant.model}')

if assistant.tools:
    print(f'Tools: {assistant.tools}')

print(f'\nInstructions:\n{assistant.instructions}')
