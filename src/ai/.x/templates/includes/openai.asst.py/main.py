import os
import sys
from openai import OpenAI
{{if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}
from openai_assistants_custom_functions import factory
from openai_assistants_functions_streaming import {ClassName}
{{else if {_IS_OPENAI_ASST_CODE_INTERPRETER_TEMPLATE}}}
from openai_assistants_code_interpreter_streaming import {ClassName}
{{else if {_IS_OPENAI_ASST_FILE_SEARCH_TEMPLATE}}}
from openai_assistants_file_search_streaming import {ClassName}
{{else if {_IS_OPENAI_ASST_STREAMING_TEMPLATE}}}
from openai_assistants_streaming import {ClassName}
{{else}}
from openai_assistants import {ClassName}
{{endif}}

def main():

    # Which assistant, which thread?
    ASSISTANT_ID = os.getenv('ASSISTANT_ID') or "<insert your OpenAI assistant ID here>"
    threadId = sys.argv[1] if len(sys.argv) > 1 else None

    {{@include openai.asst.or.chat.create.openai.py}}

    # Create the assistants streaming helper class instance
    {{if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}
    assistant = {ClassName}(ASSISTANT_ID, factory, openai)
    {{else}}
    assistant = {ClassName}(ASSISTANT_ID, openai)
    {{endif}}

    # Get or create the thread, and display the messages if any
    if threadId is None:
        assistant.create_thread()
    else:
        assistant.retrieve_thread(threadId)
        assistant.get_thread_messages(lambda role, content: print(f'{role.capitalize()}: {content}', end=''))

    # Loop until the user types 'exit'
    while True:
        # Get user input
        user_input = input('User: ')
        if user_input == 'exit' or user_input == '':
            break

        # Get the Assistant's response
        {{if {_IS_OPENAI_ASST_STREAMING_TEMPLATE}}}
        print('\nAssistant: ', end='')
        assistant.get_response(user_input, lambda content: print(content, end=''))

        print('\n')
        {{else}}
        response = assistant.get_response(user_input)
        print(f'\nAssistant: {response}\n')
        {{endif}}

    print(f"Bye! (threadId: {assistant.thread.id})")

if __name__ == '__main__':
    try:
        main()
    except EOFError:
        pass
    except Exception as e:
        print(f"The sample encountered an error: {e}")
        sys.exit(1)