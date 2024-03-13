# `ai test`

`ai test` is a YAML-based test framework/runner that can be used to run tests on any command-line tool or script. It is designed to be simple to use and understand, and to be able to run tests in parallel.

Examples:

```yaml
- name: Build search index
  command: ai search index update --files "data/*.md" --index-name myindex
  expect: |
    Updating search index 'myindex' ...
    Updating search index 'myindex' ... Done!
    
- name: Evaluate chat model
  command: ai chat evaluate --data-input questions.jsonl
  expect-gpt: |
    The GPT coherence score must be at least 4.2
```

The test case YAML file contains a list of test cases. Each test case is a dictionary with the following keys:

* `name` (required): The name of the test case.
* `command`, `script`, `bash` (required): The command or script to run.
- `env` (optional): A dictionary of environment variables to set before running the command or script.
- `input` (optional): The input to pass to the command or script.
- `expect` (optional): A list of regular expressions that must be matched in the stdout/stderr output.
- `expect-gpt` (optional): A string that instructs the LLM (e.g. GPT-4) to decide pass/fail based on stdout/stderr.
- `not-expect` (optional): A list of regular expressions that must not be matched in the stdout/stderr output.
- `parallelize` (optional): Whether the test case should run in parallel with other test cases.
- `skipOnFailure` (optional): Whether the test case should be skipped when it fails.
- `tag`/`tags` (optional): A list of tags to associate with the test case.
- `timeout` (optional): The maximum time allowed to execute the test case, in milliseconds.
- `workingDirectory` (optional): The working directory where the test will be run.

Test cases can be organized into areas, sub-areas, and so on.

```yaml
- area: Area 1
  tests:

  - name: Test 1
    command: echo "Hello, world!"

  - name: Test 2
    command: echo "Goodbye, world!"
```

Test cases can also be grouped into classes. 

```yaml
- class: Class 1
  tests:

  - name: Test 1
    command: echo "Hello, world!"

  - name: Test 2
    command: echo "Goodbye, world!"
```

If no class is specified, the default class is "TestCases".

## `command`, `script`, `bash`

Required.

Represents how the test case will be run.

If the specified command or script returns an error level of non-zero, the test will fail. If it returns zero, it will pass (given that all 'expect' conditions are also met).

Example command:

```yaml
command: ai chat --interactive
```

Example for a bash script:

```yaml
bash: |
  if [ -f /etc/os-release ]; then 
    python3 script.py 
  else 
    py script.py
  fi
```

## `env`

Optional. Inherits from parent.

When present, a dictionary of environment variables to set before running the command or script.

Example:

```yaml
env:
  JAVA_HOME: /path/to/java
```

## `input`

Optional.

When present, will be passed to the command or script as stdin.

Example:

```yaml
input: |
  Tell me a joke
  Tell me another
  exit
```

## `expect`

Optional.

Each string (or line in multiline string) is a regular expression that must be matched in the stdout/stderr output.

If any regular expression is not matched, the test will fail. If all expressions are matched, in order, the test will pass.

Example:

```yaml
expect: |
  Regex 1
  Regex 2
```

## `expect-gpt`

Optional.

Represents instructions given to LLM (e.g. GPT-4) along with stdout/stderr to decide whether the test passes or fails.

Example: 

```yaml
expect-gpt: the output must have exactly two jokes
```

## `not-expect`

Optional.

When present, each string (or line in multiline string) is a regular expression that must not be matched in the stdout/stderr output.

If any regular expression is matched, the test fails. If none match, the test passes.

Example:

```yaml
not-expect: |
  ERROR
  curseword1
  curseword2
```

## `parallelize`

Optional.

When present, specifies if the test cases should run in parallel or not.

By default, it is set to `false` for all tests, except for the first step in a `steps` test sequence.

Example:

```yaml
parallelize: true
```

## `skipOnFailure`

Optional.

When present, specifies if the test case should be skipped when it fails.

By default, it is set to `false`.

Example: 

```yaml
skipOnFailure: true
```

## `tag`/`tags`

Optional. Inherits from parent.

When present, specifies a list of tags to associate with the test case.

Tags accumulate from parent to child, so if a tag is specified in a parent, it will be inherited by all children.

Examples:

```yaml
tag: skip
```

```yaml
tags:
- slow
- performance
- long-running
```

```yaml
area: Area 1
tags: [echo]
tests:

- name: Test 1
  command: echo "Hello, world!"
  tags: [hello]

- name: Test 2
  command: echo "Goodbye, world!"
  tags: [bye]
```

## `timeout`

Optional.

When present, specifies the maximum time allowed to execute the test case, in milliseconds. Defaults to infinite.

Example:

```yaml
timeout: 3000  # 3 seconds
```


## `workingDirectory`

Optional. Inherits from parent.

When present, specifies an absolute path or relative path where the test will be run.

When specified as a relative path, it will be relative to the working directory of the parent, or if no parent exists, where the test case file is located.

