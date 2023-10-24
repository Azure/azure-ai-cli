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













# TEMP: wrapper around chat completion function until chat_completion protocol is supported
def copilot_qna(question, chat_completion_fn):
    # Call the async chat function with a single question and print the response

    if platform.system() == 'Windows':
        asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())

    result = asyncio.run(
        chat_completion_fn([{"role": "user", "content": question}])
    )
    response = result['choices'][0]
    return {
        "question": question,
        "answer": response["message"]["content"],
        "context": response["context"]
    }

 # Define helper methods
def load_jsonl(path):
    with open(path, "r") as f:
        return [json.loads(line) for line in f.readlines()]

def run_evaluation(subscription_id, resource_group_name, project_name, function, name, dataset_path):
    from azure.ai.generative.evaluate import evaluate


    module_function_parts = function.rsplit(":", 1)

    if len(module_function_parts) != 2:
        print("Invalid argument format. Please use MODULE:FUNCTION.")
        sys.exit(1)

    module_name = module_function_parts[0]
    function_name = module_function_parts[1]







    path = pathlib.Path.cwd() / dataset_path
    dataset = load_jsonl(path)

    qna_fn = lambda question: copilot_qna(question, chat_completion_fn)

    client = AIClient.from_config(DefaultAzureCredential())
    result = evaluate(
        evaluation_name=name,
        asset=qna_fn,
        data=dataset,
        task_type="qa",
        truth_data="truth",
        metrics_config={
            "openai_params": {
                "api_version": "2023-05-15",
                "api_base": os.getenv("OPENAI_API_BASE"),
                "api_type": "azure",
                "api_key": os.getenv("OPENAI_API_KEY"),
                "deployment_id": os.getenv("AZURE_OPENAI_EVALUATION_DEPLOYMENT")
            },
            "questions": "question",
            "contexts": "context",
            "y_pred": "answer",
            "y_test": "answer"
        },
        tracking_uri=client.tracking_uri,
    )
    return result

def main():

    import argparse
    parser = argparse.ArgumentParser(description="Evaluate a function call")
    parser.add_argument("--subscription", required=True, help="Azure subscription ID")
    parser.add_argument("--group", required=False, help="Azure resource group name")
    parser.add_argument("--project-name", required=True, help="Azure AI project project name.")
    parser.add_argument("--function", required=True, help="Module and function name in the format MODULE:FUNCTION.")
    args = parser.parse_args()

    subscription_id = args.subscription
    resource_group_name = args.group
    project_name = args.project_name
    function = args.function

    result = run_evaluation(subscription_id, resource_group_name, project_name, function)
    formatted = json.dumps(result, indent=2)

    print("---")
    print(formatted)

if __name__ == "__main__":
    main()
