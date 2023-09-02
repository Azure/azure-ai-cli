<style>
    .options { display: flex; }
    .option { padding: 5px; border: 1px solid #ccc; cursor: pointer; margin-right: 6px; }
    .option.selected { background-color: #000000; color: #fff; }
    .content { display: none; }
    .content.active { display: block; }
</style>

# Using the Azure AI CLI

The Azure `AI` Command-Line Interface (CLI) is a cross-platform command-line tool to connect to Azure AI services and execute control-plane and data-plane operations without having to write any code. The CLI allows the execution of commands through a terminal using interactive command-line prompts or via script. 

You can easily use the `AI` CLI to experiment with key Azure AI service features and see how they work with your use cases. Within minutes, you can setup all the required Azure resources needed, and build a customized Copilot using OpenAI's chat completions APIs and your own data. You can try it out interactively, or script larger processes to automate your own workflows and evaluations as part of your CI/CD system.

In the future, you'll even be able to use the `AI` CLI to dynamically create code in the programming language of your choice to integrate with your own applications.

## **STEP 1**: Setup your development environment

You can install the Azure `AI` CLI locally on Linux, Mac, or Windows computers, or use it thru an internet browser or Docker container. 

During this private preview, we recommend using the Azure `AI` CLI thru GitHub Codespaces. This will allow you to quickly get started without having to install anything locally.

<div class="options">
    <div class="option selected" onclick="showContent(1)">GitHub Codespaces</div>
    <div class="option" onclick="showContent(2)">VS Code Dev Container</div>
    <div class="option" onclick="showContent(3)">Install locally</div>
</div>
<div class="content" id="content1">

### GitHub Codespaces

You can run the Azure `AI` CLI in a browser using GitHub Codespaces:

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/Azure/aistudio-chat-demo?quickstart=1)

</div><div class="content" id="content2">

### VS Code Devcontainer

You can run the Azure `AI` CLI in a Docker container using VS Code Dev Containers:

1. Follow the [installation instructions](https://code.visualstudio.com/docs/devcontainers/containers#_installation) for VS Code Dev Containers.
2. Clone the [aistudio-chat-demo](https://github.com/Azure/aistudio-chat-demo) repository and open it with VS Code:
    ```
    git clone https://github.com/azure/aistudio-chat-demo
    code aistudio-chat-demo
    ```
3. Click the button "Reopen in Dev Containers", if it does not appear open the command pallete (`Ctrl+Shift+P` on Windows/Linux, `Cmd+Shift+P` on Mac) and run the `Dev Containers: Reopen in Container` command

</div><div class="content" id="content3">

### Install locally

You can install the Azure `AI` CLI locally on Linux, Mac, or Windows computers:

1. Install the [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
2. Install the [Generative AI SDK Packages](./use_azureai_sdk.md#install-the-generative-ai-sdk-packages)
3. Install the [Azure AI CLI](https://csspeechstorage.blob.core.windows.net/drop/private/ai/Azure.AI.CLI.1.0.0-alpha9.nupkg)
    ```
    wget https://csspeechstorage.blob.core.windows.net/drop/private/ai/Azure.AI.CLI.1.0.0-alpha9.nupkg && \
    dotnet tool install --global --add-source . Azure.AI.CLI --version 1.0.0-alpha9 && \
    rm Azure.AI.CLI.1.0.0-alpha9.nupkg
    ```

</div>

<script>
    showContent(1);
    function showContent(optionNumber) {
        for (let i = 1; i <= 3; i++) {
            const content = document.getElementById("content" + i);
            const option = document.querySelector(".option:nth-child(" + i + ")");
            if (i === optionNumber) {
                content.classList.add("active");
                option.classList.add("selected");
            } else {
                content.classList.remove("active");
                option.classList.remove("selected");
            }
        }
    }
</script>






## **STEP 2**: Initialize the Azure AI CLI

You can initialize the Azure `AI` CLI by running the following command:

```
ai init
```

## ai chat

Chats with an AOAI deployment, supports chatting with data and is interactive

```
ai chat --interactive
```


## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
