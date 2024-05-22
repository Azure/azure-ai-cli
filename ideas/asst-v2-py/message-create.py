import sys
from openai import OpenAI

thread_id = ""
message = "Tell me a joke"

for i, arg in enumerate(sys.argv):
    if arg == "--thread":
        thread_id = sys.argv[i + 1]
    elif arg == "--content":
        message = sys.argv[i + 1]

if not thread_id or not message:
    print('Usage: python message-create.py --thread <thread_id> --content <message>')
    sys.exit(1)

client = OpenAI()
message = client.beta.threads.messages.create(
  thread_id=thread_id,
  role="user",
  content=message
)

print(f'Thread: {message.thread_id}')
print(f'Message: {message.id}')

print(f'\nContent: {message.content[0].text.value}')
print(f'Role: {message.role}')