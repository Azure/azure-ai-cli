const { OpenAI } = require('openai');

class OpenAIChatCompletionsStreamingClass {

  // Create the class using the Azure OpenAI API, which requires a different setup, baseURL, api-key headers, and query parameters
  static createUsingAzure(azureOpenAIAPIVersion, azureOpenAIEndpoint, azureOpenAIKey, azureOpenAIDeploymentName, systemPrompt) {
    console.log("Using Azure OpenAI API...");
    return new OpenAIChatCompletionsStreamingClass(azureOpenAIDeploymentName, systemPrompt,
      new OpenAI({
        apiKey: azureOpenAIKey,
        baseURL: `${azureOpenAIEndpoint.replace(/\/+$/, '')}/openai/deployments/${azureOpenAIDeploymentName}`,
        defaultQuery: { 'api-version': azureOpenAIAPIVersion },
        defaultHeaders: { 'api-key': azureOpenAIKey },
      }));
  }

  // Create the class using the OpenAI API and an optional organization
  static createUsingOpenAI(openAIKey, openAIModelName, systemPrompt, openAIOrganization = null) {
    console.log("Using OpenAI API...");
    return new OpenAIChatCompletionsStreamingClass(openAIModelName, systemPrompt,
      new OpenAI({
        apiKey: openAIKey,
        organization: openAIOrganization,
      }));
  }

  // Constructor
  constructor(openAIModelOrDeploymentName, systemPrompt, openai) {
    this.systemPrompt = systemPrompt;
    this.openAIModelOrDeploymentName = openAIModelOrDeploymentName;
    this.openai = openai;
  
    this.clearConversation();
  }

  // Clear the conversation
  clearConversation() {
    this.messages = [
      { role: 'system', content: this.systemPrompt }
    ];
  }

  // Get the response from the Assistant
  async getResponse(userInput, callback) {
    this.messages.push({ role: 'user', content: userInput });

    let response = '';
    const events = await this.openai.chat.completions.create({
      model: this.openAIModelOrDeploymentName,
      messages: this.messages,
      stream: true
    });

    for await (const event of events) {
      for (const choice of event.choices) {

        let content = choice.delta?.content;
        if (choice.finish_reason === 'length') {
          content = `${content}\nERROR: Exceeded token limit!`;
        }

        if (content != null) {
          if(callback != null) {
            callback(content);
          }
          response += content;
        }
      }
    }

    this.messages.push({ role: 'assistant', content: response });
    return response;
  }
}

exports.OpenAIChatCompletionsStreamingClass = OpenAIChatCompletionsStreamingClass;