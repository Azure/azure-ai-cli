import asyncio
from helper_function_factory_usage import create_function_factory_for_custom_functions

# Example of custom functions
def greet(name: str) -> str:
    return f'Hello, {name}!'

def farewell(name: str) -> str:
    return f'Goodbye, {name}!'

# Save custom functions to a python file
with open('custom_funcs.py', 'w') as f:
    f.write('def greet(name: str) -> str:\n    return f\'Hello, {name}!\'\n\ndef farewell(name: str) -> str:\n    return f\'Goodbye, {name}!\'\n')

# Use the factory to load the custom functions
factory = create_function_factory_for_custom_functions('custom_funcs.py')

# Use the loaded functions
async def main():
    print(await factory.call_function('greet', '{"name": "John"}'))
    print(await factory.call_function('farewell', '{"name": "John"}'))

asyncio.run(main())