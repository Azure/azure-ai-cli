const { OpenAIAssistantsStreamingClass } = require("./OpenAIAssistantsStreamingClass");

const readline = require('readline');
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
});

async function main() {

  // Which assistant, and what thread to use
  const openAIAssistantId = "asst_W6RbXQnkqkmSMWT0QYzA88hH";
  const openAIAssistantThreadId = process.argv[2] || null;

  // Connection info and authentication for OpenAI API
  const openAIKey = process.env["OPENAI_API_KEY"] || "YOUR-KEY-HERE";
  const openAIOrganization = process.env["OPENAI_ORG_ID"] || null;

  // Connection info and authentication for Azure OpenAI API
  const azureOpenAIAPIVersion = "2024-03-01-preview"; // process.env["AZURE_OPENAI_API_VERSION"] || "2024-03-01-preview";
  const azureOpenAIEndpoint = process.env["AZURE_OPENAI_ENDPOINT"] || "<insert your Azure OpenAI endpoint here>";
  const azureOpenAIKey = process.env["AZURE_OPENAI_API_KEY"] || "<insert your Azure OpenAI API key here>";
  const azureOpenAIChatDeploymentName = process.env["AZURE_OPENAI_CHAT_DEPLOYMENT"] || "<insert your Azure OpenAI chat deployment name here>";

  // Create the right one based on what is available
  const useAzure = azureOpenAIEndpoint?.startsWith("https://");
  const assistant = useAzure
    ? OpenAIAssistantsStreamingClass.createUsingAzure(azureOpenAIAPIVersion, azureOpenAIEndpoint, azureOpenAIKey, azureOpenAIChatDeploymentName, openAIAssistantId)
    : OpenAIAssistantsStreamingClass.createUsingOpenAI(openAIKey, openAIOrganization, openAIAssistantId);

  // Get or create the thread, and display the messages if any
  await assistant.getOrCreateThread(openAIAssistantThreadId);
  await assistant.getThreadMessages((role, content) => {
    role = role.charAt(0).toUpperCase() + role.slice(1);
    process.stdout.write(`${role}: ${content}`);
  });

  // Loop until the user types 'exit'
  while (true) {

    // Get user input
    const input = await new Promise(resolve => rl.question('User: ', resolve));
    if (input === 'exit' || input === '') break;

    // Get the Assistant's response
    process.stdout.write('\nAssistant: ');
    await assistant.getResponse(input, (content) => {
      process.stdout.write(content);
    });

    process.stdout.write('\n\n');
  }

  console.log(`Bye! (threadId: ${assistant.thread.id})`);
  process.exit();
}

main().catch((err) => {
  if (err.code !== 'ERR_USE_AFTER_CLOSE') { // filter out expected error (EOF on redirected input)
    console.error("The sample encountered an error:", err);
    process.exit(1);
  }
});

module.exports = { main };