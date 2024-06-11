{{if {_IS_LEARN_DOC_TEMPLATE}}}
const { AzureOpenAI } = require("openai");
{{else}}
const { OpenAI } = require("openai");
const { {ClassName} } = require("./OpenAIChatCompletionsClass");
const { readline } = require("./ReadLineWrapper");
{{endif}}

{{if {_IS_LEARN_DOC_TEMPLATE}}}
// Load the .env file if it exists
const dotenv = require("dotenv");
dotenv.config();

// You will need to set these environment variables or edit the following values
const endpoint = process.env["AZURE_OPENAI_ENDPOINT"] || "<endpoint>";
const apiKey = process.env["AZURE_OPENAI_API_KEY"] || "<api key>";
const apiVersion = "2024-05-01-preview";
const deployment = "gpt-4o"; //This must match your deployment name.
require("dotenv/config");
{{endif}}

async function main() {
{{@include openai.js/create.openai.js}}
{{if {_IS_LEARN_DOC_TEMPLATE}}}
  for (const choice of result.choices) {
    console.log(choice.message);
  }
}
{{else}}
  // Loop until the user types 'exit'
  while (true) {

    // Get user input
    const input = await readline.question('User: ');
    if (input === 'exit' || input === '') break;

    // Get the response
    const response = await chat.getResponse(input);
    process.stdout.write(`\nAssistant: ${response}\n\n`);
  }

  console.log('Bye!');
  process.exit();
}

main().catch((err) => {
  console.error("The sample encountered an error:", err);
  process.exit(1);
});

module.exports = { main };
{{endif}}