<#@ template hostspecific="true" #>
<#@ output extension=".js" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
const { OpenAI } = require('openai');

class <#= ClassName #> {

  // Create the class using the Azure OpenAI API, which requires a different setup, baseURL, api-key headers, and query parameters
  static createUsingAzure(azureOpenAIAPIVersion, azureOpenAIEndpoint, azureOpenAIKey, azureOpenAIDeploymentName, openAIAssistantId) {
    console.log("Using Azure OpenAI API...");
    return new <#= ClassName #>(openAIAssistantId,
      new OpenAI({
        apiKey: azureOpenAIKey,
        baseURL: `${azureOpenAIEndpoint.replace(/\/+$/, '')}/openai`,
        defaultQuery: { 'api-version': azureOpenAIAPIVersion },
        defaultHeaders: { 'api-key': azureOpenAIKey },
        }),
      30);
  }

  // Create the class using the OpenAI API and an optional organization
  static createUsingOpenAI(openAIKey, openAIOrganization, openAIAssistantId) {
    console.log("Using OpenAI API...");
    return new <#= ClassName #>(openAIAssistantId,
      new OpenAI({
        apiKey: openAIKey,
        organization: openAIOrganization,
      }));
  }

  // Constructor
  constructor(openAIAssistantId, openai, simulateTypingDelay = 0) {
    this.simulateTypingDelay = simulateTypingDelay;
    this.openAIAssistantId = openAIAssistantId;
    this.thread = null;
    this.openai = openai;
  }

  // Get or create the thread
  async getOrCreateThread(threadId = null) {
    this.thread = threadId == null
      ? await this.openai.beta.threads.create()
      : await this.openai.beta.threads.retrieve(threadId);
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

exports.<#= ClassName #> = <#= ClassName #>;