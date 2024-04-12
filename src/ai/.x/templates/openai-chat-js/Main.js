<#@ template hostspecific="true" #>
<#@ output extension=".js" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_API_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
const { <#= ClassName #> } = require("./OpenAIChatCompletionsClass");

const readline = require('node:readline/promises');
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
});

async function main() {

  const openAIAPIKey = process.env["AZURE_OPENAI_API_KEY"] || "<#= AZURE_OPENAI_API_KEY #>";
  const openAIEndpoint = process.env["AZURE_OPENAI_ENDPOINT"] || "<#= AZURE_OPENAI_ENDPOINT #>";
  const openAIChatDeploymentName = process.env["AZURE_OPENAI_CHAT_DEPLOYMENT"] || "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>" ;
  const openAISystemPrompt = process.env["AZURE_OPENAI_SYSTEM_PROMPT"] || "<#= AZURE_OPENAI_SYSTEM_PROMPT #>" ;

  const chat = new <#= ClassName #>(openAIEndpoint, openAIAPIKey, openAIChatDeploymentName, openAISystemPrompt);

  while (true) {

    const input = await rl.question('User: ');
    if (input === 'exit' || input === '') break;

    let response = await chat.getChatCompletions(input);
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
