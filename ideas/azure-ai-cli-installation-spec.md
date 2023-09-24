# Azure AI CLI installation

## Installation via script on Debian/Ubuntu

To support installation from a script, we will support the following approach:

```bash
curl -sL https://aka.ms/InstallAzureAICLIDeb | bash
```

Customer Requirements:
- Support Debian 10, 11, and 12
- Support Ubunutu 20.04 and 22.04
- Check and install Azure CLI if not present
- Check and install dotnet 7.0 if not present
- Check and install Python azure.ai.generative SDK if not present
- Update user's shell rc file (e.g. `$HOME/.bashrc` and/or `$HOME/.zshrc`)

CI/CD Requirements:
- Package (`Azure.AI.CLI-${VERSION}.nupkg`) and script (`InstallAzureAICLIDeb-${VERSION}.sh`) upload automated via "release" pipeline
- BVT level testing of uploaded package and script via "release" pipeline

Script hosting and redirecting:
- `$HOST/$PATH` is currently `https://csspeechstorage.blob.core.windows.net/drop/private/ai`
- `$HOST/$PATH` likely (???) should be something else that does not contain "speech" in the URL HOST
- Registered link (`aka.ms/InstallAzureAICLIDeb`) will point to versioned URL (`${HOST}/${PATH}/InstallAzureAICLIDeb-${VERSION}.sh`)
- Registered link will be updated post human verification of release pipeline success

## Installation/usage from Docker container

To support usage from Docker containers, we will support the following approach:

```BASH
docker pull ${REGISTRY}/azure-ai-cli
docker run -it -v __:___ ${REGISTRY}/azure-ai-cli
```

Customer Requirements:
- Support Debian 10, 11, and 12, and Ubunutu 20.04 and 22.04
- Uses VS Code base images (e.g. `mcr.microsoft.com/devcontainers/base:bookworm`)
- Tagged similarly to VS Code base images (e.g. `${REGISTRY}/azure-ai-cli:bookworm`)
- Tagged with versions as well (e.g. `${REGISTRY}/azure-ai-cli:bookworm-1.0.0-alpha924.3`)
- `${REGISTRY}` is currently `acrbn.azurecr.io`
- `${REGISTRY}` likely (???) should be `mcr.microsoft.com`
- `latest` tag points to `${REGISTRY}/azure-ai-cli:bookworm`

CI/CD Requirements:
- Uses same installation script from above (`InstallAzureAICLIDeb-${VERSION}.sh`)
- Images are built, tested, and uploaded to container registry via "release" pipeline

ADDITIONAL OPEN QUESTIONS:
- What should the working directory be?
- How best to map in `.ai` directory into running container?
- How should we manage keeping Python SDK `requirements.txt` up to date?
- Should `aistudio-chat-demo` `devcontainer.json` use `${REGISTRY}/azure-ai-cli:bookworm`?
