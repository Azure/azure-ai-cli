const { OpenAI } = require('openai');

class OpenAIChatCompletionsStreamingClass {
  constructor(openAIKey, openAIOrganization, openAIModelName, openAISystemPrompt) {
    this.openAISystemPrompt = openAISystemPrompt;
    this.openAIModelName = openAIModelName;
    this.openai = new OpenAI({
      apiKey: openAIKey,
      organization: openAIOrganization,
    });
    
    this.clearConversation();
  }

  clearConversation() {
    this.messages = [
      { role: 'system', content: this.openAISystemPrompt }
    ];
  }

  async getResponse(userInput, callback) {
    this.messages.push({ role: 'user', content: userInput });

    let response = '';
    const events = await this.openai.chat.completions.create({
      model: this.openAIModelName,
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