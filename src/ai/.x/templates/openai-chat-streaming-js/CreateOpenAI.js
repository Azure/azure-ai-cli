const { InteractiveBrowserCredential } = require('@azure/identity');
const { OpenAIEnvInfo } = require('./OpenAIEnvInfo');
const { OpenAI } = require('openai');

class CreateOpenAI {

  static async forAssistantsAPI({ dangerouslyAllowBrowser, errorCallback } = {}) {
    return this.forAPI({ api: 'assistants', dangerouslyAllowBrowser, errorCallback });
  }

  static async forChatCompletionsAPI({ dangerouslyAllowBrowser, errorCallback } = {}) {
    return this.forAPI({ api: 'chat', dangerouslyAllowBrowser, errorCallback });
  }

  static async forAPI({ api, dangerouslyAllowBrowser, errorCallback } = {}) {

    if (!OpenAIEnvInfo.isConnectionInfoOk(errorCallback)) return null;

    if (OpenAIEnvInfo.isAzureAADConnectionInfoOk()) {
      const baseURL = api === 'assistants' ? OpenAIEnvInfo.getAzureAssistantAPIBaseUrl() : OpenAIEnvInfo.getAzureChatCompletionsAPIBaseUrl();
      return dangerouslyAllowBrowser
        ? await CreateOpenAI.fromAzureInteractiveBrowserCredential(baseURL)
        : await CreateOpenAI.fromDefaultAzureCredential(baseURL);
    }

    if (OpenAIEnvInfo.isAzureKeyConnectionInfoOk()) {
      const baseURL = api === 'assistants' ? OpenAIEnvInfo.getAzureAssistantAPIBaseUrl() : OpenAIEnvInfo.getAzureChatCompletionsAPIBaseUrl();
      return dangerouslyAllowBrowser
        ? await CreateOpenAI.fromAzureOpenAIWithKeyFromBrowser(baseURL)
        : await CreateOpenAI.fromAzureOpenAIWithKeyFromNode(baseURL);
    }

    if (OpenAIEnvInfo.isOpenAIConnectionInfoOk()) {
      return dangerouslyAllowBrowser
        ? CreateOpenAI.fromOpenAIKeyFromBrowser()
        : CreateOpenAI.fromOpenAIKeyFromNode();
    }

    return null;
  }

  static async fromAzureInteractiveBrowserCredential(baseURL) {
    const token = await CreateOpenAI.getAzureInteractiveBrowserCredentialToken(OpenAIEnvInfo.AZURE_CLIENT_ID, OpenAIEnvInfo.AZURE_TENANT_ID);
    return CreateOpenAI.fromAzureWithToken(baseURL, OpenAIEnvInfo.AZURE_OPENAI_API_VERSION, token, true);
  }

  static async fromDefaultAzureCredential(baseURL) {
    const token = await CreateOpenAI.getAzureDefaultAzureCredentialToken();
    return CreateOpenAI.fromAzureWithToken(baseURL, OpenAIEnvInfo.AZURE_OPENAI_API_VERSION, token, false);
  }

  static async fromAzureOpenAIWithKeyFromBrowser(baseURL) {
    return CreateOpenAI.fromAzureWithKey(baseURL, OpenAIEnvInfo.AZURE_OPENAI_API_VERSION, OpenAIEnvInfo.AZURE_OPENAI_API_KEY, true);
  }

  static async fromAzureOpenAIWithKeyFromNode(baseURL) {
    return CreateOpenAI.fromAzureWithKey(baseURL, OpenAIEnvInfo.AZURE_OPENAI_API_VERSION, OpenAIEnvInfo.AZURE_OPENAI_API_KEY, false);
  }

  static fromOpenAIKeyFromBrowser() {
    return CreateOpenAI.fromOpenAIKey(OpenAIEnvInfo.OPENAI_API_KEY, OpenAIEnvInfo.OPENAI_ORGANIZATION, true);
  }

  static fromOpenAIKeyFromNode() {
    return CreateOpenAI.fromOpenAIKey(OpenAIEnvInfo.OPENAI_API_KEY, OpenAIEnvInfo.OPENAI_ORGANIZATION, false);
  }

  static fromAzureWithToken(baseURL, apiVersion, token, dangerouslyAllowBrowser = false) {
    console.log('Using Azure OpenAI (w/ AAD) ...');
    return new OpenAI({
      baseURL: baseURL,
      defaultQuery: { 'api-version': apiVersion },
      defaultHeaders: { Authorization: `Bearer ${token}` },
      dangerouslyAllowBrowser: dangerouslyAllowBrowser
    });
  }

  static fromAzureWithKey(baseURL, apiVersion, apiKey, dangerouslyAllowBrowser = false) {
    console.log('Using Azure OpenAI (w/ API Key)...');
    return new OpenAI({
      apiKey: apiKey,
      baseURL: baseURL,
      defaultQuery: { 'api-version': apiVersion },
      defaultHeaders: { 'api-key': apiKey },
      dangerouslyAllowBrowser: dangerouslyAllowBrowser
    });
  }

  static fromOpenAIKey(openAIAPIKey, openAIOrganization, dangerouslyAllowBrowser = false) {
    console.log('Using OpenAI...');
    return new OpenAI({
      apiKey: openAIAPIKey,
      organization: openAIOrganization,
      dangerouslyAllowBrowser: dangerouslyAllowBrowser
    });
  }

  static async getAzureInteractiveBrowserCredentialToken(clientId, tenantId) {
    try {
      const credential = new InteractiveBrowserCredential({ clientId, tenantId, loginStyle: 'redirect' });
      return await CreateOpenAI.getCognitiveServicesTokenFromCredential(credential);  
    } catch (error) {  
      console.error('Error getting access token:', error);  
      throw error;  
    }  
  }

  static async getAzureDefaultAzureCredentialToken() {
    try {
      const credential = new DefaultAzureCredential();
      return await CreateOpenAI.getCognitiveServicesTokenFromCredential(credential);
    } catch (error) {  
      console.error('Error getting access token:', error);  
      throw error;  
    }
  }

  static async getCognitiveServicesTokenFromCredential(credential) {
    const token = await credential.getToken("https://cognitiveservices.azure.com/.default");
    return token.token;
  }
}

exports.CreateOpenAI = CreateOpenAI;