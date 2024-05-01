const { OpenAI } = require('openai');
const { {ClassName} } = require("./OpenAIChatCompletionsClass");

const readline = require('node:readline/promises');
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
});

async function main() {

  // What's the system prompt?
  const AZURE_OPENAI_SYSTEM_PROMPT = process.env.AZURE_OPENAI_SYSTEM_PROMPT ?? "You are a helpful AI assistant.";

  {{@include openai.asst.or.chat.create.openai.node.js}}

  // Create the streaming chat completions helper
  {{if {USE_AZURE_OPENAI}}}
  const chat = new {ClassName}(AZURE_OPENAI_CHAT_DEPLOYMENT, AZURE_OPENAI_SYSTEM_PROMPT, openai);
  {{else}}
  const chat = new {ClassName}(OPENAI_MODEL_NAME, AZURE_OPENAI_SYSTEM_PROMPT, openai);
  {{endif}}
  
  // Loop until the user types 'exit'
  while (true) {

    // Get user input
    const input = await rl.question('User: ');
    if (input === 'exit' || input === '') break;

    // Get the response
    const response = await chat.getResponse(input);
    process.stdout.write(`\nAssistant: ${response}\n\n`);
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
