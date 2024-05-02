const { OpenAI } = require('openai');
const { {ClassName} } = require("./OpenAIAssistantsStreamingClass");

const rl = require('readline').createInterface({
  input: process.stdin,
  output: process.stdout
});

async function* getLines() {
  for await (const line of rl) {
    yield line;
  }
}

async function readLine(prompt) {
  const lineGenerator = getLines();
  process.stdout.write(prompt);
  const result = await lineGenerator.next();
  if(result.done) {
    rl.close();
    return '';
  }
  return result.value;
}

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
    const input = await readLine('User: ');
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

main();

module.exports = { main };
