const { OpenAI } = require('openai');
const { {ClassName} } = require("./OpenAIAssistantsClass");

const readline = require('node:readline/promises');
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
});

async function main() {

  // Which assistant, which thread?
  const ASSISTANT_ID = process.env.ASSISTANT_ID ?? "{ASSISTANT_ID}";
  const threadId = process.argv[2] || null;

  {{if {USE_AZURE_OPENAI} && contains(toupper("{AZURE_OPENAI_AUTH_METHOD}"), "KEY")}}
  // Get the required environment variables, and form the base URL for Azure OpenAI Assistants API
  const AZURE_OPENAI_API_KEY = process.env.AZURE_OPENAI_API_KEY ?? "<insert your Azure OpenAI API key here>";
  const AZURE_OPENAI_API_VERSION = process.env.AZURE_OPENAI_API_VERSION ?? "<insert your Azure OpenAI API version here>";
  const AZURE_OPENAI_ENDPOINT = process.env.AZURE_OPENAI_ENDPOINT ?? "<insert your Azure OpenAI endpoint here>";
  const AZURE_OPENAI_BASE_URL = `${AZURE_OPENAI_ENDPOINT.replace(/\/+$/, '')}/openai`;

  // Create the OpenAI client
  console.log('Using Azure OpenAI (w/ API Key)...');
  const openai = new OpenAI({
    apiKey: AZURE_OPENAI_API_KEY,
    baseURL: AZURE_OPENAI_BASE_URL,
    defaultQuery: { 'api-version': AZURE_OPENAI_API_VERSION },
    defaultHeaders: { 'api-key': AZURE_OPENAI_API_KEY }
  });
  {{else if {USE_AZURE_OPENAI} && !contains(toupper("{AZURE_OPENAI_AUTH_METHOD}"), "KEY")}}
  // Get the required environment variables, and form the base URL for Azure OpenAI Assistants API
  const AZURE_OPENAI_API_VERSION = process.env.AZURE_OPENAI_API_VERSION ?? "<insert your Azure OpenAI API version here>";
  const AZURE_OPENAI_ENDPOINT = process.env.AZURE_OPENAI_ENDPOINT ?? "<insert your Azure OpenAI endpoint here>";
  const AZURE_OPENAI_BASE_URL = `${AZURE_OPENAI_ENDPOINT.replace(/\/+$/, '')}/openai`;

  // Get the access token using the DefaultAzureCredential
  let token = null;
  try {
    const { DefaultAzureCredential } = require('@azure/identity');
    const credential = new DefaultAzureCredential();
    const response = await credential.getToken("https://cognitiveservices.azure.com/.default");
    token = response.token;
  } catch (error) {  
    console.error('Error getting access token:', error);  
    throw error;  
  }

  // Create the OpenAI client
  console.log('Using Azure OpenAI (w/ AAD)...');
  const openai = new OpenAI({
    apiKey: '',
    baseURL: AZURE_OPENAI_BASE_URL,
    defaultQuery: { 'api-version': AZURE_OPENAI_API_VERSION },
    defaultHeaders: { Authorization: `Bearer ${token}` }
  });
  {{else}}
  // Get the required environment variables, and form the base URL for Azure OpenAI Assistants API
  const OPENAI_API_KEY = process.env.OPENAI_API_KEY ?? "<insert your OpenAI API key here>";
  const OPENAI_ORG_ID = process.env.OPENAI_ORG_ID ?? null;

  // Create the OpenAI client
  console.log('Using OpenAI...');
  const openai =  new OpenAI({
    apiKey: OPENAI_API_KEY,
    organization: OPENAI_ORG_ID
  });
  {{endif}}

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
    const input = await rl.question('User: ');
    if (input === 'exit' || input === '') break;

    // Get the Assistant's response
    let response = await assistant.getResponse(input);
    process.stdout.write(`\nAssistant: ${response}\n\n`);
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
