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

  async getChatCompletions(userInput) {
    this.messages.push({ role: 'user', content: userInput });

    const result = await this.client.getChatCompletions(this.deploymentName, this.messages);
    const responseContent = result.choices[0].message.content;

    this.messages.push({ role: 'assistant', content: responseContent });
    return responseContent;
  }
}

exports.<#= ClassName #> = <#= ClassName #>;