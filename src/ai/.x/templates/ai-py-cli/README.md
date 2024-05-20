# Overview

The OpenAI Python CLI provides a command-line interface for interacting with OpenAI's services. Below are the available commands for managing and interacting with assistants, vector stores, and files.

# Commands

## Assistant

* `create`: Creates an assistant with the specified options.
* `delete`: Deletes the assistant with the supplied ID.
* `get`: Retrieves the details of the assistant with the given ID.
* `list`: Lists all the assistants.

## Vector Store

* `create`: Creates a new vector store with the specified options.
* `delete`: Deletes the vector store with the given ID.
* `get`: Retrieves the details of the vector store with the specified ID.
* `list`: Lists all the vector stores.

## File

* `upload`: Uploads a file to the specified vector store.
* `delete`: Deletes the file with the supplied ID.
* `get`: Retrieves the file with the given ID.
* `list`: Lists all the files.

## Usage

To use the OpenAI Python CLI, you need to have Python installed on your system.

Here are some examples:

```
ai-py-cli.py assistant list
ai-py-cli.py assistant create --name "My Assistant"
ai-py-cli.py assistant create --name "My Assistant" --file-search
ai-py-cli.py assistant get --id "assistant-id"

ai-py-cli.py vector-store list
ai-py-cli.py vector-store create --name "My Vector Store"
ai-py-cli.py vector-store get --id "vector-store-id"

ai-py-cli.py file list
ai-py-cli.py file upload --vector-store-id "vector-store-id" --file "path/to/file"
```

For all commands, you can add --help after the command to see its usage, for example, ai-py-cli.py assistant create --help.
