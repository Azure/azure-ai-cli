const { InteractiveBrowserCredential } = require('@azure/identity');
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

  static async fromAzureOpenAIEndpoint(azureOpenAIEndpoint, azureOpenAIAPIVersion, azureOpenAIAPIKey, clientId = null, tenantId = null) {
    const baseURL = `${azureOpenAIEndpoint.replace(/\/+$/, '')}/openai`;
    return await CreateOpenAI.fromAzureOpenAIBaseUrl(baseURL, azureOpenAIAPIVersion, azureOpenAIAPIKey, clientId, tenantId);
  }

  static async fromAzureOpenAIEndpointAndDeployment(azureOpenAIEndpoint, azureOpenAIAPIVersion, azureOpenAIDeploymentName, azureOpenAIAPIKey, clientId = null, tenantId = null) {
    const baseURL = `${azureOpenAIEndpoint.replace(/\/+$/, '')}/openai/deployments/${azureOpenAIDeploymentName}`;
    return await CreateOpenAI.fromAzureOpenAIBaseUrl(baseURL, azureOpenAIAPIVersion, azureOpenAIAPIKey, clientId, tenantId);
  }

  static async fromAzureOpenAIBaseUrl(baseURL, azureOpenAIAPIVersion, azureOpenAIAPIKey, clientId, tenantId) {
    console.log('Using Azure OpenAI...');
    const useBearerToken = !azureOpenAIAPIKey || azureOpenAIAPIKey.startsWith('<');
    return new OpenAI({
      apiKey: useBearerToken ? '' : azureOpenAIAPIKey,
      baseURL: baseURL,
      defaultQuery: { 'api-version': azureOpenAIAPIVersion },
      defaultHeaders: useBearerToken
        ? { Authorization: `Bearer ${await CreateOpenAI.getAzureOpenAIToken(clientId, tenantId)}` }
        : { 'api-key': azureOpenAIAPIKey },
      dangerouslyAllowBrowser: true
      });
  }

  static async getAzureOpenAIToken(clientId, tenantId) {
    try {
      const credential = new InteractiveBrowserCredential({
        clientId: clientId,
        tenantId: tenantId,
        loginStyle: 'redirect'
      });
      const token = await credential.getToken("https://cognitiveservices.azure.com/.default");
      return token.token;  
    } catch (error) {  
      console.error('Error getting access token:', error);  
      throw error;  
    }  
  }  
}

exports.CreateOpenAI = CreateOpenAI;