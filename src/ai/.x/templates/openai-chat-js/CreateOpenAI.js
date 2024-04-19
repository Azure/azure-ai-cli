const { DefaultAzureCredential } = require('@azure/identity');
const { OpenAI } = require('openai');

class CreateOpenAI {

  static fromOpenAIKey(openAIAPIKey, openAIOrganization) {
    console.log('Using OpenAI...');
    return new OpenAI({
      apiKey: openAIAPIKey,
      organization: openAIOrganization,
    });
  }

  static async fromAzureOpenAIEndpoint(azureOpenAIAPIKey, azureOpenAIEndpoint, azureOpenAIAPIVersion) {
    const baseURL = `${azureOpenAIEndpoint.replace(/\/+$/, '')}/openai`;
    return await CreateOpenAI.fromAzureOpenAIBaseUrl(azureOpenAIAPIKey, baseURL, azureOpenAIAPIVersion);
  }

  static async fromAzureOpenAIEndpointAndDeployment(azureOpenAIAPIKey, azureOpenAIEndpoint, azureOpenAIAPIVersion, azureOpenAIDeploymentName) {
    const baseURL = `${azureOpenAIEndpoint.replace(/\/+$/, '')}/openai/deployments/${azureOpenAIDeploymentName}`;
    return await CreateOpenAI.fromAzureOpenAIBaseUrl(azureOpenAIAPIKey, baseURL, azureOpenAIAPIVersion);
  }

  static async fromAzureOpenAIBaseUrl(azureOpenAIAPIKey, baseURL, azureOpenAIAPIVersion) {
    console.log('Using Azure OpenAI...');
    const useBearerToken = !azureOpenAIAPIKey || azureOpenAIAPIKey.startsWith('<');
    return new OpenAI({
      apiKey: useBearerToken ? '' : azureOpenAIAPIKey,
      baseURL: baseURL,
      defaultQuery: { 'api-version': azureOpenAIAPIVersion },
      defaultHeaders: useBearerToken
        ? { Authorization: `Bearer ${await CreateOpenAI.getAzureOpenAIToken()}` }
        : { 'api-key': azureOpenAIAPIKey }
    });
  }

  static async getAzureOpenAIToken() {
    try {
      const credential = new DefaultAzureCredential();
      const token = await credential.getToken("https://cognitiveservices.azure.com/.default");
      return token.token;  
    } catch (error) {  
      console.error('Error getting access token:', error);  
      throw error;  
    }  
  }  
}

exports.CreateOpenAI = CreateOpenAI;