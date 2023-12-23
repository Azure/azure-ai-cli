<#@ template hostspecific="true" #>
<#@ output extension=".js" encoding="utf-8" #>
<#@ parameter type="System.String" name="OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="OPENAI_API_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
function getCurrentWeather(function_arguments) {
  const location = JSON.parse(function_arguments).location;
  return `The weather in ${location} is 72 degrees and sunny.`;
}

const getCurrentWeatherSchema = {
  name: "get_current_weather",
  description: "Get the current weather in a given location",
  parameters: {
    type: "object",
    properties: {
      location: {
        type: "string",
        description: "The city and state, e.g. San Francisco, CA",
      },
      unit: {
        type: "string",
        enum: ["celsius", "fahrenheit"],
      },
    },
    required: ["location"],
  },
};

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

const { OpenAIClient, AzureKeyCredential } = require("@azure/openai");

class OpenAIStreamingChatCompletions {
  constructor(systemPrompt, endpoint, azureApiKey, deploymentName) {
    this.systemPrompt = systemPrompt;
    this.endpoint = endpoint;
    this.azureApiKey = azureApiKey;
    this.deploymentName = deploymentName;
    this.client = new OpenAIClient(this.endpoint, new AzureKeyCredential(this.azureApiKey));
    this.clearConversation();
  }

  clearConversation() {
    this.messages = [
      { role: 'system', content: this.systemPrompt }
    ];
  }

  async getChatCompletions(userInput, callback) {
    this.messages.push({ role: 'user', content: userInput });

    let contentComplete = "";

    while (true)
    {
      const events = this.client.listChatCompletions(this.deploymentName, this.messages, {
        functions: [getCurrentWeatherSchema, getCurrentDateSchema]
      });

      let function_name = "";
      let function_arguments = "";

      for await (const event of events) {
        for (const choice of event.choices) {

          const name = choice.delta?.functionCall?.name;
          if (function_name === "" && name !== undefined) {
            function_name = name;
          }

          const args = choice.delta?.functionCall?.arguments;
          if (args !== undefined) {
            function_arguments = `${function_arguments}${args}`;
          }

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

      if (function_name !== "" && function_arguments !== "") {
        let result = "";
        if (function_name === "get_current_weather") {
          result = getCurrentWeather(function_arguments);
        }
        else if (function_name === "get_current_date") {
          result = getCurrentDate(function_arguments);
        }
        else {
          result = `ERROR: Unknown function ${function_name}`;
        }

        console.log(`assistant-function: ${function_name}(${function_arguments}) => ${result}`);

        this.messages.push({ role: 'assistant', function_call: { name: function_name, arguments: function_arguments } });
        this.messages.push({ role: 'function', content: result, name: function_name });
        function_name = "";
        function_arguments = "";
        continue;
      }

      this.messages.push({ role: 'assistant', content: contentComplete });
      return contentComplete;
    }
  }
}

const readline = require('readline');
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
});

async function main() {
  const endpoint = process.env["OPENAI_ENDPOINT"] || "<#= OPENAI_ENDPOINT #>";
  const azureApiKey = process.env["OPENAI_API_KEY"]  || "<#= OPENAI_API_KEY #>";
  const deploymentName = process.env["AZURE_OPENAI_CHAT_DEPLOYMENT"] || "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>" ;
  const systemPrompt = process.env["AZURE_OPENAI_SYSTEM_PROMPT"] || "<#= AZURE_OPENAI_SYSTEM_PROMPT #>" ;

  const streamingChatCompletions = new OpenAIStreamingChatCompletions(systemPrompt, endpoint, azureApiKey, deploymentName);

  while (true) {

    const input = await new Promise(resolve => rl.question('User: ', resolve));
    if (input === 'exit' || input === '') break;

    let response = await streamingChatCompletions.getChatCompletions(input, (content) => {
      console.log(`assistant-streaming: ${content}`);
    });

    console.log(`\nAssistant: ${response}\n`);
  }

  console.log('Bye!');
}

main().catch((err) => {
  console.error("The sample encountered an error:", err);
});

module.exports = { main };
