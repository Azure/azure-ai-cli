import click
from openai_helpers import OpenAIHelpers
from openai.types.beta.assistant import Assistant

class AssistantCmds():

    @staticmethod
    def print_assistant(assistant: Assistant, prefix: str = '  ', detailed: bool = False):

        print(f'{prefix}{assistant.name} ({assistant.id})')

        if detailed:
            print(f'{prefix}Model: {assistant.model}')

            if assistant.tools:
                print(f'{prefix}Tools: {assistant.tools}')

            print(f'\n{prefix}Instructions:\n{prefix}{assistant.instructions}')

    @staticmethod
    @click.command()
    @click.option('--name', required=True)
    @click.option('--deployment', required=True)
    @click.option('--instructions')
    @click.option('--code-interpreter', type=bool, default=False)
    @click.option('--file-search', type=bool, default=False)
    @click.option('--vector-store')
    @click.option('--file')
    @click.option('--files', multiple=True)
    @click.option('--file-id')
    @click.option('--file-ids', multiple=True)
    def create(name, deployment, instructions, code_interpreter, 
                file_search, vector_store, file, files, file_id, file_ids):

        client = OpenAIHelpers.InitClient()
        print(f'\nCreating assistant...', end='')

        tools = []
        if code_interpreter:
            tools.append({"type": "code_interpreter"})
        if file_search:
            tools.append({"type": "file_search"})
        if not instructions:
            instructions = "You are a helpful AI assistant."

        assistant = client.beta.assistants.create(
            name=name,
            instructions=instructions,
            tools=tools,
            model=deployment
        )

        print(' Done.\n')
        AssistantCmds.print_assistant(assistant)

    @staticmethod
    @click.command()
    @click.option('--id', required=True)
    def delete(id):

        client = OpenAIHelpers.InitClient()
        print(f'\nDeleting assistant {id}...', end='')

        client.beta.assistants.delete(id)
        print(' Done.')

    @staticmethod
    @click.command(name='list')
    def list_cmds():

        client = OpenAIHelpers.InitClient()
        print('\nRetrieving assistants...', end='')

        assistants = client.beta.assistants.list(order="asc")
        print(' Done.\n')

        print("Assistants:")
        if not assistants.data:
            print('  No assistants found.')
        for assistant in assistants.data:
            AssistantCmds.print_assistant(assistant)

    @staticmethod
    @click.command()
    @click.option('--id', required=True)
    def get(id):

        client = OpenAIHelpers.InitClient()
        print(f'\nRetrieving assistant {id}...', end='')

        assistant = client.beta.assistants.retrieve(id)

        print(' Done.\n')
        AssistantCmds.print_assistant(assistant, detailed=True)

        return assistant
        