from openai import OpenAI
client = OpenAI()

my_assistants = client.beta.assistants.list(
    order="desc",
    limit="20",
)

print("Assistants:")
for assistant in my_assistants.data:
    print(f'{assistant.name} ({assistant.id})')