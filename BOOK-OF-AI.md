# AI CLI

Let's do a quick tour of the various AI capabilities available in Azure via the new Azure AI CLI (`ai`). 

**INSTALLATION and SETUP**  
[CHAPTER 1: CLI Installation](#chapter-1-cli-installation)  
[CHAPTER 2: Setup w/ Azure OpenAI](#chapter-2-setup-w-azure-openai)  

**OPENAI CHAT COMPLETIONS**  
[CHAPTER 3: Chat Completion Basics](#chapter-3-openai-chat-completions-basics)  
[CHAPTER 4: Chat Completions w/ Function Calling](#chapter-4-openai-chat-completions-w-function-calling)  
[CHAPTER 5: Chat Completions w/ RAG + AI Search](#chapter-5-openai-chat-completions-w-rag--ai-search)  

**OPENAI ASSISTANTS API**  
[CHAPTER 6: Assistants API](#chapter-6-openai-assistants-api)  
[CHAPTER 7: Assistants Basics](#chapter-7-openai-assistants-basics)  
[CHAPTER 8: Assistants w/ Code Interpreter](#chapter-8-openai-assistants-w-code-interpreter)  
[CHAPTER 9: Assistants w/ Function Calling](#chapter-9-openai-assistants-w-function-calling)  
[CHAPTER 10: Assistants w/ File Search](#chapter-10-openai-assistants-w-file-search)  

**GITHUB MODEL MARKETPLACE**  
[CHAPTER 11: Model Marketplace](#chapter-11-github-model-marketplace)  
[CHAPTER 12: Chat Completion Basics](#chapter-12-chat-completion-basics)  
[CHAPTER 13: Chat Completions W/ Function Calling](#chapter-13-chat-completions-w-function-calling)  

**AZURE AI INFERENCING**  
[CHAPTER 14: AI Studio and the Model Catalog](#chapter-14-ai-studio-and-the-model-catalog)  
[CHAPTER 15: Chat Completions Basics](#chapter-15-chat-completions-basics)  
[CHAPTER 16: Chat Completions w/ Function Calling](#chapter-16-chat-completions-w-function-calling)🚧  

**LOCAL INFERENCING W/ ONNX AND PHI-3**  
[CHAPTER 17: PHI-3 Models](#chapter-17-phi-3-models)  
[CHAPTER 18: ONNX Chat Completions](#chapter-18-onnx-chat-completions)  
[CHAPTER 19: ONNX Chat Completions w/ Function Calling](#chapter-19-onnx-chat-completions-w-function-calling)  

**SPEECH INPUT AND OUTPUT**  
[CHAPTER 20: Speech Synthesis](#chapter-20-speech-synthesis)  
[CHAPTER 21: Speech Recognition](#chapter-21-speech-recognition)    
[CHAPTER 22: Speech Translation](#chapter-22-speech-translation)    
[CHAPTER 23: Speech Recognition w/ Keyword Spotting](#chapter-23-speech-recognition-w-keyword-spotting)  

**MULTI-MODAL AI**  
[CHAPTER 24: Multi-Modal AI](#chapter-24-multi-modal-ai)  
[CHAPTER 25: Chat Completions w/ Speech Input](#chapter-25-chat-completions-w-speech-input)  
[CHAPTER 26: Chat Completions w/ Speech Input and Output](#chapter-26-chat-completions-w-speech-input-and-output)  
[CHAPTER 27: Chat Completions w/ Image Input](#chapter-27-chat-completions-w-image-input)  
[CHAPTER 28: Chat Completions w/ Image Output](#chapter-28-chat-completions-w-image-output)  

**SEMANTIC KERNEL AGENTS**  
[CHAPTER 29: Semantic Kernel Basics](#chapter-29-semantic-kernel-basics)  
[CHAPTER 30: Semantic Kernel w/ Function Calling](#chapter-30-semantic-kernel-w-function-calling)  
[CHAPTER 31: Semantic Kernel w/ Basic Agents](#chapter-31-semantic-kernel-w-basic-agents)  
[CHAPTER 32: Semantic Kernel w/ Advanced Agents](#chapter-32-semantic-kernel-w-advanced-agents)  

## CHAPTER 1: CLI Installation

🔗 [**CLI Installation**](#chapter-1-cli-installation)  

⏹️ Install the pre-requisites for the Azure AI CLI (`ai`)  
⏹️ `winget install Microsoft.DotNet.SDK.8`  

⏹️ Install the Azure AI CLI (`ai`) on Linux, Mac, or Windows  
⏹️ `dotnet tool install -g Microsoft.Azure.AI.CLI --prerelease`  

⏹️ OR: Use the Azure AI CLI (`ai`) in a GitHub Codespace  
⏹️ OR: Use the Azure AI CLI (`ai`) in a Docker container  

## CHAPTER 2: Setup w/ Azure OpenAI

🔗 [**Setup w/ Azure OpenAI**](#chapter-2-setup-w-azure-openai)  

⏹️ `ai init openai`  
⏹️ Select your Azure subscription  
⏹️ Select or create your Azure OpenAI resource  
⏹️ Select or create an OpenAI chat model deployment (e.g. gpt-4o)  
⏹️ Select or create an OpenAi embeddings model deployment  

## CHAPTER 3: OpenAI Chat Completions Basics

🔗 [**OpenAI Chat Completion Basics**](#chapter-3-openai-chat-completions-basics)  

⏹️ System prompts, user input, interactive chats w/ history  
⏹️ `ai chat --user "What is the capital of France?"`  
⏹️ `ai chat --interactive`  
⏹️ `ai chat --interactive --system @prompt.txt`  
⏹️ `ai chat --interactive --system @prompt.txt --user "Tell me a joke"`  
⏹️ `ai chat --interactive --output-answer answer.txt`  
⏹️ `ai chat --interactive --output-chat-history history.jsonl`  
⏹️ `ai chat --interactive --input-chat-history history.jsonl`  

⏹️ Generate console apps for chat completions  
⏹️ `ai dev new list`  
⏹️ `ai dev new list chat`  
⏹️ `ai dev new openai-chat --csharp` or `--python` or `--javascript` ...  
⏹️ `ai dev new openai-chat-streaming --csharp` or `--python` or `--javascript` ...  

⏹️ Go over what was generated in the console app  
⏹️ ... getting connection info/secrets from environment variables  
⏹️ ... using a helper class to encapsulate the OpenAI API calls  
⏹️ ... getting input from the user  
⏹️ ... sending the input to the helper class  
⏹️ ... getting the response from the helper class  
⏹️ ... deeper dive into the helper class  

⏹️ Install the dependencies  
⏹️ `dotnet restore` or `pip install -r requirements.txt` or `npm install` ...  

⏹️ Run the console app  
⏹️ `ai dev shell`  
⏹️ `dotnet run` or `python main.py` or `node main.js` ...

## CHAPTER 4: OpenAI Chat Completions w/ Function Calling

🔗 [**OpenAI Chat Completions w/ Function Calling**](#chapter-4-openai-chat-completions-w-function-calling)  

⏹️ Extending the LLM's world knowledge with functions  
⏹️ `ai chat --user "What time is it?"` => doesn't know the time  
⏹️ `ai chat --user "What time is it?" --built-in-functions` => works!  
⏹️ `ai chat --user "What is in the README.md file?" --built-in-functions`  

⏹️ Allowing the LLM to interact with your code  
⏹️ `ai chat --user "Save the pledge of allegiance to 'pledge.txt'"` => doesn't work  
⏹️ `ai chat --user "Save the pledge of allegiance to 'pledge.txt'" --built-in-functions` => works!  

⏹️ Generating code for function calling  
⏹️ `ai dev new list function`  
⏹️ `ai dev new openai-chat-streaming-with-functions --csharp` or `--python` or `--javascript` ...  

⏹️ Go over what was generated in the console app  
⏹️ ... builds on previous chapter's console app  
⏹️ ... see how functions are defined, given to "function factory"  
⏹️ ... in helper class, see how functions are given to the LLM  
⏹️ ... see how the LLM streams back the function call requests  
⏹️ ... see how the helper class processes the function call responses  

⏹️ Install the dependencies  
⏹️ `dotnet restore` or `pip install -r requirements.txt` or `npm install` ...  

⏹️ Run the console app  
⏹️ `ai dev shell`  
⏹️ `dotnet run` or `python main.py` or `node main.js` ...

## CHAPTER 5: OpenAI Chat Completions w/ RAG + AI Search

🔗 [**OpenAI Chat Completions w/ RAG + AI Search**](#chapter-5-openai-chat-completions-w-rag--ai-search)  

⏹️ `ai init search`  
⏹️ Select your Azure subscription  
⏹️ Select or create your Azure AI Search resource  

⏹️ Create or update your Azure AI Search index  
⏹️ `ai search index create --name MyFiles --files *.md --blob-container https://...`  
⏹️ `ai search index update --name MyFiles --files *.md --blob-container https://...`  

⏹️ Use the RAG model to search your AI Search index  
⏹️ `ai chat --user "What is the capital of France?" --index MyFiles`  

⏹️ Generate code for RAG + AI Search  
⏹️ `ai dev new openai-chat-streaming-with-data --csharp` or `--python` or `--javascript` ...  

⏹️ Go over what was generated in the console app  
⏹️ ... builds on Chapter 4's console app  
⏹️ ... see how the helper class gives the LLM access to the AI Search index  
⏹️ ... see how the LLM sends back citations to the helper class  
⏹️ ... see how the helper class processes the citations  

⏹️ Install the dependencies  
⏹️ `dotnet restore` or `pip install -r requirements.txt` or `npm install` ...  

⏹️ Run the console app  
⏹️ `ai dev shell`  
⏹️ `dotnet run` or `python main.py` or `node main.js` ...

## CHAPTER 6: OpenAI Assistants API

🔗 [OpenAI Assistants API](#chapter-6-openai-assistants-api)  

⏹️ Differences between chat completions and assistants  
⏹️ ... stateless vs stateful  
⏹️ ... customer controlled chat history vs threads  
⏹️ ... automatic context window management  
⏹️ ... advanced features: code interpreter, function calling, file search  

⏹️ Listing, creating, updating, and deleting assistants  
⏹️ `ai chat assistant`  
⏹️ `ai chat assistant list`  
⏹️ `ai chat assistant create --name MyAssistant`  
⏹️ `ai chat assistant update --instructions @instructions.txt`  
⏹️ `ai chat assistant delete --id ID`  

## CHAPTER 7: OpenAI Assistants Basics

🔗 [OpenAI Assistants Basics](#chapter-7-openai-assistants-basics)  

⏹️ Create a simple assistant  
⏹️ `ai chat assistant create --name MyAssistant`  
⏹️ `ai config @assistant.id`

⏹️ Threads ...  
⏹️ `ai chat --interactive`  
⏹️ `ai chat --interactive --thread-id ID` (from previous chat)  

⏹️ `ai chat --question "..." --output-thread-id myNewThread.txt`  
⏹️ `ai chat --question "..." --thread-id @myNewThread.txt`  
⏹️ `ai chat --interactive --thread-id @myNewThread.txt --output-chat-history history.jsonl`  

⏹️ Delete the assistant  
⏹️ `ai chat assistant delete`  
⏹️ `ai config --clear assistant.id`  

⏹️ Generate code for using assistants  
⏹️ `ai dev new list asst`  
⏹️ `ai dev new openai-asst-streaming --csharp` or `--python` or `--javascript` ...  

⏹️ Go over what was generated in the console app  
⏹️ ... similar to console apps generated in earlier chapters
⏹️ ... see how the LLM sends back citations to the helper class  
⏹️ ... see how the helper class processes the citations  

⏹️ Install the dependencies  
⏹️ `dotnet restore` or `pip install -r requirements.txt` or `npm install` ...  

⏹️ Run the console app  
⏹️ `ai dev shell`  
⏹️ `dotnet run` or `python main.py` or `node main.js` ...


## CHAPTER 8: OpenAI Assistants w/ Code Interpreter

🔗 [OpenAI Assistants w/ Code Interpreter](#chapter-8-openai-assistants-w-code-interpreter)  

⏹️ Create or update an assistant with a code interpreter  
⏹️ `ai chat assistant create --name MyCodeAssistant --code-interpreter`  
⏹️ `ai chat assistant update --code-interpreter`  

⏹️ Use the code interpreter in the assistant  
⏹️ `ai chat --interactive --question "how many e's are there in the pledge of allegiance?"`  
⏹️ ... `how'd you do that?`  

## CHAPTER 9: OpenAI Assistants w/ Function Calling

🔗 [OpenAI Assistants w/ Function Calling](#chapter-9-openai-assistants-w-function-calling)  

...

## CHAPTER 10: OpenAI Assistants w/ File Search

🔗 [OpenAI Assistants w/ File Search](#chapter-10-openai-assistants-w-file-search)  

...

## CHAPTER 11: GitHub Model Marketplace

🔗 [GitHub Model Marketplace](#chapter-11-github-model-marketplace)  

...

## CHAPTER 12: Chat Completion Basics

🔗 [Chat Completion Basics](#chapter-12-chat-completion-basics)  

...

## CHAPTER 13: Chat Completions W/ Function Calling

🔗 [Chat Completions W/ Function Calling](#chapter-13-chat-completions-w-function-calling)  

...

## CHAPTER 14: AI Studio and the Model Catalog

🔗 [AI Studio and the Model Catalog](#chapter-14-ai-studio-and-the-model-catalog)  

...

## CHAPTER 15: Chat Completions Basics

🔗 [Chat Completions Basics](#chapter-15-chat-completions-basics)  

...

## CHAPTER 16: Chat Completions w/ Function Calling

🔗 [Chat Completions w/ Function Calling](#chapter-16-chat-completions-w-function-calling)  

...

## CHAPTER 17: PHI-3 Models

🔗 [PHI-3 Models](#chapter-17-phi-3-models)  

...

## CHAPTER 18: ONNX Chat Completions

🔗 [ONNX Chat Completions](#chapter-18-onnx-chat-completions)

...

## CHAPTER 19: ONNX Chat Completions w/ Function Calling

🔗 [ONNX Chat Completions w/ Function Calling](#chapter-19-onnx-chat-completions-w-function-calling)  

...

## CHAPTER 20: Speech Synthesis

🔗 [Speech Synthesis](#chapter-20-speech-synthesis)  

...

## CHAPTER 21: Speech Recognition

🔗 [Speech Recognition](#chapter-21-speech-recognition)  

...

## CHAPTER 22: Speech Translation

🔗 [Speech Translation](#chapter-22-speech-translation)  

...

## CHAPTER 23: Speech Recognition w/ Keyword Spotting

🔗 [Speech Recognition w/ Keyword Spotting](#chapter-23-speech-recognition-w-keyword-spotting)  

...

## CHAPTER 24: Multi-Modal AI

🔗 [Multi-Modal AI](#chapter-24-multi-modal-ai)  

...

## CHAPTER 25: Chat Completions w/ Speech Input

🔗 [Chat Completions w/ Speech Input](#chapter-25-chat-completions-w-speech-input)  

...

## CHAPTER 26: Chat Completions w/ Speech Input and Output

🔗 [Chat Completions w/ Speech Input and Output](#chapter-26-chat-completions-w-speech-input-and-output)  

...

## CHAPTER 27: Chat Completions w/ Image Input

🔗 [Chat Completions w/ Image Input](#chapter-27-chat-completions-w-image-input)  

...

## CHAPTER 28: Chat Completions w/ Image Output

🔗 [Chat Completions w/ Image Output](#chapter-28-chat-completions-w-image-output)  

...

## CHAPTER 29: Semantic Kernel Basics

🔗 [Semantic Kernel Basics](#chapter-29-semantic-kernel-basics)  

...

## CHAPTER 30: Semantic Kernel w/ Function Calling

🔗 [Semantic Kernel w/ Function Calling](#chapter-30-semantic-kernel-w-function-calling)  

...

## CHAPTER 31: Semantic Kernel w/ Basic Agents

🔗 [Semantic Kernel w/ Basic Agents](#chapter-31-semantic-kernel-w-basic-agents)  

...

## CHAPTER 32: Semantic Kernel w/ Advanced Agents

🔗 [Semantic Kernel w/ Advanced Agents](#chapter-32-semantic-kernel-w-advanced-agents)  

...

