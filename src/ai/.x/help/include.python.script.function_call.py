import argparse
import importlib
import inspect
import json
import os
import sys
from typing import Generator

def ensure_and_strip_module_path(module_path) -> str:
    """
    Ensure the module path is in the sys.path list and return the module name.

    Args:
        module_path (str): The full path of the module to import.

    Returns:
        str: The name of the module to import.
    """
    module_path = os.path.join(os.getcwd(), module_path)
    module_name = os.path.basename(module_path)

    if os.path.exists(module_path + ".py"):
        module_dirname = os.path.dirname(module_path)
        if module_dirname not in sys.path:
            sys.path.append(module_dirname)
        return module_name
    
    raise ModuleNotFoundError("Module not found: " + module_path)
    
def call_function(module_path: str, function_name: str, json_params: str):
    """
    Call a function in a module with the specified parameters.

    Args:
        module_path (str): The fqn of the module to import.
        function_name (str): The name of the function to call.
        json_params (str): A JSON string containing the parameters to pass to the function.
    """
    try:
        module_name = ensure_and_strip_module_path(module_path)

        # Import the module
        target_module = importlib.import_module(module_name)

        # Use inspect to find the function by name
        target_function = getattr(target_module, function_name)

        # Check if the target_function is callable
        if callable(target_function):
            # Parse the JSON parameter string to a dictionary
            params = json.loads(json_params)

            # Get the function's signature
            sig = inspect.signature(target_function)

            # Bind the parameters to the function's signature
            bound_args = sig.bind(**params)

            # Call the function with the bound arguments
            result = target_function(*bound_args.args, **bound_args.kwargs)
            return result

        else:
            return "Function not found or not callable."
        
    # handle TypeError
    except ModuleNotFoundError:
        print("Module not found: " + module_path)
        raise
    except AttributeError:
        print("Function not found: " + function_name)
        raise
    except TypeError:
        print("Invalid JSON parameter(s): " + str(sys.exc_info()[1]))
        raise
    except json.JSONDecodeError:
        print("Invalid JSON parameter: " + json_params)
        raise

def ensure_args() -> list:
    """
    Check the command line arguments for the module name and function name.
    """
    args = sys.argv[1:]
    if len(args) < 1:
        print("Usage: callit.py module_name:function_name [json_params]")
        sys.exit(1)
    return args

def get_args(args) -> tuple[str, str, str]:
    """
    Get the module name, function name, and JSON parameters from the command line arguments.
    
    Args:
        args (list): A list of command line arguments.
        
    Returns:
        tuple: A tuple containing the module name, function name, and JSON parameters.
    """
    module_function_parts = args[0].rsplit(":", 1)

    if len(module_function_parts) != 2:
        print("Invalid argument format. Please use module_name:function_name.")
        sys.exit(1)

    module_name = module_function_parts[0]
    function_name = module_function_parts[1]

    json_params = args[1] if len(args) == 2 else "{}"
    return module_name,function_name,json_params

def main():
    """Parse the command line arguments and call the specified function."""

    module_path, function_name, json_params = get_args(ensure_args())
    result = call_function(module_path, function_name, json_params)

    if result is not None:
        # if it's a string...
        if isinstance(result, str):
            print("---it's a string---")
            for word in result.split():
                print(word)

        # if it's a list of strings
        elif isinstance(result, list) and all(isinstance(item, str) for item in result):
            print("---it's a list---")
            for item in result:
                for word in item.split():
                    print(word)

        # if it's a generator
        elif issubclass(type(result), Generator) :
            print("---it's a generator---")
            for item in result:
                for word in item.split():
                    print(word)

        else:
            print("---it's something else---")
            print(type(result))

if __name__ == "__main__":
    main()
