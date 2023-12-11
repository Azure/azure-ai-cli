from typing import Any, Dict

import helper_function_factory


def add_functions(options: Dict[str, Any], function_factory: helper_function_factory.HelperFunctionFactory) -> Dict[str, Any]:
    for function_name in function_factory.list_functions():
        parameters, return_type = function_factory.get_function_spec(function_name)
        options['functions'][function_name] = {
            'parameters': parameters,
            'return_type': return_type,
        }
    return options


def try_call_function(options: Dict[str, Any], function_name: str, arguments_as_json: str) -> Any:
    return options['functions'][function_name](arguments_as_json)