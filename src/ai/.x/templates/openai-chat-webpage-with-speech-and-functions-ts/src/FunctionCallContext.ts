import { FunctionFactory } from "./FunctionFactory";
import { ChatCompletionMessageParam } from "openai/resources/index.js";
import { ChatCompletionChunk } from "openai/resources/index.js";

export class FunctionCallContext {
  private function_factory: FunctionFactory;
  private messages: Array<ChatCompletionMessageParam>;
  private function_name: string;
  private function_arguments: string;

  constructor(function_factory: FunctionFactory, messages: Array<ChatCompletionMessageParam>) {
    this.function_factory = function_factory;
    this.messages = messages;
    this.function_name = "";
    this.function_arguments = "";
  }

  checkForUpdate(choice: ChatCompletionChunk.Choice): boolean {
    let updated = false;

    const name = choice.delta?.function_call?.name;
    if (name !== undefined) {
      this.function_name = name;
      updated = true;
    }

    const args = choice.delta?.function_call?.arguments;
    if (args !== undefined) {
      this.function_arguments = `${this.function_arguments}${args}`;
      updated = true;
    }

    return updated;
  }

  tryCallFunction(): string | undefined {
    let result = this.function_factory.tryCallFunction(this.function_name, this.function_arguments);
    if (result === undefined) {
      return undefined;
    }

    console.log(`assistant-function: ${this.function_name}(${this.function_arguments}) => ${result}`);

    this.messages.push({ role: 'assistant', content: '', function_call: { name: this.function_name, arguments: this.function_arguments } });
    this.messages.push({ role: 'function', content: result, name: this.function_name });

    return result;
  }

  clear(): void {
    this.function_name = "";
    this.function_arguments = "";
  }
}