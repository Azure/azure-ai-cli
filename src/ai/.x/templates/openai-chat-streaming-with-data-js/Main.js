<#@ template hostspecific="true" #>
<#@ output extension=".js" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_API_VERSION" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
<#@ parameter type="System.String" name="AZURE_AI_SEARCH_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_AI_SEARCH_KEY" #>
<#@ parameter type="System.String" name="AZURE_AI_SEARCH_INDEX_NAME" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_EMBEDDING_DEPLOYMENT" #>
const { <#= ClassName #> } = require("./OpenAIChatCompletionsStreamingWithDataClass");

const readline = require('readline');
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
});

async function main() {

  const openAIEndpoint = process.env["AZURE_OPENAI_ENDPOINT"] || "<#= AZURE_OPENAI_ENDPOINT #>";
  const openAIKey = process.env["AZURE_OPENAI_KEY"] || "<#= AZURE_OPENAI_KEY #>";
  const openAIChatDeploymentName = process.env["AZURE_OPENAI_CHAT_DEPLOYMENT"] || "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>" ;
  const openAISystemPrompt = process.env["AZURE_OPENAI_SYSTEM_PROMPT"] || "<#= AZURE_OPENAI_SYSTEM_PROMPT #>" ;
  const openAIAPIVersion = process.env["AZURE_OPENAI_API_VERSION"] || "<#= AZURE_OPENAI_API_VERSION #>" ;
  const searchEndpoint = process.env["AZURE_AI_SEARCH_ENDPOINT"] || "<#= AZURE_AI_SEARCH_ENDPOINT #>" ;
  const searchAPIKey = process.env["AZURE_AI_SEARCH_KEY"] || "<#= AZURE_AI_SEARCH_KEY #>" ;
  const searchIndexName = process.env["AZURE_AI_SEARCH_INDEX_NAME"] || "<#= AZURE_AI_SEARCH_INDEX_NAME #>" ;
  const openAIEmbeddingsDeploymentName = process.env["AZURE_OPENAI_EMBEDDING_DEPLOYMENT"] || "<#= AZURE_OPENAI_EMBEDDING_DEPLOYMENT #>" ;
  const openAIEmbeddingsEndpoint = `${openAIEndpoint.replace(/\/+$/, '')}/openai/deployments/${openAIEmbeddingsDeploymentName}/embeddings?api-version=${openAIAPIVersion}`;

  const chat = new <#= ClassName #>(openAIEndpoint, openAIKey, openAIChatDeploymentName, openAISystemPrompt, searchEndpoint, searchAPIKey, searchIndexName, openAIEmbeddingsEndpoint);

  while (true) {

    const input = await new Promise(resolve => rl.question('User: ', resolve));
    if (input === 'exit' || input === '') break;

    let response = await chat.getChatCompletions(input, (content) => {
      console.log(`assistant-streaming: ${content}`);
    });

    console.log(`\nAssistant: ${response}\n`);
  }

  console.log('Bye!');
  process.exit();
}

main().catch((err) => {
  if (err.code !== 'ERR_USE_AFTER_CLOSE') { // filter out expected error (EOF on redirected input)
    console.error("The sample encountered an error:", err);
    process.exit(1);
  }
});

module.exports = { main };
