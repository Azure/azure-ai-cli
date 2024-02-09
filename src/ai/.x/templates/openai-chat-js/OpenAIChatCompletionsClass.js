<#@ template hostspecific="true" #>
<#@ output extension=".js" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
const { OpenAIClient, AzureKeyCredential } = require("@azure/openai");

class <#= ClassName #> {
  constructor(openAIEndpoint, openAIKey, openAIChatDeploymentName, openAISystemPrompt) {
    this.openAISystemPrompt = openAISystemPrompt;
    this.openAIChatDeploymentName = openAIChatDeploymentName;
    this.client = new OpenAIClient(openAIEndpoint, new AzureKeyCredential(openAIKey));
    this.clearConversation();
  }

  clearConversation() {
    this.messages = [
      { role: 'system', content: this.openAISystemPrompt }
    ];
  }

  async getChatCompletions(userInput) {
    this.messages.push({ role: 'user', content: userInput });

    const result = await this.client.getChatCompletions(this.openAIChatDeploymentName, this.messages);
    const responseContent = result.choices[0].message.content;

    this.messages.push({ role: 'assistant', content: responseContent });
    return responseContent;
  }
}

exports.<#= ClassName #> = <#= ClassName #>;