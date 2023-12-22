<#@ template hostspecific="true" #>
<#@ output extension=".js" encoding="utf-8" #>
<#@ parameter type="System.String" name="OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="OPENAI_API_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
const { OpenAIClient, AzureKeyCredential } = require("@azure/openai");

class OpenAIStreamingChatCompletions {
  constructor(systemPrompt, endpoint, azureApiKey, deploymentName) {
    this.systemPrompt = systemPrompt;
    this.endpoint = endpoint;
    this.azureApiKey = azureApiKey;
    this.deploymentName = deploymentName;
    this.client = new OpenAIClient(this.endpoint, new AzureKeyCredential(this.azureApiKey));
    this.clearConversation();
  }

  clearConversation() {
    this.messages = [
      { role: 'system', content: this.systemPrompt }
    ];
  }

  async getChatCompletions(userInput, callback) {
    this.messages.push({ role: 'user', content: userInput });

    const events = this.client.listChatCompletions(this.deploymentName, this.messages);

    let contentComplete = '';
    for await (const event of events) {
      for (const choice of event.choices) {

        let content = choice.delta?.content;
        if (choice.finishReason === 'length') {
          content = `${content}\nERROR: Exceeded token limit!`;
        }

        if (content != null) {
          callback(content);
          await new Promise(r => setTimeout(r, 50));
          contentComplete += content;
        }
      }
    }

    this.messages.push({ role: 'assistant', content: contentComplete });
    return contentComplete;
  }
}

const readline = require('readline');
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
});

async function main() {
  const endpoint = process.env["OPENAI_ENDPOINT"] || "<#= OPENAI_ENDPOINT #>";
  const azureApiKey = process.env["OPENAI_API_KEY"]  || "<#= OPENAI_API_KEY #>";
  const deploymentName = process.env["AZURE_OPENAI_CHAT_DEPLOYMENT"] || "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>" ;
  const systemPrompt = process.env["AZURE_OPENAI_SYSTEM_PROMPT"] || "<#= AZURE_OPENAI_SYSTEM_PROMPT #>" ;

  const streamingChatCompletions = new OpenAIStreamingChatCompletions(systemPrompt, endpoint, azureApiKey, deploymentName);

  while (true) {

    const input = await new Promise(resolve => rl.question('User: ', resolve));
    if (input === 'exit' || input === '') break;

    let response = await streamingChatCompletions.getChatCompletions(input, (content) => {
      console.log(`assistant-streaming: ${content}`);
    });

    console.log(`\nAssistant: ${response}\n`);
  }

  console.log('Bye!');
}

main().catch((err) => {
  console.error("The sample encountered an error:", err);
});

module.exports = { main };
