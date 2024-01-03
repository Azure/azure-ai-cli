<#@ template hostspecific="true" #>
<#@ output extension=".js" encoding="utf-8" #>
<#@ parameter type="System.String" name="OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="OPENAI_API_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
const customFunctions = require("./ChatCompletionsCustomFunctions");
const { getCurrentWeatherSchema, getCurrentWeather } = customFunctions;
const { getCurrentDateSchema, getCurrentDate } = customFunctions;
const { FunctionFactory } = require("./FunctionFactory");
const { ChatCompletionsFunctionsStreaming } = require("./ChatCompletionsFunctionsStreaming");

const readline = require('readline');
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
});

async function main() {

  let factory = new FunctionFactory();
  factory.addFunction(getCurrentWeatherSchema, getCurrentWeather);
  factory.addFunction(getCurrentDateSchema, getCurrentDate);

  const endpoint = process.env["OPENAI_ENDPOINT"] || "<#= OPENAI_ENDPOINT #>";
  const azureApiKey = process.env["OPENAI_API_KEY"]  || "<#= OPENAI_API_KEY #>";
  const deploymentName = process.env["AZURE_OPENAI_CHAT_DEPLOYMENT"] || "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>" ;
  const systemPrompt = process.env["AZURE_OPENAI_SYSTEM_PROMPT"] || "<#= AZURE_OPENAI_SYSTEM_PROMPT #>" ;

  const streamingChatCompletions = new ChatCompletionsFunctionsStreaming(systemPrompt, endpoint, azureApiKey, deploymentName, factory);

  while (true) {

    const input = await new Promise(resolve => rl.question('User: ', resolve));
    if (input === 'exit' || input === '') break;

    let response = await streamingChatCompletions.getChatCompletions(input, (content) => {
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
