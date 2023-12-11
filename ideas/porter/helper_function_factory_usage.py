import os
import glob
import importlib.util
import inspect

from helper_function_factory import HelperFunctionFactory

def create_function_factory_for_custom_functions(custom_functions: str) -> HelperFunctionFactory:
    factory = HelperFunctionFactory()

    patterns = custom_functions.replace('\r', ';').replace('\n', ';').split(';')
    for pattern in patterns:
        files = glob.glob(pattern)
        if not files:
            files = glob.glob(os.path.join(os.environ.get('PATH', ''), pattern))

        for file in files:
            print(f"Trying to load custom functions from: {file}")
            spec = importlib.util.spec_from_file_location("custom_funcs", file)
            custom_funcs = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(custom_funcs)

            for func_name, func in inspect.getmembers(custom_funcs, inspect.isfunction):
                factory.add_function(func_name, func)

    return factory


# Usage:
# factory = create_function_factory_for_custom_functions(custom_functions)