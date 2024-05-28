import { OpenAI } from "openai";
import { ChatCompletionMessageParam } from "openai/resources/index.js";
import { FunctionCallContext } from "./FunctionCallContext"
import { FunctionFactory } from "./FunctionFactory"

export class OpenAIChatCompletionsFunctionsStreamingClass {
  private systemPrompt: string;
  private openAIModelOrDeploymentName: string;
  private openai: OpenAI;
  private messages: Array<ChatCompletionMessageParam>;
  private functionCallContext: FunctionCallContext | undefined;
  private functionFactory: FunctionFactory;

  // Constructor
  constructor(openAIModelOrDeploymentName: string, systemPrompt: string, openai: OpenAI, functionFactory: FunctionFactory) {
    this.systemPrompt = systemPrompt;
    this.openAIModelOrDeploymentName = openAIModelOrDeploymentName;
    this.openai = openai;
    this.functionFactory = functionFactory;
    this.messages = [
      { role: 'system', content: this.systemPrompt }
    ];
    this.functionCallContext = new FunctionCallContext(this.functionFactory, this.messages);
  }

  // Clear the conversation
  clearConversation() {
    this.messages = [
      { role: 'system', content: this.systemPrompt }
    ];
    this.functionCallContext = new FunctionCallContext(this.functionFactory, this.messages);
  }

  // Get the response from Chat Completions
  async getResponse(userInput: string, callback: ((arg0: string) => void) | null) {
    this.messages.push({ role: 'user', content: userInput });

    let response = '';
    while (true) {

      const events = await this.openai.chat.completions.create({
        model: this.openAIModelOrDeploymentName,
        messages: this.messages,
        functions: this.functionFactory.getFunctionSchemas(),
        stream: true
      });

      for await (const event of events) {
        for (const choice of event.choices) {

          this.functionCallContext!.checkForUpdate(choice);

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

      if (this.functionCallContext!.tryCallFunction() !== undefined) {
        this.functionCallContext!.clear();
        continue;
      }

      this.messages.push({ role: 'assistant', content: response });
      return response;
    }
  }
}