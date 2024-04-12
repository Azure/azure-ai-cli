Status: Draft in Progress
Owner: Rob Chambers

# Using the Azure AI CLI

The Azure `ai` Command-Line Interface (CLI) is a cross-platform command-line tool to connect and immediately use Azure AI services with or without writing code. The CLI allows the execution of commands through a terminal using interactive command-line prompts or via script.

You can easily use the `ai` CLI to experiment with key Azure AI service features and see how they work with your use cases. Within minutes, you can setup the required Azure resources, and build a customized Copilot using OpenAI's chat completions APIs and your own data. You can try it out interactively, or script larger processes to automate your own workflows as part of your CI/CD system.

Additionally you can use the `ai` CLI to dynamically create code to integrate with your own applications in the programming language of your choice (C#, Go, Java, JavaScript, Python, TypeScript).

## **STEP 1**: Setup your development environment

You can install the Azure `ai` CLI locally on Linux, Mac, or Windows computers, or use it thru an internet browser or Docker container.

During this public preview, we recommend using the Azure `ai` CLI thru GitHub Codespaces. This will allow you to quickly get started without having to install anything locally.

### OPTION 1: GitHub Codespaces

You can run the Azure `ai` CLI in a browser using GitHub Codespaces:

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/Azure/azure-ai-cli?quickstart=1)

</div><div class="content" id="content2">

### OPTION 2: VS Code Dev Container

You can run the Azure `ai` CLI in a Docker container using VS Code Dev Containers:

1. Follow the [installation instructions](https://code.visualstudio.com/docs/devcontainers/containers#_installation) for VS Code Dev Containers.
2. Clone the [azure-ai-cli](https://github.com/Azure/azure-ai-cli) repository and open it with VS Code:
    ```
    git clone https://github.com/Azure/azure-ai-cli
    code azure-ai-cli
    ```
3. Click the button "Reopen in Dev Containers", if it does not appear open the command pallete (`Ctrl+Shift+P` on Windows/Linux, `Cmd+Shift+P` on Mac) and run the `Dev Containers: Reopen in Container` command

### OPTION 3: Local Installation

You can install the Azure `ai` CLI locally on your computer:

1. Install [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

    On Linux, follow these ([instructions]()).  
    On macOS, follow these ([instructions]());  
    On Windows, follow these ([instructions]()), or use this command:  

    ```bash
    winget install Microsoft.DotNet.SDK.8
    ```

2. Install or update the Azure `ai` CLI:

    ```bash
    dotnet tool install -g Azure.AI.CLI --prerelease
    ```

    or

    ```bash
    dotnet tool update -g Azure.AI.CLI --prerelease
    ```

</div><div class="content" id="content3">

## **STEP 2**: Initialize resource connections and configuration w/ `ai init`

You can initialize the Azure `ai` CLI by running the following command:

```
ai init
```

<img src="./media/ai-cli-init.png" height=200 alt="ai init console screen showing listbox of choices"/>

Follow the prompts, selecting the Azure subscription, followed by selecting or creating the Azure AI services you want to use.


## **STEP 3**: Inspect various command options and examples w/ `ai help`

You can interactively browse and explore the Azure `ai` CLI commands and options by running the following command:

```
ai help
```

<img src="./media/ai-cli-help.png" height=240 alt="ai help console screen help content"/>


## **STEP 4**:  Chat with your LLM w/ `ai chat`

You can chat interactively or non-interactively with an AI language model using the `ai chat` command.

**Interactive chat**

```bash
ai chat --interactive
```

**Non-interactive chat**

```bash
ai chat --user "Tell me about Azure OpenAI"
```

**Command line options**

```
USAGE: ai chat [...]

  CONNECTION                            (see: ai help connection)
    --deployment DEPLOYMENT             (see: ai help chat deployment)
    --endpoint ENDPOINT                 (see: ai help chat endpoint)
    --key KEY                           (see: ai help chat key)

  INPUT                                 (see: ai help chat input)
    --interactive                       (see: ai help chat interactive)
    --system PROMPT                     (see: ai help chat system prompt)
    --user MESSAGE                      (see: ai help chat user message)
    --chat-history FILE                 (see: ai help chat history)

  CHAT WITH DATA                        (see: ai help chat with data)
    --index-name INDEX                  (see: ai help index name)
    --search-endpoint ENDPOINT          (see: ai help search endpoint)
    --search-api-key KEY                (see: ai help search key)

  OPTIONS                               (see: ai help chat options)
    --temperature TEMPERATURE           (see: ai help chat options temperature)
    --max-tokens MAX_TOKENS             (see: ai help chat options max-tokens)
    --top-p TOP_P                       (see: ai help chat options top-p)
    --n N                               (see: ai help chat options n)
```

## **STEP 5**: Create and update an AI Search Index w/ `ai search index update`

You can create an AI Search Index using the `ai search index create` command.

```bash
ai search index create --index-name MyMarkdownFiles --files "*.md" --blob-container https://aitest123.blob.core.windows.net/product-info
```

**Command line options**

```
USAGE: ai index create [...]

  AZURE SEARCH
    --index-name NAME                       (see: ai help search index name)
    --search-api-key KEY                    (see: ai help search api key)
    --search-endpoint ENDPOINT              (see: ai help search endpoint)

  AZURE SEARCH DATA SOURCE
    --data-source-connection NAME           (see: ai help search data source connection)
    --blob-container ENDPOINT/NAME          (see: ai help search data source blob container)
    --indexer-name NAME                     (see: ai help search indexer name)
    --skillset-name NAME                    (see: ai help search skillset name)
    --id-field NAME                         (see: ai help search id field name)
    --content-field NAME                    (see: ai help search content field name)
    --vector-field NAME                     (see: ai help search vector field name)

  OPENAI EMBEDDINGS
    --embedding-deployment DEPLOYMENT       (see: ai help search embedding deployment)
    --embedding-model MODEL                 (see: ai help search embedding model)

  DATA
    --file FILE                             (see: ai help search index file)
    --files FILEs                           (see: ai help search index files)
    --external-source                       (see: ai help search index external source)
```

## **STEP 6**: Chat with your LLM using your AI Search Index w/ `ai chat --index-name`

You can chat interactively or non-interactively with an AI language model with your AI Search indexed data using the `ai chat` command with the `--index-name` option.

First, let's create a system prompt file that will be used to seed the chat with a question about a product:

```bash
nano prompt.txt
```

Copy and paste the following text into the file:

```
You are an AI assistant helping users with queries related to
outdoor/camping gear and clothing. Use the following pieces of context
to answer the questions about outdoor/camping gear and clothing as
completely, correctly, and concisely as possible. If the question is not
related to outdoor/camping gear and clothing, just say Sorry, I only can
answer question related to outdoor/camping gear and clothing. So how can
I help? Don't try to make up an answer. If the question is related to
outdoor/camping gear and clothing but vague ask for clarifying questions.
Do not add documentation reference in the response.
```

**Interactive chat**

```bash
ai chat --system @prompt.txt --index-name MyMarkdownFiles --interactive 
```

**Non-interactive chat**

```bash
ai chat --system @prompt.txt --index-name MyMarkdownFiles --user "What is the best tent for camping?"
```

## **STEP 7**: Create an application that uses your AI Search Index w/ `ai dev new`

You can create a new application that uses your AI Search Index using the `ai dev new` command.

First, discover all the quick start sample templates available using the `ai dev new list` command, or filtered using `ai dev new list FILTER1 FILTER2`:

`ai dev new list Chat`:

```
Short Name                              Language
------------------------------------    --------------------------------
openai-chat                             C#, Go, Java, JavaScript, Python
openai-chat-streaming                   C#, Go, Java, JavaScript, Python
openai-chat-streaming-with-data         C#, Go, Java, JavaScript, Python
openai-chat-streaming-with-functions    C#, Go, JavaScript, Python
openai-chat-webpage                     JavaScript, TypeScript
openai-chat-webpage-with-functions      JavaScript, TypeScript
```

`ai dev new list Assistants`:

```
Short Name                              Language
------------------------------------    ----------------------
openai-asst                             JavaScript
openai-asst-streaming                   JavaScript
openai-asst-streaming-with-functions    JavaScript
openai-asst-webpage                     JavaScript, TypeScript
openai-asst-webpage-with-functions      JavaScript
```

Now, let's create a JavaScript sample demonstrating how to "chat with your data", with streaming output from the LLM: 

`ai dev new openai-chat-streaming-with-data --javascript`:

```
Generating 'openai-chat-streaming-with-data-js' (3 files)...

  Main.js
  OpenAIChatCompletionsStreamingWithDataClass.js
  package.json

Generating 'openai-chat-streaming-with-data-js' (3 files)... DONE!
```

## **STEP 8**: Run your application w/ `ai dev shell --run`

You can run your application using the `ai dev shell --run` command.

First, navigate to the directory created with the `ai dev new` command above:

```bash
cd openai-chat-streaming-with-data-js
```

Install the dependencies for your application:

```bash
npm install
```

Then, run your application:

```bash
ai dev shell --run "node Main.js"
```
