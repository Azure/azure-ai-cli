import json
import sys
import pathlib
import importlib
import asyncio
import os

def ensure_and_strip_module_path(module_path) -> str:
    module_path = os.path.join(os.getcwd(), module_path)
    module_name = os.path.basename(module_path)

    if os.path.exists(module_path + ".py"):
        module_dirname = os.path.dirname(module_path)
        if module_dirname not in sys.path:
            sys.path.append(module_dirname)
        if os.getcwd() not in sys.path:
            sys.path.append(os.getcwd())
        return module_name

    raise ModuleNotFoundError("Module not found: " + module_path)

def get_function(module_path, function_name):

    try:
        module_name = ensure_and_strip_module_path(module_path)
        module = importlib.import_module(module_name)
        return getattr(module, function_name)
    except ModuleNotFoundError:
        raise Exception("Module not found: " + module_path)
    except AttributeError:
        raise Exception("Function not found: " + function_name)

def create_wrapper(module_path, function_name):

    fn = get_function(module_path, function_name)

    async def async_wrapper(messages_field, stream_field, session_field, context_field, question_field, kwargs):

        messages = kwargs[messages_field] if messages_field in kwargs else []
        question = kwargs[question_field] if question_field in kwargs else None
        if question is not None:
            messages.append({"role": "user", "content": question})

        stream = kwargs[stream_field] if stream_field in kwargs else False
        session_state = kwargs[session_field] if session_field in kwargs else None
        context = kwargs[context_field] if context_field in kwargs else {}

        try:
            return await fn(messages = messages, stream = stream, session_state = session_state, context = context)

        except TypeError:
            ex1 = sys.exc_info()[1]

            try:
                return await fn(question)

            except TypeError:
                ex2 = sys.exc_info()[1]
                raise Exception("Invalid parameters: " + str(ex1) + " and " + str(ex2))

            except Exception:
                return None

        except Exception:
            return None

    def sync_wrapper(messages_field, stream_field, session_field, context_field, question_field, kwargs):

        messages = kwargs[messages_field] if messages_field in kwargs else []
        question = kwargs[question_field] if question_field in kwargs else None
        if question is not None:
            messages.append({"role": "user", "content": question})

        stream = kwargs[stream_field] if stream_field in kwargs else False
        session_state = kwargs[session_field] if session_field in kwargs else None
        context = kwargs[context_field] if context_field in kwargs else {}

        try:
            return fn(messages = messages, stream = stream, session_state = session_state, context = context)

        except TypeError:
            ex1 = sys.exc_info()[1]

            try:
                return fn(question)

            except TypeError:
                ex2 = sys.exc_info()[1]
                raise Exception("Invalid parameters: " + str(ex1) + " and " + str(ex2))

            except Exception:
                return None

        except Exception:
            return None

    if asyncio.iscoroutinefunction(fn):
        return lambda messages_field, stream_field, session_field, context_field, question_field, kwargs: asyncio.run(async_wrapper(messages_field, stream_field, session_field, context_field, question_field, kwargs))
    elif callable(fn):
        return sync_wrapper
    else:
        raise Exception("Function not found, not an asynchronous function, or not callable.")

def load_jsonl(path):
    with open(path, "r") as f:
        return [json.loads(line) for line in f.readlines()]

def evaluate(
    module_path: str,
    function_name: str,
    dataset_or_path: [str, list],
    messages_field: str = "messages",
    stream_field: str  = "stream",
    session_state_field: str  = "session_state",
    context_field: str  = "context",
    question_field: str  = "question",
    truth_field: str  = "truth",
    answer_field: str  = "answer",
    correct_field: str  = "correct"):

    wrapper = create_wrapper(module_path, function_name)

    if isinstance(dataset_or_path, str):
        path = pathlib.Path.cwd() / dataset_or_path
        dataset = load_jsonl(path)
    elif isinstance(dataset_or_path, list):
        dataset = dataset_or_path

    results = []
    for d in dataset:
        answer = wrapper(messages_field, stream_field, session_state_field, context_field, question_field, d)
        results.append({
            "question": d[question_field],
            "truth": d[truth_field],
            "answer": answer,
            "correct": answer == d[truth_field]
        })
    return results

if __name__ == "__main__":

    dataset_path = "/workspaces/aistudio-copilot-sample/src/tests/evaluation_dataset.jsonl"
    # data looks like this:
    # {"question": "Which tent is the most waterproof?", "truth": "The Alpine Explorer Tent has the highest rainfly waterproof rating at 3000m", "extra2": "blah blah blah"}
    # {"question": "Which camping table holds the most weight?", "truth": "The Adventure Dining Table has a higher weight capacity than all of the other camping tables mentioned"}
    # {"question": "How much does TrailWalker Hiking Shoes cost? ", "truth": "$110", "extra1": "blah blah blah"}

    moduleAndFunction = "copilot_semantickernel/chat:chat_completion" # or "copilot_aisdk.chat:async_chat_completion"
    # copilot_aisdk/chat.py
    #
    # # Example question function
    # def chat_completion1(question: str, kwargs) -> List[str]:
    #    answers = []
    #    # ...
    #    return answers
    #
    # # Example question function
    # async def chat_completion2(question: str, kwargs) -> List[str]:
    #    answers = []
    #    # ...
    #    return answers
    #
    # # Example async chat completion function
    # def chat_completionA(messages: list[dict] = None, stream: bool = False,
    #     session_state: Any = None, context: dict[str, Any] = {}):
    #    answers = []
    #    # ...
    #    return answers
    #
    # async def chat_completionB(messages: list[dict] = None, stream: bool = False,
    #     session_state: Any = None, context: dict[str, Any] = {}):
    #    answers = []
    #    # ...
    #    return answers

    module, function = moduleAndFunction.rsplit(":", 1)

    results = evaluate(module, function, dataset_path)
    formatted = json.dumps(results, indent=2)
    print(formatted)
