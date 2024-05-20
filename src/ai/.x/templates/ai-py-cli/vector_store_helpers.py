import click
from openai_helpers import OpenAIHelpers
from openai.types.beta.vector_store import VectorStore

class VectorStoreCmds():

    @staticmethod
    def print_vector_store(vector_store: VectorStore, prefix: str ='  ', detailed: bool = False):

        print(f'{prefix}{vector_store.name} ({vector_store.id})')

        if detailed:
            if vector_store.file_counts.total:
                print(f'{prefix}Completed: {vector_store.file_counts.completed}')
                print(f'{prefix}Pending: {vector_store.file_counts.in_progress}')
                print(f'{prefix}Failed: {vector_store.file_counts.failed}')
                print(f'{prefix}Total: {vector_store.file_counts.total}')

    @staticmethod
    @click.command()
    @click.option('--name', required=True)
    @click.option('--file')
    @click.option('--files', multiple=True)
    @click.option('--file-id')
    @click.option('--file-ids', multiple=True)
    def create(name, file, files, file_id, file_ids):

        client = OpenAIHelpers.InitClient()
        print(f'\nCreating vector store...', end='')

        if file:
            stream = open(file, 'rb')
            file = client.files.create(file=stream, purpose='vector_stores')
            file_ids.append(file.id)
        if files:
            for file in files:
                stream = open(file, 'rb')
                file = client.files.create(file=stream, purpose='vector_stores')
                file_ids.append(file.id)
        if file_id:
            file_ids.append(file_id)
        if file_ids:
            file_ids.extend(file_ids)

        vector_store = client.beta.vector_stores.create(
            name=name,
            file_ids=file_ids
        )

        print(' Done.\n')
        VectorStoreCmds.print_vector_store(vector_store)

    @staticmethod
    @click.command()
    @click.option('--id', required=True)
    def delete(id):

        client = OpenAIHelpers.InitClient()
        print(f'\nDeleting vector store {id}...', end='')

        client.beta.vector_stores.delete(id)
        print(' Done.')

    @staticmethod
    @click.command(name='list')
    def list_cmds():

        client = OpenAIHelpers.InitClient()
        print(f'\nListing vector stores...', end='')

        vector_stores = client.beta.vector_stores.list(order='asc')
        print(' Done.\n')

        print('Vector Stores:')
        if not vector_stores:
            print('  No vector stores found.')
        for vector_store in vector_stores:
            VectorStoreCmds.print_vector_store(vector_store)

    @staticmethod
    @click.command()
    @click.option('--id', required=True)
    def get(id):

        client = OpenAIHelpers.InitClient()
        print(f'\nGetting vector store {id}...', end='')

        vector_store = client.beta.vector_stores.retrieve(id)
        
        print(' Done.\n')
        VectorStoreCmds.print_vector_store(vector_store, detailed=True)
