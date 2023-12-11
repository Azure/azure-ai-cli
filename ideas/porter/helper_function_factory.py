import asyncio
import inspect
import json
from typing import Any, Callable, Dict, List, Optional, Tuple, Type, Union


class HelperFunctionFactory:

    def __init__(self):
        self._functions: Dict[str, Callable[..., Any]] = {}

    async def call_function(self, function_name: str, arguments_as_json: str) -> Any:
        function = self._functions.get(function_name)
        if function is None:
            raise KeyError(f'Function not found: {function_name}')

        arguments = json.loads(arguments_as_json)
        if asyncio.iscoroutinefunction(function):
            return await function(**arguments)
        else:
            return function(**arguments)

    def add_function(self, function_name: str, function: Callable[..., Any]) -> None:
        self._functions[function_name] = function

    def remove_function(self, function_name: str) -> None:
        self._functions.pop(function_name, None)

    def list_functions(self) -> List[str]:
        return list(self._functions.keys())

    def get_function_spec(self, function_name: str) -> Optional[Tuple[str, str]]:
        function = self._functions.get(function_name)
        if function is None:
            return None

        signature = inspect.signature(function)
        parameters = str(signature.parameters)
        return_type = str(signature.return_annotation)
        return parameters, return_type