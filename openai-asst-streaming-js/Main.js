const { OpenAIAssistantsStreamingClass } = require("./OpenAIAssistantsStreamingClass");

const readline = require('readline');
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
});

async function main() {

  const openAIKey = "YOUR-KEY-HERE";
  const openAIOrganization = null;
  const openAIAssistantId = "asst_W6RbXQnkqkmSMWT0QYzA88hH";
  const openAIAssistantThreadId = process.argv[2] || null;

  const assistant = new OpenAIAssistantsStreamingClass(openAIKey, openAIOrganization, openAIAssistantId);
  await assistant.getOrCreateThread(openAIAssistantThreadId);

  await assistant.getThreadMessages((role, content) => {
    role = role.charAt(0).toUpperCase() + role.slice(1);
    process.stdout.write(`${role}: ${content}`);
  });

  while (true) {

    const input = await new Promise(resolve => rl.question('User: ', resolve));
    if (input === 'exit' || input === '') break;

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