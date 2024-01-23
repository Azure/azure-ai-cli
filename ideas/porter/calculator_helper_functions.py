def add_floats(a, b):
    return a + b

def subtract_floats(a, b):
    return a - b

def multiply_floats(a, b):
    return a * b

def divide_floats(a, b):
    return a / b

def add_integers(a, b):
    return a + b

def subtract_integers(a, b):
    return a - b

def multiply_integers(a, b):
    return a * b

def divide_integers(a, b):
    return a / b

def average(numbers):
    return sum(numbers) / len(numbers) if len(numbers) > 0 else 0

def standard_deviation(numbers):
    average = sum(numbers) / len(numbers) if len(numbers) > 0 else 0
    return (sum((x - average) ** 2 for x in numbers) / len(numbers)) ** 0.5 if len(numbers) > 0 else 0