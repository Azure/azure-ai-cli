const { OpenAIChatCompletionsStreamingClass } = require("./OpenAIChatCompletionsStreamingClass");

const readline = require('readline');
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
});

async function main() {

  const openAISystemPrompt = process.env["AZURE_OPENAI_SYSTEM_PROMPT"] || "You are a helpful AI assistant.";

  // OpenAI API
  const openAIKey = process.env["OPENAI_API_KEY"] || "YOUR-KEY-HERE";
  const openAIModelName = process.env["OPENAI_MODEL_NAME"] || "gpt-4-turbo-preview";
  const openAIOrganization = process.env["OPENAI_ORG_ID"] || null;

  // Azure OpenAI API
  const azureOpenAIAPIVersion = process.env["AZURE_OPENAI_API_VERSION"] || "2024-03-01-preview";
  const azureOpenAIEndpoint = process.env["AZURE_OPENAI_ENDPOINT"] || "<insert your Azure OpenAI endpoint here>";
  const azureOpenAIKey = process.env["AZURE_OPENAI_API_KEY"] || "<insert your Azure OpenAI API key here>";
  const azureOpenAIChatDeploymentName = process.env["AZURE_OPENAI_CHAT_DEPLOYMENT"] || "<insert your Azure OpenAI chat deployment name here>";

  // Create the right one based on what is available
  const chat = !azureOpenAIEndpoint?.startsWith("https://")
    ? OpenAIChatCompletionsStreamingClass.createUsingOpenAI(openAIKey, openAIModelName, openAISystemPrompt, openAIOrganization)
    : OpenAIChatCompletionsStreamingClass.createUsingAzure(azureOpenAIAPIVersion, azureOpenAIEndpoint, azureOpenAIKey, azureOpenAIChatDeploymentName, openAISystemPrompt);

  // Start the conversation, and loop until the user types 'exit'
  while (true) {

    // Get user input
    const input = await new Promise(resolve => rl.question('User: ', resolve));
    if (input === 'exit' || input === '') break;

    // Get the Assistant's response
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