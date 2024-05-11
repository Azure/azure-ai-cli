import sys
from openai import OpenAI

if len(sys.argv) != 2:
    print("Usage: python files-get.py <file_id>")
    sys.exit(1)

file_id = sys.argv[1]

client = OpenAI()
file_data = client.files.content(file_id)
file_data_bytes = file_data.read()

with open(file_id, "wb") as file:
    file.write(file_data_bytes)