const { OpenAIClient, AzureKeyCredential } = require('@azure/openai');

async function getChatCompletions(userInput, systemPrompt, endpoint, azureApiKey, deploymentName) {
  const client = new OpenAIClient(endpoint, new AzureKeyCredential(azureApiKey));
  const messages = [
    { role: 'system', content: systemPrompt },
    { role: 'user', content: userInput }
  ];
  const events = client.listChatCompletions(deploymentName, messages);
  let response_content = '';
  for await (const event of events) {
    for (const choice of event.choices) {
      const content = choice.delta?.content;
      if (choice.finishReason === 'length') {
        content = `${content}\nERROR: Exceeded token limit!`;
      }
      if (content != null) response_content += content;
    }
  }
  return response_content;
}

module.exports = { getChatCompletions };