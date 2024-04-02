const { OpenAI } = require('openai');

class OpenAIAssistantsStreamingClass {

  // Create the class using the Azure OpenAI API, which requires a different setup, baseURL, api-key headers, and query parameters
  static createUsingAzure(azureOpenAIAPIVersion, azureOpenAIEndpoint, azureOpenAIKey, azureOpenAIDeploymentName, openAIAssistantId) {
    console.log("Using Azure OpenAI API...");
    return new OpenAIAssistantsStreamingClass(openAIAssistantId,
      new OpenAI({
        apiKey: azureOpenAIKey,
        baseURL: `${azureOpenAIEndpoint.replace(/\/+$/, '')}/openai`,
        defaultQuery: { 'api-version': azureOpenAIAPIVersion },
        defaultHeaders: { 'api-key': azureOpenAIKey },
        dangerouslyAllowBrowser: true
        }),
      30);
  }

  // Create the class using the OpenAI API and an optional organization
  static createUsingOpenAI(openAIKey, openAIOrganization, openAIAssistantId) {
    console.log("Using OpenAI API...");
    return new OpenAIAssistantsStreamingClass(openAIAssistantId,
      new OpenAI({
        apiKey: openAIKey,
        organization: openAIOrganization,
        dangerouslyAllowBrowser: true
      }));
  }

  // Constructor
  constructor(openAIAssistantId, openai, simulateTypingDelay = 0) {
    this.simulateTypingDelay = simulateTypingDelay;
    this.openAIAssistantId = openAIAssistantId;
    this.thread = null;
    this.openai = openai;
  }

  // Create a new the thread
  async createThread() {
    this.thread = await this.openai.beta.threads.create();
    console.log(`Thread ID: ${this.thread.id}`);
    return this.thread;
  }
  
  // Retrieve an existing thread
  async retrieveThread(threadId) {
    this.thread =await this.openai.beta.threads.retrieve(threadId);
    console.log(`Thread ID: ${this.thread.id}`);
    return this.thread;
  }

  // Get the messages in the thread
  async getThreadMessages(callback) {

    const messages = await this.openai.beta.threads.messages.list(this.thread.id);
    messages.data.reverse();

    for (const message of messages.data) {
      let content = message.content.map(item => item.text.value).join('') + '\n\n';
      callback(message.role, content);
    }
  }

  // Get the response from the Assistant
  async getResponse(userInput, callback) {

    if (this.thread == null) {
      await this.getOrCreateThread();
    }

    await this.openai.beta.threads.messages.create(this.thread.id, { role: "user", content: userInput });

    let response = '';
    let stream = await this.openai.beta.threads.runs.createAndStream(
      this.thread.id,
      { assistant_id: this.openAIAssistantId }
    )
    .on('textDelta', async (textDelta, snapshot) => {
      let content = textDelta.value;
      if (content != null) {
        if(callback != null) {
          callback(content);
          if (this.simulateTypingDelay > 0) {
            await new Promise(r => setTimeout(r, this.simulateTypingDelay));
          }
      }
        response += content;
      }
    });

    await stream.finalMessages();
    return response;
  }
}

exports.OpenAIAssistantsStreamingClass = OpenAIAssistantsStreamingClass;