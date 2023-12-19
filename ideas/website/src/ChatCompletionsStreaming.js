const { OpenAIClient, AzureKeyCredential } = require('@azure/openai');

class StreamingChatCompletionsHelper {
  constructor(systemPrompt, endpoint, azureApiKey, deploymentName) {
    this.systemPrompt = systemPrompt;
    this.endpoint = endpoint;
    this.azureApiKey = azureApiKey;
    this.deploymentName = deploymentName;
    this.client = new OpenAIClient(this.endpoint, new AzureKeyCredential(this.azureApiKey));
    this.messages = [
      { role: 'system', content: this.systemPrompt }
    ];
  }

  async getChatCompletions(userInput, callback) {
    this.messages.push({ role: 'user', content: userInput });

    const events = this.client.listChatCompletions(this.deploymentName, this.messages);

    let response_content = '';
    for await (const event of events) {
      for (const choice of event.choices) {

        let content = choice.delta?.content;
        if (choice.finishReason === 'length') {
          content = `${content}\nERROR: Exceeded token limit!`;
        }

        if (content != null) {
          console.log(`Assistant: ${content}`);
          callback(content);
          await new Promise(r => setTimeout(r, 50));
          response_content += content;
        }
      }
    }

    console.log(`Assistant: ${response_content}`);
    this.messages.push({ role: 'assistant', content: response_content });
    return response_content;
  }
}

module.exports = { StreamingChatCompletionsHelper };
