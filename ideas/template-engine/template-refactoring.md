{@./template-info.md}

## Template refactoring

The process of refactoring templates involves extracting common code into shared files using conditional logic and placeholders for true/false or names of classes/methods to create more maintainable and efficient templates.

We will begin by looking a pair of templates, and a specific pair of files inside that template project.
- Directory 1: `{dir1}`
- Directory 2: `{dir2}`
- File 1: `{dir1}/{file1}`
- File 2: `{dir2}/{file2}`
- Common code: `{dir3}/{file3}` (under includes directory)
- Common code includes directory: is typically located at `c:\src\ai-cli\src\ai\.x\templates\includes`
- Conditional Flag 1: `{flag1}` (use for conditional logic when merging file 1 into common code)
- Conditional Flag 2: `{flag2}` (use for conditional logic when merging file 2 into common code)

The steps for refactoring templates are as follows:

### 0. Read all the files

- **Read the Files**: Review the contents of the template files to understand the structure and identify common code that can be extracted.
   - `{dir3}/{file3}` (if it exists in the includes directory)
   - `{dir1}/{file1}`
   - `{dir2}/{file2}`
   - `_.json` files for `{dir1}` and `{dir2}`

### 1. Analyze the Templates

- **Understand what's there**: Understand the structure of the files; what's already there; what's different from one to the other
   - If the common code already exists, make note of it, exactly.
     - You shouldn't remove anything in the existing common code; you can add to it though, with conditional logic.
     - Make note of all existing conditional logic in the common code and be sure to maintain it.
   - Compare the two template files to see what's different between them, and how that's different than the common code (if it already exists).
     - Make note of new conditional logic that may be needed to handle the differences between the two files.
     - Remember, don't remove any existing conditional logic; only add to it.

### 2. Extract Common Code

- **Create Shared Code**: For any code that is common across multiple template files, create a new (or update the existing) common code.
  - Example: If both `Program.cs` files share the same main logic, extract this logic into a shared file such as `{dir3}/Program.cs`.

### 3. Refactor Templates to Use Includes

- **Replace with Include Directives**: In each template file that contained the common code, replace the code with an include directive that points to the shared code.
  - Syntax: Use `{{@include directory/file}}` where `directory/file` is the path to the shared code file relative to the base includes directory.

### 4. Use Placeholders for Dynamic Content

- **Variable Substitution**: Identify parts of the code that vary between templates and replace them with placeholders. Define these placeholders in the `_.json` configuration file for each template.
  - Example: Replace `public class SpecificClassName` with `public class {ClassName}` and set `"ClassName": "DesiredClassName"` in `_.json`.

### 5. Implement Conditional Logic (if needed)

- **Conditional Code Blocks**: For sections of code that should be included or excluded based on certain conditions, use conditional logic.
  - Syntax: Use `{{if {condition}}}` to start and `{{endif}}` to end a block of conditional code.
  - Example:
    ```csharp
    {{if {_IS_WITH_DATA_TEMPLATE}}}
    // Code specific to data templates
    {{endif}}
    ```

### 6. Ensure `_.json` settings

- **Update Configuration**: Make sure that the `_.json` configuration file for each template contains the necessary variables and conditions for the refactored template structure.
  - Verify that all placeholders are defined in the configuration file.
  - Check that any conditional logic flags are correctly set based on the template requirements.

## Do it!

Now, based on all the information, you should now do the refacoring for the files specified. 
1. Read all the files you need to read. Make note of the differences.
2. Extract mostly common code into the single common-code file that uses conditional logic to include/exclude parts of the code based on the template.
3. Replace the common code in `{dir1}/{file1}` and `{dir2}/{file2}` with include directives pointing to the common code file. The include directive must start with `{{` and end with `}}` and be in the format `{{@include directory/file}}` (where directory is relative to includes directory).
4. Ensure that the placeholders and conditionals in the template files are correctly defined in the `_.json` configuration files.

NOTES:
- Do **NOT** remove things you should not remove.
- Do **NOT** remove comments.
- Do **NOT** add things you should not add.
- Do **NOT** ask me any questions. Just do the work.  
- If you have any questions, make a decision and move on.
