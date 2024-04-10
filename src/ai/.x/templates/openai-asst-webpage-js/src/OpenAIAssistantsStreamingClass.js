<#@ template hostspecific="true" #>
<#@ output extension=".js" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
const { OpenAI } = require('openai');

class <#= ClassName #> {

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
    .on('event', async (event) => {
      if (event.event === 'thread.message.chunk') { // TODO: Remove once AOAI service on same version (per Salman)
        let content = event.data.delta.content.map(item => item.text.value).join('');
        callback(content);
      }
    })
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