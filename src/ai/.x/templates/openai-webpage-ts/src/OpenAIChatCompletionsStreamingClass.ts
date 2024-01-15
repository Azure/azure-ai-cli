<#@ template hostspecific="true" #>
<#@ output extension=".ts" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
import { OpenAIClient, AzureKeyCredential, ChatRequestMessage } from "@azure/openai";

export class <#= ClassName #> {
  private openAISystemPrompt: string;
  private openAIChatDeploymentName: string;
  private client: OpenAIClient;
  private messages: ChatRequestMessage[] = [];

  constructor(openAIEndpoint: string, openAIKey: string, openAIChatDeploymentName: string, openAISystemPrompt: string) {
    this.openAISystemPrompt = openAISystemPrompt;
    this.openAIChatDeploymentName = openAIChatDeploymentName;
    this.client = new OpenAIClient(openAIEndpoint, new AzureKeyCredential(openAIKey));
    this.clearConversation();
  }

  clearConversation(): void {
    this.messages = [
      { role: 'system', content: this.openAISystemPrompt }
    ];
  }

  async getChatCompletions(userInput: string, callback: (content: string) => void): Promise<string> {
    this.messages.push({ role: 'user', content: userInput });

    let contentComplete = '';
    const events = await this.client.streamChatCompletions(this.openAIChatDeploymentName, this.messages);

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