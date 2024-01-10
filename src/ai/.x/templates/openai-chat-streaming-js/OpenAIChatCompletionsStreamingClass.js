<#@ template hostspecific="true" #>
<#@ output extension=".js" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
const { OpenAIClient, AzureKeyCredential } = require("@azure/openai");

class <#= ClassName #> {
  constructor(systemPrompt, endpoint, azureApiKey, deploymentName) {
    this.systemPrompt = systemPrompt;
    this.deploymentName = deploymentName;
    this.client = new OpenAIClient(endpoint, new AzureKeyCredential(azureApiKey));
    this.clearConversation();
  }

  clearConversation() {
    this.messages = [
      { role: 'system', content: this.systemPrompt }
    ];
  }

  async getChatCompletions(userInput, callback) {
    this.messages.push({ role: 'user', content: userInput });

    let contentComplete = '';
    const events = await this.client.streamChatCompletions(this.deploymentName, this.messages);

    for await (const event of events) {
      for (const choice of event.choices) {

        let content = choice.delta?.content;
        if (choice.finishReason === 'length') {
          content = `${content}\nERROR: Exceeded token limit!`;
        }

        if (content != null) {
          callback(content);
          await new Promise(r => setTimeout(r, 50)); // delay to simulate real-time output, word by word
          contentComplete += content;
        }
      }
    }

    this.messages.push({ role: 'assistant', content: contentComplete });
    return contentComplete;
  }
}

exports.<#= ClassName #> = <#= ClassName #>;