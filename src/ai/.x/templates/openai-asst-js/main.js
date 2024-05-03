const { OpenAI } = require('openai');
const { {ClassName} } = require("./OpenAIAssistantsClass");
const { readline } = require("./ReadLineWrapper");

async function main() {

  // Which assistant, which thread?
  const ASSISTANT_ID = process.env.ASSISTANT_ID ?? "<insert your OpenAI assistant ID here>";
  const threadId = process.argv[2] || null;

  {{@include openai.asst.or.chat.create.openai.node.js}}

  // Create the assistants streaming helper class instance
  const assistant = new {ClassName}(ASSISTANT_ID, openai);

  // Get or create the thread, and display the messages if any
  if (threadId === null) {
    await assistant.createThread()
  } else {
    await assistant.retrieveThread(threadId);
    await assistant.getThreadMessages((role, content) => {
      role = role.charAt(0).toUpperCase() + role.slice(1);
      process.stdout.write(`${role}: ${content}`);
      });
  }

  // Loop until the user types 'exit'
  while (true) {

    // Get user input
    const input = await readline.question('User: ');
    if (input === 'exit' || input === '') break;

    // Get the Assistant's response
    let response = await assistant.getResponse(input);
    process.stdout.write(`\nAssistant: ${response}\n\n`);
  }

  console.log(`Bye! (threadId: ${assistant.thread.id})`);
  process.exit();
}

main().catch((err) => {
  console.error("The sample encountered an error:", err);
  process.exit(1);
});

module.exports = { main };
