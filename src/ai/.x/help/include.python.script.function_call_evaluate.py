import asyncio
import pathlib
import platform
import json
import os
import sys
from azure.identity import DefaultAzureCredential
from azure.ai.generative import AIClient
from azure.ai.generative.operations._index_data_source import LocalSource, ACSOutputConfig
from azure.ai.generative.functions.build_mlindex import build_mlindex
from azure.ai.generative.entities.mlindex import MLIndex
import asyncio
import argparse
import importlib
import inspect
import json
import os
import sys
from typing import Any, List, Dict, Generator

class AutoFlushingStream:
    def __init__(self, stream):
        self.stream = stream

    def write(self, data):
        self.stream.write(data)
        self.stream.flush()

    def flush(self):
        self.stream.flush()

sys.stdout = AutoFlushingStream(sys.stdout)
sys.stderr = AutoFlushingStream(sys.stderr)

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

def bulk_run_part(
    module_and_function: str,
    dataset_or_path: [str, list],
    messages_field: str = "messages",
    stream_field: str  = "stream",
    session_state_field: str  = "session_state",
    context_field: str  = "context",
    question_field: str  = "question",
    truth_field: str  = "truth",
    answer_field: str  = "answer",
    correct_field: str  = "correct"):

    module_path, function_name = module_and_function.rsplit(":", 1)
    wrapper = create_wrapper(module_path, function_name)

    if isinstance(dataset_or_path, str):
        path = pathlib.Path.cwd() / dataset_or_path
        dataset = load_jsonl(path)
    elif isinstance(dataset_or_path, list):
        dataset = dataset_or_path

    run_results = []
    for d in dataset:
        result = wrapper(messages_field, stream_field, session_state_field, context_field, question_field, d)

        answer = None
        if result is not None:
            if isinstance(result, str):
                print("===it's a string===")
                print(result)
                answer = result
                
            elif isinstance(result, list) and all(isinstance(item, str) for item in result):
                print("===it's a list===")
                print(result)
                answer = "\n".join(result)

            # if it's a generator
            elif issubclass(type(result), Generator) :
                print("===it's a generator===")
                print(result)
                answer = "\n".join(result)

            # if the "openai.openai_object.OpenAIObject"
            elif (type(result).__name__ == "OpenAIObject"):
                print("===it's an OpenAIObject===")
                print(result.choices[0].message.content)
                answer = result.choices[0].message.content

            # if it's a dictionary that has a "choices" key
            elif (isinstance(result, dict) and "choices" in result):
                print("===it's a dictionary with a 'choices' key===")
                print(result["choices"][0]["message"]["content"])
                answer = result["choices"][0]["message"]["content"]

            else:
                print("===it's something else===")
                print(type(result))
                print(result)
                answer = str(result)

        if answer is not None:
            result = { "question": d[question_field], "truth": d[truth_field], "answer": answer, "context": "" }
            print(result)
            run_results.append(result)

    return run_results

def run_evaluate_part(subscription_id, resource_group_name, project_name, run_results, name):
    client = AIClient(
        credential=DefaultAzureCredential(),
        subscription_id=subscription_id,
        resource_group_name=resource_group_name,
        project_name=project_name,
        user_agent="ai-cli 0.0.1"
    )

    def dont_call_this_method(kwargs):
        raise Exception("This method should not be called.")

    from azure.ai.generative.evaluate import evaluate
    eval_results = evaluate(
        evaluation_name=name,
        target=dont_call_this_method,
        data=run_results,
        truth_data="truth",
        prediction_data="answer",
        task_type="qa",
        data_mapping={
            "questions": "question",
            "contexts": "context",
            "y_pred": "answer",
            "y_test": "truth"
        },
        model_config={
            "api_version": os.getenv("OPENAI_API_VERSION"),
            "api_base": os.getenv("OPENAI_API_BASE"),
            "api_type": os.getenv("OPENAI_API_TYPE"),
            "api_key": os.getenv("OPENAI_API_KEY"),
            "deployment_id": os.getenv("AZURE_OPENAI_EVALUATION_DEPLOYMENT")
        },
        tracking_uri=client.tracking_uri,
    )

    return eval_results

def run_and_or_evaluate(subscription_id, resource_group_name, project_name, module_and_function, dataset, name):

    if isinstance(dataset, str):
        path = pathlib.Path.cwd() / dataset
        dataset = load_jsonl(path)
    elif isinstance(dataset, list):
        dataset = dataset

    if module_and_function is not None:
        print("Running...")
        dataset = bulk_run_part(module_and_function, dataset)
        print("Running... Done!")
        print(dataset)
    
    print("Evaluating...")
    eval_results = run_evaluate_part(subscription_id, resource_group_name, project_name, dataset, name)
    print("Evaluating... Done!")
    print(eval_results)

    return eval_results

def main():

    import argparse
    parser = argparse.ArgumentParser(description="Evaluate a function call")
    parser.add_argument("--subscription", required=True, help="Azure subscription ID")
    parser.add_argument("--group", required=False, help="Azure resource group name")
    parser.add_argument("--project-name", required=True, help="Azure AI project project name.")
    parser.add_argument("--function", required=False, help="Module and function name in the format MODULE:FUNCTION.")
    parser.add_argument("--data", required=True, help="Path to the dataset file")
    parser.add_argument("--name", required=True, help="Name of the data evaluation")
    args = parser.parse_args()

    subscription_id = args.subscription
    resource_group_name = args.group
    project_name = args.project_name
    module_and_function = args.function
    dataset = args.data
    name = args.name

    result = run_and_or_evaluate(subscription_id, resource_group_name, project_name, module_and_function, dataset, name)
    formatted = json.dumps(result, indent=2)

    print("---")
    print(formatted)

if __name__ == "__main__":
    main()
