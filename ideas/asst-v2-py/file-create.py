import sys
from openai import OpenAI

if len(sys.argv) != 2:
    print("Usage: python files-create.py <file_name>")
    sys.exit(1)

file_name = sys.argv[1]

client = OpenAI()
file = client.files.create(
  file=open(file_name, "rb"),
  purpose='assistants'
)

print(f'File: {file.id}')
