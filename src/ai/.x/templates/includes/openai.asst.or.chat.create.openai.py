{{if !{USE_AZURE_OPENAI} || contains(toupper("{AZURE_OPENAI_AUTH_METHOD}"), "KEY")}}
    # NOTE: Never deploy your API Key in client-side environments like browsers or mobile apps
    # SEE: https://help.openai.com/en/articles/5112595-best-practices-for-api-key-safety

{{endif}}
{{if {USE_AZURE_OPENAI}}}
    {{if {_IS_OPENAI_ASST_TEMPLATE}}}
    # Get the required environment variables, and form the base URL for Azure OpenAI Assistants API
    {{else}}
    # Get the required environment variables, and form the base URL for Azure OpenAI Chat Completions API
    {{endif}}
    {{if contains(toupper("{AZURE_OPENAI_AUTH_METHOD}"), "KEY")}}
    
    AZURE_OPENAI_API_KEY = os.getenv('AZURE_OPENAI_API_KEY', '<insert your Azure OpenAI API key here>')
    {{endif}}
    AZURE_OPENAI_API_VERSION = os.getenv('AZURE_OPENAI_API_VERSION', '<insert your Azure OpenAI API version here>')
    {{if !{_IS_OPENAI_ASST_TEMPLATE}}}
    AZURE_OPENAI_CHAT_DEPLOYMENT = os.getenv('AZURE_OPENAI_CHAT_DEPLOYMENT', '<insert your Azure OpenAI chat deployment name here>')
    {{endif}}
    AZURE_OPENAI_ENDPOINT = os.getenv('AZURE_OPENAI_ENDPOINT', '<insert your Azure OpenAI endpoint here>')

    # Check if the required environment variables are set
    ok = \
        {{if {_IS_OPENAI_ASST_TEMPLATE}}}
        ASSISTANT_ID != None and not ASSISTANT_ID.startswith('<insert') and \
        {{else}}
        AZURE_OPENAI_SYSTEM_PROMPT != None and not AZURE_OPENAI_SYSTEM_PROMPT.startswith('<insert') and \
        {{endif}}
        {{if contains(toupper("{AZURE_OPENAI_AUTH_METHOD}"), "KEY")}}
        AZURE_OPENAI_API_KEY != None and not AZURE_OPENAI_API_KEY.startswith('<insert') and \
        {{endif}}
        AZURE_OPENAI_API_VERSION != None and not AZURE_OPENAI_API_VERSION.startswith('<insert') and \
        {{if !{_IS_OPENAI_ASST_TEMPLATE}}}
        AZURE_OPENAI_CHAT_DEPLOYMENT != None and not AZURE_OPENAI_CHAT_DEPLOYMENT.startswith('<insert') and \
        {{endif}}
        AZURE_OPENAI_ENDPOINT != None and not AZURE_OPENAI_ENDPOINT.startswith('<insert')

    if not ok:
        print('To use Azure OpenAI, set the following environment variables:\n' +
            {{if {_IS_OPENAI_ASST_TEMPLATE}}}
            '\n  ASSISTANT_ID' +
            {{else}}
            '\n  AZURE_OPENAI_SYSTEM_PROMPT' +
            {{endif}}
            {{if contains(toupper("{AZURE_OPENAI_AUTH_METHOD}"), "KEY")}}
            '\n  AZURE_OPENAI_API_KEY' +
            {{else}}
            {{endif}}
            '\n  AZURE_OPENAI_API_VERSION' +
            {{if !{_IS_OPENAI_ASST_TEMPLATE}}}
            '\n  AZURE_OPENAI_CHAT_DEPLOYMENT' +
            {{endif}}
            '\n  AZURE_OPENAI_ENDPOINT')
        print('\nYou can easily do that using the Azure AI CLI by doing one of the following:\n' +
          '\n  ai init' +
          '\n  ai dev shell' +
          '\n  python main.js' +
          '\n' +
          '\n  or' +
          '\n' +
          '\n  ai init' +
          '\n  ai dev shell --run "python main.js"')
        os._exit(1)

    {{if contains(toupper("{AZURE_OPENAI_AUTH_METHOD}"), "KEY")}}
    # Create the OpenAI client
    print('Using Azure OpenAI (w/ API Key)...')
    
    openai = AzureOpenAI(
        api_key = AZURE_OPENAI_API_KEY,
        api_version = AZURE_OPENAI_API_KEY,
        azure_endpoint = AZURE_OPENAI_ENDPOINT
    )
  
    {{else}}
    # Get the access token using the DefaultAzureCredential
    # TBD: Add support for DefaultAzureCredential
    {{endif}}
{{else}}
  # Get the required environment variables, and form the base URL for OpenAI Platform API
    OPENAI_API_KEY = os.getenv('OPENAI_API_KEY', '<insert your OpenAI API key here>')
    {{if !{_IS_OPENAI_ASST_TEMPLATE}}}
    OPENAI_MODEL_NAME = os.getenv('OPENAI_MODEL_NAME', '<insert your OpenAI model name here>')
    {{endif}}
    OPENAI_ORG_ID = os.getenv('OPENAI_ORG_ID', '<insert your OpenAI organization ID here>')

    # Check if the required environment variables are set
    ok = \
        OPENAI_API_KEY != None and not OPENAI_API_KEY.startswith('<insert') and \
        {{if {_IS_OPENAI_ASST_TEMPLATE}}}
        ASSISTANT_ID != None and not ASSISTANT_ID.startswith('<insert') and \
        {{else}}
        OPENAI_MODEL_NAME != None and not OPENAI_MODEL_NAME.startswith('<insert') and \
        AZURE_OPENAI_SYSTEM_PROMPT != None and not AZURE_OPENAI_SYSTEM_PROMPT.startswith('<insert')
        {{endif}}

    if not ok:
        print('To use OpenAI, set the following environment variables:\n' +
            '\n  OPENAI_API_KEY' +
            '\n  OPENAI_ORG_ID (optional)' +
            {{if {_IS_OPENAI_ASST_TEMPLATE}}}
            '\n  ASSISTANT_ID')
            {{else}}
            '\n  OPENAI_MODEL_NAME' +
            '\n  AZURE_OPENAI_SYSTEM_PROMPT')
            {{endif}}
        print('\nYou can easily obtain some of these values by visiting these links:\n' +
            '\n  https://platform.openai.com/api-keys' +
            '\n  https://platform.openai.com/settings/organization/general' +
            '\n  https://platform.openai.com/playground/assistants' +
            '\n' +
            '\n Then, do one of the following:\n' +
            '\n  ai dev shell' +
            '\n  python main.js' +
            '\n' +
            '\n  or' +
            '\n' +
            '\n  ai dev shell --run "python main.js"');
        os._exit(1)
        {{endif}}

    # Create the OpenAI client
    print('Using OpenAI...');
    openai = OpenAI(
        api_key=OPENAI_API_KEY,
        org=OPENAI_ORG_ID
    })
{{endif}}