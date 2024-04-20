<#@ template hostspecific="true" #>
<#@ output extension=".js" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
const { factory } = require("./OpenAIChatCompletionsCustomFunctions");
const { CreateOpenAI } = require("./CreateOpenAI");
const { OpenAIEnvInfo } = require("./OpenAIEnvInfo");
const { <#= ClassName #> } = require("./OpenAIChatCompletionsFunctionsStreamingClass");

const readline = require('node:readline/promises');
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
});

async function main() {

  // Create the OpenAI client
  const openai = await CreateOpenAI.forChatCompletionsAPI({
    errorCallback: text => process.stdout.write(text)
  });

  // Create the streaming chat completions helper
  const useAzure = OpenAIEnvInfo.AZURE_OPENAI_ENDPOINT?.startsWith('https://');
  const chat = useAzure
    ? new <#= ClassName #>(OpenAIEnvInfo.AZURE_OPENAI_CHAT_DEPLOYMENT, OpenAIEnvInfo.AZURE_OPENAI_SYSTEM_PROMPT, factory, openai, 20)
    : new <#= ClassName #>(OpenAIEnvInfo.OPENAI_MODEL_NAME, OpenAIEnvInfo.AZURE_OPENAI_SYSTEM_PROMPT, factory, openai);

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
