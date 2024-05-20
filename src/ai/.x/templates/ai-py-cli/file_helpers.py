import click
from openai_helpers import OpenAIHelpers
from openai.types.file_object import FileObject


class FileCmds():

    @staticmethod
    def print_file(file: FileObject, prefix: str = '  ', detailed: bool = False):

        print(f'{prefix}{file.filename} ({file.id})')

        if detailed:
            print(f'{prefix}{str(file)}')

    @staticmethod
    @click.command()
    @click.option('--file', required=True)
    @click.option('--vector-store')
    def upload(file, vector_store):
            
        client = OpenAIHelpers.InitClient()
        print(f'\nUploading file {file}...', end='')

        stream = open(file, 'rb')
        file = client.files.create(file=stream, purpose='assistants')

        print(' Done.\n')
        FileCmds.print_file(file)

    @staticmethod
    @click.command()
    @click.option('--id', required=True)
    def delete(id):
        
        client = OpenAIHelpers.InitClient()
        print(f'\nDeleting file {id}...', end='')

        client.files.delete(id)
        print(' Done.')

    @staticmethod
    @click.command(name='list')
    def list_cmds():

        client = OpenAIHelpers.InitClient()
        print(f'\nListing files...', end='')

        files = client.files.list()
        print(' Done.\n')

        print('Files:')
        if not files:
            print('  No files found.')
        for file in files:
            FileCmds.print_file(file)

    @staticmethod
    @click.command()
    @click.option('--id', required=True)
    def get(id):

        client = OpenAIHelpers.InitClient()
        print(f'\nGetting file {id}...', end='')

        file = client.files.retrieve(id)
        print(' Done.\n')

        FileCmds.print_file(file, detailed=True)
