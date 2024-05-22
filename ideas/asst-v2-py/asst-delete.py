import sys
from openai import OpenAI

for i, arg in enumerate(sys.argv):
    if arg == "--id":
        id = sys.argv[i + 1]
  
client = OpenAI()
assistant = client.beta.assistants.delete(id)

print(f'Assistant: {assistant.id}')
print('\nDeleted!!')