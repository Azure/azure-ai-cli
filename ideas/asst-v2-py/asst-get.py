import sys
from openai import OpenAI

if len(sys.argv) < 2:
    print('Usage: python asst-get.py <assistant_id>')
    sys.exit(1)

assistant_id = sys.argv[1]

client = OpenAI()
assistant = client.beta.assistants.retrieve(assistant_id)

print(f'Assistant: {assistant.id}')
print(f'Name: {assistant.name}')
print(f'Model: {assistant.model}')

if assistant.tools:
    print(f'Tools: {assistant.tools}')

print(f'\nInstructions:\n{assistant.instructions}')
