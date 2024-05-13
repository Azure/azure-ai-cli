import os
import sys
from openai import OpenAI
from openai_assistants_custom_functions import factory
from openai_assistants_functions_streaming import OpenAIAssistantsFunctionsStreamingClass

def main():

    # Which assistant, which thread?
    ASSISTANT_ID = os.getenv('ASSISTANT_ID') or "<insert your OpenAI assistant ID here>"
    threadId = sys.argv[1] if len(sys.argv) > 1 else None

    {{@include openai.asst.or.chat.create.openai.py}}

    # Create the assistants streaming helper class instance
    assistant = OpenAIAssistantsFunctionsStreamingClass(ASSISTANT_ID, factory, openai)

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
        print('\nAssistant: ', end='')
        assistant.get_response(user_input, lambda content: print(content, end=''))

        print('\n')

    print(f"Bye! (threadId: {assistant.thread.id})")

if __name__ == "__main__":
    main()
