<#@ template hostspecific="true" #>
<#@ output extension=".js" encoding="utf-8" #>
<#@ parameter type="System.String" name="ASSISTANT_ID" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_API_KEY" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_API_VERSION" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_ENDPOINT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_CHAT_DEPLOYMENT" #>
<#@ parameter type="System.String" name="AZURE_OPENAI_SYSTEM_PROMPT" #>
<#@ parameter type="System.String" name="OPENAI_API_KEY" #>
<#@ parameter type="System.String" name="OPENAI_MODEL_NAME" #>
export class OpenAIEnvInfo {
  // NOTE: Never deploy your key in client-side environments like browsers or mobile apps
  //  SEE: https://help.openai.com/en/articles/5112595-best-practices-for-api-key-safety

  static ASSISTANT_ID = import.meta.env.ASSISTANT_ID ?? "<insert your OpenAI assistant ID here>";

  static AZURE_CLIENT_ID = import.meta.env.AZURE_CLIENT_ID ?? null;
  static AZURE_TENANT_ID = import.meta.env.AZURE_TENANT_ID ?? null;

  static AZURE_OPENAI_API_KEY = import.meta.env.AZURE_OPENAI_API_KEY ?? "<insert your Azure OpenAI API key here>";
  static AZURE_OPENAI_API_VERSION = import.meta.env.AZURE_OPENAI_API_VERSION ?? "<insert your Azure OpenAI API version here>";
  static AZURE_OPENAI_ENDPOINT = import.meta.env.AZURE_OPENAI_ENDPOINT ?? "<insert your Azure OpenAI endpoint here>";
  static AZURE_OPENAI_CHAT_DEPLOYMENT = import.meta.env.AZURE_OPENAI_CHAT_DEPLOYMENT ?? "<insert your Azure OpenAI chat deployment name here>";

  static AZURE_OPENAI_SYSTEM_PROMPT = import.meta.env.AZURE_OPENAI_SYSTEM_PROMPT ?? "You are a helpful AI assistant.";

  static OPENAI_API_KEY = import.meta.env.OPENAI_API_KEY ?? "<insert your OpenAI API key here>";
  static OPENAI_ORG_ID = import.meta.env.OPENAI_ORG_ID ?? null;
  static OPENAI_MODEL_NAME = import.meta.env.OPENAI_MODEL_NAME ?? "<insert your OpenAI model name here>";

  static isConnectionInfoOk(errorCallback) {
    const ok = this.isAzureAADConnectionInfoOk() || this.isAzureKeyConnectionInfoOk() || this.isOpenAIConnectionInfoOk();
    if (!ok) {
      errorCallback(
        'To use **OpenAI**, set `OPENAI_API_KEY` and `OPENAI_MODEL_NAME` environment variables' + '\n\n' +
        'To use **Azure OpenAI**, set `AZURE_OPENAI_API_VERSION`, `AZURE_OPENAI_ENDPOINT`, and `AZURE_OPENAI_CHAT_DEPLOYMENT` environment variables' + '\n\n' +
        'For Azure OpenAI **w/ AAD**,  set `AZURE_CLIENT_ID` and `AZURE_TENANT_ID` environment variables' + '\n\n' +
        'For Azure OpenAI **w/ API KEY**, set `AZURE_OPENAI_API_KEY` environment variable');
      return false;
    }
    return true;
  }

  static isAzureAADConnectionInfoOk() {
    return this._isOk(this.AZURE_OPENAI_API_VERSION, this.AZURE_OPENAI_ENDPOINT, this.AZURE_OPENAI_CHAT_DEPLOYMENT, this.AZURE_CLIENT_ID, this.AZURE_TENANT_ID);
  }

  static isAzureKeyConnectionInfoOk() {
    return this._isOk(this.AZURE_OPENAI_API_VERSION, this.AZURE_OPENAI_ENDPOINT, this.AZURE_OPENAI_CHAT_DEPLOYMENT, this.AZURE_OPENAI_API_KEY);
  }

  static isOpenAIConnectionInfoOk() {
    return this._isOk(this.OPENAI_API_KEY, this.OPENAI_MODEL_NAME);
  }

  static getAzureAssistantAPIBaseUrl() {
    return `${this.AZURE_OPENAI_ENDPOINT.replace(/\/+$/, '')}/openai`;
  }

  static getAzureChatCompletionsAPIBaseUrl() {
    return `${this.AZURE_OPENAI_ENDPOINT.replace(/\/+$/, '')}/openai/deployments/${this.AZURE_OPENAI_CHAT_DEPLOYMENT}`;
  }

  static _isOk(...vars) {
    for (let s of vars) {
      if (s == null || s.startsWith('<insert')) {
        return false;
      }
    }
    return true;
  }
}