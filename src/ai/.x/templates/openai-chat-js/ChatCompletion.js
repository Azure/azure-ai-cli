<#@ template hostspecific="true" #>
<#@ output extension=".js" encoding="utf-8" #>
<#@ parameter type="System.String" name="OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="OPENAI_API_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
const { OpenAIClient, AzureKeyCredential } = require("@azure/openai");
const readline = require('readline');
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
});

const endpoint = process.env["OPENAI_ENDPOINT"] || "<#= OPENAI_ENDPOINT #>";
const azureApiKey = process.env["OPENAI_API_KEY"]  || "<#= OPENAI_API_KEY #>";
const deploymentName = process.env["AZURE_OPENAI_CHAT_DEPLOYMENT"] || "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>" ;
const systemPrompt = process.env["AZURE_OPENAI_SYSTEM_PROMPT"] || "<#= AZURE_OPENAI_SYSTEM_PROMPT #>" ;

messages = [
  { role: "system", content: systemPrompt },
];

async function main() {

  const client = new OpenAIClient(endpoint, new AzureKeyCredential(azureApiKey));

  while (true) {

    const input = await new Promise(resolve => rl.question('User: ', resolve));
    if (input === 'exit' || input === '') break;

    messages.push({ role: "user", content: input });
    const result = await client.getChatCompletions(deploymentName, messages);

    const response_content = result.choices[0].message.content;
    messages.push({ role: "assistant", content: response_content });

    console.log(`\nAssistant: ${response_content}\n`);
  }

  console.log('Bye!');
}

main().catch((err) => {
  console.error("The sample encountered an error:", err);
});

module.exports = { main };
