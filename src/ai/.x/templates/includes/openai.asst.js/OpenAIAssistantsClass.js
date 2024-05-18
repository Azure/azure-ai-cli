const { OpenAI } = require('openai');

class {ClassName} {

  // Constructor
  {{if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}
  constructor(openAIAssistantId, functionFactory, openai, simulateTypingDelay = 0) {
  {{else}}
  constructor(openAIAssistantId, openai, simulateTypingDelay = 0) {
  {{endif}}
    this.simulateTypingDelay = simulateTypingDelay;
    this.openAIAssistantId = openAIAssistantId;
    {{if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}
    this.functionFactory = functionFactory;
    {{endif}}
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
  {{if {_IS_OPENAI_ASST_STREAMING_TEMPLATE}}}
  async getResponse(userInput, callback) {
  {{else}}
  async getResponse(userInput) {
  {{endif}}

    if (this.thread == null) {
      await this.createThread();
    }

    await this.openai.beta.threads.messages.create(this.thread.id, { role: "user", content: userInput });
    {{if {_IS_OPENAI_ASST_STREAMING_TEMPLATE}}}
    let stream = await this.openai.beta.threads.runs.stream(this.thread.id, {
      assistant_id: this.openAIAssistantId,
      {{if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}
      tools: this.functionFactory.getTools()
      {{endif}}
    });

    let runCompletedPromise = new Promise((resolve) => {
      this.resolveRunCompletedPromise = resolve;
    });

    await this.handleStreamEvents(stream, callback);
    
    await runCompletedPromise;
    runCompletedPromise = null;
    {{else}}
    const run = await this.openai.beta.threads.runs.createAndPoll(this.thread.id, { assistant_id: this.openAIAssistantId });
    if (run.status === 'completed') {
      const messages = await this.openai.beta.threads.messages.list(run.thread_id)
      return messages.data[0].content.map(item => item.text.value).join('');
    }

    return run.status.toString();
    {{endif}}
  }
  {{if {_IS_OPENAI_ASST_STREAMING_TEMPLATE}}}

  // Handle the stream events
  async handleStreamEvents(stream, callback) {
    stream.on('textDelta', async (textDelta, snapshot) => await this.onTextDelta(textDelta, callback));
    stream.on('event', async (event) => {
      if (event.event == 'thread.run.completed') {
        this.resolveRunCompletedPromise();
      }
      else if (event.event == 'thread.run.failed') {
        console.log(JSON.stringify(event));
        throw new Error('Run failed');
      }
      {{if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}
      else if (event.event == 'thread.run.requires_action') {
        await this.onThreadRunRequiresAction(event, callback);
      }
      {{endif}}
    });
    {{if {_IS_OPENAI_ASST_CODE_INTERPRETER_TEMPLATE}}}
    stream.on('toolCallCreated', (toolCall) => {
      if (toolCall.type === 'code_interpreter') {
        process.stdout.write('\n\nassistant-code:\n');
      }
    });
    stream.on('toolCallDelta', (toolCallDelta, snapshot) => {
      if (toolCallDelta.type === 'code_interpreter') {
        if (toolCallDelta.code_interpreter.input) {
          process.stdout.write(toolCallDelta.code_interpreter.input);
        }
        if (toolCallDelta.code_interpreter.outputs) {
          process.stdout.write('\n\nassistant-output:');
          toolCallDelta.code_interpreter.outputs.forEach(output => {
            if (output.type === "logs") {
              process.stdout.write(`\n${output.logs}\n`);
            }
          });
        }
      }
    });
    {{endif}}
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
  {{if {_IS_OPENAI_ASST_FUNCTIONS_TEMPLATE}}}

  async onThreadRunRequiresAction(event, callback) {
    let toolCalls = event.data?.required_action?.submit_tool_outputs?.tool_calls;
    if (toolCalls != null) {
      let tool_outputs = this.getToolOutputs(toolCalls);
      let stream = await this.openai.beta.threads.runs.submitToolOutputsStream(this.thread.id, event.data.id, { tool_outputs: tool_outputs})
      await this.handleStreamEvents(stream, callback);
    }
  }

  getToolOutputs(toolCalls) {
    let tool_outputs = [];
    for (let toolCall of toolCalls) {
      if (toolCall.type == 'function') {
        let result = this.functionFactory.tryCallFunction(toolCall.function?.name, toolCall.function?.arguments);
        tool_outputs.push({
          output: result,
          tool_call_id: toolCall.id
        });
      }
    }
    return tool_outputs;
  }
  {{endif}}
  {{endif}}
}

exports.{ClassName} = {ClassName};