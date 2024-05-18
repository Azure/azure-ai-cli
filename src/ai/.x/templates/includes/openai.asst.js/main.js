const { OpenAI } = require('openai');
{{if {_IS_OPENAI_ASST_CODE_INTERPRETER_TEMPLATE}}}
const { {ClassName} } = require("./OpenAIAssistantsCodeInterpreterStreamingClass");
{{else if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}
const { factory } = require("./OpenAIAssistantsCustomFunctions");
const { {ClassName} } = require("./OpenAIAssistantsFunctionsStreamingClass");
{{else if {_IS_OPENAI_ASST_STREAMING_TEMPLATE}}}
const { {ClassName} } = require("./OpenAIAssistantsStreamingClass");
{{else}}
const { {ClassName} } = require("./OpenAIAssistantsClass");
{{endif}}
const { readline } = require("./ReadLineWrapper");

async function main() {

  // Which assistant, which thread?
  const ASSISTANT_ID = process.env.ASSISTANT_ID ?? "<insert your OpenAI assistant ID here>";
  const threadId = process.argv[2] || null;

  {{@include openai.asst.or.chat.create.openai.js}}

  // Create the assistants streaming helper class instance
  {{if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}
  const assistant = new {ClassName}(ASSISTANT_ID, factory, openai);
  {{else}}
  const assistant = new {ClassName}(ASSISTANT_ID, openai);
  {{endif}}

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
    {{if {_IS_OPENAI_ASST_STREAMING_TEMPLATE}}}
    process.stdout.write('\nAssistant: ');
    await assistant.getResponse(input, (content) => {
      process.stdout.write(content);
    });

    process.stdout.write('\n\n');
    {{else}}
    let response = await assistant.getResponse(input);
    process.stdout.write(`\nAssistant: ${response}\n\n`);
    {{endif}}
  }

  console.log(`Bye! (threadId: ${assistant.thread.id})`);
  process.exit();
}

main().catch((err) => {
  console.error("The sample encountered an error:", err);
  process.exit(1);
});

module.exports = { main };
