const { OpenAI } = require('openai');

class OpenAIAssistantsStreamingClass {

  static createUsingAzure(azureOpenAIAPIVersion, azureOpenAIEndpoint, azureOpenAIKey, azureOpenAIDeploymentName, openAIAssistantId) {
    return new OpenAIAssistantsStreamingClass(openAIAssistantId, new OpenAI({
      apiKey: azureOpenAIKey,
      baseURL: `${azureOpenAIEndpoint.replace(/\/+$/, '')}/openai`,
      defaultQuery: { 'api-version': azureOpenAIAPIVersion },
      defaultHeaders: { 'api-key': azureOpenAIKey },
    }));
  }

  static createUsingOpenAI(openAIKey, openAIOrganization, openAIAssistantId) {
    return new OpenAIAssistantsStreamingClass(openAIAssistantId, new OpenAI({
      apiKey: openAIKey,
      organization: openAIOrganization,
    }));
  }

  constructor(openAIAssistantId, openai) {
    this.openAIAssistantId = openAIAssistantId;
    this.thread = null;
    this.openai = openai;
  }

  async getOrCreateThread(threadId = null) {
    this.thread = threadId == null
      ? await this.openai.beta.threads.create()
      : await this.openai.beta.threads.retrieve(threadId);
    return this.thread;
  }

  async getThreadMessages(callback) {

    const messages = await this.openai.beta.threads.messages.list(this.thread.id);
    messages.data.reverse();

    for (const message of messages.data) {
      let content = message.content.map(item => item.text.value).join('') + '\n\n';
      callback(message.role, content);
    }
  }

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
        }
        response += content;
      }
    });

    await stream.finalMessages();
    return response;
  }
}

exports.OpenAIAssistantsStreamingClass = OpenAIAssistantsStreamingClass;