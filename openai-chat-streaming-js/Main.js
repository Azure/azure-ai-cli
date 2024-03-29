const { OpenAIChatCompletionsStreamingClass } = require("./OpenAIChatCompletionsStreamingClass");

const readline = require('readline');
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
});

async function main() {

  const openAIKey = "YOUR-KEY-HERE";
  const openAIOrganization = null;
  const openAIModel = "gpt-4-turbo-preview";
  const openAISystemPrompt = "You are a helpful AI assistant.";

  const chat = new OpenAIChatCompletionsStreamingClass(openAIKey, openAIOrganization, openAIModel, openAISystemPrompt);

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