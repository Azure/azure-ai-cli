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
    return this.thread;
  }
  
  // Retrieve an existing thread
  async retrieveThread(threadId) {
    this.thread = await this.openai.beta.threads.retrieve(threadId);
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

    let runCompletedPromise = new Promise((resolve) => {
      this.resolveRunCompletedPromise = resolve;
    });

    let message = await this.openai.beta.threads.messages.create(this.thread.id, { role: "user", content: userInput });
    let stream = await this.openai.beta.threads.runs.stream(this.thread.id, {
      assistant_id: this.openAIAssistantId,
    });

    await this.handleStreamEvents(stream, callback);
    
    await runCompletedPromise;
    runCompletedPromise = null;
  }

  // Handle the stream events
  async handleStreamEvents(stream, callback) {
    stream.on('textDelta', async (textDelta, snapshot) => await this.onTextDelta(textDelta, callback));
    stream.on('event', async (event) => {
      if (event.event == 'thread.run.completed') {
        this.resolveRunCompletedPromise();
      }
    });
  }

  async onTextDelta(textDelta, callback) {
      let content = textDelta.value;
      if (content != null) {
        if(callback != null) {
          callback(content);
          if (this.simulateTypingDelay > 0) {
            await new Promise(r => setTimeout(r, this.simulateTypingDelay));
          }
        }
    }
  }
}

exports.<#= ClassName #> = <#= ClassName #>;