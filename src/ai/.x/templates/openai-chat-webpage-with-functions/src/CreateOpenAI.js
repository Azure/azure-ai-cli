const { OpenAI } = require('openai');

class CreateOpenAI {

  static fromOpenAIKey(openAIAPIKey, openAIOrganization) {
    console.log('Using OpenAI...');
    return new OpenAI({
      apiKey: openAIAPIKey,
      organization: openAIOrganization,
      dangerouslyAllowBrowser: true
    });
  }

  static fromAzureOpenAIKey(azureOpenAIAPIKey, azureOpenAIEndpoint, azureOpenAIAPIVersion) {
    console.log('Using Azure OpenAI...');
    return new OpenAI({
      apiKey: azureOpenAIAPIKey,
      baseURL: `${azureOpenAIEndpoint.replace(/\/+$/, '')}/openai`,
      defaultQuery: { 'api-version': azureOpenAIAPIVersion },
      defaultHeaders: { 'api-key': azureOpenAIAPIKey },
      dangerouslyAllowBrowser: true
    });
  }

  static fromAzureOpenAIKeyAndDeployment(azureOpenAIAPIKey, azureOpenAIEndpoint, azureOpenAIAPIVersion, azureOpenAIDeploymentName) {
    console.log('Using Azure OpenAI...');
    return new OpenAI({
      apiKey: azureOpenAIAPIKey,
      baseURL: `${azureOpenAIEndpoint.replace(/\/+$/, '')}/openai/deployments/${azureOpenAIDeploymentName}`,
      defaultQuery: { 'api-version': azureOpenAIAPIVersion },
      defaultHeaders: { 'api-key': azureOpenAIAPIKey },
      dangerouslyAllowBrowser: true
    });
  }
}

exports.CreateOpenAI = CreateOpenAI;