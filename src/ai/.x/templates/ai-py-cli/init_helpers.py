import click
from assistant_helpers import AssistantCmds

class InitCmds():

    @staticmethod
    @click.command()
    @click.option('--assistant-id')
    @click.option('--assistant-name')
    def file_search(assistant_id, assistant_name):

        print(f'\nInitializing file search...')
        # Create or get the assistant
        assistant = None
        # if assistant_id:
        #     assistant = AssistantCmds.get(assistant_id)
        # else:
        #     assistant = Assistant.create()
    