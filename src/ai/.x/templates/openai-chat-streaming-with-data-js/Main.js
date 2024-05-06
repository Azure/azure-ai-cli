const { {ClassName} } = require("./OpenAIChatCompletionsStreamingWithDataClass");
const { readline } = require("./ReadLineWrapper");

async function main() {

  const openAIAPIKey = process.env["AZURE_OPENAI_API_KEY"] || "{AZURE_OPENAI_API_KEY}";
  const openAIAPIVersion = process.env["AZURE_OPENAI_API_VERSION"] || "{AZURE_OPENAI_API_VERSION}" ;
  const openAIEndpoint = process.env["AZURE_OPENAI_ENDPOINT"] || "{AZURE_OPENAI_ENDPOINT}";
  const openAIChatDeploymentName = process.env["AZURE_OPENAI_CHAT_DEPLOYMENT"] || "{AZURE_OPENAI_CHAT_DEPLOYMENT}" ;
  const openAIEmbeddingsDeploymentName = process.env["AZURE_OPENAI_EMBEDDING_DEPLOYMENT"] || "{AZURE_OPENAI_EMBEDDING_DEPLOYMENT}" ;
  const openAIEmbeddingsEndpoint = `${openAIEndpoint.replace(/\/+$/, '')}/openai/deployments/${openAIEmbeddingsDeploymentName}/embeddings?api-version=${openAIAPIVersion}`;
  const openAISystemPrompt = process.env["AZURE_OPENAI_SYSTEM_PROMPT"] || "{AZURE_OPENAI_SYSTEM_PROMPT}" ;
  const searchEndpoint = process.env["AZURE_AI_SEARCH_ENDPOINT"] || "{AZURE_AI_SEARCH_ENDPOINT}" ;
  const searchAPIKey = process.env["AZURE_AI_SEARCH_KEY"] || "{AZURE_AI_SEARCH_KEY}" ;
  const searchIndexName = process.env["AZURE_AI_SEARCH_INDEX_NAME"] || "{AZURE_AI_SEARCH_INDEX_NAME}" ;

  const chat = new {ClassName}(openAIEndpoint, openAIAPIKey, openAIChatDeploymentName, openAISystemPrompt, searchEndpoint, searchAPIKey, searchIndexName, openAIEmbeddingsEndpoint);

  while (true) {

    const input = await readline.question('User: ');
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
  console.error("The sample encountered an error:", err);
  process.exit(1);
});

module.exports = { main };
