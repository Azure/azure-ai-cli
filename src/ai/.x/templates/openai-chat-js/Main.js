{{if {_IS_AUTHOR_COMMENT} }}
 // Showcasing syntax differences for the same content:
 //   The Learn docs use CommonJS module syntax, which includes `require` and `module.exports`
 //     This is an older standard for working with modules in JavaScript and is widely supported, especially in Node.js.
 //   The Studio "view code" uses ES6 module syntax, which includes `import` and `export`.
 //     This is a newer standard for working with modules in JavaScript and is supported in modern browsers and Node.js versions
{{endif}}
{{if {_IS_STUDIO_VIEW_CODE_TEMPLATE}}}
import { AzureOpenAI } from "openai";
import { DefaultAzureCredential, getBearerTokenProvider } from "@azure/identity";
{{else if {_IS_LEARN_DOC_TEMPLATE} || !{_IS_STUDIO_VIEW_CODE_TEMPLATE}}}
const { OpenAI } = require("openai");
{{endif}}
{{if !{_IS_LEARN_DOC_TEMPLATE} || !{_IS_STUDIO_VIEW_CODE_TEMPLATE}}}
const { {ClassName} } = require("./OpenAIChatCompletionsClass");
const { readline } = require("./ReadLineWrapper");
{{endif}}

{{if {_IS_LEARN_DOC_TEMPLATE}}}
// Load the .env file if it exists
const dotenv = require("dotenv");
dotenv.config();

  {{if {_IS_ENTRA_ID}}}
  const scope = "https://cognitiveservices.azure.com/.default";
  const azureADTokenProvider = getBearerTokenProvider(new DefaultAzureCredential(), scope);
  {{else}}
  // You will need to set these environment variables or edit the following values
  const endpoint = process.env["AZURE_OPENAI_ENDPOINT"] || "<endpoint>";
  const apiKey = process.env["AZURE_OPENAI_API_KEY"] || "<api key>";
  {{endif}}
  const apiVersion = "2024-05-01-preview";
  const deployment = "gpt-4o"; //This must match your deployment name.
  require("dotenv/config");
{{endif}}

{{if {_IS_STUDIO_VIEW_CODE_TEMPLATE}}}
export async function main() {
{{else}}
async function main() {
{{endif}}

  {{if {_IS_LEARN_DOC_TEMPLATE}}}
      const client = new AzureOpenAI({ endpoint, apiKey, apiVersion, deployment });
      const result = await client.chat.completions.create({
        messages: [
        { role: "system", content: "You are a helpful assistant." },
        { role: "user", content: "Does Azure OpenAI support customer managed keys?" },
        { role: "assistant", content: "Yes, customer managed keys are supported by Azure OpenAI?" },
        { role: "user", content: "Do other Azure AI services support this too?" },
        ],
        model: "",
      });

  {{else if {_IS_STUDIO_VIEW_CODE_TEMPLATE}}}
      const scope = "https://cognitiveservices.azure.com/.default";
      const azureADTokenProvider = getBearerTokenProvider(new DefaultAzureCredential(), scope);
      const deployment = "gpt-35-turbo";
      const apiVersion = "2024-04-01-preview";
      const client = new AzureOpenAI({ azureADTokenProvider, deployment, apiVersion });
      const result = await client.chat.completions.create({
        messages:  [
          { role: "system", content: "You are a helpful assistant. You will talk like a pirate." },
          { role: "user", content: "Can you help me?" },
        ],
        model: '',
      });

  {{else}}
    // What's the system prompt?
    const AZURE_OPENAI_SYSTEM_PROMPT = process.env.AZURE_OPENAI_SYSTEM_PROMPT ?? "You are a helpful AI assistant.";

    {{@include openai.js/create.openai.js}}
    // Create the streaming chat completions helper
    {{if contains(toupper("{OPENAI_CLOUD}"), "AZURE")}}
    const chat = new {ClassName}(AZURE_OPENAI_CHAT_DEPLOYMENT, AZURE_OPENAI_SYSTEM_PROMPT, openai);
    {{else}}
    const chat = new {ClassName}(OPENAI_MODEL_NAME, AZURE_OPENAI_SYSTEM_PROMPT, openai);
    {{endif}}
  {{endif}}

{{if {_IS_LEARN_DOC_TEMPLATE}} || {_IS_STUDIO_VIEW_CODE_TEMPLATE}}
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
{{endif}}

main().catch((err) => {
  console.error("The sample encountered an error:", err);
  process.exit(1);
});

{{if !{_IS_STUDIO_VIEW_CODE_TEMPLATE}}}
module.exports = { main };
{{endif}}