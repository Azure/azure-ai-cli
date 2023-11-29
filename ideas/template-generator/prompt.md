# GOAL: Create a scenario project template using files from an existing scenario template, and sample code snippets for a completely different scenario

## Phase 1: Learning about text templating

1.1. Read all files from the `input-template` directory to learn how we use Mono.TextTemplating to create new project templates.
1.2. Do this step before proceeding. Do it now.

### How to interpret the `_.json` file in the `input-template` directory:

Inside the `input-template` directory this is a file called `_.json` that represents meta data about the project that will be created.
- `"_Name"` - represents the name of the project template that will be created
- `"AICLIExtensionReferencePath"` - represents the path that you can use to reference the AI CLI extension assemblies.
- You can add more parameters to this file, and use them in the template files.

## Phase 2: Learning about a new scenario

2.1. Read all files from the `input-sample` directory to learn about a different scenario, and how the SDK APIs for that scenario work.
2.2. Do this step before proceeding. Do it now.

## Phase 3: Create a new scenario project template

Reminder: Millions of developers will create their first projects based on the template that we create for this scenario.

3.1. From your direct observations from reading and understanding the files in `input-template` and `input-sample` directories from Phases 1 and 2,
   outline in words, not code, what you believe the new scenario source code should do, what's required for the project file,
   and what's required for the `_.json` file. Be as brief as possible. This is not a design document. This is a set of notes for yourself.

3.2. The new scenario project template should be created in a new `output-template` directory, in the same directory as the `input-template` directory.

3.3. Create the new files, one-by-one, in the new directory, saving each file to disk as you go.

## Phase 4: Create README.md

4.1. Finally, briefly summarize what developers will need to know about the new scenario project template in the `README.md` file in the new directory.
   NOTE: Do not refer to the `_.json` file in the `input-template` directory in the README.md. Develoeprs should not need to know about this file.

## Critical success factors:
- Ensure that the code you create is complete, compilable, and runnable.
- If the input-sample demonstrates an aspect of a use case, your code should demonstrate the same use case.
- Only include project references in the project file if they are needed.
- Use existing files as a reference but do not copy them directly. Do not create a test template just because the input-sample has a test file.
- Avoid using hard-coded strings in the source code, except as method arguments, global constants, or to retrieve information at runtime from environment variables.

Once you've completed these steps, all the files for the new scenario project template should be in the new directory.

## Go

If you haven't started your work, start it now.
