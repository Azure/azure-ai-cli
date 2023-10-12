# Chat Protocol Sample Service

This is a simple service that knows how to talk chat protocol. For functionality it just works as a proxy between a client talking the protocol and Azure Open AI.

For it to work a few environment variables are needed:

```bash
export CHAT_AZURE_OPEN_AI_KEY="<your-azure-open-ai-key>"
export CHAT_AZURE_OPEN_AI_ENDPOINT="<your-azure-open-ai-endpoint>";
export CHAT_AZURE_OPEN_AI_DEPLOYMENT="<the-deployment-to-use>";
```

```pwsh
$env:CHAT_AZURE_OPEN_AI_KEY="<your-azure-open-ai-key>"
$env:CHAT_AZURE_OPEN_AI_ENDPOINT="<your-azure-open-ai-endpoint>";
$env:CHAT_AZURE_OPEN_AI_DEPLOYMENT="<the-deployment-to-use>";
```
