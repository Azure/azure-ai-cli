# next steps

you should try to get this to work:

```
ai init project
ai help list topics --dump
ai search index update --files *.md --index ai-help
ai chat --interactive --index ai-help
```

then you'll have all the parts to update the `ai wizard` for the `/w your data` scenario

# making names easier to enter

```c#
private IEnumerable<string> GetResourceNamesFromResourceGroupName(string rg, string resourceKindAbbreviation, string resourceKindName)
{
    var startsWith = rg.StartsWith("rg-") ? "rg-" : rg.StartsWith("ResourceGroup") ? "ResourceGroup" : null;
    var endsWith = rg.EndsWith("-rg") ? "-rg" : rg.EndsWith("ResourceGroup") ? "ResourceGroup" : null;
    var contains = rg.Contains("-rg-") ? "-rg-" : rg.Contains("ResourceGroup") ? "ResourceGroup" : null;

    if (!startsWith || !endsWith || !contains) yield break;

    if (startsWith)
    {
        var name = rg.Substring(startsWith.Length);
        yield return $"{resourceKindName}-{name}";
        yield return $"{resourceKindAbbreviation}-{name}"
    }

    if (endsWith)
    {
        var name = rg.Substring(0, rg.Length - endsWith.Length);
        yield return $"{name}-{resourceKindName}";
        yield return $"{name}-{resourceKindAbbreviation}"
    }

    if (contains)
    {
        var parts = rg.Split(contains);
        if (parts.Length == 2)
        {
            yield return $"{parts[0]}-{resourceKindName}-{parts[1]}";
            yield return $"{parts[0]}-{resourceKindAbbreviation}-{parts[1]}";
        }
    }
}
```

# `ai` CLI Tasks to complete  

(similar breakdwon on ADO at [https://crbn.us/ai.ado](https://crbn.us/ai.ado))  

|    |Hrs|Aug|Ignite|   |
|----|---|---|---|---|
|    | - |✔️|   |  ✅ `ai-cli` github repo (private for now)  
|    | - |✔️|   |  ✅ `ai` CLI: basic structure (csproj, files, etc.)  
|    |   |   |   |  
|    | - |✔️|   |  ✅ CI: scripts + nuget tool package  
|    | - |✔️|   |  ✅ CI: scripts + github build actions  
|    |40+|???|???|  ⏹️ CI: scripts + github test actions  
|    |16+|???|???|  ⏹️ CD: how to get nuget to vs code hosted container
|    |   |   |   |  
|    | - |✔️|   |  ✅ `ai help`: basic functionality and IA  
|    | - |✔️|   |  ✅ `ai help`: placeholder help content for all commands/sub-commands  
|    |   |   |   |  
|    | - |✔️|   |  ✅ `ai wizard`: basic functionality and IA  
|    | 4 | x |???|  ⏹️ `ai wizard`: placeholder for all August-Hero wizard steps  
|    |   |   |   |  
|    | - | ✔️|   |  ✅ `ai speech`: placeholder for speech commands (parser, warning)  
|    | - | ✔️|   |  ✅ `ai vision`:  placeholder for vision commands (parser, warning)  
|    | - | ✔️|   |  ✅ `ai language`: placeholder for language commands (parser, warning)  
|    | - | ✔️|   |  ✅ `ai search`: initial placeholder for search commands (parser, warning)  
|    | - | ✔️|   |  ✅ `ai service`: initial placeholder for service commands (parser, warning)  
|    | - | ✔️|   |  ✅ `ai tool`: initial placeholder for tool commands (parser, warning)  
|    |   |   |   |  
|    | - | ✔️|   |  ⏹️ python: proof of life: enumerate hubs  
|    | - | ✔️|   |  ⏹️ python: proof of life: create hubs  
|    | 1 | x |???|  ⏹️ python: proof of life: update hubs  
|    | 1 | x |???|  ⏹️ python: proof of life: delete hubs  
|    |   |   |???|  
|    | - | ✔️|   |  ⏹️ python: proof of life: enumerate projects  
|    | - | ✔️|   |  ⏹️ python: proof of life: create projects  
|    | 1 | x |???|  ⏹️ python: proof of life: update projects  
|    | 1 | x |???|  ⏹️ python: proof of life: delete projects  
|    |   |   |   |  
|    | 1 | x |???|  ⏹️ python: proof of life: enumerate deployments  
|    | 1 | x |???|  ⏹️ python: proof of life: create deployments  
|    | 1 | x |???|  ⏹️ python: proof of life: update deployments  
|    | 1 | x |???|  ⏹️ python: proof of life: delete deployments  
|    |   |   |   |  
| P1 | 1 | x |   |  ⏹️ python: proof of life: enumerate indexes  
| P1 | 1 | x |   |  ⏹️ python: proof of life: create indexes  
|    | 1 | x |???|  ⏹️ python: proof of life: update indexes  
|    | 1 | x |???|  ⏹️ python: proof of life: delete indexes  
|    |   |   |   |  
|    | 1 | x |???|  ⏹️ python: proof of life: enumerate connection  
|    | 1 | x |???|  ⏹️ python: proof of life: create connection  
|    | 1 | x |???|  ⏹️ python: proof of life: update connection  
|    | 1 | x |???|  ⏹️ python: proof of life: delete connection  
|    |   |   |   |  
|    | 1 |   | x |  ⏹️ python: proof of life: enumerate evaluations  
|    | 1 |   | x |  ⏹️ python: proof of life: create evaluations  
|    | 1 |   | x |  ⏹️ python: proof of life: update evaluations  
|    | 1 |   | x |  ⏹️ python: proof of life: delete evaluations  
|    |   |   |   |  
|    | 1 |   | x |  ⏹️ python: proof of life: enumerate flows  
|    | 1 |   | x |  ⏹️ python: proof of life: create flows  
|    | 1 |   | x |  ⏹️ python: proof of life: update flows  
|    | 1 |   | x |  ⏹️ python: proof of life: delete flows  
|    |   |   |   |  
|    | - |✔️|   |  ⏹️ `ai service`: `help`  
|    | - |✔️|   |  ⏹️ `ai service`: parser  
|    |   |   |   |  
|    | - |✔️|   |  ⏹️ `ai service resource`: `--help`
|    | - |✔️|   |  ⏹️ `ai service resource`: create hub
|    | - |✔️|   |  ⏹️ `ai service resource`: list hubs
|    | 1 |???|???|  ⏹️ `ai service resource`: update hub
|    | 1 |???|???|  ⏹️ `ai service resource`: delete hub
|    | 4 |   | x |  ⏹️ `ai service resource`: `init`
|    | - |✔️|   |  ⏹️ `ai service resource`: `--output`
|    |   |   |   |  
|    | - |✔️|   |  ⏹️ `ai service project`: `--help`
|    | - |✔️|   |  ⏹️ `ai service project`: create
|    | - |✔️|   |  ⏹️ `ai service project`: list
|    | 1 |???|???|  ⏹️ `ai service project`: update
|    | 1 |???|???|  ⏹️ `ai service project`: delete  
|    | 4 |   | x |  ⏹️ `ai service project`: `init`
|    | 2 |✔️|   |  ⏹️ `ai service project`: `--output`
|    |   |   |   |  
|    | 1 |???|???|  ⏹️ `ai service connection`: `--help`
|    | 1 |???|???|  ⏹️ `ai service connection`: create
|    | 1 |???|???|  ⏹️ `ai service connection`: status
|    | 2 |???|???|  ⏹️ `ai service connection`: list
|    | 1 |???|???|  ⏹️ `ai service connection`: update
|    | 1 |???|???|  ⏹️ `ai service connection`: delete  
|    | 4 |???|???|  ⏹️ `ai service connection`: `init`
|    | 2 |???|???|  ⏹️ `ai service connection`: `--output`  
|    | 1 |???|???|  ⏹️ `ai service connection`: `--wait`  
|    |   |   |   |  
|    | - |✔️|   |  ⏹️ `ai service deployment`: `--help`
| P1 | 1 | x |   |  ⏹️ `ai service deployment`: create
|    | 1 |???|???|  ⏹️ `ai service deployment`: status
| P1 | 2 | x |   |  ⏹️ `ai service deployment`: list
|    | 1 |???|???|  ⏹️ `ai service deployment`: update
|    | 1 |???|???|  ⏹️ `ai service deployment`: delete  
|    | 4 |???|???|  ⏹️ `ai service deployment`: `init`
| P1 | 2 | x |   |  ⏹️ `ai service deployment`: `--output`
|    |   |   |   |  
|    | 1 |???|???|  ⏹️ `ai service flow`: `--help`
|    | 1 |???|???|  ⏹️ `ai service flow`: create
|    | 1 |???|???|  ⏹️ `ai service flow`: status
|    | 2 |???|???|  ⏹️ `ai service flow`: list
|    | 1 |???|???|  ⏹️ `ai service flow`: update
|    | 1 |???|???|  ⏹️ `ai service flow`: delete  
|    | 1 |???|???|  ⏹️ `ai service flow`: `--output`  
|    | 1 |???|???|  ⏹️ `ai service flow`: `--wait`  
|    |   |   |   |  
|    | 1 | x |???|  ⏹️ `ai service evaluation`: `--help`
|    | 1 |   | x |  ⏹️ `ai service evaluation`: create
|    | 1 |   | x |  ⏹️ `ai service evaluation`: status
|    | 2 |   | x |  ⏹️ `ai service evaluation`: list
|    | 1 |   | x |  ⏹️ `ai service evaluation`: update
|    | 1 |   | x |  ⏹️ `ai service evaluation`: delete  
|    | 4 |   | x |  ⏹️ `ai service evaluation`: `init`
|    | 2 |   | x |  ⏹️ `ai service evaluation`: `--output`  
|    | 1 |   | x |  ⏹️ `ai service evaluation`: `--wait`  
|    |   |   |   |  
|    | - |✔️|   |  ✅ `ai init`: basic functionality (OpenAI resource, not AI Hub)  
|    | - |✔️|   |  ✅ `ai init`: deployement, pick or create, list models, pick  
| P1 | 4 | x |   |  ⏹️ `ai init`: update to use AI Hubs  
|    |   |   |   |  
|    | - |✔️|   |  ✅ `ai config`: basic functionality and IA  
| P1 | 4 | x |   |  ⏹️ `ai config`: update defaults structure to match AI Hub/Project usage  
|    |   |   |   |  
|    | - |✔️|   |  ✅ `ai chat`: basic functionality for interactive chat  
|    | - |✔️|   |  ✅ `ai chat`: w/ my data poc: embeddings provider, index provider, etc.  
|    | - |✔️|   |  ✅ `ai chat`: command line inputs: prompt, temperature, top-p, frequency penalty, presence penalty, etc.  
| P1 | 4 | x |   |  ⏹️ `ai chat`: command line inputs: non-interactive chat: input file, output file, etc.  
|    |12 | x |???|  ⏹️ `ai chat`: interactive chat (hot-reload)  
|    |   |   |   |  
|    | 2 | x |???|  ⏹️ `ai chat`: spec and write help for all inputs  
| P1 | 4 | x |???|  ⏹️ `ai chat`: w/ my data: migrate to AI Hub usage  
|    |   |   |   |  
|    | 8 |???|???|  ⏹️ `ai eval`: spec and write help for all inputs  
|    | 8 |???|???|  ⏹️ `ai eval`: basic functionality  
|    |???|???|???|  ⏹️ `ai eval`: more advanced functionality (needs breakdown)  
|    |   |   |   |  
|    | 4 |???|???|  ⏹️ aka.ms: links for all `https://` used in help and code  
|    |   |   |   |  
|    | - |✔️|   |  ✅ `ai wizard`: Chat (OpenAI) w/ OpenAIClient w/ key, endpoint, region  
| P1 | 2 | x |   |  ⏹️ `ai wizard`: Chat (OpenAI) w/ OpenAIClient w/ AI Hub  
|    | 4 | x |   |  ⏹️ `ai wizard`: Chat (OpenAI) Quickstart  
|    | 2 |???|???|  ⏹️ `ai wizard`: Chat (OpenAI) - Step 1: Initialize  
|    | 2 |???|???|  ⏹️ `ai wizard`: Chat (OpenAI) - Step 2: Interact/chat  
|    |16 | x |???|  ⏹️ `ai wizard`: Chat (OpenAI) - Step 3: Generate code (python) 
|    |16 | x |???|  ⏹️ `ai wizard`: Chat (OpenAI) - Step 3: Generate code (C#) 
|    |???|???|???|  ⏹️ `ai wizard`: Chat (OpenAI) - Step 4: Deploy  
|    |???|???|???|  ⏹️ `ai wizard`: Chat (OpenAI) - Step 5: Evaluate  
|    |???|???|???|  ⏹️ `ai wizard`: Chat (OpenAI) - Step 6: Update  
|    | 4 |???|???|  ⏹️ `ai wizard`: Chat (OpenAI) - Step 7: Clean up  
|    |   |   |   |  
|    | - |✔️|   |  ✅ `ai wizard`: Chat (OpenAI) w/ your prompt w/ OpenAIClient w/ key, endpoint, region  
| P1 | 2 | x |???|  ⏹️ `ai wizard`: Chat (OpenAI) w/ your prompt w/ OpenAIClient w/ AI Hub  
|    | 4 | x |   |  ⏹️ `ai wizard`: Chat (OpenAI) w/ your prompt Quickstart  
|    | 2 |???|???|  ⏹️ `ai wizard`: Chat (OpenAI) w/ your prompt - Step 1: Initialize  
|    | 2 |???|???|  ⏹️ `ai wizard`: Chat (OpenAI) w/ your prompt - Step 2: Interact/chat  
|    | 8 | x |???|  ⏹️ `ai wizard`: Chat (OpenAI) w/ your prompt - Step 3: Generate code (python)
|    | 4 | x |???|  ⏹️ `ai wizard`: Chat (OpenAI) w/ your prompt - Step 3: Generate code (C#)
|    |???|???|???|  ⏹️ `ai wizard`: Chat (OpenAI) w/ your prompt - Step 4: Deploy  
|    |???|???|???|  ⏹️ `ai wizard`: Chat (OpenAI) w/ your prompt - Step 5: Evaluate  
|    |???|???|???|  ⏹️ `ai wizard`: Chat (OpenAI) w/ your prompt - Step 6: Update  
|    | 4 |???|???|  ⏹️ `ai wizard`: Chat (OpenAI) w/ your prompt - Step 7: Clean up  
|    |   |   |   |  
|    | - |✔️|   |  ✅ `ai wizard`: Chat (OpenAI) w/ your data w/ OpenAIClient w/ key, endpoint, region  
| P1 | 2 | x |   |  ⏹️ `ai wizard`: Chat (OpenAI) w/ your data w/ OpenAIClient w/ AI Hub  
|    | 4 | x |   |  ⏹️ `ai wizard`: Chat (OpenAI) w/ your data Quickstart  
|    | 2 |???|???|  ⏹️ `ai wizard`: Chat (OpenAI) w/ your data - Step 1: Initialize  
|    | 2 |???|???|  ⏹️ `ai wizard`: Chat (OpenAI) w/ your data - Step 2: Interact/chat  
|    | 8 | x |???|  ⏹️ `ai wizard`: Chat (OpenAI) w/ your data - Step 3: Generate code (python)
|    | 4 | x |???|  ⏹️ `ai wizard`: Chat (OpenAI) w/ your data - Step 3: Generate code (C#)
|    |???|???|???|  ⏹️ `ai wizard`: Chat (OpenAI) w/ your data - Step 4: Deploy  
|    |???|???|???|  ⏹️ `ai wizard`: Chat (OpenAI) w/ your data - Step 5: Evaluate  
|    |???|???|???|  ⏹️ `ai wizard`: Chat (OpenAI) w/ your data - Step 6: Update  
|    | 4 |???|???|  ⏹️ `ai wizard`: Chat (OpenAI) w/ your data - Step 7: Clean up  
|    |   |   |   |  
|    |12 |   | x |  ⏹️ `ai speech recognize`: Basic SR functionality  
|    |12 |   | x |  ⏹️ `ai speech synthesize`: Basic TTS functionality  
|    |12 |   | x |  ⏹️ `ai speech synthesize`: Basic Translation functionality  
|    |   |   |   |  
|    | ! |???|???|  ⏹️ PM: prompt flow: what's relationship between prompt flow CLI and `ai` CLI?  
|    | ! |???|???|  ⏹️ PM: help: how/where/who will docs? samples?  
|    | ! |???|???|  ⏹️ PM: studio: integration plan with studio (needs breakdown)  
|    | ! |???|???|  ⏹️ PM: vscode: integration plan with hosted VS Code plans (needs breakdown)  
|    | ! |???|???|  ⏹️ PM: cloud shell: how to get it here almost by default if not default
|    | ! |???|???|  ⏹️ PM: nuget: how to publish to nuget with the correct owner (same as az-cli)  
|    | ! |???|???|  ⏹️ PM: content safety aspects to CLI?  
|    | ! |???|???|  ⏹️ PM: "model" aspects to CLI?  
|    |   |   |   |  
|    |XL |   | x |  ⏹️ `ai speech`: port all SPX functionality (needs more breakdown)  
|    |XL |   | x |  ⏹️ `ai vision`: port all VZ functionality (needs more breakdown)  
|    |XL++|  | x |  ⏹️ `ai language`: create all CLI functionality (needs more breakdown)
|    |XL++|  | x |  ⏹️ `ai translation`: create all CLI functionality (needs more breakdown)
|    |XL++|  | x |  ⏹️ `ai search`: create all CLI functionality (needs more breakdown)
|    |   |   |   |  

