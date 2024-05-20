import click
from assistant_helpers import AssistantCmds
from vector_store_helpers import VectorStoreCmds
from file_helpers import FileCmds
from init_helpers import InitCmds

@click.group()
def cli():
    pass

@click.group(name='init')
def init():
    pass

init.add_command(InitCmds.file_search)

@click.group(name='assistant')
def assistant():
    pass

assistant.add_command(AssistantCmds.create)
assistant.add_command(AssistantCmds.delete)
assistant.add_command(AssistantCmds.list_cmds)
assistant.add_command(AssistantCmds.get)
cli.add_command(assistant)

@click.group(name='vector-store')
def vector_store():
    pass

vector_store.add_command(VectorStoreCmds.create)
vector_store.add_command(VectorStoreCmds.delete)
vector_store.add_command(VectorStoreCmds.list_cmds)
vector_store.add_command(VectorStoreCmds.get)
cli.add_command(vector_store)

@click.group(name='file')
def file():
    pass

file.add_command(FileCmds.upload)
file.add_command(FileCmds.delete)
file.add_command(FileCmds.list_cmds)
file.add_command(FileCmds.get)
cli.add_command(file)

if __name__ == '__main__':
    cli()
