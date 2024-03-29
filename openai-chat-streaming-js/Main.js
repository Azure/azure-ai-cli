const { OpenAIChatCompletionsStreamingClass } = require("./OpenAIChatCompletionsStreamingClass");

const readline = require('readline');
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
});

async function main() {

  const openAIEndpoint = process.env["AZURE_OPENAI_ENDPOINT"] || "<insert your OpenAI endpoint here>";
  const openAIKey = process.env["AZURE_OPENAI_KEY"] || "<insert your OpenAI API key here>";
  const openAIChatDeploymentName = process.env["AZURE_OPENAI_CHAT_DEPLOYMENT"] || "<insert your OpenAI chat deployment name here>" ;
  const openAISystemPrompt = process.env["AZURE_OPENAI_SYSTEM_PROMPT"] || "You are a helpful AI assistant." ;

  const chat = new OpenAIChatCompletionsStreamingClass(openAIEndpoint, openAIKey, openAIChatDeploymentName, openAISystemPrompt);

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