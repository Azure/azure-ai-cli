from typing import Any, List

def chat_completion(question: str) -> List[str]:
    answers = []

    # Implement a switch statement for various questions
    if question == "what is your name?":
        answers.append("My name is ChatGPT.")
    elif question == "what is the capital of France?":
        answers.append("The capital of France is Paris.")
    elif question == "what is 2 + 2?":
        answers.append("2 + 2 is 4.")
    elif question == "tell me a joke":
        answers.append("Why did the chicken cross the road? To get to the other side!")
    else:
        answers.append("I don't know the answer to that question.")
    return answers

    return answers

async def async_chat_completion(messages: list[dict] = None, stream: bool = False,
    session_state: Any = None, context: dict[str, Any] = {}):

    if (messages is None):
        return chat_completion(question)
    
    print(messages)

    # get search documents for the last user message in the conversation
    question = messages[-1]["content"]

    return chat_completion(question)
