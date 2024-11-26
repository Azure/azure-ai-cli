# Azure AI CLI templates

Users can use the Azure AI CLI to generate source code templates for AI applications. Users can see the templates available by using `ai dev new list` and they can instance the templates by using `ai dev new <template-name>`. See [ai.dev.new.list.output.txt](./ai.dev.new.list.output.txt) for a partial list of templates from the most recnet published version of the Azure AI CLI.

## Template Engine

The template engine in the Azure AI CLI (`ai`) is designed to create and manage dynamic source code templates that generate files using placeholders, conditional logic, and include directives. These templates leverage a base include directory for reusable code and are configured through `_.json` files tailored to each template scenario.

Templates are a great way to ensure multiple similar templates are consistent and to reduce the amount of manual work required to create new files. By defining variables, conditions, and includes, we can generate source code that adapts to different configurations and scenarios, making it easier to develop AI applications with the Azure AI CLI.

## Template Directory Structure

Typical repository is located at `c:\src\ai-cli` or `d:\src\ai-cli`.

- **Base Include Directory**: `c:\src\ai-cli\src\ai\.x\templates\includes`
  - Contains reusable code that can be included in template files using the `{{@include directory/file}}` syntax.
  - `directory/file` would be the path to the desired code within the base include directory (e.g., `openai-cs/environment_vars.cs` would live at `c:\src\ai-cli\src\ai\.x\templates\includes\openai-cs\environment_vars.cs`).

- **Template Directories**:
  - Each directory under `c:\src\ai-cli\src\ai\.x\templates` containing a `_.json` file is a template directory.
  - These directories correspond to specific AI functionalities and scenarios, such as `openai-chat-cs`, `openai-asst-cs`, and `speech-to-text-cs`.

## Template Configuration

- Each template directory should have a `_.json` file defining the variables and conditions for that template.
- Example configuration from `openai-chat-cs/_.json`:
  ```json
  {
    "_LongName": "OpenAI Chat Completions",
    "_ShortName": "openai-chat",
    "_Language": "C#",
    "ClassName": "OpenAIChatCompletionsClass",
    "AZURE_OPENAI_API_KEY": "<insert your OpenAI API key here>",
    "AZURE_OPENAI_ENDPOINT": "<insert your OpenAI endpoint here>",
    "AZURE_OPENAI_CHAT_DEPLOYMENT": "<insert your OpenAI chat deployment name here>",
    "AZURE_OPENAI_SYSTEM_PROMPT": "You are a helpful AI assistant.",
    "_IS_OPENAI_CHAT_STREAMING_TEMPLATE": "false",
    "_IS_WITH_FUNCTIONS_TEMPLATE": "false",
    "_IS_WITH_DATA_TEMPLATE": "false"
  }
  ```

- Variables in the `_.json` file are used for variable substitution in the template files.
- Variables starting with an underscore are reserved for template replacement or conditional logic.
- `_LongName` and `_ShortName` are used for display purposes.
- `_Language` specifies the programming language for the template.

## Template File Syntax

Text files in the template directories can contain template syntax for variable substitution, conditional logic, and include directives. The syntax is designed to generate dynamic source code based on the configuration provided in the `_.json` file.

### Include Directives

- Use `{{@include directory/file}}` to include code from the base include directory.
  - Example: `{{@include openai-cs/environment_vars.cs}}` includes environment setup code.
  - **Common Practice**: Ensure that include directives are used consistently across templates to maintain uniformity. This is especially important for files that define environment variables or common configurations (`{{@include openai-cs/Program.cs}}`).
- Include directives must be the only content on line; whitespace before and after the `{{` and `}}` is allowed.

### Variable Substitution

- Use `{VariableName}` to insert values from the corresponding `_.json` configuration file.
- Example: `public class {ClassName}` will dynamically replace `{ClassName}` with the specified class name in the JSON.
- Variable substitutions can apppear anywhere on a line and can be combined with other text.

### Conditional Logic

- `{{if...}}`, `{{else...}}`, and `{{endif}}` must appear on their own lines; whitespace before and after the `{{` and `}}` is allowed.

- Use `{{if {condition}}}` to start and `{{endif}}` to end a block of conditional code.
- Example:
  ```csharp
  {{if {_IS_WITH_DATA_TEMPLATE}}}
  // This code will only be included if the condition is true.
  {{endif}}
  ```
  
- Branching conditions can be combined with `||` (OR) and `&&` (AND) operators.
- Example:
  ```csharp
  {{if {_IS_WITH_DATA_TEMPLATE} || {_IS_WITH_FUNCTIONS_TEMPLATE}}}
    // This code will only be included if both conditions are true.
  {{endif}}
  ```

- Use `{{else if {condition}}}` and `{{else}}` to create additional branches and fallbacks in the conditional logic.
- Example:
  ```csharp
  {{if {_IS_WITH_DATA_TEMPLATE}}}
    // This code will only be included if the first condition is true.
  {{else if {_IS_WITH_FUNCTIONS_TEMPLATE}}}
    // This code will only be included if the second condition is true.
  {{else}}
    // This code will only be included if none of the conditions are true.
  {{endif}}
  ```

- String functions can be used within the conditional logic to manipulate values before comparison.
- Example:
  ```csharp
  {{if CONTAINS("{ClassName}", "Chat")}}
    // This block is included if {ClassName} contains the substring "Chat"
  {{endif}}
  ```

#### Supported String Functions

Here are the string functions you can use within the template syntax:

- **TOLOWER**: Converts a string to lower case.
- **TOUPPER**: Converts a string to upper case.
- **CONTAINS**: Checks if a string contains a specified substring.
- **STARTSWITH**: Checks if a string starts with a specified substring.
- **ENDSWITH**: Checks if a string ends with a specified substring.
- **ISEMPTY**: Checks if a string is empty.

### Set Directive

- The `{{set}}` directive can be used to initialize or modify template variables dynamically within the template, affecting the flow and logic.
  ```csharp
  {{set _IS_OPENAI_ASST_TEMPLATE = false}}
  ```

- The `{{set}}` directive can be used within conditionals to ensure variables are set based on logical conditions, including setting default values if not already defined.
  ```csharp
  {{if not ISEMPTY("{SOME_VARIABLE}")}}
  {{set _DEFAULT_VALUE = "{SOME_VARIABLE}"}}
  {{else}}
  {{set _DEFAULT_VALUE = "default"}}
  {{endif}}
  ```
  This allows for the configuration of variables dynamically, ensuring they have sensible defaults or are aligned with specific conditions in your template setup.

### Platform-Specific Code Adaptation

- **Conditional Export/Import Logic**: You can use conditional logic to handle different module systems or environments. For instance, you might need to choose between using ES6 module syntax or Node.js require/exports syntax based on a configuration variable:
  ```javascript
  {{if {_IMPORT_EXPORT_USING_ES6}}}
  export class ClassName {
  {{else}}
  class ClassName {
  {{endif}}
  ```
  This pattern allows the same template to generate code compatible with different environments or module systems.

- **Conditional Console Logging**: Similarly, to adapt logging or output mechanisms depending on the runtime environment (e.g., browser vs. Node.js), you can use conditional logic:
  ```javascript
  {{if {_IS_BROWSER_TEMPLATE}}}
  console.log(`message`);
  {{else}}
  process.stdout.write(`message`);
  {{endif}}
  ```
  This ensures that templates can cater to different runtime environments while maintaining clean, readable code.
