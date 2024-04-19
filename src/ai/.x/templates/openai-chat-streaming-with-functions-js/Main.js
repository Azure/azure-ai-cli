<#@ template hostspecific="true" #>
<#@ output extension=".js" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_API_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_API_VERSION" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
<#@ parameter type="System.String" name="OPENAI_API_KEY" #>
<#@ parameter type="System.String" name="OPENAI_ORG_ID" #>
<#@ parameter type="System.String" name="OPENAI_MODEL_NAME" #>
const { factory } = require("./OpenAIChatCompletionsCustomFunctions");
const { CreateOpenAI } = require("./CreateOpenAI");
const { <#= ClassName #> } = require("./OpenAIChatCompletionsFunctionsStreamingClass");

const readline = require('node:readline/promises');
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
});

// NOTE: Never deploy your key in client-side environments like browsers or mobile apps
//  SEE: https://help.openai.com/en/articles/5112595-best-practices-for-api-key-safety

async function main() {

  // What's the system prompt
  const openAISystemPrompt = process.env["AZURE_OPENAI_SYSTEM_PROMPT"] || "<#= AZURE_OPENAI_SYSTEM_PROMPT #>";

  // Connection info
  const azureOpenAIAPIKey = process.env["AZURE_OPENAI_API_KEY"] || "<#= AZURE_OPENAI_API_KEY #>";
  const azureOpenAIAPIVersion = process.env["AZURE_OPENAI_API_VERSION"] || "<#= AZURE_OPENAI_API_VERSION #>";
  const azureOpenAIEndpoint = process.env["AZURE_OPENAI_ENDPOINT"] || "<#= AZURE_OPENAI_ENDPOINT #>";
  const azureOpenAIChatDeploymentName = process.env["AZURE_OPENAI_CHAT_DEPLOYMENT"] || "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>";
  const openAIAPIKey = process.env["OPENAI_API_KEY"] || "<#= OPENAI_API_KEY #>";
  const openAIOrganization = process.env["OPENAI_ORG_ID"] || null;
  const openAIModelName = process.env["OPENAI_MODEL_NAME"] || "<#= OPENAI_MODEL_NAME #>";

  // Create the OpenAI client
  const useAzure = azureOpenAIEndpoint?.startsWith("https://");
  const openai = useAzure
    ? await CreateOpenAI.fromAzureOpenAIEndpointAndDeployment(azureOpenAIAPIKey, azureOpenAIEndpoint, azureOpenAIAPIVersion, azureOpenAIChatDeploymentName)
    : CreateOpenAI.fromOpenAIKey(openAIAPIKey, openAIOrganization);

  // Create the streaming chat completions helper
  const chat = streamingChatCompletions = useAzure
    ? new <#= ClassName #>(azureOpenAIChatDeploymentName, openAISystemPrompt, factory, openai, 20)
    : new <#= ClassName #>(openAIModelName, openAISystemPrompt, factory, openai);

  // Loop until the user types 'exit'
  while (true) {

    // Get user input
    const input = await rl.question('User: ');
    if (input === 'exit' || input === '') break;

    // Get the response
    process.stdout.write('\nAssistant: ');
    await chat.getResponse(input, (content) => {
      process.stdout.write(content);
    });

    process.stdout.write('\n\n');
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
