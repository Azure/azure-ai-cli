import os
from openai import OpenAI

class OpenAIHelpers:

    @staticmethod
    def InitClient():

        #-----------------------
        # NOTE: Never deploy your API Key in client-side environments like browsers or mobile apps
        # SEE: https://help.openai.com/en/articles/5112595-best-practices-for-api-key-safety

        # Get the required environment variables, and form the base URL for Azure OpenAI Assistants API
        AZURE_OPENAI_API_KEY = os.getenv('AZURE_OPENAI_API_KEY', '<insert your Azure OpenAI API key here>')
        AZURE_OPENAI_API_VERSION = os.getenv('AZURE_OPENAI_API_VERSION', '<insert your Azure OpenAI API version here>')
        AZURE_OPENAI_ENDPOINT = os.getenv('AZURE_OPENAI_ENDPOINT', '<insert your Azure OpenAI endpoint here>')
        AZURE_OPENAI_CHAT_DEPLOYMENT = os.getenv('AZURE_OPENAI_CHAT_DEPLOYMENT', '<insert your Azure OpenAI chat deployment here>')
        AZURE_OPENAI_BASE_URL = f'{AZURE_OPENAI_ENDPOINT.rstrip("/")}/openai'

        # Get the required environment variables, and form the base URL for OpenAI Platform API
        OPENAI_API_KEY = os.getenv('OPENAI_API_KEY', '<insert your OpenAI API key here>')
        OPENAI_MODEL_NAME = os.getenv('OPENAI_MODEL_NAME', '<insert your OpenAI model name here>')
        OPENAI_ORG_ID = os.getenv('OPENAI_ORG_ID', None)

        # Check if the required environment variables are set
        azureOk = \
        AZURE_OPENAI_API_KEY != None and not AZURE_OPENAI_API_KEY.startswith('<insert') and \
        AZURE_OPENAI_API_VERSION != None and not AZURE_OPENAI_API_VERSION.startswith('<insert') and \
        AZURE_OPENAI_CHAT_DEPLOYMENT != None and not AZURE_OPENAI_CHAT_DEPLOYMENT.startswith('<insert') and \
        AZURE_OPENAI_ENDPOINT != None and not AZURE_OPENAI_ENDPOINT.startswith('<insert')
        oaiOk = \
        OPENAI_API_KEY != None and not OPENAI_API_KEY.startswith('<insert') and \
        OPENAI_MODEL_NAME != None and not OPENAI_MODEL_NAME.startswith('<insert')
        ok = azureOk or oaiOk

        if not ok:
            print('To use OpenAI, set the following environment variables:\n' +
                '\n  ASSISTANT_ID' +
                '\n  OPENAI_API_KEY' +
                '\n  OPENAI_MODEL_NAME' +
                '\n  OPENAI_ORG_ID (optional)' +
                '\n  VECTOR_STORE_ID (optional)')
            print('\nYou can easily obtain some of these values by visiting these links:\n' +
                '\n  https://platform.openai.com/api-keys' +
                '\n  https://platform.openai.com/settings/organization/general' +
                '\n  https://platform.openai.com/playground/assistants' +
                '\n' +
                '\n Then, do one of the following:\n' +
                '\n  ai dev shell' +
                '\n  python ai-py-cli.py ...' +
                '\n' +
                '\n  or' +
                '\n' +
                '\n  ai dev shell --run "python ai-py-cli.py ..."');
            print()
            print('To use Azure OpenAI, set the following environment variables:\n' +
                '\n  ASSISTANT_ID' +
                '\n  AZURE_OPENAI_API_KEY' +
                '\n  AZURE_OPENAI_API_VERSION' +
                '\n  AZURE_OPENAI_CHAT_DEPLOYMENT' +
                '\n  AZURE_OPENAI_ENDPOINT')
            print('\nYou can easily do that using the Azure AI CLI by doing one of the following:\n' +
            '\n  ai init' +
            '\n  ai dev shell' +
            '\n  python ai-py-cli.py ...' +
            '\n' +
            '\n  or' +
            '\n' +
            '\n  ai init' +
            '\n  ai dev shell --run "python ai-py-cli.py ..."')
            os._exit(1)

        # Create the OpenAI client
        if azureOk:
            print('Using Azure OpenAI (w/ API Key)...')
            client = OpenAI(
                api_key = AZURE_OPENAI_API_KEY,
                base_url = AZURE_OPENAI_BASE_URL,
                default_query= { 'api-version': AZURE_OPENAI_API_VERSION },
                default_headers = { 'api-key': AZURE_OPENAI_API_KEY }
            )
        else:
            print('Using OpenAI...')
            client = OpenAI(
                api_key = OPENAI_API_KEY,
                organization = OPENAI_ORG_ID
            )

        return client
