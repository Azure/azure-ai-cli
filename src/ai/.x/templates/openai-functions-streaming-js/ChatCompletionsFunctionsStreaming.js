const { OpenAIClient, AzureKeyCredential } = require("@azure/openai");
const { FunctionCallContext } = require("./FunctionCallContext");

class ChatCompletionsFunctionsStreaming {
  constructor(systemPrompt, endpoint, azureApiKey, deploymentName, functionFactory) {
    this.systemPrompt = systemPrompt;
    this.deploymentName = deploymentName;
    this.client = new OpenAIClient(endpoint, new AzureKeyCredential(azureApiKey));
    this.functionFactory = functionFactory;
    this.clearConversation();
  }

  clearConversation() {
    this.messages = [
      { role: 'system', content: this.systemPrompt }
    ];
    this.functionCallContext = new FunctionCallContext(this.functionFactory, this.messages);
  }

  async getChatCompletions(userInput, callback) {
    this.messages.push({ role: 'user', content: userInput });

    let contentComplete = '';
    while (true) {
      const events = this.client.listChatCompletions(this.deploymentName, this.messages, {
        functions: this.functionFactory.getFunctionSchemas(),
      });

      for await (const event of events) {
        for (const choice of event.choices) {

          this.functionCallContext.checkForUpdate(choice);

          let content = choice.delta?.content;
          if (choice.finishReason === 'length') {
            content = `${content}\nERROR: Exceeded token limit!`;
          }

          if (content != null) {
            callback(content);
            await new Promise(r => setTimeout(r, 50)); // delay to simulate real-time output, word by word
            contentComplete += content;
          }
        }
      }

      if (this.functionCallContext.tryCallFunction() !== undefined) {
        this.functionCallContext.clear();
        continue;
      }

      this.messages.push({ role: 'assistant', content: contentComplete });
      return contentComplete;
    }
  }
}

exports.ChatCompletionsFunctionsStreaming = ChatCompletionsFunctionsStreaming;