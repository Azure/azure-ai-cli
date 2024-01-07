const { OpenAIClient, AzureKeyCredential } = require("@azure/openai");

class ChatCompletions {
  constructor(systemPrompt, endpoint, azureApiKey, deploymentName) {
    this.systemPrompt = systemPrompt;
    this.deploymentName = deploymentName;
    this.client = new OpenAIClient(endpoint, new AzureKeyCredential(azureApiKey));
    this.clearConversation();
  }

  clearConversation() {
    this.messages = [
      { role: 'system', content: this.systemPrompt }
    ];
  }

  async getChatCompletions(userInput) {
    this.messages.push({ role: 'user', content: userInput });

    const result = await this.client.getChatCompletions(this.deploymentName, this.messages);
    const response_content = result.choices[0].message.content;

    this.messages.push({ role: 'assistant', content: response_content });
    return response_content;
  }
}

exports.ChatCompletions = ChatCompletions;