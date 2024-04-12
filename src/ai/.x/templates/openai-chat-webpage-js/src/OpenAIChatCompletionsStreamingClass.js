<#@ template hostspecific="true" #>
<#@ output extension=".js" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
const { OpenAI } = require('openai');

class <#= ClassName #> {

  // Create the class using the Azure OpenAI API, which requires a different setup, baseURL, api-key headers, and query parameters
  static createUsingAzure(azureOpenAIAPIVersion, azureOpenAIEndpoint, azureOpenAIAPIKey, azureOpenAIDeploymentName, systemPrompt) {
    console.log("Using Azure OpenAI API...");
    return new OpenAIChatCompletionsStreamingClass(azureOpenAIDeploymentName, systemPrompt,
      new OpenAI({
        apiKey: azureOpenAIAPIKey,
        baseURL: `${azureOpenAIEndpoint.replace(/\/+$/, '')}/openai/deployments/${azureOpenAIDeploymentName}`,
        defaultQuery: { 'api-version': azureOpenAIAPIVersion },
        defaultHeaders: { 'api-key': azureOpenAIAPIKey },
        dangerouslyAllowBrowser: true
        }),
      30);
  }

  // Create the class using the OpenAI API and an optional organization
  static createUsingOpenAI(openAIAPIKey, openAIModelName, systemPrompt, openAIOrganization = null) {
    console.log("Using OpenAI API...");
    return new OpenAIChatCompletionsStreamingClass(openAIModelName, systemPrompt,
      new OpenAI({
        apiKey: openAIAPIKey,
        organization: openAIOrganization,
        dangerouslyAllowBrowser: true
      }));
  }

  // Constructor
  constructor(openAIModelOrDeploymentName, systemPrompt, openai, simulateTypingDelay = 0) {
    this.simulateTypingDelay = simulateTypingDelay;
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

  // Get the response from Chat Completions
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
            if (this.simulateTypingDelay > 0) {
              await new Promise(r => setTimeout(r, this.simulateTypingDelay));
            }
          }
          response += content;
        }
      }
    }

    this.messages.push({ role: 'assistant', content: response });
    return response;
  }
}

exports.<#= ClassName #> = <#= ClassName #>;