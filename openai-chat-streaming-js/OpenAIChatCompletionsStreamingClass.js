const { OpenAI } = require('openai');

class OpenAIChatCompletionsStreamingClass {
  constructor(openAIKey, openAIOrganization, openAIModelName, openAISystemPrompt) {
    this.openAISystemPrompt = openAISystemPrompt;
    this.openAIModelName = openAIModelName;
    this.client = new OpenAI({
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

  async getChatCompletions(userInput, callback) {
    this.messages.push({ role: 'user', content: userInput });

    let contentComplete = '';
    const events = await this.client.chat.completions.create({
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
          await new Promise(r => setTimeout(r, 50)); // delay to simulate real-time output, word by word
          contentComplete += content;
        }
      }
    }

    this.messages.push({ role: 'assistant', content: contentComplete });
    return contentComplete;
  }
}

exports.OpenAIChatCompletionsStreamingClass = OpenAIChatCompletionsStreamingClass;