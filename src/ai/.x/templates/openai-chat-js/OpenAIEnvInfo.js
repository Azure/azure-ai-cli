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
class OpenAIEnvInfo {

  // NOTE: Never deploy your key in client-side environments like browsers or mobile apps
  //  SEE: https://help.openai.com/en/articles/5112595-best-practices-for-api-key-safety

  static ASSISTANT_ID = process.env.ASSISTANT_ID ?? "<#= ASSISTANT_ID #>";

  static AZURE_CLIENT_ID = process.env.AZURE_CLIENT_ID ?? null;
  static AZURE_TENANT_ID = process.env.AZURE_TENANT_ID ?? null;

  static AZURE_OPENAI_API_KEY = process.env.AZURE_OPENAI_API_KEY ?? "<#= AZURE_OPENAI_API_KEY #>";
  static AZURE_OPENAI_API_VERSION = process.env.AZURE_OPENAI_API_VERSION ?? "<#= AZURE_OPENAI_API_VERSION #>";
  static AZURE_OPENAI_ENDPOINT = process.env.AZURE_OPENAI_ENDPOINT ?? "<#= AZURE_OPENAI_ENDPOINT #>";
  static AZURE_OPENAI_CHAT_DEPLOYMENT = process.env.AZURE_OPENAI_CHAT_DEPLOYMENT ?? "<#= AZURE_OPENAI_CHAT_DEPLOYMENT #>";

  static AZURE_OPENAI_SYSTEM_PROMPT = process.env.AZURE_OPENAI_SYSTEM_PROMPT ?? "<#= AZURE_OPENAI_SYSTEM_PROMPT #>";

  static OPENAI_API_KEY = process.env.OPENAI_API_KEY ?? "<#= OPENAI_API_KEY #>";
  static OPENAI_ORG_ID = process.env.OPENAI_ORG_ID ?? null;
  static OPENAI_MODEL_NAME = process.env.OPENAI_MODEL_NAME ?? "<#= OPENAI_MODEL_NAME #>";

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

exports.OpenAIEnvInfo = OpenAIEnvInfo;
