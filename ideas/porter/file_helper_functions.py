import os
import glob


def file_exists(file_name):
    return os.path.exists(file_name)

def read_text_from_file(file_name):
    if file_exists(file_name):
        with open(file_name, 'r') as file:
            return file.read()
    return ''

def create_file_and_save_text(file_name, text):
    with open(file_name, 'w') as file:
        file.write(text)
    return True

def append_text_to_file(file_name, text):
    with open(file_name, 'a') as file:
        file.write(text)
    return True

def directory_create(directory_name):
    os.makedirs(directory_name, exist_ok=True)
    return True

def find_all_files():
    return glob.glob('**', recursive=True)

def find_files_matching_pattern(pattern):
    return glob.glob(pattern, recursive=True)

def find_text_in_all_files(text):
    return find_text_in_files_matching_pattern(text, '**')

def find_text_in_files_matching_pattern(text, pattern):
    files = find_files_matching_pattern(pattern)
    result = []
    for file in files:
        with open(file, 'r') as f:
            if text in f.read():
                result.append(file)
    return result