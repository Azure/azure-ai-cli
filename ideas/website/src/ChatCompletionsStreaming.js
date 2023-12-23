function getCurrentDate() {
  const date = new Date();
  return `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`;
}

const getCurrentDateSchema = {
  name: "get_current_date",
  description: "Get the current date",
  parameters: {
    type: "object",
    properties: {
    },
  },
};

class FunctionFactory {
  constructor() {
    this.functions = {};
  }

  addFunction(schema, fun) {
    this.functions[schema.name] = { schema: schema, function: fun };
  }

  getFunctionSchemas() {
    return Object.values(this.functions).map(value => value.schema);
  }

  tryCallFunction(function_name, function_arguments) {
    const function_info = this.functions[function_name];
    if (function_info === undefined) {
      return undefined;
    }

    return function_info.function(function_arguments);
  }
}  

class FunctionCallContext {
  constructor(function_factory, messages) {
    this.function_factory = function_factory;
    this.messages = messages;
    this.function_name = "";
    this.function_arguments = "";
  }

  checkForUpdate(choice) {
    let updated = false;

    const name = choice.delta?.functionCall?.name;
    if (name !== undefined) {
      this.function_name = name;
      updated = true;
    }

    const args = choice.delta?.functionCall?.arguments;
    if (args !== undefined) {
      this.function_arguments = `${this.function_arguments}${args}`;
      updated = true;
    }

    return updated;
  }

  tryCallFunction() {
    let result = this.function_factory.tryCallFunction(this.function_name, this.function_arguments);
    if (result === undefined) {
      return undefined;
    }

    console.log(`assistant-function: ${this.function_name}(${this.function_arguments}) => ${result}`);

    this.messages.push({ role: 'assistant', function_call: { name: this.function_name, arguments: this.function_arguments } });
    this.messages.push({ role: 'function', content: result, name: this.function_name });

    return result;
  }

  clear() {
    this.function_name = "";
    this.function_arguments = "";
  }
}

const { OpenAIClient, AzureKeyCredential } = require('@azure/openai');

class StreamingChatCompletionsHelper {
  constructor(systemPrompt, endpoint, azureApiKey, deploymentName) {
    this.systemPrompt = systemPrompt;
    this.endpoint = endpoint;
    this.azureApiKey = azureApiKey;
    this.deploymentName = deploymentName;
    this.client = new OpenAIClient(this.endpoint, new AzureKeyCredential(this.azureApiKey));

    let factory = new FunctionFactory();
    factory.addFunction(getCurrentDateSchema, getCurrentDate);
  
    this.functionFactory = factory;
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

    let contentComplete = "";
    while (true)
    {
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
            await new Promise(r => setTimeout(r, 50));
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

module.exports = { StreamingChatCompletionsHelper };
