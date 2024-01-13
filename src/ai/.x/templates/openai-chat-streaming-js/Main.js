<#@ template hostspecific="true" #>
<#@ output extension=".js" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
const { <#= ClassName #> } = require("./OpenAIChatCompletionsStreamingClass");

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

  const chat = new <#= ClassName #>(openAIEndpoint, openAIKey, openAIChatDeploymentName, openAISystemPrompt);

  while (true) {

    const input = await new Promise(resolve => rl.question('User: ', resolve));
    if (input === 'exit' || input === '') break;

    let response = await chat.getChatCompletions(input, (content) => {
      console.log(`assistant-streaming: ${content}`);
    });

    console.log(`\nAssistant: ${response}\n`);
  }

  console.log('Bye!');
}

main().catch((err) => {
  console.error("The sample encountered an error:", err);
});

module.exports = { main };
